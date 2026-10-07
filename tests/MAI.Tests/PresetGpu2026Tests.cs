using System;
using System.Collections.Generic;
using System.Linq;
using MAI.Models;
using MAI.Services;
using Xunit;

namespace MAI.Tests;

public class PresetGpu2026Tests
{
    private const double Tolerance = 5e-4;
    private const double SensTolerance = 2e-3;

    [Fact]
    public void ClassicSaaty_CriteriaWeights_MatchBenchmarkValues()
    {
        var preset = PresetService.GetGpu2026Preset();
        var options = AhpCalculationOptions.ClassicSaatyProfile;
        var result = AhpCalculator.SolveHierarchy(
            preset.Criteria,
            preset.Alternatives,
            preset.CriteriaMatrix,
            preset.AlternativeMatrices,
            options);

        // Ваги P / F / A / C: 0,3659 / 0,2326 / 0,1238 / 0,2778
        var pWeight = result.Criteria.First(c => c.Id == "P").Weight;
        var fWeight = result.Criteria.First(c => c.Id == "F").Weight;
        var aWeight = result.Criteria.First(c => c.Id == "A").Weight;
        var cWeight = result.Criteria.First(c => c.Id == "C").Weight;

        Assert.InRange(pWeight, 0.3659 - Tolerance, 0.3659 + Tolerance);
        Assert.InRange(fWeight, 0.2326 - Tolerance, 0.2326 + Tolerance);
        Assert.InRange(aWeight, 0.1238 - Tolerance, 0.1238 + Tolerance);
        Assert.InRange(cWeight, 0.2778 - Tolerance, 0.2778 + Tolerance);
    }

    [Fact]
    public void ClassicSaaty_ConsistencyRatios_MatchBenchmarkValues()
    {
        var preset = PresetService.GetGpu2026Preset();
        var options = AhpCalculationOptions.ClassicSaatyProfile;
        var result = AhpCalculator.SolveHierarchy(
            preset.Criteria,
            preset.Alternatives,
            preset.CriteriaMatrix,
            preset.AlternativeMatrices,
            options);

        // CR (таблиця RI): критерії, P, F, A, C: 0,0170; 0,0190; 0,0306; 0,0000; 0,0437
        Assert.InRange(result.CriteriaMatrixResult.CRTable, 0.0170 - Tolerance, 0.0170 + Tolerance);
        Assert.InRange(result.AlternativeMatrixResults["P"].CRTable, 0.0190 - Tolerance, 0.0190 + Tolerance);
        Assert.InRange(result.AlternativeMatrixResults["F"].CRTable, 0.0306 - Tolerance, 0.0306 + Tolerance);
        Assert.InRange(result.AlternativeMatrixResults["A"].CRTable, 0.0000 - Tolerance, 0.0000 + Tolerance);
        Assert.InRange(result.AlternativeMatrixResults["C"].CRTable, 0.0437 - Tolerance, 0.0437 + Tolerance);

        // CRh: 0,0221
        Assert.InRange(result.HierarchicalCRTable, 0.0221 - Tolerance, 0.0221 + Tolerance);
    }

