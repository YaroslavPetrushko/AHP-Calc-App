using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MAI.Models;

namespace MAI.Services;

/// <summary>
/// Сервіс збереження та завантаження проєктів МАІ (JSON) та формування текстових звітів.
/// </summary>
public static class ProjectStorageService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Збереження повної задачі у файл формату JSON.
    /// </summary>
    public static void SaveProject(string filePath, AhpProjectData data)
    {
        data.SavedAt = DateTime.Now;
        string json = JsonSerializer.Serialize(data, JsonOptions);
        File.WriteAllText(filePath, json, Encoding.UTF8);
    }

    /// <summary>
    /// Валідація структури та числових даних проєкту МАІ. Повертає список виявлених помилок (порожній, якщо проєкт валідний).
    /// </summary>
    public static List<string> ValidateProject(AhpProjectData data)
    {
        var errors = new List<string>();

        if (data == null)
        {
            errors.Add("Дані проєкту відсутні (null).");
            return errors;
        }

        // 1. Кількість критеріїв і альтернатив у межах 2–10
        int critCount = data.Criteria?.Count ?? 0;
        if (data.Criteria == null || critCount < 2 || critCount > 10)
        {
            errors.Add($"Кількість критеріїв повинна бути в межах від 2 до 10 (отримано: {critCount}).");
        }

        int altCount = data.Alternatives?.Count ?? 0;
        if (data.Alternatives == null || altCount < 2 || altCount > 10)
        {
            errors.Add($"Кількість альтернатив повинна бути в межах від 2 до 10 (отримано: {altCount}).");
        }

        // 2. Унікальні Id критеріїв, унікальні Id альтернатив
        if (data.Criteria != null)
        {
            var duplicateCritIds = data.Criteria
                .GroupBy(c => c.Id, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            if (duplicateCritIds.Count > 0)
            {
                errors.Add($"Виявлено дублікати ID критеріїв: {string.Join(", ", duplicateCritIds)}.");
            }
        }

        if (data.Alternatives != null)
        {
            var duplicateAltIds = data.Alternatives
                .GroupBy(a => a.Id, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            if (duplicateAltIds.Count > 0)
            {
                errors.Add($"Виявлено дублікати ID альтернатив: {string.Join(", ", duplicateAltIds)}.");
            }
        }

        // 3. Унікальні назви критеріїв (регістронезалежно) та назви альтернатив
        if (data.Criteria != null)
        {
            var duplicateCritNames = data.Criteria
                .GroupBy(c => c.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            if (duplicateCritNames.Count > 0)
            {
                errors.Add($"Виявлено повторювані назви критеріїв: {string.Join(", ", duplicateCritNames)}.");
            }
        }

        if (data.Alternatives != null)
        {
            var duplicateAltNames = data.Alternatives
                .GroupBy(a => a.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            if (duplicateAltNames.Count > 0)
            {
                errors.Add($"Виявлено повторювані назви альтернатив: {string.Join(", ", duplicateAltNames)}.");
            }
        }

        // 4. Розмір CriteriaMatrix дорівнює кількості критеріїв; для кожного критерію є матриця потрібного розміру
        if (data.Criteria != null && critCount >= 2 && critCount <= 10)
        {
            if (data.CriteriaMatrix == null || data.CriteriaMatrix.Length != critCount)
            {
                errors.Add($"Розмір матриці критеріїв ({data.CriteriaMatrix?.Length ?? 0}) не відповідає кількості критеріїв ({critCount}).");
            }
            else
            {
                for (int i = 0; i < critCount; i++)
                {
                    if (data.CriteriaMatrix[i] == null || data.CriteriaMatrix[i].Length != critCount)
                    {
                        errors.Add($"Рядок {i + 1} матриці критеріїв має некоректний розмір ({data.CriteriaMatrix[i]?.Length ?? 0} замість {critCount}).");
                        break;
                    }
                }
            }
        }

        if (data.Criteria != null && data.Alternatives != null && critCount >= 2 && critCount <= 10 && altCount >= 2 && altCount <= 10)
        {
            if (data.AlternativeMatrices == null)
            {
                errors.Add("Словник матриць порівняння альтернатив відсутній.");
            }
            else
            {
                foreach (var c in data.Criteria)
                {
                    if (!data.AlternativeMatrices.TryGetValue(c.Id, out var altMat) || altMat == null)
                    {
                        errors.Add($"Відсутня матриця альтернатив для критерію «{c.Name}» (ID: {c.Id}).");
                        continue;
                    }

                    if (altMat.Length != altCount)
                    {
                        errors.Add($"Розмір матриці альтернатив для критерію «{c.Name}» ({altMat.Length}) не відповідає кількості альтернатив ({altCount}).");
                        continue;
                    }

                    for (int i = 0; i < altCount; i++)
                    {
                        if (altMat[i] == null || altMat[i].Length != altCount)
                        {
                            errors.Add($"Рядок {i + 1} матриці альтернатив для критерію «{c.Name}» має некоректний розмір ({altMat[i]?.Length ?? 0} замість {altCount}).");
                            break;
                        }
                    }
                }
            }
        }

        // 5. Усі значення з верхнього трикутника в межах [1/9; 9] з допуском 1e-6
        const double minAllowed = (1.0 / 9.0) - 1e-6;
        const double maxAllowed = 9.0 + 1e-6;

        if (data.CriteriaMatrix != null && data.CriteriaMatrix.Length == critCount)
        {
            for (int i = 0; i < critCount; i++)
            {
                if (data.CriteriaMatrix[i] == null || data.CriteriaMatrix[i].Length != critCount) continue;
                for (int j = i + 1; j < critCount; j++)
                {
                    double v = data.CriteriaMatrix[i][j];
                    if (double.IsNaN(v) || double.IsInfinity(v) || v < minAllowed || v > maxAllowed)
                    {
                        errors.Add($"Значення в матриці критеріїв на позиції [{i + 1}, {j + 1}] дорівнює {v}, що виходить за межі шкали Сааті [1/9; 9].");
                    }
                }
            }
        }

        if (data.AlternativeMatrices != null && data.Criteria != null && altCount >= 2)
        {
            foreach (var c in data.Criteria)
            {
                if (data.AlternativeMatrices.TryGetValue(c.Id, out var altMat) && altMat != null && altMat.Length == altCount)
                {
                    for (int i = 0; i < altCount; i++)
                    {
                        if (altMat[i] == null || altMat[i].Length != altCount) continue;
                        for (int j = i + 1; j < altCount; j++)
                        {
                            double v = altMat[i][j];
                            if (double.IsNaN(v) || double.IsInfinity(v) || v < minAllowed || v > maxAllowed)
                            {
                                errors.Add($"Значення в матриці альтернатив для «{c.Name}» на позиції [{i + 1}, {j + 1}] дорівнює {v}, що виходить за межі шкали Сааті [1/9; 9].");
                            }
                        }
                    }
                }
            }
        }

        return errors;
    }

    /// <summary>
    /// Завантаження задачі з файлу JSON із повною валідацією структури та числових обмежень.
    /// </summary>
    public static AhpProjectData LoadProject(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Файл проєкту не знайдено: {filePath}");

        string json = File.ReadAllText(filePath, Encoding.UTF8);
        var data = JsonSerializer.Deserialize<AhpProjectData>(json, JsonOptions);

        if (data == null)
            throw new InvalidOperationException("Не вдалося десеріалізувати дані проєкту. Файл пошкоджений або порожній.");

        var errors = ValidateProject(data);
        if (errors.Count > 0)
        {
            throw new InvalidOperationException("Помилки валідації файлу проєкту:\n" + string.Join("\n", errors.Select(e => $"• {e}")));
        }

        return data;
    }

    /// <summary>
    /// Генерація повного текстового звіту результатів розрахунку МАІ.
    /// </summary>
    public static string GenerateFullReportText(AhpResult result, string recommendationText = "")
    {
        var sb = new StringBuilder();
        sb.AppendLine("================================================================================");
        sb.AppendLine("        ЗВІТ ЗА МЕТОДОМ АНАЛІЗУ ІЄРАРХІЙ (МАІ) ТОМАСА СААТІ (.NET 10)");
        sb.AppendLine("================================================================================");
        sb.AppendLine($"Дата та час розрахунку: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
        string weightDesc = result.Options.WeightMethod switch
        {
            WeightCalculationMethod.ColumnNormalization => "Нормалізація стовпців (Column Normalization)",
            WeightCalculationMethod.GeometricMean => "Геометричне середнє (Geometric Mean / LLSM)",
            WeightCalculationMethod.PrincipalEigenvector => "Степеневий метод (Power Iteration)",
            _ => result.Options.WeightMethod.ToString()
        };
        string lambdaDesc = AhpCalculationOptions.GetLambdaMethodDescription(result.Options.LambdaMethod);
        string riDesc = result.ActiveRiMode == RiCalculationMode.Empirical
            ? "Емпірична формула (1.98 · (n-2) / n)"
            : "Табличні значення Сааті";
        string threshDesc = result.Options.ThresholdMode switch
        {
            ConsistencyThresholdMode.Uniform10Percent => "Фіксований 10% для всіх n",
            ConsistencyThresholdMode.SaatyAdaptive => "Адаптивний Сааті (5% при n=3, 8% при n=4, 10% при n≥5)",
            _ => result.Options.ThresholdMode.ToString()
        };

        sb.AppendLine($"Параметри конфігурації розрахунку:");
        sb.AppendLine($"  • Метод обчислення векторів ваг: {weightDesc}");
        sb.AppendLine($"  • Метод власного значення (λmax): {lambdaDesc}");
        sb.AppendLine($"  • Індекс випадкової узгодженості (RI): {riDesc}");
        sb.AppendLine($"  • Поріг узгодженості: {threshDesc} (Поріг для критеріїв n={result.Criteria.Count}: {result.CriteriaMatrixResult.Threshold:P0})");
        sb.AppendLine();

        sb.AppendLine("--- 1. ПІДСУМКОВИЙ РЕЙТИНГ АЛЬТЕРНАТИВ ---");
        foreach (var alt in result.RankedAlternatives)
        {
            sb.AppendLine($"#{alt.Rank,-2} {alt.Name,-35} | Глобальний пріоритет: {alt.GlobalPriority:F4} ({alt.GlobalPriority * 100,5:F1}%)");
        }
        sb.AppendLine();

        sb.AppendLine("--- 2. ВАГОВІ КОЕФІЦІЄНТИ КРИТЕРІЇВ ---");
        foreach (var c in result.Criteria)
        {
            sb.AppendLine($"{c.Name,-35} | Вага: {c.Weight:F4} ({c.Weight * 100,5:F1}%)");
        }
        sb.AppendLine();

        sb.AppendLine("--- 3. ПЕРЕВІРКА УЗГОДЖЕНОСТІ СУДЖЕНЬ (CI, RI, CR) ---");
        sb.AppendLine($"Матриця критеріїв:           λmax={result.CriteriaMatrixResult.LambdaMax:F4}, CI={result.CriteriaMatrixResult.CI:F4}, CR(табл)={result.CriteriaMatrixResult.CRTable:P1}, CR(емп)={result.CriteriaMatrixResult.CREmpirical:P1} [{result.CriteriaMatrixResult.ConsistencyMessage}]");

        foreach (var kvp in result.AlternativeMatrixResults)
        {
            var res = kvp.Value;
            var critObj = result.Criteria.FirstOrDefault(c => c.Id == kvp.Key);
            string title = critObj != null ? critObj.Name : res.Name;
            sb.AppendLine($"Альтернативи ({title,-20}): λmax={res.LambdaMax:F4}, CI={res.CI:F4}, CR(табл)={res.CRTable:P1}, CR(емп)={res.CREmpirical:P1} [{res.ConsistencyMessage}]");
        }

        sb.AppendLine($"Композиція всієї ієрархії:   CIh={result.HierarchicalCI:F4}, CRh(табл)={result.HierarchicalCRTable:P1}, CRh(емп)={result.HierarchicalCREmpirical:P1} [{result.HierarchyConsistencyMessage}]");
        if (result.InconsistentMatrices.Count > 0)
        {
            sb.AppendLine($"УВАГА: Матриці з порушенням порогу: {string.Join(", ", result.InconsistentMatrices)}");
        }
        sb.AppendLine();

        sb.AppendLine("--- 4. МАТРИЦЯ СИНТЕЗУ (Lij · wj) ---");
        sb.Append($"{"Альтернатива",-30} | ");
        foreach (var c in result.Criteria)
        {
            sb.Append($"{c.Name,-16} | ");
        }
        sb.AppendLine("Підсумок (Pi)");

        foreach (var alt in result.RankedAlternatives)
        {
            sb.Append($"{alt.Name,-30} | ");
            foreach (var c in result.Criteria)
            {
                double score = alt.WeightedScores.TryGetValue(c.Id, out var ws) ? ws : 0.0;
                sb.Append($"{score,16:F3} | ");
            }
            sb.AppendLine($"{alt.GlobalPriority:F3}");
        }

        sb.AppendLine();
        sb.AppendLine("--- 5. АНАЛІЗ ЧУТЛИВОСТІ РЕЙТИНГУ ДО ВАГ КРИТЕРІЇВ (SENSITIVITY ANALYSIS) ---");
        foreach (var c in result.Criteria)
        {
            try
            {
                var sens = AhpCalculator.CalculateSensitivity(result, c.Id);
                sb.AppendLine($"Критерій: {c.Name} (вихідна вага: {c.Weight:P1})");
                sb.AppendLine($"  {sens.StabilitySummary}");
                if (sens.LeadershipIntervals.Count > 0)
                {
                    sb.AppendLine($"  Інтервали лідерства: {string.Join(" | ", sens.LeadershipIntervals.Select(i => i.DisplayText))}");
                }
                var leaderIntersections = sens.Intersections.Where(p => p.IsLeadershipChange).ToList();
                if (leaderIntersections.Count > 0)
                {
                    sb.AppendLine($"  Критичні точки зміни лідера: " + string.Join("; ", leaderIntersections.Select(p => $"{p.Alt1Name} = {p.Alt2Name} при t*={p.WeightT:P1} (P*={p.PriorityP:F3})")));
                }
                sb.AppendLine();
            }
            catch
            {
                // ignore
            }
        }

        if (!string.IsNullOrWhiteSpace(recommendationText))
        {
            sb.AppendLine("--- 6. АНАЛІТИЧНИЙ ВИСНОВОК ТА РЕКОМЕНДАЦІЇ ---");
            sb.AppendLine(recommendationText);
        }

        sb.AppendLine("================================================================================");
        return sb.ToString();
    }
}
