using System;
using System.Collections.Generic;
using System.Linq;
using MAI.Models;

namespace MAI.Services;

/// <summary>
/// Будівник структурованих таблиць розрахунку МАІ для експорту у звіти.
/// </summary>
public static class ReportTableBuilder
{
    public static string FormatNumber(double value) => value.ToString("0.0000", LocalizationService.ActiveCulture);

    public static string FormatPercent(double value) => (value * 100.0).ToString("0.00", LocalizationService.ActiveCulture) + " %";

    /// <summary>
    /// Побудова повного переліку таблиць для звіту за результатами розрахунку.
    /// </summary>
    public static List<ReportTable> Build(
        AhpResult result,
        AhpCalculationOptions? calculationOptions = null,
        Dictionary<string, CriterionSensitivityResult>? sensitivityResults = null)
    {
        var tables = new List<ReportTable>();
        var opts = calculationOptions ?? result.Options ?? AhpCalculationOptions.Default;

        // 1. Параметри розрахунку
        string weightDesc = opts.WeightMethod switch
        {
            WeightCalculationMethod.ColumnNormalization => LocalizationService.Get("Str_Menu_Weight_ColNorm"),
            WeightCalculationMethod.GeometricMean => LocalizationService.Get("Str_Menu_Weight_Geometric"),
            WeightCalculationMethod.PrincipalEigenvector => LocalizationService.Get("Str_Menu_Weight_PowerIteration"),
            _ => opts.WeightMethod.ToString()
        };
        string lambdaDesc = AhpCalculationOptions.GetLambdaMethodDescription(opts.LambdaMethod);
        string riDesc = opts.RiMode == RiCalculationMode.Empirical
            ? LocalizationService.Get("Str_Menu_Ri_Empirical")
            : LocalizationService.Get("Str_Menu_Ri_Table");
        string threshDesc = opts.ThresholdMode switch
        {
            ConsistencyThresholdMode.Uniform10Percent => LocalizationService.Get("Str_Menu_Threshold_Uniform"),
            ConsistencyThresholdMode.SaatyAdaptive => LocalizationService.Get("Str_Menu_Threshold_Adaptive"),
            _ => opts.ThresholdMode.ToString()
        };

        tables.Add(new ReportTable(
            LocalizationService.Get("Str_Report_TableParams"),
            new[] { LocalizationService.Get("Str_Report_Param"), LocalizationService.Get("Str_Report_Value") },
            new List<string[]>
            {
                new[] { LocalizationService.Get("Str_Menu_Profile"), opts.ProfileName },
                new[] { LocalizationService.Get("Str_Menu_WeightMethod"), weightDesc },
                new[] { LocalizationService.Get("Str_Menu_LambdaMethod"), lambdaDesc },
                new[] { LocalizationService.Get("Str_Menu_RiMode"), riDesc },
                new[] { LocalizationService.Get("Str_Report_Threshold"), threshDesc }
            }
        ));

        // 2. Матриця парних порівнянь критеріїв
        var critRes = result.CriteriaMatrixResult;
        int nCrit = result.Criteria.Count;
        var critHeaders = new[] { LocalizationService.Get("Str_Main_CornerCrit") }.Concat(result.Criteria.Select(c => c.Name)).ToArray();
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
                row[j + 1] = ComparisonMatrix.FormatSaatyValue(v);
            }
            critPwRows.Add(row);
        }
        tables.Add(new ReportTable(LocalizationService.Get("Str_Report_TableCritPw"), critHeaders, critPwRows));

        // 3. Нормалізована матриця критеріїв і вектор ваг
        string weightHeader = LocalizationService.CurrentLanguage == AppLanguage.En ? "Weight (w)" : "Вага (w)";
        var critNormHeaders = new[] { LocalizationService.Get("Str_Main_CornerCrit") }.Concat(result.Criteria.Select(c => c.Name)).Concat(new[] { weightHeader }).ToArray();
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
        colSumsRow[0] = LocalizationService.Get("Str_Report_ColSum");
        for (int j = 0; j < nCrit; j++)
        {
            colSumsRow[j + 1] = FormatNumber(critRes.ColumnSums.Length > j ? critRes.ColumnSums[j] : 0.0);
        }
        colSumsRow[nCrit + 1] = FormatNumber(critRes.Weights.Sum());
        critNormRows.Add(colSumsRow);
        tables.Add(new ReportTable(LocalizationService.Get("Str_Report_TableCritNorm"), critNormHeaders, critNormRows));

        // 4. Показники узгодженості матриці критеріїв
        tables.Add(new ReportTable(
            LocalizationService.Get("Str_Report_TableCritConsistency"),
            new[] { LocalizationService.Get("Str_Report_Indicator"), LocalizationService.Get("Str_Report_Value") },
            new List<string[]>
            {
                new[] { "λmax", FormatNumber(critRes.LambdaMax) },
                new[] { LocalizationService.Get("Str_Report_CI"), FormatNumber(critRes.CI) },
                new[] { LocalizationService.Get("Str_Report_RI"), FormatNumber(critRes.ActiveRI) },
                new[] { LocalizationService.Get("Str_Report_CR"), FormatPercent(critRes.ActiveCR) },
                new[] { LocalizationService.Get("Str_Report_Threshold"), FormatPercent(critRes.Threshold) },
                new[] { LocalizationService.Get("Str_Report_Conclusion"), critRes.IsConsistent ? LocalizationService.Get("Str_Report_Consistent") : LocalizationService.Get("Str_Report_Inconsistent") }
            }
        ));

        // 5. Для кожного критерію: матриця альтернатив, нормалізована матриця та рядок показників
        int nAlt = result.Alternatives.Count;
        var altHeaders = new[] { LocalizationService.Get("Str_Main_CornerAlt") }.Concat(result.Alternatives.Select(a => a.Name)).ToArray();

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
                    row[j + 1] = ComparisonMatrix.FormatSaatyValue(v);
                }
                altPwRows.Add(row);
            }
            tables.Add(new ReportTable(LocalizationService.Format("Str_Report_TableAltPwFmt", c.Name), altHeaders, altPwRows));

            // 5b. Нормалізована матриця й локальні пріоритети
            var altNormHeaders = new[] { LocalizationService.Get("Str_Main_CornerAlt") }.Concat(result.Alternatives.Select(a => a.Name)).Concat(new[] { LocalizationService.Get("Str_Report_LocalPriority") }).ToArray();
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
            altColSumsRow[0] = LocalizationService.Get("Str_Report_ColSum");
            for (int j = 0; j < nAlt; j++)
            {
                altColSumsRow[j + 1] = FormatNumber(altRes.ColumnSums.Length > j ? altRes.ColumnSums[j] : 0.0);
            }
            altColSumsRow[nAlt + 1] = FormatNumber(altRes.Weights.Sum());
            altNormRows.Add(altColSumsRow);
            tables.Add(new ReportTable(LocalizationService.Format("Str_Report_TableAltNormFmt", c.Name), altNormHeaders, altNormRows));

            // 5c. Рядок показників узгодженості за критерієм
            string thresholdColName = LocalizationService.CurrentLanguage == AppLanguage.En ? "Threshold" : "Поріг";
            tables.Add(new ReportTable(
                LocalizationService.Format("Str_Report_TableAltConsistencyFmt", c.Name),
                new[] { "λmax", "CI", "RI", "CR", thresholdColName, LocalizationService.Get("Str_Report_Conclusion") },
                new List<string[]>
                {
                    new[]
                    {
                        FormatNumber(altRes.LambdaMax),
                        FormatNumber(altRes.CI),
                        FormatNumber(altRes.ActiveRI),
                        FormatPercent(altRes.ActiveCR),
                        FormatPercent(altRes.Threshold),
                        altRes.IsConsistent ? LocalizationService.Get("Str_Report_Consistent") : LocalizationService.Get("Str_Report_Inconsistent")
                    }
                }
            ));
        }

        // 6. Зведення локальних пріоритетів
        var summaryHeaders = new[] { LocalizationService.Get("Str_Main_CornerAlt") }.Concat(result.Criteria.Select(c => c.Name)).Concat(new[] { LocalizationService.Get("Str_Report_GlobalPriorityCol"), LocalizationService.Get("Str_Report_RankCol") }).ToArray();
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
            row[nCrit + 2] = alt.RankText;
            summaryRows.Add(row);
        }
        tables.Add(new ReportTable(LocalizationService.Get("Str_Report_TableSummary"), summaryHeaders, summaryRows));

        // 7. Узгодженість ієрархії
        tables.Add(new ReportTable(
            LocalizationService.Get("Str_Report_TableHierarchy"),
            new[] { LocalizationService.Get("Str_Report_Indicator"), LocalizationService.Get("Str_Report_Value") },
            new List<string[]>
            {
                new[] { LocalizationService.Get("Str_Report_CIh"), FormatNumber(result.HierarchicalCI) },
                new[] { LocalizationService.Get("Str_Report_RIh"), FormatNumber(result.ActiveHierarchicalRI) },
                new[] { LocalizationService.Get("Str_Report_CRh"), FormatPercent(result.ActiveHierarchicalCR) },
                new[] { LocalizationService.Get("Str_Report_Threshold"), FormatPercent(result.HierarchyThreshold) },
                new[] { LocalizationService.Get("Str_Report_HierarchyStatus"), result.IsHierarchyConsistent ? LocalizationService.Get("Str_Report_Consistent") : LocalizationService.Get("Str_Report_Inconsistent") },
                new[] { LocalizationService.Get("Str_Report_InconsistentMatrices"), result.InconsistentMatrices.Count > 0 ? string.Join(", ", result.InconsistentMatrices) : LocalizationService.Get("Str_Report_NoneAllConsistent") }
            }
        ));

        // 8. Для кожного критерію: чутливість (точки перетину і зміни лідера)
        foreach (var c in result.Criteria)
        {
            CriterionSensitivityResult? sens = null;
            string? sensError = null;
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
                catch (Exception ex)
                {
                    sensError = ex.Message;
                }
            }

            if (sens != null)
            {
                var sensHeaders = new[] { LocalizationService.Get("Str_Report_PairAlt"), LocalizationService.Get("Str_Report_WeightCritT"), LocalizationService.Get("Str_Report_PriorityP"), LocalizationService.Get("Str_Report_EventType"), LocalizationService.Get("Str_Report_Description") };
                var sensRows = new List<string[]>();

                if (sens.Intersections.Count == 0)
                {
                    sensRows.Add(new[] { "—", "—", "—", "—", LocalizationService.Get("Str_Report_NoIntersections") });
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
                            p.IsLeadershipChange ? LocalizationService.Get("Str_Report_LeaderChange") : LocalizationService.Get("Str_Report_RankChange"),
                            p.EventDescription
                        });
                    }
                }

                tables.Add(new ReportTable(
                    LocalizationService.Format("Str_Report_TableSensFmt", c.Name),
                    sensHeaders,
                    sensRows,
                    sens.StabilitySummary
                ));
            }
            else if (sensError != null)
            {
                var sensHeaders = new[] { LocalizationService.Get("Str_Report_PairAlt"), LocalizationService.Get("Str_Report_WeightCritT"), LocalizationService.Get("Str_Report_PriorityP"), LocalizationService.Get("Str_Report_EventType"), LocalizationService.Get("Str_Report_Description") };
                string errorMsg = LocalizationService.CurrentLanguage == AppLanguage.En
                    ? $"Sensitivity calculation unavailable: {sensError}"
                    : $"Розрахунок чутливості недоступний: {sensError}";
                var sensRows = new List<string[]>
                {
                    new[] { "—", "—", "—", "—", errorMsg }
                };
                tables.Add(new ReportTable(
                    LocalizationService.Format("Str_Report_TableSensFmt", c.Name),
                    sensHeaders,
                    sensRows,
                    LocalizationService.Format("Str_Report_SensUnavailable", c.Name, sensError)
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
