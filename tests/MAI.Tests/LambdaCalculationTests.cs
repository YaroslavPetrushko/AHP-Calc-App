using System;
using MAI.Models;
using MAI.Services;
using Xunit;

namespace MAI.Tests;

public class LambdaCalculationTests
{
    private const double Tolerance = 5e-4;

    [Fact]
    public void CriteriaMatrix_ColumnNormalization_LambdaDiffersByMethod()
    {
        var preset = PresetService.GetReferencePreset();

        // 1. ColumnSumProduct з нормалізацією стовпців: lambda ≈ 4.0493
        var optsColSum = new AhpCalculationOptions
        {
            WeightMethod = WeightCalculationMethod.ColumnNormalization,
            LambdaMethod = LambdaCalculationMethod.ColumnSumProduct,
            RiMode = RiCalculationMode.Empirical,
            ThresholdMode = ConsistencyThresholdMode.Uniform10Percent
        };
        var resColSum = AhpCalculator.CalculateMatrix(preset.CriteriaMatrix, "Критерії", optsColSum);
        Assert.InRange(resColSum.LambdaMax, 4.0493 - Tolerance, 4.0493 + Tolerance);

        // 2. ExactEigenvalue з нормалізацією стовпців: lambda ≈ 4.0458 (степеневий метод)
        var optsExact = new AhpCalculationOptions
        {
            WeightMethod = WeightCalculationMethod.ColumnNormalization,
            LambdaMethod = LambdaCalculationMethod.ExactEigenvalue,
            RiMode = RiCalculationMode.SaatyTable,
            ThresholdMode = ConsistencyThresholdMode.SaatyAdaptive
        };
        var resExact = AhpCalculator.CalculateMatrix(preset.CriteriaMatrix, "Критерії", optsExact);
        Assert.InRange(resExact.LambdaMax, 4.0458 - Tolerance, 4.0458 + Tolerance);
    }

    [Fact]
    public void LambdaMethodDescriptions_MatchStandardSpecification()
    {
        Assert.Equal("λmax = Σ(сума j-го стовпця · wⱼ) (за методичними рекомендаціями)",
            AhpCalculationOptions.GetLambdaMethodDescription(LambdaCalculationMethod.ColumnSumProduct));

        Assert.Equal("λmax = середнє (A·w)ᵢ / wᵢ",
            AhpCalculationOptions.GetLambdaMethodDescription(LambdaCalculationMethod.RowRatioMean));

        Assert.Equal("λmax — головне власне число (степеневий метод)",
            AhpCalculationOptions.GetLambdaMethodDescription(LambdaCalculationMethod.ExactEigenvalue));
    }
}
