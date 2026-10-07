using System;
using System.Linq;
using MAI.Models;
using MAI.Services;
using Xunit;

namespace MAI.Tests;

public class SensitivityTests
{
    private const double Tolerance = 2e-3;

    [Fact]
    public void Sensitivity_ClassicSaaty_LeaderChangesMatchReferenceValues()
    {
        var preset = PresetService.GetReferencePreset();
        var result = AhpCalculator.SolveHierarchy(
            preset.Criteria,
            preset.Alternatives,
            preset.CriteriaMatrix,
            preset.AlternativeMatrices,
            AhpCalculationOptions.ClassicSaatyProfile);

        // Критерій P: 0,064 (A1 → A4); 0,429 (A4 → A5)
        var sensP = AhpCalculator.CalculateSensitivity(result, "P");
        var leaderP = sensP.Intersections.Where(p => p.IsLeadershipChange).OrderBy(p => p.WeightT).ToList();
        Assert.Equal(2, leaderP.Count);
        Assert.InRange(leaderP[0].WeightT, 0.064 - Tolerance, 0.064 + Tolerance);
        Assert.Equal("A1", leaderP[0].OldLeaderId);
        Assert.Equal("A4", leaderP[0].NewLeaderId);
        Assert.InRange(leaderP[1].WeightT, 0.429 - Tolerance, 0.429 + Tolerance);
        Assert.Equal("A4", leaderP[1].OldLeaderId);
        Assert.Equal("A5", leaderP[1].NewLeaderId);

        // Критерій F: 0,188 (A5 → A4)
        var sensF = AhpCalculator.CalculateSensitivity(result, "F");
        var leaderF = sensF.Intersections.Where(p => p.IsLeadershipChange).OrderBy(p => p.WeightT).ToList();
        Assert.Single(leaderF);
        Assert.InRange(leaderF[0].WeightT, 0.188 - Tolerance, 0.188 + Tolerance);
        Assert.Equal("A5", leaderF[0].OldLeaderId);
        Assert.Equal("A4", leaderF[0].NewLeaderId);

        // Критерій A: 0,021 (A5 → A4)
        var sensA = AhpCalculator.CalculateSensitivity(result, "A");
        var leaderA = sensA.Intersections.Where(p => p.IsLeadershipChange).OrderBy(p => p.WeightT).ToList();
        Assert.Single(leaderA);
        Assert.InRange(leaderA[0].WeightT, 0.021 - Tolerance, 0.021 + Tolerance);
        Assert.Equal("A5", leaderA[0].OldLeaderId);
        Assert.Equal("A4", leaderA[0].NewLeaderId);

        // Критерій C: 0,355 (A4 → A3); 0,367 (A3 → A1)
        var sensC = AhpCalculator.CalculateSensitivity(result, "C");
        var leaderC = sensC.Intersections.Where(p => p.IsLeadershipChange).OrderBy(p => p.WeightT).ToList();
        Assert.Equal(2, leaderC.Count);
        Assert.InRange(leaderC[0].WeightT, 0.355 - Tolerance, 0.355 + Tolerance);
        Assert.Equal("A4", leaderC[0].OldLeaderId);
        Assert.Equal("A3", leaderC[0].NewLeaderId);
        Assert.InRange(leaderC[1].WeightT, 0.367 - Tolerance, 0.367 + Tolerance);
        Assert.Equal("A3", leaderC[1].OldLeaderId);
        Assert.Equal("A1", leaderC[1].NewLeaderId);
    }

    [Fact]
    public void Sensitivity_MethodicalProfile_LeaderChangesMatchReferenceValues()
    {
        var preset = PresetService.GetReferencePreset();
        var result = AhpCalculator.SolveHierarchy(
            preset.Criteria,
            preset.Alternatives,
            preset.CriteriaMatrix,
            preset.AlternativeMatrices,
            AhpCalculationOptions.MethodicalProfile);

        // Критерій P: 0,065 та 0,419
        var sensP = AhpCalculator.CalculateSensitivity(result, "P");
        var leaderP = sensP.Intersections.Where(p => p.IsLeadershipChange).OrderBy(p => p.WeightT).ToList();
        Assert.Equal(2, leaderP.Count);
        Assert.InRange(leaderP[0].WeightT, 0.065 - Tolerance, 0.065 + Tolerance);
        Assert.InRange(leaderP[1].WeightT, 0.419 - Tolerance, 0.419 + Tolerance);

        // Критерій F: 0,195
        var sensF = AhpCalculator.CalculateSensitivity(result, "F");
        var leaderF = sensF.Intersections.Where(p => p.IsLeadershipChange).OrderBy(p => p.WeightT).ToList();
        Assert.Single(leaderF);
        Assert.InRange(leaderF[0].WeightT, 0.195 - Tolerance, 0.195 + Tolerance);

        // Критерій A: 0,036
        var sensA = AhpCalculator.CalculateSensitivity(result, "A");
        var leaderA = sensA.Intersections.Where(p => p.IsLeadershipChange).OrderBy(p => p.WeightT).ToList();
        Assert.Single(leaderA);
        Assert.InRange(leaderA[0].WeightT, 0.036 - Tolerance, 0.036 + Tolerance);

        // Критерій C: 0,349 та 0,367
        var sensC = AhpCalculator.CalculateSensitivity(result, "C");
        var leaderC = sensC.Intersections.Where(p => p.IsLeadershipChange).OrderBy(p => p.WeightT).ToList();
        Assert.Equal(2, leaderC.Count);
        Assert.InRange(leaderC[0].WeightT, 0.349 - Tolerance, 0.349 + Tolerance);
        Assert.InRange(leaderC[1].WeightT, 0.367 - Tolerance, 0.367 + Tolerance);
    }

