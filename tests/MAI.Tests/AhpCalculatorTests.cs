using System;
using System.Linq;
using MAI.Models;
using MAI.Services;
using Xunit;

namespace MAI.Tests;

public class AhpCalculatorTests
{
    private const double Tolerance = 5e-4;

    [Fact]
    public void SolveHierarchy_MethodicalProfile_MatchesReferenceValues()
    {
        var preset = PresetService.GetReferencePreset();
        var result = AhpCalculator.SolveHierarchy(
            preset.Criteria,
            preset.Alternatives,
            preset.CriteriaMatrix,
            preset.AlternativeMatrices,
            AhpCalculationOptions.MethodicalProfile);

        // 1. Ваги критеріїв P / F / A / C
        var w = result.Criteria.ToDictionary(c => c.Id, c => c.Weight);
        Assert.InRange(w["P"], 0.3645 - Tolerance, 0.3645 + Tolerance);
        Assert.InRange(w["F"], 0.2336 - Tolerance, 0.2336 + Tolerance);
        Assert.InRange(w["A"], 0.1242 - Tolerance, 0.1242 + Tolerance);
        Assert.InRange(w["C"], 0.2777 - Tolerance, 0.2777 + Tolerance);

        // 2. λmax, CI, CR матриці критеріїв
        var critRes = result.CriteriaMatrixResult;
        Assert.InRange(critRes.LambdaMax, 4.0493 - Tolerance, 4.0493 + Tolerance);
        Assert.InRange(critRes.CI, 0.0164 - Tolerance, 0.0164 + Tolerance);
        Assert.InRange(critRes.CREmpirical, 0.0166 - Tolerance, 0.0166 + Tolerance);

        // 3. CR матриць альтернатив P / F / A / C (активний режим — емпіричний)
        Assert.InRange(result.AlternativeMatrixResults["P"].CREmpirical, 0.0263 - Tolerance, 0.0263 + Tolerance);
        Assert.InRange(result.AlternativeMatrixResults["F"].CREmpirical, 0.0475 - Tolerance, 0.0475 + Tolerance);
        Assert.InRange(result.AlternativeMatrixResults["A"].CREmpirical, 0.0035 - Tolerance, 0.0035 + Tolerance);
        Assert.InRange(result.AlternativeMatrixResults["C"].CREmpirical, 0.0253 - Tolerance, 0.0253 + Tolerance);

        // 4. CIh, CRh
        Assert.InRange(result.HierarchicalCI, 0.0499 - Tolerance, 0.0499 + Tolerance);
        Assert.InRange(result.HierarchicalCREmpirical, 0.0229 - Tolerance, 0.0229 + Tolerance);

        // 5. Глобальні пріоритети A1...A5
        var p = result.Alternatives.ToDictionary(a => a.Id, a => a.GlobalPriority);
        Assert.InRange(p["A1"], 0.1712 - Tolerance, 0.1712 + Tolerance);
        Assert.InRange(p["A2"], 0.1616 - Tolerance, 0.1616 + Tolerance);
        Assert.InRange(p["A3"], 0.2222 - Tolerance, 0.2222 + Tolerance);
        Assert.InRange(p["A4"], 0.2287 - Tolerance, 0.2287 + Tolerance);
        Assert.InRange(p["A5"], 0.2163 - Tolerance, 0.2163 + Tolerance);

        // 6. Ранжування A4 > A3 > A5 > A1 > A2
        var ranking = result.RankedAlternatives.Select(a => a.Id).ToList();
        Assert.Equal(new[] { "A4", "A3", "A5", "A1", "A2" }, ranking);
    }

    [Fact]
    public void SolveHierarchy_ClassicSaatyProfile_MatchesReferenceValues()
    {
        var preset = PresetService.GetReferencePreset();
        var result = AhpCalculator.SolveHierarchy(
            preset.Criteria,
            preset.Alternatives,
            preset.CriteriaMatrix,
            preset.AlternativeMatrices,
            AhpCalculationOptions.ClassicSaatyProfile);

        // 1. Ваги критеріїв P / F / A / C
        var w = result.Criteria.ToDictionary(c => c.Id, c => c.Weight);
        Assert.InRange(w["P"], 0.3659 - Tolerance, 0.3659 + Tolerance);
        Assert.InRange(w["F"], 0.2326 - Tolerance, 0.2326 + Tolerance);
        Assert.InRange(w["A"], 0.1238 - Tolerance, 0.1238 + Tolerance);
        Assert.InRange(w["C"], 0.2778 - Tolerance, 0.2778 + Tolerance);

        // 2. λmax, CI, CR матриці критеріїв
        var critRes = result.CriteriaMatrixResult;
        Assert.InRange(critRes.LambdaMax, 4.0458 - Tolerance, 4.0458 + Tolerance);
        Assert.InRange(critRes.CI, 0.0153 - Tolerance, 0.0153 + Tolerance);
        Assert.InRange(critRes.CRTable, 0.0170 - Tolerance, 0.0170 + Tolerance);

        // 3. CR матриць альтернатив P / F / A / C (активний режим — табличний)
        Assert.InRange(result.AlternativeMatrixResults["P"].CRTable, 0.0244 - Tolerance, 0.0244 + Tolerance);
        Assert.InRange(result.AlternativeMatrixResults["F"].CRTable, 0.0363 - Tolerance, 0.0363 + Tolerance);
        Assert.InRange(result.AlternativeMatrixResults["A"].CRTable, 0.0030 - Tolerance, 0.0030 + Tolerance);
        Assert.InRange(result.AlternativeMatrixResults["C"].CRTable, 0.0185 - Tolerance, 0.0185 + Tolerance);

        // 4. CIh, CRh
        Assert.InRange(result.HierarchicalCI, 0.0409 - Tolerance, 0.0409 + Tolerance);
        Assert.InRange(result.HierarchicalCRTable, 0.0202 - Tolerance, 0.0202 + Tolerance);

        // 5. Глобальні пріоритети A1...A5
        var p = result.Alternatives.ToDictionary(a => a.Id, a => a.GlobalPriority);
        Assert.InRange(p["A1"], 0.1714 - Tolerance, 0.1714 + Tolerance);
        Assert.InRange(p["A2"], 0.1607 - Tolerance, 0.1607 + Tolerance);
        Assert.InRange(p["A3"], 0.2228 - Tolerance, 0.2228 + Tolerance);
        Assert.InRange(p["A4"], 0.2298 - Tolerance, 0.2298 + Tolerance);
        Assert.InRange(p["A5"], 0.2153 - Tolerance, 0.2153 + Tolerance);

        // 6. Ранжування A4 > A3 > A5 > A1 > A2
        var ranking = result.RankedAlternatives.Select(a => a.Id).ToList();
        Assert.Equal(new[] { "A4", "A3", "A5", "A1", "A2" }, ranking);
    }
}
