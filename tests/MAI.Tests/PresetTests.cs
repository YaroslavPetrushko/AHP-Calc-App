using System;
using MAI.Services;
using Xunit;

namespace MAI.Tests;

public class PresetTests
{
    private const double Tolerance = 1e-4;

    [Fact]
    public void Lab34Preset_MatricesMatchAppendixA3()
    {
        var preset = PresetService.GetLab34Preset();

        // 1. Матриця критеріїв (порядок: P=0, F=1, A=2, C=3)
        // P/F = 2, P/A = 3, P/C = 1, F/A = 2, F/C = 1, A/C = 1/2
        var cm = preset.CriteriaMatrix;
        Assert.Equal(2.0, cm.Values[0, 1]);
        Assert.Equal(3.0, cm.Values[0, 2]);
        Assert.Equal(1.0, cm.Values[0, 3]);
        Assert.Equal(2.0, cm.Values[1, 2]);
        Assert.Equal(1.0, cm.Values[1, 3]);
        Assert.InRange(cm.Values[2, 3], 0.5 - Tolerance, 0.5 + Tolerance);

        // 2. Матриця P:
        // A1/A2 = 1/5, A1/A3 = 1/5, A1/A4 = 1/5, A1/A5 = 1/6,
        // A2/A3 = 1/2, A2/A4 = 1/2, A2/A5 = 1/2, A3/A4 = 1, A3/A5 = 1/2, A4/A5 = 1/2
        var pMat = preset.AlternativeMatrices["P"];
        Assert.InRange(pMat.Values[0, 1], 1.0 / 5.0 - Tolerance, 1.0 / 5.0 + Tolerance);
        Assert.InRange(pMat.Values[0, 2], 1.0 / 5.0 - Tolerance, 1.0 / 5.0 + Tolerance);
        Assert.InRange(pMat.Values[0, 3], 1.0 / 5.0 - Tolerance, 1.0 / 5.0 + Tolerance);
        Assert.InRange(pMat.Values[0, 4], 1.0 / 6.0 - Tolerance, 1.0 / 6.0 + Tolerance);
        Assert.InRange(pMat.Values[1, 2], 0.5 - Tolerance, 0.5 + Tolerance);
        Assert.InRange(pMat.Values[1, 3], 0.5 - Tolerance, 0.5 + Tolerance);
        Assert.InRange(pMat.Values[1, 4], 0.5 - Tolerance, 0.5 + Tolerance);
        Assert.Equal(1.0, pMat.Values[2, 3]);
        Assert.InRange(pMat.Values[2, 4], 0.5 - Tolerance, 0.5 + Tolerance);
        Assert.InRange(pMat.Values[3, 4], 0.5 - Tolerance, 0.5 + Tolerance);

        // 3. Матриця F:
        // A1/A2 = 1/2, A1/A3 = 1/5, A1/A4 = 1/5, A1/A5 = 1/4,
        // A2/A3 = 1/5, A2/A4 = 1/5, A2/A5 = 1/2, A3/A4 = 1/2, A3/A5 = 3, A4/A5 = 3
        var fMat = preset.AlternativeMatrices["F"];
        Assert.InRange(fMat.Values[0, 1], 0.5 - Tolerance, 0.5 + Tolerance);
        Assert.InRange(fMat.Values[0, 2], 1.0 / 5.0 - Tolerance, 1.0 / 5.0 + Tolerance);
        Assert.InRange(fMat.Values[0, 3], 1.0 / 5.0 - Tolerance, 1.0 / 5.0 + Tolerance);
        Assert.InRange(fMat.Values[0, 4], 0.25 - Tolerance, 0.25 + Tolerance);
        Assert.InRange(fMat.Values[1, 2], 1.0 / 5.0 - Tolerance, 1.0 / 5.0 + Tolerance);
        Assert.InRange(fMat.Values[1, 3], 1.0 / 5.0 - Tolerance, 1.0 / 5.0 + Tolerance);
        Assert.InRange(fMat.Values[1, 4], 0.5 - Tolerance, 0.5 + Tolerance);
        Assert.InRange(fMat.Values[2, 3], 0.5 - Tolerance, 0.5 + Tolerance);
        Assert.Equal(3.0, fMat.Values[2, 4]);
        Assert.Equal(3.0, fMat.Values[3, 4]);

        // 4. Матриця A:
        // A1/A2 = 1, A1/A3 = 1/3, A1/A4 = 1/3, A1/A5 = 1/2,
        // A2/A3 = 1/3, A2/A4 = 1/3, A2/A5 = 1/2, A3/A4 = 1, A3/A5 = 2, A4/A5 = 2
        var aMat = preset.AlternativeMatrices["A"];
        Assert.Equal(1.0, aMat.Values[0, 1]);
        Assert.InRange(aMat.Values[0, 2], 1.0 / 3.0 - Tolerance, 1.0 / 3.0 + Tolerance);
        Assert.InRange(aMat.Values[0, 3], 1.0 / 3.0 - Tolerance, 1.0 / 3.0 + Tolerance);
        Assert.InRange(aMat.Values[0, 4], 0.5 - Tolerance, 0.5 + Tolerance);
        Assert.InRange(aMat.Values[1, 2], 1.0 / 3.0 - Tolerance, 1.0 / 3.0 + Tolerance);
        Assert.InRange(aMat.Values[1, 3], 1.0 / 3.0 - Tolerance, 1.0 / 3.0 + Tolerance);
        Assert.InRange(aMat.Values[1, 4], 0.5 - Tolerance, 0.5 + Tolerance);
        Assert.Equal(1.0, aMat.Values[2, 3]);
        Assert.Equal(2.0, aMat.Values[2, 4]);
        Assert.Equal(2.0, aMat.Values[3, 4]);

        // 5. Матриця C:
        // A1/A2 = 2, A1/A3 = 5, A1/A4 = 7, A1/A5 = 5,
        // A2/A3 = 3, A2/A4 = 5, A2/A5 = 3, A3/A4 = 3, A3/A5 = 1, A4/A5 = 1/3
        var cMat = preset.AlternativeMatrices["C"];
        Assert.Equal(2.0, cMat.Values[0, 1]);
        Assert.Equal(5.0, cMat.Values[0, 2]);
        Assert.Equal(7.0, cMat.Values[0, 3]);
        Assert.Equal(5.0, cMat.Values[0, 4]);
        Assert.Equal(3.0, cMat.Values[1, 2]);
        Assert.Equal(5.0, cMat.Values[1, 3]);
        Assert.Equal(3.0, cMat.Values[1, 4]);
        Assert.Equal(3.0, cMat.Values[2, 3]);
        Assert.Equal(1.0, cMat.Values[2, 4]);
        Assert.InRange(cMat.Values[3, 4], 1.0 / 3.0 - Tolerance, 1.0 / 3.0 + Tolerance);
    }