    [Fact]
    public void ClassicSaaty_GlobalPrioritiesAndRanks_MatchBenchmarkValues()
    {
        var preset = PresetService.GetGpu2026Preset();
        var options = AhpCalculationOptions.ClassicSaatyProfile;
        var result = AhpCalculator.SolveHierarchy(
            preset.Criteria,
            preset.Alternatives,
            preset.CriteriaMatrix,
            preset.AlternativeMatrices,
            options);

        // Глобальні пріоритети A1…A5: 0,2110 / 0,2059 / 0,2213 / 0,1683 / 0,1936
        var a1 = result.RankedAlternatives.First(a => a.Id == "A1");
        var a2 = result.RankedAlternatives.First(a => a.Id == "A2");
        var a3 = result.RankedAlternatives.First(a => a.Id == "A3");
        var a4 = result.RankedAlternatives.First(a => a.Id == "A4");
        var a5 = result.RankedAlternatives.First(a => a.Id == "A5");

        Assert.InRange(a1.GlobalPriority, 0.2110 - Tolerance, 0.2110 + Tolerance);
        Assert.InRange(a2.GlobalPriority, 0.2059 - Tolerance, 0.2059 + Tolerance);
        Assert.InRange(a3.GlobalPriority, 0.2213 - Tolerance, 0.2213 + Tolerance);
        Assert.InRange(a4.GlobalPriority, 0.1683 - Tolerance, 0.1683 + Tolerance);
        Assert.InRange(a5.GlobalPriority, 0.1936 - Tolerance, 0.1936 + Tolerance);

        // Ранжування: A3 > A1 > A2 > A5 > A4
        Assert.Equal(1, a3.Rank);
        Assert.Equal(2, a1.Rank);
        Assert.Equal(3, a2.Rank);
        Assert.Equal(4, a5.Rank);
        Assert.Equal(5, a4.Rank);

        var rankedIds = result.RankedAlternatives.Select(a => a.Id).ToList();
        Assert.Equal(new[] { "A3", "A1", "A2", "A5", "A4" }, rankedIds);
    }

    [Fact]
    public void MethodicalProfile_CriteriaWeights_MatchBenchmarkValues()
    {
        var preset = PresetService.GetGpu2026Preset();
        var options = AhpCalculationOptions.MethodicalProfile;
        var result = AhpCalculator.SolveHierarchy(
            preset.Criteria,
            preset.Alternatives,
            preset.CriteriaMatrix,
            preset.AlternativeMatrices,
            options);

        // Ваги P / F / A / C: 0,3645 / 0,2336 / 0,1242 / 0,2777
        var pWeight = result.Criteria.First(c => c.Id == "P").Weight;
        var fWeight = result.Criteria.First(c => c.Id == "F").Weight;
        var aWeight = result.Criteria.First(c => c.Id == "A").Weight;
        var cWeight = result.Criteria.First(c => c.Id == "C").Weight;

        Assert.InRange(pWeight, 0.3645 - Tolerance, 0.3645 + Tolerance);
        Assert.InRange(fWeight, 0.2336 - Tolerance, 0.2336 + Tolerance);
        Assert.InRange(aWeight, 0.1242 - Tolerance, 0.1242 + Tolerance);
        Assert.InRange(cWeight, 0.2777 - Tolerance, 0.2777 + Tolerance);
    }

    [Fact]
    public void MethodicalProfile_ConsistencyRatios_MatchBenchmarkValues()
    {
        var preset = PresetService.GetGpu2026Preset();
        var options = AhpCalculationOptions.MethodicalProfile;
        var result = AhpCalculator.SolveHierarchy(
            preset.Criteria,
            preset.Alternatives,
            preset.CriteriaMatrix,
            preset.AlternativeMatrices,
            options);

        // CR (емпіричний RI): критерії, P, F, A, C: 0,0166; 0,0254; 0,0363; 0,0000; 0,0627
        Assert.InRange(result.CriteriaMatrixResult.CREmpirical, 0.0166 - Tolerance, 0.0166 + Tolerance);
        Assert.InRange(result.AlternativeMatrixResults["P"].CREmpirical, 0.0254 - Tolerance, 0.0254 + Tolerance);
        Assert.InRange(result.AlternativeMatrixResults["F"].CREmpirical, 0.0363 - Tolerance, 0.0363 + Tolerance);
        Assert.InRange(result.AlternativeMatrixResults["A"].CREmpirical, 0.0000 - Tolerance, 0.0000 + Tolerance);
        Assert.InRange(result.AlternativeMatrixResults["C"].CREmpirical, 0.0627 - Tolerance, 0.0627 + Tolerance);

        // CRh: 0,0267
        Assert.InRange(result.HierarchicalCREmpirical, 0.0267 - Tolerance, 0.0267 + Tolerance);
    }

