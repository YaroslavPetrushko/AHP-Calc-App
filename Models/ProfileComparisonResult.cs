using System;
using System.Collections.Generic;
using System.Linq;
using MAI.Services;

namespace MAI.Models;

/// <summary>
/// Результати розрахунку задачі за окремим математичним профілем.
/// </summary>
public class ProfileComparisonItem
{
    public string ProfileName { get; set; } = string.Empty;
    public AhpCalculationOptions Options { get; set; } = new();
    public AhpResult Result { get; set; } = new();
    public string WinnerId { get; set; } = string.Empty;
    public string WinnerName { get; set; } = string.Empty;
    public double WinnerPriority { get; set; }
    public bool RankingMatchesBenchmark { get; set; }
    public double MaxDeltaPriorityFromBenchmark { get; set; }
}

/// <summary>
/// Агреговані результати порівняння розрахункових профілів МАІ в одному вікні.
/// </summary>
public class ProfileComparisonResult
{
    public List<Criterion> Criteria { get; set; } = new();
    public List<Alternative> Alternatives { get; set; } = new();
    public List<ProfileComparisonItem> Profiles { get; set; } = new();

    public ProfileComparisonItem BenchmarkProfile =>
        Profiles.FirstOrDefault(p => p.Options.IsClassicSaatyProfile)
        ?? Profiles.FirstOrDefault()
        ?? new ProfileComparisonItem();

    public bool AllRankingsMatchBenchmark => Profiles.All(p => p.RankingMatchesBenchmark);

    public double MaxDeltaPriorityAcrossProfiles =>
        Profiles.Count > 0 ? Profiles.Max(p => p.MaxDeltaPriorityFromBenchmark) : 0.0;

    public string SummaryText { get; set; } = string.Empty;

    private static string FormatNumber(double val) => val.ToString("0.0000", LocalizationService.ActiveCulture);
    private static string FormatPercent(double val) => (val * 100.0).ToString("0.00", LocalizationService.ActiveCulture) + " %";

