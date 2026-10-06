using System;
using MAI.Models;
using MAI.Services;
using Xunit;

namespace MAI.Tests;

public class JudgedPairsTests
{
    [Fact]
    public void NewMatrix4x4_HasZeroJudgedCount_AndSixTotalPairs()
    {
        var matrix = new ComparisonMatrix(4);
        Assert.Equal(0, matrix.JudgedCount);
        Assert.Equal(6, matrix.TotalPairs);
        Assert.False(matrix.IsFullyJudged);
    }

    [Fact]
    public void SetComparison_MarksPairAsJudged_EvenWhenValueIsOne()
    {
        var matrix = new ComparisonMatrix(4);
        Assert.Equal(0, matrix.JudgedCount);

        matrix.SetComparison(0, 1, 1.0);
        Assert.Equal(1, matrix.JudgedCount);
        Assert.True(matrix.IsPairJudged(0, 1));
        Assert.True(matrix.IsPairJudged(1, 0));

        // Same pair edited again should not increment count
        matrix.SetComparison(1, 0, 2.0);
        Assert.Equal(1, matrix.JudgedCount);

        // Another pair
        matrix.SetComparison(2, 3, 3.0);
        Assert.Equal(2, matrix.JudgedCount);
    }

    [Fact]
    public void LoadFromJaggedArray_AllOnes_HasZeroJudgedCount()
    {
        var matrix = new ComparisonMatrix(4);
        var ones = new double[][]
        {
            new double[] { 1, 1, 1, 1 },
            new double[] { 1, 1, 1, 1 },
            new double[] { 1, 1, 1, 1 },
            new double[] { 1, 1, 1, 1 }
        };

        matrix.LoadFromJaggedArray(ones);
        Assert.Equal(0, matrix.JudgedCount);
        Assert.Equal(6, matrix.TotalPairs);
        Assert.False(matrix.IsFullyJudged);
    }

    [Fact]
    public void LoadFromJaggedArray_NonUnitValues_MarksAllPairsAsJudged()
    {
        var matrix = new ComparisonMatrix(3);
        var array = new double[][]
        {
            new double[] { 1, 2, 3 },
            new double[] { 0.5, 1, 2 },
            new double[] { 1.0 / 3, 0.5, 1 }
        };

        matrix.LoadFromJaggedArray(array);
        Assert.Equal(3, matrix.JudgedCount);
        Assert.Equal(3, matrix.TotalPairs);
        Assert.True(matrix.IsFullyJudged);
    }

    [Fact]
    public void RemoveAt_PreservesRemainingJudgedPairs_AndDoesNotExceedTotalPairs()
    {
        var matrix = new ComparisonMatrix(4);
        matrix.SetComparison(0, 1, 2.0);
        matrix.SetComparison(0, 2, 3.0);
        matrix.SetComparison(0, 3, 4.0);
        matrix.SetComparison(1, 2, 2.0);
        matrix.SetComparison(1, 3, 1.0);
        matrix.SetComparison(2, 3, 0.5);

        Assert.Equal(6, matrix.JudgedCount);
        Assert.Equal(6, matrix.TotalPairs);
        Assert.True(matrix.IsFullyJudged);

        // Remove element 1 (old 1)
        // New matrix is 3x3 with total pairs = 3*2/2 = 3
        matrix.RemoveAt(1);

        Assert.Equal(3, matrix.Size);
        Assert.Equal(3, matrix.TotalPairs);
        Assert.True(matrix.JudgedCount <= matrix.TotalPairs, "Judged count must not exceed total pairs after RemoveAt");
        Assert.Equal(3, matrix.JudgedCount);
        Assert.True(matrix.IsFullyJudged);
    }

    [Fact]
    public void PresetLab34_AllMatricesAreFullyJudged()
    {
        var preset = PresetService.GetLab34Preset();
        Assert.True(preset.CriteriaMatrix.IsFullyJudged);
        Assert.Equal(preset.CriteriaMatrix.TotalPairs, preset.CriteriaMatrix.JudgedCount);

        foreach (var kvp in preset.AlternativeMatrices)
        {
            Assert.True(kvp.Value.IsFullyJudged);
            Assert.Equal(kvp.Value.TotalPairs, kvp.Value.JudgedCount);
        }
    }
}
