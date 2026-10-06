using System;
using System.Collections.Generic;
using System.Linq;
using MAI.Models;
using MAI.Services;
using Xunit;

namespace MAI.Tests;

public class RankToleranceTests
{
    [Fact]
    public void AllMatricesUnit_AllAlternativesRankOneWithEquals()
    {
        // Задача, де всі матриці складаються з одиниць
        var criteria = new List<Criterion>
        {
            new("C1", "Критерій 1"),
            new("C2", "Критерій 2")
        };
        var alternatives = new List<Alternative>
        {
            new("A1", "Альтернатива 1"),
            new("A2", "Альтернатива 2"),
            new("A3", "Альтернатива 3")
        };

        var critMatrix = new ComparisonMatrix(2);
        var altMatrices = new Dictionary<string, ComparisonMatrix>
        {
            { "C1", new ComparisonMatrix(3) },
            { "C2", new ComparisonMatrix(3) }
        };

        var result = AhpCalculator.SolveHierarchy(criteria, alternatives, critMatrix, altMatrices);

        Assert.Equal(3, result.RankedAlternatives.Count);
        foreach (var alt in result.RankedAlternatives)
        {
            Assert.Equal(1, alt.Rank);
            Assert.Equal("1=", alt.RankText);
        }
    }

    [Fact]
    public void TwoAlternativesWithIdenticalJudgments_HaveIdenticalTiedRanks()
    {
        // Задача з трьома альтернативами, де A2 та A3 мають ідентичні судження в усіх матрицях
        var criteria = new List<Criterion>
        {
            new("C1", "Критерій 1")
        };
        var alternatives = new List<Alternative>
        {
            new("A1", "Лідер"),
            new("A2", "Клон 1"),
            new("A3", "Клон 2")
        };

        var critMatrix = new ComparisonMatrix(1);
        var altMatrix = new ComparisonMatrix(3);
        // A1 переважає A2 та A3 в 3 рази: A1/A2 = 3, A1/A3 = 3, A2/A3 = 1
        altMatrix.SetComparison(0, 1, 3.0);
        altMatrix.SetComparison(0, 2, 3.0);
        altMatrix.SetComparison(1, 2, 1.0);

        var altMatrices = new Dictionary<string, ComparisonMatrix>
        {
            { "C1", altMatrix }
        };

        var result = AhpCalculator.SolveHierarchy(criteria, alternatives, critMatrix, altMatrices);

        var leader = result.RankedAlternatives.First(a => a.Id == "A1");
        Assert.Equal(1, leader.Rank);
        Assert.Equal("1", leader.RankText);

        var clone1 = result.RankedAlternatives.First(a => a.Id == "A2");
        var clone2 = result.RankedAlternatives.First(a => a.Id == "A3");

        Assert.Equal(2, clone1.Rank);
        Assert.Equal(2, clone2.Rank);
        Assert.Equal("2=", clone1.RankText);
        Assert.Equal("2=", clone2.RankText);
    }

    [Fact]
    public void Lab34Preset_DistinctPriorities_RanksDoNotContainEquals()
    {
        var preset = PresetService.GetLab34Preset();
        var result = AhpCalculator.SolveHierarchy(preset.Criteria, preset.Alternatives, preset.CriteriaMatrix, preset.AlternativeMatrices);

        var expectedRanks = new[] { 1, 2, 3, 4, 5 };
        var actualRanks = result.RankedAlternatives.Select(a => a.Rank).ToArray();
        Assert.Equal(expectedRanks, actualRanks);

        foreach (var alt in result.RankedAlternatives)
        {
            Assert.Equal(alt.Rank.ToString(), alt.RankText);
            Assert.DoesNotContain("=", alt.RankText);
        }
    }
}