    [Fact]
    public void MethodicalProfile_GlobalPrioritiesAndRanks_MatchBenchmarkValues()
    {
        var preset = PresetService.GetGpu2026Preset();
        var options = AhpCalculationOptions.MethodicalProfile;
        var result = AhpCalculator.SolveHierarchy(
            preset.Criteria,
            preset.Alternatives,
            preset.CriteriaMatrix,
            preset.AlternativeMatrices,
            options);

        // Глобальні пріоритети A1…A5: 0,2092 / 0,2081 / 0,2209 / 0,1683 / 0,1935
        var a1 = result.RankedAlternatives.First(a => a.Id == "A1");
        var a2 = result.RankedAlternatives.First(a => a.Id == "A2");
        var a3 = result.RankedAlternatives.First(a => a.Id == "A3");
        var a4 = result.RankedAlternatives.First(a => a.Id == "A4");
        var a5 = result.RankedAlternatives.First(a => a.Id == "A5");

        Assert.InRange(a1.GlobalPriority, 0.2092 - Tolerance, 0.2092 + Tolerance);
        Assert.InRange(a2.GlobalPriority, 0.2081 - Tolerance, 0.2081 + Tolerance);
        Assert.InRange(a3.GlobalPriority, 0.2209 - Tolerance, 0.2209 + Tolerance);
        Assert.InRange(a4.GlobalPriority, 0.1683 - Tolerance, 0.1683 + Tolerance);
        Assert.InRange(a5.GlobalPriority, 0.1935 - Tolerance, 0.1935 + Tolerance);

        // Ранжування: A3 > A1 > A2 > A5 > A4
        Assert.Equal(1, a3.Rank);
        Assert.Equal(2, a1.Rank);
        Assert.Equal(3, a2.Rank);
        Assert.Equal(4, a5.Rank);
        Assert.Equal(5, a4.Rank);

        var rankedIds = result.RankedAlternatives.Select(a => a.Id).ToList();
        Assert.Equal(new[] { "A3", "A1", "A2", "A5", "A4" }, rankedIds);
    }

    [Fact]
    public void ClassicSaaty_Sensitivity_LeaderChanges_MatchBenchmarkValues()
    {
        var preset = PresetService.GetGpu2026Preset();
        var options = AhpCalculationOptions.ClassicSaatyProfile;
        var result = AhpCalculator.SolveHierarchy(
            preset.Criteria,
            preset.Alternatives,
            preset.CriteriaMatrix,
            preset.AlternativeMatrices,
            options);

        // P: 0,345 (A1 → A3)
        var sensP = AhpCalculator.CalculateSensitivity(result, "P");
        var leaderP = sensP.Intersections.Where(p => p.IsLeadershipChange).OrderBy(p => p.WeightT).ToList();
        Assert.Single(leaderP);
        Assert.InRange(leaderP[0].WeightT, 0.345 - SensTolerance, 0.345 + SensTolerance);
        Assert.Equal("A1", leaderP[0].OldLeaderId);
        Assert.Equal("A3", leaderP[0].NewLeaderId);

        // F: 0,071 (A1 → A3); 0,272 (A3 → A2)
        var sensF = AhpCalculator.CalculateSensitivity(result, "F");
        var leaderF = sensF.Intersections.Where(p => p.IsLeadershipChange).OrderBy(p => p.WeightT).ToList();
        Assert.Equal(2, leaderF.Count);
        Assert.InRange(leaderF[0].WeightT, 0.071 - SensTolerance, 0.071 + SensTolerance);
        Assert.Equal("A1", leaderF[0].OldLeaderId);
        Assert.Equal("A3", leaderF[0].NewLeaderId);
        Assert.InRange(leaderF[1].WeightT, 0.272 - SensTolerance, 0.272 + SensTolerance);
        Assert.Equal("A3", leaderF[1].OldLeaderId);
        Assert.Equal("A2", leaderF[1].NewLeaderId);

        // A: змін немає (A3 на всьому діапазоні)
        var sensA = AhpCalculator.CalculateSensitivity(result, "A");
        var leaderA = sensA.Intersections.Where(p => p.IsLeadershipChange).ToList();
        Assert.Empty(leaderA);

        // C: 0,294 (A3 → A1)
        var sensC = AhpCalculator.CalculateSensitivity(result, "C");
        var leaderC = sensC.Intersections.Where(p => p.IsLeadershipChange).OrderBy(p => p.WeightT).ToList();
        Assert.Single(leaderC);
        Assert.InRange(leaderC[0].WeightT, 0.294 - SensTolerance, 0.294 + SensTolerance);
        Assert.Equal("A3", leaderC[0].OldLeaderId);
        Assert.Equal("A1", leaderC[0].NewLeaderId);
    }

