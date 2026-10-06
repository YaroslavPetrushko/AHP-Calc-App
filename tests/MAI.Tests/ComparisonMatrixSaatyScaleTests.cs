using System;
using MAI.Models;
using Xunit;

namespace MAI.Tests;

public class ComparisonMatrixSaatyScaleTests
{
    [Theory]
    [InlineData("1/3", 1.0 / 3.0)]
    [InlineData("0,5", 0.5)]
    [InlineData("0.333", 1.0 / 3.0)] // Прив'язано до 1/3 (< 0.5% rel diff)
    [InlineData("0.33", 0.33)]       // Не прив'язано (> 0.5% rel diff)
    public void TryParseSaatyValue_AcceptedValues_ProduceExpectedNumbers(string input, double expected)
    {
        bool success = ComparisonMatrix.TryParseSaatyValue(input, out double actual);
        Assert.True(success);
        Assert.Equal(expected, actual, precision: 12);
    }

    [Theory]
    [InlineData("10")]
    [InlineData("0.05")]
    [InlineData("1e3")]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1/0")]
    [InlineData("-3")]
    public void TryParseSaatyValue_RejectedValues_ReturnFalse(string input)
    {
        bool success = ComparisonMatrix.TryParseSaatyValue(input, out _);
        Assert.False(success);
    }

    [Fact]
    public void SetComparison_WithPoint333_SnapsToThirdAndSetsExactReciprocal()
    {
        var mat = new ComparisonMatrix(2, new[] { "A1", "A2" });
        mat.SetComparison(0, 1, 0.333);

        Assert.Equal(1.0 / 3.0, mat.Values[0, 1]);
        Assert.Equal(3.0, mat.Values[1, 0]);
    }

    [Fact]
    public void SetComparison_OutOfRangeValue_IsIgnored()
    {
        var mat = new ComparisonMatrix(2, new[] { "A1", "A2" });
        mat.SetComparison(0, 1, 2.0);

        // Спроба записати некоректні значення (повинні бути проігноровані)
        mat.SetComparison(0, 1, 10.0);
        Assert.Equal(2.0, mat.Values[0, 1]);

        mat.SetComparison(0, 1, 0.05);
        Assert.Equal(2.0, mat.Values[0, 1]);
    }
}
