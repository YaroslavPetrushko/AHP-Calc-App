using System;
using MAI.Models;
using Xunit;

namespace MAI.Tests;

public class ComparisonMatrixTests
{
    [Fact]
    public void RemoveAt_FromMiddle_PreservesCorrectElementMapping_ReferenceCheck()
    {
        // Матриця 5x5 з унікальними значеннями у верхньому трикутнику
        var mat = new ComparisonMatrix(5, new[] { "E0", "E1", "E2", "E3", "E4" });

        // Встановлюємо довільні унікальні значення
        double val01 = 2.0;
        double val02 = 3.0;
        double val03 = 4.0;
        double val04 = 5.0;
        double val12 = 6.0;
        double val13 = 7.0;
        double val14 = 8.0;
        double val23 = 9.0;
        double val24 = 2.5;
        double val34 = 3.5;

        mat.SetComparison(0, 1, val01);
        mat.SetComparison(0, 2, val02);
        mat.SetComparison(0, 3, val03);
        mat.SetComparison(0, 4, val04);
        mat.SetComparison(1, 2, val12);
        mat.SetComparison(1, 3, val13);
        mat.SetComparison(1, 4, val14);
        mat.SetComparison(2, 3, val23);
        mat.SetComparison(2, 4, val24);
        mat.SetComparison(3, 4, val34);

        // Видаляємо елемент з індексом 1 (E1)
        mat.RemoveAt(1);

        Assert.Equal(4, mat.Size);
        Assert.Equal(new[] { "E0", "E2", "E3", "E4" }, mat.Labels);

        // За Додатком А.7:
        // (0,2) -> (0,1)
        Assert.Equal(val02, mat.Values[0, 1]);
        Assert.Equal(1.0 / val02, mat.Values[1, 0]);

        // (0,3) -> (0,2)
        Assert.Equal(val03, mat.Values[0, 2]);
        Assert.Equal(1.0 / val03, mat.Values[2, 0]);

        // (0,4) -> (0,3)
        Assert.Equal(val04, mat.Values[0, 3]);
        Assert.Equal(1.0 / val04, mat.Values[3, 0]);

        // (2,3) -> (1,2)
        Assert.Equal(val23, mat.Values[1, 2]);
        Assert.Equal(1.0 / val23, mat.Values[2, 1]);

        // (2,4) -> (1,3)
        Assert.Equal(val24, mat.Values[1, 3]);
        Assert.Equal(1.0 / val24, mat.Values[3, 1]);

        // (3,4) -> (2,3)
        Assert.Equal(val34, mat.Values[2, 3]);
        Assert.Equal(1.0 / val34, mat.Values[3, 2]);

        // Діагональ залишається 1.0
        for (int i = 0; i < 4; i++)
        {
            Assert.Equal(1.0, mat.Values[i, i]);
        }
    }

    [Fact]
    public void ReciprocalSymmetry_IsAlwaysMaintained()
    {
        var mat = new ComparisonMatrix(3, new[] { "A", "B", "C" });
        mat.SetComparison(0, 1, 5.0);
        mat.SetComparison(1, 2, 3.0);
        mat.SetComparison(0, 2, 7.0);

        Assert.Equal(5.0, mat.Values[0, 1]);
        Assert.Equal(0.2, mat.Values[1, 0]);
        Assert.Equal(3.0, mat.Values[1, 2]);
        Assert.Equal(1.0 / 3.0, mat.Values[2, 1]);
        Assert.Equal(7.0, mat.Values[0, 2]);
        Assert.Equal(1.0 / 7.0, mat.Values[2, 0]);
    }
}
