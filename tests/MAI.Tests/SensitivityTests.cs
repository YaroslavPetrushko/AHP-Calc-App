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
    public void Sensitivity_ClassicSaaty_LeaderChangesMatchAppendixA4()
    {
        var preset = PresetService.GetLab34Preset();
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
    public void Sensitivity_Lab4_LeaderChangesMatchAppendixA4()
    {
        var preset = PresetService.GetLab34Preset();
        var result = AhpCalculator.SolveHierarchy(
            preset.Criteria,
            preset.Alternatives,
            preset.CriteriaMatrix,
            preset.AlternativeMatrices,
            AhpCalculationOptions.Lab4Profile);

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
        var preset = PresetService.GetLab34Preset();
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
}