    [Fact]
    public void ClassicSaaty_Sensitivity_StabilityMarginsAndSuccessors_MatchBenchmarkValues()
    {
        var preset = PresetService.GetGpu2026Preset();
        var options = AhpCalculationOptions.ClassicSaatyProfile;
        var result = AhpCalculator.SolveHierarchy(
            preset.Criteria,
            preset.Alternatives,
            preset.CriteriaMatrix,
            preset.AlternativeMatrices,
            options);

        const double tolMargin = 0.2; // 0.2 п.п.

        // P: лідер A3, поточна вага 36,6 %, інтервал 34,5 %–100 %, запас −2,1 п.п. вниз; нижче 34,5 % лідером стає «RTX 5060 8GB»
        var sensP = AhpCalculator.CalculateSensitivity(result, "P");
        Assert.InRange(sensP.StabilityIntervalMin, 0.345 - SensTolerance, 0.345 + SensTolerance);
        Assert.InRange(sensP.StabilityIntervalMax, 1.0 - SensTolerance, 1.0 + SensTolerance);
        Assert.NotNull(sensP.LowerMarginPp);
        Assert.InRange(sensP.LowerMarginPp.Value, 2.1 - tolMargin, 2.1 + tolMargin);
        Assert.Null(sensP.UpperMarginPp);
        Assert.Contains("5060 8GB", sensP.LowerSuccessorName);
        Assert.Null(sensP.UpperSuccessorName);

        // C: лідер A3, поточна вага 27,8 %, інтервал 0–29,4 %, запас +1,6 п.п. вгору; вище 29,4 % лідером стає «RTX 5060 8GB»
        var sensC = AhpCalculator.CalculateSensitivity(result, "C");
        Assert.InRange(sensC.StabilityIntervalMin, 0.0 - SensTolerance, 0.0 + SensTolerance);
        Assert.InRange(sensC.StabilityIntervalMax, 0.294 - SensTolerance, 0.294 + SensTolerance);
        Assert.Null(sensC.LowerMarginPp);
        Assert.NotNull(sensC.UpperMarginPp);
        Assert.InRange(sensC.UpperMarginPp.Value, 1.6 - tolMargin, 1.6 + tolMargin);
        Assert.Null(sensC.LowerSuccessorName);
        Assert.Contains("5060 8GB", sensC.UpperSuccessorName);

        // F: інтервал 7,1 %–27,2 %, запаси −16,2 / +3,9 п.п.; нижче 7,1 % лідером стає «RTX 5060 8GB», вище 27,2 % — «RTX 5060 Ti 16GB»
        var sensF = AhpCalculator.CalculateSensitivity(result, "F");
        Assert.InRange(sensF.StabilityIntervalMin, 0.071 - SensTolerance, 0.071 + SensTolerance);
        Assert.InRange(sensF.StabilityIntervalMax, 0.272 - SensTolerance, 0.272 + SensTolerance);
        Assert.NotNull(sensF.LowerMarginPp);
        Assert.InRange(sensF.LowerMarginPp.Value, 16.2 - tolMargin, 16.2 + tolMargin);
        Assert.NotNull(sensF.UpperMarginPp);
        Assert.InRange(sensF.UpperMarginPp.Value, 3.9 - tolMargin, 3.9 + tolMargin);
        Assert.Contains("5060 8GB", sensF.LowerSuccessorName);
        Assert.Contains("5060 Ti", sensF.UpperSuccessorName);

        // A: лідер A3 на всьому діапазоні
        var sensA = AhpCalculator.CalculateSensitivity(result, "A");
        Assert.InRange(sensA.StabilityIntervalMin, 0.0 - SensTolerance, 0.0 + SensTolerance);
        Assert.InRange(sensA.StabilityIntervalMax, 1.0 - SensTolerance, 1.0 + SensTolerance);
        Assert.Null(sensA.LowerMarginPp);
        Assert.Null(sensA.UpperMarginPp);
    }

