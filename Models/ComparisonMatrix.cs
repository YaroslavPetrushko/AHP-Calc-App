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

        const double minAllowed = (1.0 / 9.0) - 1e-9;
        const double maxAllowed = 9.0 + 1e-9;
        if (value < minAllowed || value > maxAllowed)
            return;

        double snapped = SnapToSaatyScale(value);
        Values[row, col] = snapped;
        Values[col, row] = 1.0 / snapped;
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
    /// Значення стандартної 9-бальної шкали важливості Томаса Сааті (від 1/9 до 9).
    /// </summary>
    public static readonly double[] SaatyScale =
    {
        1.0 / 9.0, 1.0 / 8.0, 1.0 / 7.0, 1.0 / 6.0, 1.0 / 5.0, 1.0 / 4.0, 1.0 / 3.0, 1.0 / 2.0,
        1.0,
        2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0
    };

    /// <summary>
    /// Прив'язка значення до найближчого значення шкали Сааті, якщо відносна різниця менша за 0.5% (0.005).
    /// </summary>
    public static double SnapToSaatyScale(double v)
    {
        if (v <= 0 || double.IsNaN(v) || double.IsInfinity(v))
            return v;

        double closest = SaatyScale[0];
        double minDiff = Math.Abs(v - closest);

        for (int i = 1; i < SaatyScale.Length; i++)
        {
            double diff = Math.Abs(v - SaatyScale[i]);
            if (diff < minDiff)
            {
                minDiff = diff;
                closest = SaatyScale[i];
            }
        }

        if (minDiff / closest < 0.005)
        {
            return closest;
        }

        return v;
    }

    /// <summary>
    /// Парсинг значення зі строки (підтримує "3", "1/3", "0.333", "0.5").
    /// Стиль NumberStyles.Float, CultureInfo.InvariantCulture.
    /// Відхиляє значення поза [1/9; 9] (допуск 1e-9), нечислові, 1/0, від'ємні.
    /// Після успішного розбору застосовує SnapToSaatyScale.
    /// </summary>
    public static bool TryParseSaatyValue(string input, out double result)
    {
        result = 1.0;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        string normalized = input.Trim().Replace(',', '.');

        double parsedVal;
        if (normalized.Contains('/'))
        {
            var parts = normalized.Split('/');
            if (parts.Length != 2)
                return false;

            if (!double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double num) ||
                !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double den))
            {
                return false;
            }

            if (num <= 0 || den <= 0 || double.IsNaN(num) || double.IsNaN(den) || double.IsInfinity(num) || double.IsInfinity(den))
                return false;

            parsedVal = num / den;
        }
        else
        {
            if (!double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out double val))
                return false;

            if (val <= 0 || double.IsNaN(val) || double.IsInfinity(val))
                return false;

            parsedVal = val;
        }

        if (double.IsNaN(parsedVal) || double.IsInfinity(parsedVal))
            return false;

        const double minAllowed = (1.0 / 9.0) - 1e-9;
        const double maxAllowed = 9.0 + 1e-9;

        if (parsedVal < minAllowed || parsedVal > maxAllowed)
            return false;

        result = SnapToSaatyScale(parsedVal);
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