    [Fact]
    public void Sensitivity_ClassicSaaty_FalseLeaderChangesExcluded()
    {
        var preset = PresetService.GetReferencePreset();
        var result = AhpCalculator.SolveHierarchy(
            preset.Criteria,
            preset.Alternatives,
            preset.CriteriaMatrix,
            preset.AlternativeMatrices,
            AhpCalculationOptions.ClassicSaatyProfile);

        // Перетин A3/A5 для критерію F (≈0,197) НЕ повинен бути серед змін лідера
        var sensF = AhpCalculator.CalculateSensitivity(result, "F");
        var a3a5Intersection = sensF.Intersections.FirstOrDefault(p =>
            (p.Alt1Id == "A3" && p.Alt2Id == "A5") || (p.Alt1Id == "A5" && p.Alt2Id == "A3"));
        Assert.NotNull(a3a5Intersection);
        Assert.InRange(a3a5Intersection.WeightT, 0.197 - 0.005, 0.197 + 0.005);
        Assert.False(a3a5Intersection.IsLeadershipChange, "Перетин A3/A5 не повинен бути зміною лідера, оскільки лежить нижче A4");

        // Перетин A1/A4 для критерію C (≈0,365) НЕ повинен бути серед змін лідера
        var sensC = AhpCalculator.CalculateSensitivity(result, "C");
        var a1a4Intersection = sensC.Intersections.FirstOrDefault(p =>
            (p.Alt1Id == "A1" && p.Alt2Id == "A4") || (p.Alt1Id == "A4" && p.Alt2Id == "A1"));
        Assert.NotNull(a1a4Intersection);
        Assert.InRange(a1a4Intersection.WeightT, 0.365 - 0.005, 0.365 + 0.005);
        Assert.False(a1a4Intersection.IsLeadershipChange, "Перетин A1/A4 не повинен бути зміною лідера, оскільки лежить нижче A3");
    }

    [Fact]
    public void Sensitivity_ClassicSaaty_StabilitySummaryMatchesReferenceValues()
    {
        var preset = PresetService.GetReferencePreset();
        var result = AhpCalculator.SolveHierarchy(
            preset.Criteria,
            preset.Alternatives,
            preset.CriteriaMatrix,
            preset.AlternativeMatrices,
            AhpCalculationOptions.ClassicSaatyProfile);

        const double tolBound = 2e-3;
        const double tolMargin = 0.2; // 0.2 п.п.

        // 1. Критерій P
        var sensP = AhpCalculator.CalculateSensitivity(result, "P");
        Assert.InRange(sensP.StabilityIntervalMin, 0.064 - tolBound, 0.064 + tolBound);
        Assert.InRange(sensP.StabilityIntervalMax, 0.429 - tolBound, 0.429 + tolBound);
        Assert.NotNull(sensP.LowerMarginPp);
        Assert.InRange(sensP.LowerMarginPp.Value, 30.2 - tolMargin, 30.2 + tolMargin);
        Assert.NotNull(sensP.UpperMarginPp);
        Assert.InRange(sensP.UpperMarginPp.Value, 6.3 - tolMargin, 6.3 + tolMargin);
        Assert.Contains("3050", sensP.LowerSuccessorName);
        Assert.Contains("7700", sensP.UpperSuccessorName);

        // 2. Критерій F
        var sensF = AhpCalculator.CalculateSensitivity(result, "F");
        Assert.InRange(sensF.StabilityIntervalMin, 0.188 - tolBound, 0.188 + tolBound);
        Assert.InRange(sensF.StabilityIntervalMax, 1.0 - tolBound, 1.0 + tolBound);
        Assert.NotNull(sensF.LowerMarginPp);
        Assert.InRange(sensF.LowerMarginPp.Value, 4.4 - tolMargin, 4.4 + tolMargin);
        Assert.Null(sensF.UpperMarginPp);
        Assert.Contains("7700", sensF.LowerSuccessorName);
        Assert.Null(sensF.UpperSuccessorName);

        // 3. Критерій A
        var sensA = AhpCalculator.CalculateSensitivity(result, "A");
        Assert.InRange(sensA.StabilityIntervalMin, 0.021 - tolBound, 0.021 + tolBound);
        Assert.InRange(sensA.StabilityIntervalMax, 1.0 - tolBound, 1.0 + tolBound);
        Assert.NotNull(sensA.LowerMarginPp);
        Assert.InRange(sensA.LowerMarginPp.Value, 10.3 - tolMargin, 10.3 + tolMargin);
        Assert.Null(sensA.UpperMarginPp);
        Assert.Contains("7700", sensA.LowerSuccessorName);
        Assert.Null(sensA.UpperSuccessorName);

        // 4. Критерій C
        var sensC = AhpCalculator.CalculateSensitivity(result, "C");
        Assert.InRange(sensC.StabilityIntervalMin, 0.0 - tolBound, 0.0 + tolBound);
        Assert.InRange(sensC.StabilityIntervalMax, 0.355 - tolBound, 0.355 + tolBound);
        Assert.Null(sensC.LowerMarginPp);
        Assert.NotNull(sensC.UpperMarginPp);
        Assert.InRange(sensC.UpperMarginPp.Value, 7.7 - tolMargin, 7.7 + tolMargin);
        Assert.Null(sensC.LowerSuccessorName);
        Assert.Contains("4060", sensC.UpperSuccessorName);
    }
}
