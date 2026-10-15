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
    private static string Fmt(FormattableString fs) => fs.ToString(LocalizationService.ActiveCulture);

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
        bool isEn = LocalizationService.CurrentLanguage == AppLanguage.En;

        if (data == null)
        {
            errors.Add(isEn ? "Project data is null." : "Дані проєкту відсутні (null).");
            return errors;
        }

        // 1. Кількість критеріїв і альтернатив у межах 2–10
        int critCount = data.Criteria?.Count ?? 0;
        if (data.Criteria == null || critCount < 2 || critCount > 10)
        {
            errors.Add(isEn
                ? $"Criteria count must be between 2 and 10 (received: {critCount})."
                : $"Кількість критеріїв повинна бути в межах від 2 до 10 (отримано: {critCount}).");
        }

        int altCount = data.Alternatives?.Count ?? 0;
        if (data.Alternatives == null || altCount < 2 || altCount > 10)
        {
            errors.Add(isEn
                ? $"Alternatives count must be between 2 and 10 (received: {altCount})."
                : $"Кількість альтернатив повинна бути в межах від 2 до 10 (отримано: {altCount}).");
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
                errors.Add(isEn
                    ? $"Duplicate criteria IDs detected: {string.Join(", ", duplicateCritIds)}."
                    : $"Виявлено дублікати ID критеріїв: {string.Join(", ", duplicateCritIds)}.");
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
                errors.Add(isEn
                    ? $"Duplicate alternative IDs detected: {string.Join(", ", duplicateAltIds)}."
                    : $"Виявлено дублікати ID альтернатив: {string.Join(", ", duplicateAltIds)}.");
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
                errors.Add(isEn
                    ? $"Duplicate criteria names detected: {string.Join(", ", duplicateCritNames)}."
                    : $"Виявлено повторювані назви критеріїв: {string.Join(", ", duplicateCritNames)}.");
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
                errors.Add(isEn
                    ? $"Duplicate alternative names detected: {string.Join(", ", duplicateAltNames)}."
                    : $"Виявлено повторювані назви альтернатив: {string.Join(", ", duplicateAltNames)}.");
            }
        }

        // 4. Розмір CriteriaMatrix дорівнює кількості критеріїв; для кожного критерію є матриця потрібного розміру
        if (data.Criteria != null && critCount >= 2 && critCount <= 10)
        {
            if (data.CriteriaMatrix == null || data.CriteriaMatrix.Length != critCount)
            {
                errors.Add(isEn
                    ? $"Criteria matrix size ({data.CriteriaMatrix?.Length ?? 0}) does not match criteria count ({critCount})."
                    : $"Розмір матриці критеріїв ({data.CriteriaMatrix?.Length ?? 0}) не відповідає кількості критеріїв ({critCount}).");
            }
            else
            {
                for (int i = 0; i < critCount; i++)
                {
                    if (data.CriteriaMatrix[i] == null || data.CriteriaMatrix[i].Length != critCount)
                    {
                        errors.Add(isEn
                            ? $"Row {i + 1} of criteria matrix has incorrect size ({data.CriteriaMatrix[i]?.Length ?? 0} instead of {critCount})."
                            : $"Рядок {i + 1} матриці критеріїв має некоректний розмір ({data.CriteriaMatrix[i]?.Length ?? 0} замість {critCount}).");
                        break;
                    }
                }
            }
        }

        if (data.Criteria != null && data.Alternatives != null && critCount >= 2 && critCount <= 10 && altCount >= 2 && altCount <= 10)
        {
            if (data.AlternativeMatrices == null)
            {
                errors.Add(isEn
                    ? "Alternative comparison matrices dictionary is missing."
                    : "Словник матриць порівняння альтернатив відсутній.");
            }
            else
            {
                foreach (var c in data.Criteria)
                {
                    if (!data.AlternativeMatrices.TryGetValue(c.Id, out var altMat) || altMat == null)
                    {
                        errors.Add(isEn
                            ? $"Alternative matrix for criterion '{c.Name}' (ID: {c.Id}) is missing."
                            : $"Відсутня матриця альтернатив для критерію «{c.Name}» (ID: {c.Id}).");
                        continue;
                    }

                    if (altMat.Length != altCount)
                    {
                        errors.Add(isEn
                            ? $"Alternative matrix size for criterion '{c.Name}' ({altMat.Length}) does not match alternatives count ({altCount})."
                            : $"Розмір матриці альтернатив для критерію «{c.Name}» ({altMat.Length}) не відповідає кількості альтернатив ({altCount}).");
                        continue;
                    }

                    for (int i = 0; i < altCount; i++)
                    {
                        if (altMat[i] == null || altMat[i].Length != altCount)
                        {
                            errors.Add(isEn
                                ? $"Row {i + 1} of alternative matrix for criterion '{c.Name}' has incorrect size ({altMat[i]?.Length ?? 0} instead of {altCount})."
                                : $"Рядок {i + 1} матриці альтернатив для критерію «{c.Name}» має некоректний розмір ({altMat[i]?.Length ?? 0} замість {altCount}).");
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
                        errors.Add(isEn
                            ? $"Value in criteria matrix at [{i + 1}, {j + 1}] is {v}, which exceeds Saaty scale [1/9; 9]."
                            : $"Значення в матриці критеріїв на позиції [{i + 1}, {j + 1}] дорівнює {v}, що виходить за межі шкали Сааті [1/9; 9].");
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
                                errors.Add(isEn
                                    ? $"Value in alternative matrix for '{c.Name}' at [{i + 1}, {j + 1}] is {v}, which exceeds Saaty scale [1/9; 9]."
                                    : $"Значення в матриці альтернатив для «{c.Name}» на позиції [{i + 1}, {j + 1}] дорівнює {v}, що виходить за межі шкали Сааті [1/9; 9].");
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
        bool isEn = LocalizationService.CurrentLanguage == AppLanguage.En;
        if (!File.Exists(filePath))
            throw new FileNotFoundException(isEn ? $"Project file not found: {filePath}" : $"Файл проєкту не знайдено: {filePath}");

        string json = File.ReadAllText(filePath, Encoding.UTF8);
        var data = JsonSerializer.Deserialize<AhpProjectData>(json, JsonOptions);

        if (data == null)
            throw new InvalidOperationException(isEn
                ? "Failed to deserialize project data. File is corrupted or empty."
                : "Не вдалося десеріалізувати дані проєкту. Файл пошкоджений або порожній.");

        var errors = ValidateProject(data);
        if (errors.Count > 0)
        {
            string errHeader = isEn ? "Project file validation errors:\n" : "Помилки валідації файлу проєкту:\n";
            throw new InvalidOperationException(errHeader + string.Join("\n", errors.Select(e => $"• {e}")));
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
        sb.AppendLine($"        {LocalizationService.Get("Str_Report_Title")}");
        sb.AppendLine("================================================================================");
        sb.AppendLine(Fmt($"{LocalizationService.Format("Str_Report_CalcDate", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss", LocalizationService.ActiveCulture))}"));

        string weightDesc = result.Options.WeightMethod switch
        {
            WeightCalculationMethod.ColumnNormalization => LocalizationService.Get("Str_Menu_Weight_ColNorm"),
            WeightCalculationMethod.GeometricMean => LocalizationService.Get("Str_Menu_Weight_Geometric"),
            WeightCalculationMethod.PrincipalEigenvector => LocalizationService.Get("Str_Menu_Weight_PowerIteration"),
            _ => result.Options.WeightMethod.ToString()
        };
        string lambdaDesc = AhpCalculationOptions.GetLambdaMethodDescription(result.Options.LambdaMethod);
        string riDesc = result.ActiveRiMode == RiCalculationMode.Empirical
            ? LocalizationService.Get("Str_Menu_Ri_Empirical")
            : LocalizationService.Get("Str_Menu_Ri_Table");
        string threshDesc = result.Options.ThresholdMode switch
        {
            ConsistencyThresholdMode.Uniform10Percent => LocalizationService.Get("Str_Menu_Threshold_Uniform"),
            ConsistencyThresholdMode.SaatyAdaptive => LocalizationService.Get("Str_Menu_Threshold_Adaptive"),
            _ => result.Options.ThresholdMode.ToString()
        };

        sb.AppendLine(LocalizationService.Get("Str_Report_ConfigParams"));
        sb.AppendLine($"  • {LocalizationService.Format("Str_Report_WeightMethod", weightDesc)}");
        sb.AppendLine($"  • {LocalizationService.Format("Str_Report_LambdaMethod", lambdaDesc)}");
        sb.AppendLine($"  • {LocalizationService.Format("Str_Report_RiMode", riDesc)}");
        sb.AppendLine(Fmt($"  • {LocalizationService.Format("Str_Report_ThresholdMode", threshDesc, result.Criteria.Count, result.CriteriaMatrixResult.Threshold.ToString("P0", LocalizationService.ActiveCulture))}"));
        sb.AppendLine();

        // 1. ПІДСУМКОВИЙ РЕЙТИНГ АЛЬТЕРНАТИВ
        sb.AppendLine(LocalizationService.Get("Str_Report_Sec1"));
        string globalPriLabel = LocalizationService.Get("Str_Report_GlobalPriority");
        foreach (var alt in result.RankedAlternatives)
        {
            sb.AppendLine(Fmt($"#{alt.RankText,-3} {alt.Name,-35} | {globalPriLabel}: {alt.GlobalPriority:F4} ({alt.GlobalPriority * 100,5:F1}%)"));
        }
        sb.AppendLine();

        // 2. ВАГОВІ КОЕФІЦІЄНТИ КРИТЕРІЇВ
        sb.AppendLine(LocalizationService.Get("Str_Report_Sec2"));
        string weightLabel = LocalizationService.Get("Str_Report_Weight");
        foreach (var c in result.Criteria)
        {
            sb.AppendLine(Fmt($"{c.Name,-35} | {weightLabel}: {c.Weight:F4} ({c.Weight * 100,5:F1}%)"));
        }
        sb.AppendLine();

        // 3. ПЕРЕВІРКА УЗГОДЖЕНОСТІ СУДЖЕНЬ
        sb.AppendLine(LocalizationService.Get("Str_Report_Sec3"));
        string critMatrixLabel = LocalizationService.Get("Str_Report_CriteriaMatrix");
        sb.AppendLine(Fmt($"{critMatrixLabel,-29} λmax={result.CriteriaMatrixResult.LambdaMax:F4}, CI={result.CriteriaMatrixResult.CI:F4}, CR(табл)={result.CriteriaMatrixResult.CRTable:P1}, CR(емп)={result.CriteriaMatrixResult.CREmpirical:P1} [{result.CriteriaMatrixResult.ConsistencyMessage}]"));

        foreach (var kvp in result.AlternativeMatrixResults)
        {
            var res = kvp.Value;
            var critObj = result.Criteria.FirstOrDefault(c => c.Id == kvp.Key);
            string title = critObj != null ? critObj.Name : res.Name;
            string altLabel = LocalizationService.Format("Str_Report_AlternativesFor", title);
            sb.AppendLine(Fmt($"{altLabel,-29} λmax={res.LambdaMax:F4}, CI={res.CI:F4}, CR(табл)={res.CRTable:P1}, CR(емп)={res.CREmpirical:P1} [{res.ConsistencyMessage}]"));
        }

        string compLabel = LocalizationService.Get("Str_Report_HierarchyComp");
        sb.AppendLine(Fmt($"{compLabel,-29} CIh={result.HierarchicalCI:F4}, CRh(табл)={result.HierarchicalCRTable:P1}, CRh(емп)={result.HierarchicalCREmpirical:P1} [{result.HierarchyConsistencyMessage}]"));
        if (result.InconsistentMatrices.Count > 0)
        {
            sb.AppendLine(LocalizationService.Format("Str_Report_InconsistentWarn", string.Join(", ", result.InconsistentMatrices)));
        }
        sb.AppendLine();

        // 4. МАТРИЦЯ СИНТЕЗУ
        sb.AppendLine(LocalizationService.Get("Str_Report_Sec4"));
        string altCorner = LocalizationService.Get("Str_Main_CornerAlt");
        sb.Append($"{altCorner,-30} | ");
        foreach (var c in result.Criteria)
        {
            sb.Append($"{c.Name,-16} | ");
        }
        sb.AppendLine(LocalizationService.Get("Str_Report_SummaryPi"));

        foreach (var alt in result.RankedAlternatives)
        {
            sb.Append($"{alt.Name,-30} | ");
            foreach (var c in result.Criteria)
            {
                double score = alt.WeightedScores.TryGetValue(c.Id, out var ws) ? ws : 0.0;
                sb.Append(Fmt($"{score,16:F3} | "));
            }
            sb.AppendLine(Fmt($"{alt.GlobalPriority:F3}"));
        }
        sb.AppendLine();

        // 5. АНАЛІЗ ЧУТЛИВОСТІ
        sb.AppendLine(LocalizationService.Get("Str_Report_Sec5"));
        foreach (var c in result.Criteria)
        {
            try
            {
                var sens = AhpCalculator.CalculateSensitivity(result, c.Id);
                sb.AppendLine(Fmt($"{LocalizationService.Format("Str_Report_CritInitialWeight", c.Name, c.Weight.ToString("P1", LocalizationService.ActiveCulture))}"));
                sb.AppendLine($"  {sens.StabilitySummary}");
                if (sens.LeadershipIntervals.Count > 0)
                {
                    sb.AppendLine($"  {LocalizationService.Format("Str_Report_LeadershipIntervals", string.Join(" | ", sens.LeadershipIntervals.Select(i => i.DisplayText)))}");
                }
                var leaderIntersections = sens.Intersections.Where(p => p.IsLeadershipChange).ToList();
                if (leaderIntersections.Count > 0)
                {
                    string atWord = LocalizationService.CurrentLanguage == AppLanguage.En ? "at" : "при";
                    string pointsStr = string.Join("; ", leaderIntersections.Select(p => Fmt($"{p.Alt1Name} = {p.Alt2Name} {atWord} t*={p.WeightT:P1} (P*={p.PriorityP:F3})")));
                    sb.AppendLine($"  {LocalizationService.Format("Str_Report_CriticalLeaderPoints", pointsStr)}");
                }
                sb.AppendLine();
            }
            catch (Exception ex)
            {
                sb.AppendLine($"  {LocalizationService.Format("Str_Report_SensUnavailable", c.Name, ex.Message)}");
            }
        }

        if (!string.IsNullOrWhiteSpace(recommendationText))
        {
            sb.AppendLine(LocalizationService.Get("Str_Report_Sec6"));
            sb.AppendLine(recommendationText);
        }

        sb.AppendLine("================================================================================");
        return sb.ToString();
    }
}