    /// <summary>
    /// Генерація 3-х структурованих звітних таблиць порівняння профілів.
    /// </summary>
    public List<ReportTable> ToReportTables()
    {
        var tables = new List<ReportTable>();

        // 1. Ваги критеріїв за профілями
        string critColName = LocalizationService.Get("Str_Comp_ColCriterion");
        var critHeaders = new[] { critColName }.Concat(Profiles.Select(p => p.ProfileName)).ToArray();
        var critRows = new List<string[]>();
        for (int i = 0; i < Criteria.Count; i++)
        {
            var crit = Criteria[i];
            var row = new string[Profiles.Count + 1];
            row[0] = crit.Name;
            for (int pIdx = 0; pIdx < Profiles.Count; pIdx++)
            {
                var prof = Profiles[pIdx];
                double w = (prof.Result.CriteriaMatrixResult?.Weights != null && prof.Result.CriteriaMatrixResult.Weights.Length > i)
                    ? prof.Result.CriteriaMatrixResult.Weights[i]
                    : 0.0;
                row[pIdx + 1] = FormatNumber(w);
            }
            critRows.Add(row);
        }
        string table1Title = LocalizationService.CurrentLanguage == AppLanguage.En
            ? "Comparison of Criteria Weights Across Profiles"
            : "Порівняння ваг критеріїв за профілями";
        tables.Add(new ReportTable(table1Title, critHeaders, critRows));

        // 2. Глобальні пріоритети та ранги альтернатив за профілями
        string altColName = LocalizationService.Get("Str_Comp_ColAlternative");
        string priorityLabel = LocalizationService.CurrentLanguage == AppLanguage.En ? "Priority" : "Пріоритет";
        string rankLabel = LocalizationService.CurrentLanguage == AppLanguage.En ? "Rank" : "Ранг";

        var altHeadersList = new List<string> { altColName };
        foreach (var prof in Profiles)
        {
            altHeadersList.Add($"{prof.ProfileName} ({priorityLabel})");
            altHeadersList.Add($"{prof.ProfileName} ({rankLabel})");
        }
        var altRows = new List<string[]>();
        for (int i = 0; i < Alternatives.Count; i++)
        {
            var alt = Alternatives[i];
            var row = new string[Profiles.Count * 2 + 1];
            row[0] = alt.Name;
            for (int pIdx = 0; pIdx < Profiles.Count; pIdx++)
            {
                var prof = Profiles[pIdx];
                var matched = prof.Result.RankedAlternatives.FirstOrDefault(a => a.Id == alt.Id);
                double pVal = matched?.GlobalPriority ?? 0.0;
                string rankVal = matched?.RankText ?? "-";
                row[1 + pIdx * 2] = FormatNumber(pVal);
                row[1 + pIdx * 2 + 1] = rankVal;
            }
            altRows.Add(row);
        }
        string table2Title = LocalizationService.CurrentLanguage == AppLanguage.En
            ? "Comparison of Global Priorities and Ranks Across Profiles"
            : "Порівняння глобальних пріоритетів та рангів альтернатив за профілями";
        tables.Add(new ReportTable(table2Title, altHeadersList.ToArray(), altRows));

        // 3. Показники узгодженості: lambda_max і CR матриці критеріїв, CR кожної матриці альтернатив, CRh
        string indicatorColName = LocalizationService.Get("Str_Report_Indicator");
        var metricHeaders = new[] { indicatorColName }.Concat(Profiles.Select(p => p.ProfileName)).ToArray();
        var metricRows = new List<string[]>();

        // Рядок lambda_max критеріїв
        var rowLambda = new string[Profiles.Count + 1];
        rowLambda[0] = LocalizationService.CurrentLanguage == AppLanguage.En ? "λmax (criteria matrix)" : "λmax (матриця критеріїв)";
        for (int pIdx = 0; pIdx < Profiles.Count; pIdx++)
        {
            rowLambda[pIdx + 1] = FormatNumber(Profiles[pIdx].Result.CriteriaMatrixResult?.LambdaMax ?? 0.0);
        }
        metricRows.Add(rowLambda);

        // Рядок CR критеріїв
        var rowCrCrit = new string[Profiles.Count + 1];
        rowCrCrit[0] = LocalizationService.CurrentLanguage == AppLanguage.En ? "CR (criteria matrix)" : "CR (матриця критеріїв)";
        for (int pIdx = 0; pIdx < Profiles.Count; pIdx++)
        {
            rowCrCrit[pIdx + 1] = FormatPercent(Profiles[pIdx].Result.CriteriaMatrixResult?.ActiveCR ?? 0.0);
        }
        metricRows.Add(rowCrCrit);

        // Рядки CR для кожної матриці альтернатив за критерієм
        foreach (var c in Criteria)
        {
            var rowCrAlt = new string[Profiles.Count + 1];
            rowCrAlt[0] = LocalizationService.CurrentLanguage == AppLanguage.En ? $"CR (alternatives for '{c.Name}')" : $"CR (альтернативи за «{c.Name}»)";
            for (int pIdx = 0; pIdx < Profiles.Count; pIdx++)
            {
                var prof = Profiles[pIdx];
                double cr = (prof.Result.AlternativeMatrixResults != null && prof.Result.AlternativeMatrixResults.TryGetValue(c.Id, out var mr))
                    ? mr.ActiveCR
                    : 0.0;
                rowCrAlt[pIdx + 1] = FormatPercent(cr);
            }
            metricRows.Add(rowCrAlt);
        }

        // Рядок CRh
        var rowCrh = new string[Profiles.Count + 1];
        rowCrh[0] = LocalizationService.CurrentLanguage == AppLanguage.En ? "CRh (hierarchy consistency)" : "CRh (узгодженість ієрархії)";
        for (int pIdx = 0; pIdx < Profiles.Count; pIdx++)
        {
            rowCrh[pIdx + 1] = FormatPercent(Profiles[pIdx].Result.ActiveHierarchicalCR);
        }
        metricRows.Add(rowCrh);

        string table3Title = LocalizationService.CurrentLanguage == AppLanguage.En
            ? "Comparison of Consistency Indicators Across Profiles"
            : "Порівняння показників узгодженості за профілями";
        tables.Add(new ReportTable(table3Title, metricHeaders, metricRows, SummaryText));

        return tables;
    }
}
