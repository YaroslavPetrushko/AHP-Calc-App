using System;
using System.Collections.Generic;
using System.Globalization;

namespace MAI.Models;

/// <summary>
/// Зворотно-симетрична квадратна матриця попарних порівнянь Сааті.
/// </summary>
public class ComparisonMatrix
{
    public int Size { get; private set; }
    public List<string> Labels { get; set; } = new();
    public double[,] Values { get; private set; }

    public ComparisonMatrix(int size, IEnumerable<string>? labels = null)
    {
        Size = Math.Max(1, size);
        Values = new double[Size, Size];
        ResetToIdentity();

        if (labels != null)
        {
            Labels.AddRange(labels);
        }
        while (Labels.Count < Size)
        {
            Labels.Add($"E{Labels.Count + 1}");
        }
    }

    /// <summary>
    /// Ініціалізація матриці: діагональ = 1, решта клітинок = 1.
    /// </summary>
    public void ResetToIdentity()
    {
        for (int i = 0; i < Size; i++)
        {
            for (int j = 0; j < Size; j++)
            {
                Values[i, j] = 1.0;
            }
        }
    }

    /// <summary>
    /// Встановлення значення попарного порівняння.
    /// Автоматично встановлює a[col, row] = 1 / value.
    /// </summary>
    public void SetComparison(int row, int col, double value)
    {
        if (row < 0 || row >= Size || col < 0 || col >= Size)
            return;

        if (row == col)
        {
            Values[row, col] = 1.0;
            return;
        }

        if (value <= 0 || double.IsNaN(value) || double.IsInfinity(value))
            return;

        Values[row, col] = value;
        Values[col, row] = 1.0 / value;
    }

    /// <summary>
    /// Видалення елемента з матриці за вказаним індексом.
    /// Видаляє відповідний рядок і стовпець, зберігаючи всі інші попарні судження.
    /// </summary>
    public void RemoveAt(int index)
    {
        if (index < 0 || index >= Size || Size <= 1)
            return;

        int newSize = Size - 1;
        var newValues = new double[newSize, newSize];

        for (int i = 0, ni = 0; i < Size; i++)
        {
            if (i == index) continue;
            for (int j = 0, nj = 0; j < Size; j++)
            {
                if (j == index) continue;
                newValues[ni, nj++] = Values[i, j];
            }
            ni++;
        }

        Values = newValues;
        Size = newSize;

        if (index < Labels.Count)
        {
            Labels.RemoveAt(index);
        }
    }

    /// <summary>
    /// Зміна розміру матриці зі збереженням наявних значень.
    /// </summary>
    public void Resize(int newSize, List<string>? newLabels = null)
    {
        if (newSize < 1) return;

        var oldValues = Values;
        int oldSize = Size;

        Size = newSize;
        Values = new double[Size, Size];
        ResetToIdentity();

        int copySize = Math.Min(oldSize, newSize);
        for (int i = 0; i < copySize; i++)
        {
            for (int j = 0; j < copySize; j++)
            {
                Values[i, j] = oldValues[i, j];
            }
        }

        if (newLabels != null)
        {
            Labels = new List<string>(newLabels);
        }
        while (Labels.Count < Size)
        {
            Labels.Add($"E{Labels.Count + 1}");
        }
        if (Labels.Count > Size)
        {
            Labels = Labels.GetRange(0, Size);
        }
    }

    /// <summary>
    /// Парсинг значення зі строки (підтримує "3", "1/3", "0.333", "0.5").
    /// Перевіряє діапазон шкали Сааті [1/9; 9].
    /// </summary>
    public static bool TryParseSaatyValue(string input, out double result)
    {
        result = 1.0;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        input = input.Trim().Replace(',', '.');

        double parsedVal = 0.0;
        bool success = false;

        if (input.Contains('/'))
        {
            var parts = input.Split('/');
            if (parts.Length == 2 &&
                double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double num) &&
                double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double den) &&
                den > 0 && num > 0)
            {
                parsedVal = num / den;
                success = true;
            }
        }
        else if (double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out double val) && val > 0)
        {
            parsedVal = val;
            success = true;
        }

        if (!success)
            return false;

        // Перевірка допустимого діапазону шкали Сааті [1/9; 9]
        if (parsedVal < (1.0 / 9.0) - 1e-3 || parsedVal > 9.0 + 1e-3)
            return false;

        // Прив'язка близьких десяткових значень (наприклад, 0.333 -> 1/3, 0.143 -> 1/7, 0.111 -> 1/9)
        // Тільки якщо вхідний рядок не містив явного дробу зі слешем
        if (!input.Contains('/'))
        {
            for (int k = 2; k <= 9; k++)
            {
                if (Math.Abs(parsedVal - (1.0 / k)) < 0.005)
                {
                    parsedVal = 1.0 / k;
                    break;
                }
            }
        }

        result = parsedVal;
        return true;
    }

    /// <summary>
    /// Форматування значення для відображення (як дріб 1/k для стандартних часток Сааті).
    /// </summary>
    public static string FormatSaatyValue(double value)
    {
        if (Math.Abs(value - 1.0) < 1e-4) return "1";

        // Перевірка на цілі числа від 2 до 9
        for (int k = 2; k <= 9; k++)
        {
            if (Math.Abs(value - k) < 1e-3)
                return k.ToString();
            if (Math.Abs(value - (1.0 / k)) < 1e-3)
                return $"1/{k}";
        }

        // Перевірка на інші прості дроби
        if (Math.Abs(value - 0.5) < 1e-3) return "1/2";
        if (Math.Abs(value - 0.25) < 1e-3) return "1/4";
        if (Math.Abs(value - 0.75) < 1e-3) return "3/4";

        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Експорт значень матриці у зубчастий масив double[][] для серіалізації в JSON.
    /// </summary>
    public double[][] ToJaggedArray()
    {
        var res = new double[Size][];
        for (int i = 0; i < Size; i++)
        {
            res[i] = new double[Size];
            for (int j = 0; j < Size; j++)
            {
                res[i][j] = Values[i, j];
            }
        }
        return res;
    }

    /// <summary>
    /// Завантаження значень із зубчастого масиву double[][] з гарантією одиничної діагоналі та зворотної симетрії.
    /// </summary>
    public void LoadFromJaggedArray(double[][] array)
    {
        if (array == null) return;
        int n = Math.Min(Size, array.Length);

        // Гарантуємо одиничну діагональ
        for (int i = 0; i < Size; i++)
        {
            Values[i, i] = 1.0;
        }

        // Завантажуємо значення та примусово відновлюємо зворотну симетрію: a[j, i] = 1.0 / a[i, j]
        for (int i = 0; i < n; i++)
        {
            if (array[i] == null) continue;
            int m = Math.Min(Size, array[i].Length);
            for (int j = i + 1; j < m; j++)
            {
                double val = array[i][j];
                if (val <= 0 || double.IsNaN(val) || double.IsInfinity(val))
                {
                    val = 1.0;
                }
                Values[i, j] = val;
                Values[j, i] = 1.0 / val;
            }
        }
    }
}

