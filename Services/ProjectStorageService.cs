using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
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
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
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
    /// Завантаження задачі з файлу JSON.
    /// </summary>
    public static AhpProjectData LoadProject(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Файл проєкту не знайдено: {filePath}");

        string json = File.ReadAllText(filePath, Encoding.UTF8);
        var data = JsonSerializer.Deserialize<AhpProjectData>(json, JsonOptions);

        if (data == null)
            throw new InvalidOperationException("Не вдалося десеріалізувати дані проєкту. Файл пошкоджений або порожній.");

        if (data.Criteria == null || data.Criteria.Count < 2)
            throw new InvalidOperationException("У завантаженому файлі недостатньо критеріїв (необхідно мінімум 2).");

        if (data.Alternatives == null || data.Alternatives.Count < 2)
            throw new InvalidOperationException("У завантаженому файлі недостатньо альтернатив (необхідно мінімум 2).");

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
        string lambdaDesc = result.Options.LambdaMethod switch
        {
            LambdaCalculationMethod.ColumnSumProduct => "Добуток сум стовпців Σ(ColSum_j · w_j) [ЛР №4]",
            LambdaCalculationMethod.ExactEigenvalue => "Точне власне значення (A·w)_i / w_i [Сааті]",
            _ => result.Options.LambdaMethod.ToString()
        };
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
