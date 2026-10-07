using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using MAI.Models;

namespace MAI.Services;

/// <summary>
/// Сервіс порівняльного розрахунку задачі МАІ за різними математичними профілями в єдиному вікні.
/// </summary>
public static class ProfileComparisonService
{
    private static readonly CultureInfo UkCulture = new("uk-UA");

    /// <summary>
    /// Порівняння результатів розрахунку задачі за стандартними або визначеними профілями.
    /// </summary>
    public static ProfileComparisonResult Compare(
        List<Criterion> criteria,
        List<Alternative> alternatives,
        ComparisonMatrix criteriaMatrix,
        Dictionary<string, ComparisonMatrix> altMatrices,
        IEnumerable<AhpCalculationOptions>? customProfiles = null,
        AhpCalculationOptions? activeOptions = null)
    {
        var profileList = new List<(string Name, AhpCalculationOptions Options)>();

        if (customProfiles != null && customProfiles.Any())
        {
            foreach (var opt in customProfiles)
            {
                profileList.Add((opt.ProfileName, opt));
            }
        }
        else
        {
            // Стандартна трійка профілів
            profileList.Add(("Класичний Сааті", AhpCalculationOptions.ClassicSaatyProfile));
            profileList.Add(("ЛР №4", AhpCalculationOptions.Lab4Profile));
            profileList.Add(("Класичний, геометричне середнє", AhpCalculationOptions.GeometricMeanProfile));

            // Якщо поточний активний профіль відрізняється від усіх трьох, додаємо його
            if (activeOptions != null && !profileList.Any(p => p.Options.ValueEquals(activeOptions)))
            {
                profileList.Add(($"Поточний профіль ({activeOptions.ProfileName})", activeOptions.Clone()));
            }
        }

        var result = new ProfileComparisonResult
        {
            Criteria = criteria.Select(c => new Criterion(c.Id, c.Name, c.Description)).ToList(),
            Alternatives = alternatives.Select(a => new Alternative(a.Id, a.Name, a.Description)).ToList()
        };

        // 1. Обчислення розв'язку для кожного профілю
        foreach (var (name, opts) in profileList)
        {
            var ahpRes = AhpCalculator.SolveHierarchy(criteria, alternatives, criteriaMatrix, altMatrices, opts);
            var winner = ahpRes.RankedAlternatives.FirstOrDefault() ?? ahpRes.Alternatives.FirstOrDefault();

            result.Profiles.Add(new ProfileComparisonItem
            {
                ProfileName = name,
                Options = opts,
                Result = ahpRes,
                WinnerId = winner?.Id ?? string.Empty,
                WinnerName = winner?.Name ?? string.Empty,
                WinnerPriority = winner?.GlobalPriority ?? 0.0
            });
        }

        // 2. Порівняння з еталонним профілем (Класичний Сааті)
        var benchmark = result.BenchmarkProfile;
        var benchmarkOrder = benchmark.Result.RankedAlternatives.Select(a => a.Id).ToList();

        foreach (var p in result.Profiles)
        {
            var pOrder = p.Result.RankedAlternatives.Select(a => a.Id).ToList();
            p.RankingMatchesBenchmark = benchmarkOrder.SequenceEqual(pOrder);

            double maxDelta = 0.0;
            foreach (var alt in alternatives)
            {
                double pBench = benchmark.Result.RankedAlternatives.FirstOrDefault(a => a.Id == alt.Id)?.GlobalPriority ?? 0.0;
                double pProf = p.Result.RankedAlternatives.FirstOrDefault(a => a.Id == alt.Id)?.GlobalPriority ?? 0.0;
                maxDelta = Math.Max(maxDelta, Math.Abs(pProf - pBench));
            }
            p.MaxDeltaPriorityFromBenchmark = maxDelta;
        }

        // 3. Формування підсумкового тексту
        var sb = new StringBuilder();
        sb.AppendLine("Підсумок порівняння профілів:");
        sb.AppendLine("• Лідери за профілями:");
        foreach (var p in result.Profiles)
        {
            sb.AppendLine($"  - «{p.ProfileName}»: {p.WinnerName} ({p.WinnerPriority.ToString("0.0000", UkCulture)})");
        }

        bool allMatch = result.AllRankingsMatchBenchmark;
        sb.AppendLine($"• Ранжування збігається з еталонним (Класичний Сааті): {(allMatch ? "так" : "ні")}");

        double maxDeltaAcross = result.MaxDeltaPriorityAcrossProfiles;
        sb.Append($"• Максимальне відхилення пріоритету max |ΔP| відносно еталонного: {maxDeltaAcross.ToString("0.0000", UkCulture)}");

        result.SummaryText = sb.ToString();

        return result;
    }

    /// <summary>
    /// Порівняння результатів розрахунку задачі з об'єкта проекту AhpProjectData.
    /// </summary>
    public static ProfileComparisonResult Compare(
        AhpProjectData projectData,
        IEnumerable<AhpCalculationOptions>? customProfiles = null)
    {
        var critMatrix = new ComparisonMatrix(projectData.Criteria.Count, projectData.Criteria.Select(c => c.Name).ToList());
        critMatrix.LoadFromJaggedArray(projectData.CriteriaMatrix);

        var altMatrices = new Dictionary<string, ComparisonMatrix>();
        foreach (var kvp in projectData.AlternativeMatrices)
        {
            var mat = new ComparisonMatrix(projectData.Alternatives.Count, projectData.Alternatives.Select(a => a.Name).ToList());
            mat.LoadFromJaggedArray(kvp.Value);
            altMatrices[kvp.Key] = mat;
        }

        return Compare(
            projectData.Criteria,
            projectData.Alternatives,
            critMatrix,
            altMatrices,
            customProfiles,
            projectData.Options
        );
    }
}
