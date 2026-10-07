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
}