    [Fact]
    public void ClassicSaaty_Sensitivity_NonLeaderIntersections_MatchBenchmarkValues()
    {
        var preset = PresetService.GetGpu2026Preset();
        var options = AhpCalculationOptions.ClassicSaatyProfile;
        var result = AhpCalculator.SolveHierarchy(
            preset.Criteria,
            preset.Alternatives,
            preset.CriteriaMatrix,
            preset.AlternativeMatrices,
            options);

        const double tolInter = 0.005;

        // F: A1/A2 при ≈ 0,244 і A1/A5 при ≈ 0,298
        var sensF = AhpCalculator.CalculateSensitivity(result, "F");
        var fA1A2 = sensF.Intersections.FirstOrDefault(p =>
            (p.Alt1Id == "A1" && p.Alt2Id == "A2") || (p.Alt1Id == "A2" && p.Alt2Id == "A1"));
        Assert.NotNull(fA1A2);
        Assert.InRange(fA1A2.WeightT, 0.244 - tolInter, 0.244 + tolInter);
        Assert.False(fA1A2.IsLeadershipChange);

        var fA1A5 = sensF.Intersections.FirstOrDefault(p =>
            (p.Alt1Id == "A1" && p.Alt2Id == "A5") || (p.Alt1Id == "A5" && p.Alt2Id == "A1"));
        Assert.NotNull(fA1A5);
        Assert.InRange(fA1A5.WeightT, 0.298 - tolInter, 0.298 + tolInter);
        Assert.False(fA1A5.IsLeadershipChange);

        // C: A1/A5 при ≈ 0,249 і A2/A5 при ≈ 0,145
        var sensC = AhpCalculator.CalculateSensitivity(result, "C");
        var cA1A5 = sensC.Intersections.FirstOrDefault(p =>
            (p.Alt1Id == "A1" && p.Alt2Id == "A5") || (p.Alt1Id == "A5" && p.Alt2Id == "A1"));
        Assert.NotNull(cA1A5);
        Assert.InRange(cA1A5.WeightT, 0.249 - tolInter, 0.249 + tolInter);
        Assert.False(cA1A5.IsLeadershipChange);

        var cA2A5 = sensC.Intersections.FirstOrDefault(p =>
            (p.Alt1Id == "A2" && p.Alt2Id == "A5") || (p.Alt1Id == "A5" && p.Alt2Id == "A2"));
        Assert.NotNull(cA2A5);
        Assert.InRange(cA2A5.WeightT, 0.145 - tolInter, 0.145 + tolInter);
        Assert.False(cA2A5.IsLeadershipChange);

        // P: A2/A3 при ≈ 0,327
        var sensP = AhpCalculator.CalculateSensitivity(result, "P");
        var pA2A3 = sensP.Intersections.FirstOrDefault(p =>
            (p.Alt1Id == "A2" && p.Alt2Id == "A3") || (p.Alt1Id == "A3" && p.Alt2Id == "A2"));
        Assert.NotNull(pA2A3);
        Assert.InRange(pA2A3.WeightT, 0.327 - tolInter, 0.327 + tolInter);
        Assert.False(pA2A3.IsLeadershipChange);

        // A: для критерію A немає жодної зміни лідера (усі перетини мають IsLeadershipChange == false)
        var sensA = AhpCalculator.CalculateSensitivity(result, "A");
        Assert.True(sensA.Intersections.All(p => !p.IsLeadershipChange));
    }
}
