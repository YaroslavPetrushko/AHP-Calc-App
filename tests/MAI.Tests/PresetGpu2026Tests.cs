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

        // CR (таблиця RI): критерії, P, F, A, C: 0,0170; 0,0190; 0,0306; 0,0000; 0,0519
        Assert.InRange(result.CriteriaMatrixResult.CRTable, 0.0170 - Tolerance, 0.0170 + Tolerance);
        Assert.InRange(result.AlternativeMatrixResults["P"].CRTable, 0.0190 - Tolerance, 0.0190 + Tolerance);
        Assert.InRange(result.AlternativeMatrixResults["F"].CRTable, 0.0306 - Tolerance, 0.0306 + Tolerance);
        Assert.InRange(result.AlternativeMatrixResults["A"].CRTable, 0.0000 - Tolerance, 0.0000 + Tolerance);
        Assert.InRange(result.AlternativeMatrixResults["C"].CRTable, 0.0519 - Tolerance, 0.0519 + Tolerance);

        // CRh: 0,0234
        Assert.InRange(result.HierarchicalCRTable, 0.0234 - Tolerance, 0.0234 + Tolerance);
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

        // Глобальні пріоритети A1…A5: 0,2172 / 0,2056 / 0,2186 / 0,1621 / 0,1965
        var a1 = result.RankedAlternatives.First(a => a.Id == "A1");
        var a2 = result.RankedAlternatives.First(a => a.Id == "A2");
        var a3 = result.RankedAlternatives.First(a => a.Id == "A3");
        var a4 = result.RankedAlternatives.First(a => a.Id == "A4");
        var a5 = result.RankedAlternatives.First(a => a.Id == "A5");

        Assert.InRange(a1.GlobalPriority, 0.2172 - Tolerance, 0.2172 + Tolerance);
        Assert.InRange(a2.GlobalPriority, 0.2056 - Tolerance, 0.2056 + Tolerance);
        Assert.InRange(a3.GlobalPriority, 0.2186 - Tolerance, 0.2186 + Tolerance);
        Assert.InRange(a4.GlobalPriority, 0.1621 - Tolerance, 0.1621 + Tolerance);
        Assert.InRange(a5.GlobalPriority, 0.1965 - Tolerance, 0.1965 + Tolerance);

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

        // CR (емпіричний RI): критерії, P, F, A, C: 0,0166; 0,0254; 0,0363; 0,0000; 0,0816
        Assert.InRange(result.CriteriaMatrixResult.CREmpirical, 0.0166 - Tolerance, 0.0166 + Tolerance);
        Assert.InRange(result.AlternativeMatrixResults["P"].CREmpirical, 0.0254 - Tolerance, 0.0254 + Tolerance);
        Assert.InRange(result.AlternativeMatrixResults["F"].CREmpirical, 0.0363 - Tolerance, 0.0363 + Tolerance);
        Assert.InRange(result.AlternativeMatrixResults["A"].CREmpirical, 0.0000 - Tolerance, 0.0000 + Tolerance);
        Assert.InRange(result.AlternativeMatrixResults["C"].CREmpirical, 0.0816 - Tolerance, 0.0816 + Tolerance);

        // CRh: 0,0296
        Assert.InRange(result.HierarchicalCREmpirical, 0.0296 - Tolerance, 0.0296 + Tolerance);
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

        // Глобальні пріоритети A1…A5: 0,2133 / 0,2091 / 0,2179 / 0,1628 / 0,1969
        var a1 = result.RankedAlternatives.First(a => a.Id == "A1");
        var a2 = result.RankedAlternatives.First(a => a.Id == "A2");
        var a3 = result.RankedAlternatives.First(a => a.Id == "A3");
        var a4 = result.RankedAlternatives.First(a => a.Id == "A4");
        var a5 = result.RankedAlternatives.First(a => a.Id == "A5");

        Assert.InRange(a1.GlobalPriority, 0.2133 - Tolerance, 0.2133 + Tolerance);
        Assert.InRange(a2.GlobalPriority, 0.2091 - Tolerance, 0.2091 + Tolerance);
        Assert.InRange(a3.GlobalPriority, 0.2179 - Tolerance, 0.2179 + Tolerance);
        Assert.InRange(a4.GlobalPriority, 0.1628 - Tolerance, 0.1628 + Tolerance);
        Assert.InRange(a5.GlobalPriority, 0.1969 - Tolerance, 0.1969 + Tolerance);

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

        // P: 0,363 (A1 → A3)
        var sensP = AhpCalculator.CalculateSensitivity(result, "P");
        var leaderP = sensP.Intersections.Where(p => p.IsLeadershipChange).OrderBy(p => p.WeightT).ToList();
        Assert.Single(leaderP);
        Assert.InRange(leaderP[0].WeightT, 0.363 - SensTolerance, 0.363 + SensTolerance);
        Assert.Equal("A1", leaderP[0].OldLeaderId);
        Assert.Equal("A3", leaderP[0].NewLeaderId);

        // F: 0,215 (A1 → A3); 0,266 (A3 → A2)
        var sensF = AhpCalculator.CalculateSensitivity(result, "F");
        var leaderF = sensF.Intersections.Where(p => p.IsLeadershipChange).OrderBy(p => p.WeightT).ToList();
        Assert.Equal(2, leaderF.Count);
        Assert.InRange(leaderF[0].WeightT, 0.215 - SensTolerance, 0.215 + SensTolerance);
        Assert.Equal("A1", leaderF[0].OldLeaderId);
        Assert.Equal("A3", leaderF[0].NewLeaderId);
        Assert.InRange(leaderF[1].WeightT, 0.266 - SensTolerance, 0.266 + SensTolerance);
        Assert.Equal("A3", leaderF[1].OldLeaderId);
        Assert.Equal("A2", leaderF[1].NewLeaderId);

        // A: змін немає (A3 на всьому діапазоні)
        var sensA = AhpCalculator.CalculateSensitivity(result, "A");
        var leaderA = sensA.Intersections.Where(p => p.IsLeadershipChange).ToList();
        Assert.Empty(leaderA);

        // C: 0,280 (A3 → A1)
        var sensC = AhpCalculator.CalculateSensitivity(result, "C");
        var leaderC = sensC.Intersections.Where(p => p.IsLeadershipChange).OrderBy(p => p.WeightT).ToList();
        Assert.Single(leaderC);
        Assert.InRange(leaderC[0].WeightT, 0.280 - SensTolerance, 0.280 + SensTolerance);
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

        // P: лідер A3, поточна вага 36,6 %, інтервал 36,3 %–100 %, запас −0,3 п.п. вниз; нижче 36,3 % лідером стає «RTX 5060 8GB»
        var sensP = AhpCalculator.CalculateSensitivity(result, "P");
        Assert.InRange(sensP.StabilityIntervalMin, 0.363 - SensTolerance, 0.363 + SensTolerance);
        Assert.InRange(sensP.StabilityIntervalMax, 1.0 - SensTolerance, 1.0 + SensTolerance);
        Assert.NotNull(sensP.LowerMarginPp);
        Assert.InRange(sensP.LowerMarginPp.Value, 0.26 - tolMargin, 0.26 + tolMargin);
        Assert.Null(sensP.UpperMarginPp);
        Assert.Contains("5060 8GB", sensP.LowerSuccessorName);
        Assert.Null(sensP.UpperSuccessorName);

        // C: лідер A3, поточна вага 27,8 %, інтервал 0–28,0 %, запас +0,2 п.п. вгору; вище 28,0 % лідером стає «RTX 5060 8GB»
        var sensC = AhpCalculator.CalculateSensitivity(result, "C");
        Assert.InRange(sensC.StabilityIntervalMin, 0.0 - SensTolerance, 0.0 + SensTolerance);
        Assert.InRange(sensC.StabilityIntervalMax, 0.280 - SensTolerance, 0.280 + SensTolerance);
        Assert.Null(sensC.LowerMarginPp);
        Assert.NotNull(sensC.UpperMarginPp);
        Assert.InRange(sensC.UpperMarginPp.Value, 0.21 - tolMargin, 0.21 + tolMargin);
        Assert.Null(sensC.LowerSuccessorName);
        Assert.Contains("5060 8GB", sensC.UpperSuccessorName);

        // F: інтервал 21,5 %–26,6 %, запаси −1,8 / +3,4 п.п.; нижче 21,5 % лідером стає «RTX 5060 8GB», вище 26,6 % — «RTX 5060 Ti 16GB»
        var sensF = AhpCalculator.CalculateSensitivity(result, "F");
        Assert.InRange(sensF.StabilityIntervalMin, 0.215 - SensTolerance, 0.215 + SensTolerance);
        Assert.InRange(sensF.StabilityIntervalMax, 0.266 - SensTolerance, 0.266 + SensTolerance);
        Assert.NotNull(sensF.LowerMarginPp);
        Assert.InRange(sensF.LowerMarginPp.Value, 1.77 - tolMargin, 1.77 + tolMargin);
        Assert.NotNull(sensF.UpperMarginPp);
        Assert.InRange(sensF.UpperMarginPp.Value, 3.35 - tolMargin, 3.35 + tolMargin);
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

        // F: A1/A2 при ≈ 0,258 і A1/A5 при ≈ 0,309
        var sensF = AhpCalculator.CalculateSensitivity(result, "F");
        var fA1A2 = sensF.Intersections.FirstOrDefault(p =>
            (p.Alt1Id == "A1" && p.Alt2Id == "A2") || (p.Alt1Id == "A2" && p.Alt2Id == "A1"));
        Assert.NotNull(fA1A2);
        Assert.InRange(fA1A2.WeightT, 0.258 - tolInter, 0.258 + tolInter);
        Assert.False(fA1A2.IsLeadershipChange);

        var fA1A5 = sensF.Intersections.FirstOrDefault(p =>
            (p.Alt1Id == "A1" && p.Alt2Id == "A5") || (p.Alt1Id == "A5" && p.Alt2Id == "A1"));
        Assert.NotNull(fA1A5);
        Assert.InRange(fA1A5.WeightT, 0.309 - tolInter, 0.309 + tolInter);
        Assert.False(fA1A5.IsLeadershipChange);

        // C: A1/A5 при ≈ 0,245 і A2/A5 при ≈ 0,166
        var sensC = AhpCalculator.CalculateSensitivity(result, "C");
        var cA1A5 = sensC.Intersections.FirstOrDefault(p =>
            (p.Alt1Id == "A1" && p.Alt2Id == "A5") || (p.Alt1Id == "A5" && p.Alt2Id == "A1"));
        Assert.NotNull(cA1A5);
        Assert.InRange(cA1A5.WeightT, 0.245 - tolInter, 0.245 + tolInter);
        Assert.False(cA1A5.IsLeadershipChange);

        var cA2A5 = sensC.Intersections.FirstOrDefault(p =>
            (p.Alt1Id == "A2" && p.Alt2Id == "A5") || (p.Alt1Id == "A5" && p.Alt2Id == "A2"));
        Assert.NotNull(cA2A5);
        Assert.InRange(cA2A5.WeightT, 0.166 - tolInter, 0.166 + tolInter);
        Assert.False(cA2A5.IsLeadershipChange);

        // P: A2/A3 при ≈ 0,333
        var sensP = AhpCalculator.CalculateSensitivity(result, "P");
        var pA2A3 = sensP.Intersections.FirstOrDefault(p =>
            (p.Alt1Id == "A2" && p.Alt2Id == "A3") || (p.Alt1Id == "A3" && p.Alt2Id == "A2"));
        Assert.NotNull(pA2A3);
        Assert.InRange(pA2A3.WeightT, 0.333 - tolInter, 0.333 + tolInter);
        Assert.False(pA2A3.IsLeadershipChange);

        // A: для критерію A немає жодної зміни лідера (усі перетини мають IsLeadershipChange == false)
        var sensA = AhpCalculator.CalculateSensitivity(result, "A");
        Assert.True(sensA.Intersections.All(p => !p.IsLeadershipChange));
    }

    [Fact]
    public void AllBuiltInPresets_PassValidation_AndAreFullyJudged()
    {
        var presets = new[]
        {
            ("Reference", PresetService.GetReferencePreset()),
            ("Gpu2026", PresetService.GetGpu2026Preset())
        };

        foreach (var (name, preset) in presets)
        {
            var projectData = new AhpProjectData
            {
                Title = preset.Name,
                Description = preset.Description,
                Criteria = preset.Criteria,
                Alternatives = preset.Alternatives,
                CriteriaMatrix = preset.CriteriaMatrix.ToJaggedArray(),
                AlternativeMatrices = preset.AlternativeMatrices.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.ToJaggedArray())
            };

            var errors = ProjectStorageService.ValidateProject(projectData);
            Assert.Empty(errors);

            Assert.Equal(4, preset.Criteria.Count);
            Assert.Equal(5, preset.Alternatives.Count);

            Assert.True(preset.CriteriaMatrix.IsFullyJudged, $"{name}: criteria matrix must be fully judged");
            Assert.Equal(4, preset.AlternativeMatrices.Count);
            foreach (var (critId, altMatrix) in preset.AlternativeMatrices)
            {
                Assert.True(altMatrix.IsFullyJudged, $"{name}: alternative matrix for {critId} must be fully judged");
            }
        }
    }

    [Fact]
    public void Gpu2026Preset_Matrices_HaveExactSaatyScaleValues_AndReciprocals()
    {
        var preset = PresetService.GetGpu2026Preset();

        void VerifyMatrix(ComparisonMatrix matrix, string matrixName)
        {
            int n = matrix.Size;
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    double val = matrix.Values[i, j];
                    double reciprocal = matrix.Values[j, i];

                    // 1. Точний добуток взаємно обернених значень = 1 (1e-12)
                    Assert.True(Math.Abs(val * reciprocal - 1.0) < 1e-12,
                        $"{matrixName}[{i},{j}] * [{j},{i}] = {val * reciprocal} != 1.0");

                    // 2. Значення точно збігається зі шкалою Сааті
                    double snapped = ComparisonMatrix.SnapToSaatyScale(val);
                    Assert.True(Math.Abs(val - snapped) < 1e-12,
                        $"{matrixName}[{i},{j}] = {val} does not match Saaty scale exact value");
                }
            }
        }

        VerifyMatrix(preset.CriteriaMatrix, "CriteriaMatrix");
        foreach (var (critId, altMatrix) in preset.AlternativeMatrices)
        {
            VerifyMatrix(altMatrix, $"AltMatrix_{critId}");
        }
    }

    [Fact]
    public void LoadLegacyV120Json_WithoutOptions_DefaultsToMethodicalProfile_AndPreservesMatrices()
    {
        string legacyJson = """
        {
          "title": "Legacy Project without explicit options",
          "description": "Тест завантаження проєкту без блоку options",
          "version": "1.2.0",
          "criteria": [
            { "id": "C1", "name": "Критерій 1", "description": "" },
            { "id": "C2", "name": "Критерій 2", "description": "" }
          ],
          "alternatives": [
            { "id": "A1", "name": "Альтернатива 1", "description": "" },
            { "id": "A2", "name": "Альтернатива 2", "description": "" }
          ],
          "criteriaMatrix": [
            [ 1.0, 3.0 ],
            [ 0.3333333333333333, 1.0 ]
          ],
          "alternativeMatrices": {
            "C1": [
              [ 1.0, 2.0 ],
              [ 0.5, 1.0 ]
            ],
            "C2": [
              [ 1.0, 4.0 ],
              [ 0.25, 1.0 ]
            ]
          }
        }
        """;

        string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"ahp_legacy_{Guid.NewGuid():N}.ahp.json");
        try
        {
            System.IO.File.WriteAllText(tempFile, legacyJson);
            var loaded = ProjectStorageService.LoadProject(tempFile);

            Assert.NotNull(loaded);
            Assert.True(loaded.Options.IsMethodicalProfile, "Missing options must default to MethodicalProfile");
            Assert.Equal(WeightCalculationMethod.ColumnNormalization, loaded.Options.WeightMethod);
            Assert.Equal(LambdaCalculationMethod.ColumnSumProduct, loaded.Options.LambdaMethod);
            Assert.Equal(RiCalculationMode.Empirical, loaded.Options.RiMode);
            Assert.Equal(ConsistencyThresholdMode.Uniform10Percent, loaded.Options.ThresholdMode);

            Assert.Equal(2, loaded.Criteria.Count);
            Assert.Equal(2, loaded.Alternatives.Count);
            Assert.Equal(3.0, loaded.CriteriaMatrix[0][1]);
            Assert.Equal(2.0, loaded.AlternativeMatrices["C1"][0][1]);
            Assert.Equal(4.0, loaded.AlternativeMatrices["C2"][0][1]);
        }
        finally
        {
            if (System.IO.File.Exists(tempFile))
            {
                System.IO.File.Delete(tempFile);
            }
        }
    }
}
