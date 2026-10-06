using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MAI.Models;

namespace MAI.Services;

/// <summary>
/// Будівник структурованих таблиць розрахунку МАІ для експорту у звіти.
/// </summary>
public static class ReportTableBuilder
{
    private static readonly CultureInfo UkCulture = new("uk-UA");

    public static string FormatNumber(double value) => value.ToString("0.0000", UkCulture);

    public static string FormatPercent(double value) => (value * 100.0).ToString("0.00", UkCulture) + " %";

    /// <summary>
    /// Побудова повного переліку таблиць для звіту за результатами розрахунку.
    /// </summary>
    public static List<ReportTable> Build(
        AhpResult result,
        AhpCalculationOptions? calculationOptions = null,
        Dictionary<string, CriterionSensitivityResult>? sensitivityResults = null)
    {
        var tables = new List<ReportTable>();
        var opts = calculationOptions ?? result.Options ?? AhpCalculationOptions.ClassicSaatyProfile;

        // 1. Параметри розрахунку
        string weightDesc = opts.WeightMethod switch
        {
            WeightCalculationMethod.ColumnNormalization => "Нормалізація стовпців (Column Normalization)",
            WeightCalculationMethod.GeometricMean => "Геометричне середнє (Geometric Mean / LLSM)",
            WeightCalculationMethod.PrincipalEigenvector => "Степеневий метод (Power Iteration)",
            _ => opts.WeightMethod.ToString()
        };
        string lambdaDesc = AhpCalculationOptions.GetLambdaMethodDescription(opts.LambdaMethod);
        string riDesc = opts.RiMode == RiCalculationMode.Empirical
            ? "Емпірична формула (1.98 · (n-2) / n)"
            : "Табличні значення Сааті";
        string threshDesc = opts.ThresholdMode switch
        {
            ConsistencyThresholdMode.Uniform10Percent => "Фіксований 10% для всіх n",
            ConsistencyThresholdMode.SaatyAdaptive => "Адаптивний Сааті (5% при n=3, 8% при n=4, 10% при n≥5)",
            _ => opts.ThresholdMode.ToString()
        };

        tables.Add(new ReportTable(
            "Параметри розрахунку",
            new[] { "Параметр", "Значення" },
            new List<string[]>
            {
                new[] { "Профіль розрахунку", opts.ProfileName },
                new[] { "Метод розрахунку ваг", weightDesc },
                new[] { "Метод власного значення (λmax)", lambdaDesc },
                new[] { "Режим індексу узгодженості (RI)", riDesc },
                new[] { "Режим порогу узгодженості", threshDesc }
            }
        ));

        // 2. Матриця парних порівнянь критеріїв
        var critRes = result.CriteriaMatrixResult;
        int nCrit = result.Criteria.Count;
        var critHeaders = new[] { "Критерій" }.Concat(result.Criteria.Select(c => c.Name)).ToArray();
        var critPwRows = new List<string[]>();
        for (int i = 0; i < nCrit; i++)
        {
            var row = new string[nCrit + 1];
            row[0] = result.Criteria[i].Name;
            for (int j = 0; j < nCrit; j++)
            {
                double v = (critRes.PairwiseMatrix != null && critRes.PairwiseMatrix.GetLength(0) > i && critRes.PairwiseMatrix.GetLength(1) > j)
                    ? critRes.PairwiseMatrix[i, j]
                    : (i == j ? 1.0 : 1.0);
                row[j + 1] = FormatNumber(v);
            }
            critPwRows.Add(row);
        }
        tables.Add(new ReportTable("Матриця парних порівнянь критеріїв", critHeaders, critPwRows));

        // 3. Нормалізована матриця критеріїв і вектор ваг
        var critNormHeaders = new[] { "Критерій" }.Concat(result.Criteria.Select(c => c.Name)).Concat(new[] { "Вага (w)" }).ToArray();
        var critNormRows = new List<string[]>();
        for (int i = 0; i < nCrit; i++)
        {
            var row = new string[nCrit + 2];
            row[0] = result.Criteria[i].Name;
            for (int j = 0; j < nCrit; j++)
            {
                double v = (critRes.NormalizedMatrix != null && critRes.NormalizedMatrix.GetLength(0) > i && critRes.NormalizedMatrix.GetLength(1) > j)
                    ? critRes.NormalizedMatrix[i, j]
                    : 0.0;
                row[j + 1] = FormatNumber(v);
            }
            row[nCrit + 1] = FormatNumber(critRes.Weights.Length > i ? critRes.Weights[i] : result.Criteria[i].Weight);
            critNormRows.Add(row);
        }
        // Рядок сум колонок
        var colSumsRow = new string[nCrit + 2];
        colSumsRow[0] = "Сума стовпця";
        for (int j = 0; j < nCrit; j++)
        {
            colSumsRow[j + 1] = FormatNumber(critRes.ColumnSums.Length > j ? critRes.ColumnSums[j] : 0.0);
        }
        colSumsRow[nCrit + 1] = FormatNumber(critRes.Weights.Sum());
        critNormRows.Add(colSumsRow);
        tables.Add(new ReportTable("Нормалізована матриця критеріїв і вектор ваг", critNormHeaders, critNormRows));

        // 4. Показники узгодженості матриці критеріїв
        tables.Add(new ReportTable(
            "Показники узгодженості матриці критеріїв",
            new[] { "Показник", "Значення" },
            new List<string[]>
            {
                new[] { "λmax", FormatNumber(critRes.LambdaMax) },
                new[] { "Індекс узгодженості (CI)", FormatNumber(critRes.CI) },
                new[] { "Випадковий індекс (RI)", FormatNumber(critRes.ActiveRI) },
                new[] { "Відношення узгодженості (CR)", FormatPercent(critRes.ActiveCR) },
                new[] { "Поріг узгодженості", FormatPercent(critRes.Threshold) },
                new[] { "Висновок", critRes.IsConsistent ? "Узгоджено" : "Неузгоджено" }
            }
        ));

        // 5. Для кожного критерію: матриця альтернатив, нормалізована матриця та рядок показників
        int nAlt = result.Alternatives.Count;
        var altHeaders = new[] { "Альтернатива" }.Concat(result.Alternatives.Select(a => a.Name)).ToArray();

        foreach (var c in result.Criteria)
        {
            if (!result.AlternativeMatrixResults.TryGetValue(c.Id, out var altRes))
                continue;

            // 5a. Матриця порівнянь альтернатив
            var altPwRows = new List<string[]>();
            for (int i = 0; i < nAlt; i++)
            {
                var row = new string[nAlt + 1];
                row[0] = result.Alternatives[i].Name;
                for (int j = 0; j < nAlt; j++)
                {
                    double v = (altRes.PairwiseMatrix != null && altRes.PairwiseMatrix.GetLength(0) > i && altRes.PairwiseMatrix.GetLength(1) > j)
                        ? altRes.PairwiseMatrix[i, j]
                        : (i == j ? 1.0 : 1.0);
                    row[j + 1] = FormatNumber(v);
                }
                altPwRows.Add(row);
            }
            tables.Add(new ReportTable($"Матриця порівнянь альтернатив за критерієм «{c.Name}»", altHeaders, altPwRows));

            // 5b. Нормалізована матриця й локальні пріоритети
            var altNormHeaders = new[] { "Альтернатива" }.Concat(result.Alternatives.Select(a => a.Name)).Concat(new[] { "Локальний пріоритет" }).ToArray();
            var altNormRows = new List<string[]>();
            for (int i = 0; i < nAlt; i++)
            {
                var row = new string[nAlt + 2];
                row[0] = result.Alternatives[i].Name;
                for (int j = 0; j < nAlt; j++)
                {
                    double v = (altRes.NormalizedMatrix != null && altRes.NormalizedMatrix.GetLength(0) > i && altRes.NormalizedMatrix.GetLength(1) > j)
                        ? altRes.NormalizedMatrix[i, j]
                        : 0.0;
                    row[j + 1] = FormatNumber(v);
                }
                row[nAlt + 1] = FormatNumber(altRes.Weights.Length > i ? altRes.Weights[i] : 0.0);
                altNormRows.Add(row);
            }
            var altColSumsRow = new string[nAlt + 2];
            altColSumsRow[0] = "Сума стовпця";
            for (int j = 0; j < nAlt; j++)
            {
                altColSumsRow[j + 1] = FormatNumber(altRes.ColumnSums.Length > j ? altRes.ColumnSums[j] : 0.0);
            }
            altColSumsRow[nAlt + 1] = FormatNumber(altRes.Weights.Sum());
            altNormRows.Add(altColSumsRow);
            tables.Add(new ReportTable($"Нормалізована матриця й локальні пріоритети за критерієм «{c.Name}»", altNormHeaders, altNormRows));

            // 5c. Рядок показників узгодженості за критерієм
            tables.Add(new ReportTable(
                $"Показники узгодженості матриці альтернатив за критерієм «{c.Name}»",
                new[] { "λmax", "CI", "RI", "CR", "Поріг", "Висновок" },
                new List<string[]>
                {
                    new[]
                    {
                        FormatNumber(altRes.LambdaMax),
                        FormatNumber(altRes.CI),
                        FormatNumber(altRes.ActiveRI),
                        FormatPercent(altRes.ActiveCR),
                        FormatPercent(altRes.Threshold),
                        altRes.IsConsistent ? "Узгоджено" : "Неузгоджено"
                    }
                }
            ));
        }

        // 6. Зведення локальних пріоритетів
        var summaryHeaders = new[] { "Альтернатива" }.Concat(result.Criteria.Select(c => c.Name)).Concat(new[] { "Глобальний пріоритет", "Ранг" }).ToArray();
        var summaryRows = new List<string[]>();
        foreach (var alt in result.RankedAlternatives)
        {
            var row = new string[nCrit + 3];
            row[0] = alt.Name;
            int altIdx = result.Alternatives.FindIndex(a => a.Id == alt.Id);
            for (int j = 0; j < nCrit; j++)
            {
                var c = result.Criteria[j];
                double localPriority = 0.0;
                if (result.AlternativeMatrixResults.TryGetValue(c.Id, out var aRes) && altIdx >= 0 && altIdx < aRes.Weights.Length)
                {
                    localPriority = aRes.Weights[altIdx];
                }
                row[j + 1] = FormatNumber(localPriority);
            }
            row[nCrit + 1] = FormatNumber(alt.GlobalPriority);
            row[nCrit + 2] = alt.Rank.ToString();
            summaryRows.Add(row);
        }
        tables.Add(new ReportTable("Зведення локальних пріоритетів", summaryHeaders, summaryRows));

        // 7. Узгодженість ієрархії
        tables.Add(new ReportTable(
            "Узгодженість ієрархії",
            new[] { "Показник", "Значення" },
            new List<string[]>
            {
                new[] { "CIh (індекс узгодженості ієрархії)", FormatNumber(result.HierarchicalCI) },
                new[] { "RIh (випадковий індекс ієрархії)", FormatNumber(result.ActiveHierarchicalRI) },
                new[] { "CRh (коефіцієнт узгодженості ієрархії)", FormatPercent(result.ActiveHierarchicalCR) },
                new[] { "Поріг узгодженості", FormatPercent(result.HierarchyThreshold) },
                new[] { "Статус узгодженості", result.IsHierarchyConsistent ? "Узгоджено" : "Неузгоджено" },
                new[] { "Неузгоджені матриці", result.InconsistentMatrices.Count > 0 ? string.Join(", ", result.InconsistentMatrices) : "Відсутні (усі матриці узгоджені)" }
            }
        ));

        // 8. Для кожного критерію: чутливість (точки перетину і зміни лідера)
        foreach (var c in result.Criteria)
        {
            CriterionSensitivityResult? sens = null;
            if (sensitivityResults != null && sensitivityResults.TryGetValue(c.Id, out var cachedSens))
            {
                sens = cachedSens;
            }
            else
            {
                try
                {
                    sens = AhpCalculator.CalculateSensitivity(result, c.Id);
                }
                catch
                {
                    // Ігноруємо, якщо розрахунок чутливості недоступний
                }
            }

            if (sens != null)
            {
                var sensHeaders = new[] { "Пара альтернатив", "Вага критерію (t*)", "Пріоритет (P*)", "Тип події", "Опис" };
                var sensRows = new List<string[]>();

                if (sens.Intersections.Count == 0)
                {
                    sensRows.Add(new[] { "—", "—", "—", "—", "Точок перетину в діапазоні [0; 1] не виявлено" });
                }
                else
                {
                    foreach (var p in sens.Intersections)
                    {
                        sensRows.Add(new[]
                        {
                            $"{p.Alt1Name} = {p.Alt2Name}",
                            FormatPercent(p.WeightT),
                            FormatNumber(p.PriorityP),
                            p.IsLeadershipChange ? "Зміна лідера" : "Зміна рангів",
                            p.EventDescription
                        });
                    }
                }

                tables.Add(new ReportTable(
                    $"Чутливість: точки перетину і зміни лідера за критерієм «{c.Name}»",
                    sensHeaders,
                    sensRows,
                    sens.StabilitySummary
                ));
            }
        }

        return tables;
    }

    public static List<ReportTable> Build(AhpProjectData projectData, AhpResult result, AhpCalculationOptions? options = null)
    {
        return Build(result, options ?? projectData.Options);
    }
}