    [Fact]
    public void CourseworkDraftPreset_EmbeddedJson_LoadsAndValidates()
    {
        var preset = PresetService.GetCourseworkPreset();

        Assert.NotNull(preset);
        Assert.Contains("ЧЕРНЕТКА", preset.Name);
        Assert.Equal(4, preset.Criteria.Count);
        Assert.Equal(5, preset.Alternatives.Count);
        Assert.Equal(4, preset.CriteriaMatrix.Size);
        Assert.Equal(6, preset.CriteriaMatrix.TotalPairs);
        Assert.Equal(6, preset.CriteriaMatrix.JudgedCount);
        Assert.True(preset.CriteriaMatrix.IsFullyJudged);

        Assert.Equal(4, preset.AlternativeMatrices.Count);
        foreach (var mat in preset.AlternativeMatrices.Values)
        {
            Assert.Equal(5, mat.Size);
            Assert.Equal(10, mat.TotalPairs);
            Assert.Equal(10, mat.JudgedCount);
            Assert.True(mat.IsFullyJudged);
        }
    }

    [Fact]
    public void CourseworkFinalPreset_EmbeddedJson_LoadsAndValidates()
    {
        var preset = PresetService.GetCourseworkFinalPreset();

        Assert.NotNull(preset);
        Assert.Equal(4, preset.Criteria.Count);
        Assert.Equal(5, preset.Alternatives.Count);
        Assert.Equal(4, preset.CriteriaMatrix.Size);
        Assert.Equal(6, preset.CriteriaMatrix.TotalPairs);
        Assert.Equal(6, preset.CriteriaMatrix.JudgedCount);
        Assert.True(preset.CriteriaMatrix.IsFullyJudged);

        Assert.Equal(4, preset.AlternativeMatrices.Count);
        foreach (var mat in preset.AlternativeMatrices.Values)
        {
            Assert.Equal(5, mat.Size);
            Assert.Equal(10, mat.TotalPairs);
            Assert.Equal(10, mat.JudgedCount);
            Assert.True(mat.IsFullyJudged);
        }
    }

    [Fact]
    public void LoadPresetFromResource_MissingResource_ThrowsInvalidOperationException()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            PresetService.LoadPresetFromResource("non_existent_preset.ahp.json"));

        Assert.Contains("не знайдено в збірці", ex.Message);
    }
}
