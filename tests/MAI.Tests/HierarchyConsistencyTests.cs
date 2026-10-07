using System.Collections.Generic;
using MAI.Models;
using MAI.Services;
using Xunit;

namespace MAI.Tests;

public class HierarchyConsistencyTests
{
    [Fact]
    public void ReferencePreset_HasNoInconsistentMatrices()
    {
        var preset = PresetService.GetReferencePreset();
        var result = AhpCalculator.SolveHierarchy(
            preset.Criteria,
            preset.Alternatives,
            preset.CriteriaMatrix,
            preset.AlternativeMatrices,
            AhpCalculationOptions.ClassicSaatyProfile);

        Assert.Equal(0.10, result.HierarchicalThreshold);
        Assert.True(result.IsHierarchyConsistent);
        Assert.Empty(result.InconsistentMatrices);
    }

    [Fact]
    public void ArtificialInconsistentMatrix_IsTrackedInInconsistentMatrices()
    {
        var criteria = new List<Criterion>
        {
            new("C1", "Критерій 1", "Опис 1"),
            new("C2", "Критерій 2", "Опис 2")
        };

        var alternatives = new List<Alternative>
        {
            new("A1", "Альтернатива 1", "Опис 1"),
            new("A2", "Альтернатива 2", "Опис 2"),
            new("A3", "Альтернатива 3", "Опис 3")
        };

        var criteriaMatrix = new ComparisonMatrix(2, new[] { "C1", "C2" });
        criteriaMatrix.SetComparison(0, 1, 1.0);

        // Матриця для C1 - узгоджена
        var matC1 = new ComparisonMatrix(3, new[] { "A1", "A2", "A3" });
        matC1.SetComparison(0, 1, 2.0);
        matC1.SetComparison(0, 2, 4.0);
        matC1.SetComparison(1, 2, 2.0);

        // Матриця для C2 - грубо неузгоджена (A1 >> A2, A2 >> A3, A3 >> A1)
        var matC2 = new ComparisonMatrix(3, new[] { "A1", "A2", "A3" });
        matC2.SetComparison(0, 1, 9.0);
        matC2.SetComparison(1, 2, 9.0);
        matC2.SetComparison(0, 2, 1.0 / 9.0); // циклічне суперечливе судження

        var altMatrices = new Dictionary<string, ComparisonMatrix>
        {
            ["C1"] = matC1,
            ["C2"] = matC2
        };

        var result = AhpCalculator.SolveHierarchy(
            criteria,
            alternatives,
            criteriaMatrix,
            altMatrices,
            AhpCalculationOptions.ClassicSaatyProfile);

        Assert.Equal(0.10, result.HierarchicalThreshold);
        Assert.Contains(result.InconsistentMatrices, name => name.Contains("C2") || name.Contains("Критерій 2"));
    }
}
