using System;
using MAI.Models;
using MAI.Services;
using Xunit;

namespace MAI.Tests;

public class InconsistencyHintTests
{
    private const double Tolerance = 5e-4;

    [Fact]
    public void TestVector_FindMostInconsistentCell_MatchesBenchmark()
    {
        // Матриця 4×4 для перевірки найбільш неузгодженої пари
        // [ 1    2    4    2 ]
        // [ 1/2  1    2    4 ]
        // [ 1/4  1/2  1    2 ]
        // [ 1/2  1/4  1/2  1 ]
        var matrix = new ComparisonMatrix(4, new[] { "C1", "C2", "C3", "C4" });
        matrix.SetComparison(0, 1, 2.0);
        matrix.SetComparison(0, 2, 4.0);
        matrix.SetComparison(0, 3, 2.0); // Змінене значення (узгоджене було б 8)
        matrix.SetComparison(1, 2, 2.0);
        matrix.SetComparison(1, 3, 4.0);
        matrix.SetComparison(2, 3, 2.0);

        var opts = AhpCalculationOptions.ClassicSaatyProfile;
        var result = AhpCalculator.CalculateMatrix(matrix, "Тест А.6", opts);

        // Перевірка числових показників А.6
        // λmax ≈ 4,2492; CI ≈ 0,0831; CR ≈ 0,092
        Assert.InRange(result.LambdaMax, 4.2492 - Tolerance, 4.2492 + Tolerance);
        Assert.InRange(result.CI, 0.0831 - Tolerance, 0.0831 + Tolerance);
        Assert.InRange(result.ActiveCR, 0.092 - 0.003, 0.092 + 0.003);

        // Для n=4 поріг Сааті становить 8% (0.0801), тому матриця неузгоджена
        Assert.False(result.IsConsistent, "Матриця повинна бути неузгодженою (CR ≈ 0.092 > 0.08)");

        // Перевірка підказки про найбільш неузгоджену оцінку
        var hint = AhpCalculator.FindMostInconsistentCell(matrix, result.Weights);
        Assert.NotNull(hint);

        // Найбільше відхилення: пара (1, 4), тобто row 0, col 3
        Assert.Equal(0, hint.Value.Row);
        Assert.Equal(3, hint.Value.Col);

        // Задано: 2, узгоджене: ≈ 3.86, рекомендація: 4
        Assert.Equal(2.0, hint.Value.Current);
        Assert.InRange(hint.Value.Implied, 3.86 - 0.05, 3.86 + 0.05);
        Assert.Equal(4.0, hint.Value.Recommended);
    }

    [Fact]
    public void ConsistentMatrix_FindMostInconsistentCell_ReturnsNullOrZero()
    {
        // Повністю узгоджена матриця за вагами 8:4:2:1
        var matrix = new ComparisonMatrix(4);
        matrix.SetComparison(0, 1, 2.0);
        matrix.SetComparison(0, 2, 4.0);
        matrix.SetComparison(0, 3, 8.0);
        matrix.SetComparison(1, 2, 2.0);
        matrix.SetComparison(1, 3, 4.0);
        matrix.SetComparison(2, 3, 2.0);

        var result = AhpCalculator.CalculateMatrix(matrix, "Узгоджена", AhpCalculationOptions.ClassicSaatyProfile);
        Assert.True(result.IsConsistent);
        Assert.InRange(result.ActiveCR, 0.0, 1e-6);

        var hint = AhpCalculator.FindMostInconsistentCell(matrix, result.Weights);
        Assert.Null(hint);
    }

    [Fact]
    public void GetClosestSaatyScaleValueLog_FindsOptimalScaleValues()
    {
        Assert.Equal(4.0, AhpCalculator.GetClosestSaatyScaleValueLog(3.86));
        Assert.Equal(3.0, AhpCalculator.GetClosestSaatyScaleValueLog(3.1));
        Assert.Equal(1.0, AhpCalculator.GetClosestSaatyScaleValueLog(1.05));
        Assert.Equal(1.0 / 3.0, AhpCalculator.GetClosestSaatyScaleValueLog(0.32));
        Assert.Equal(9.0, AhpCalculator.GetClosestSaatyScaleValueLog(12.0));
        Assert.Equal(1.0 / 9.0, AhpCalculator.GetClosestSaatyScaleValueLog(0.05));
    }
}
