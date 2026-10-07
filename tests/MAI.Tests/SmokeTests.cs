using MAI.Services;
using Xunit;

namespace MAI.Tests;

public class SmokeTests
{
    [Fact]
    public void SmokeTest_PresetAndCalculatorExist()
    {
        // Перевірка існування пресету ЛР 3-4 та коректної кількості елементів
        var preset = PresetService.GetLab34Preset();
        Assert.NotNull(preset);
        Assert.Equal(4, preset.Criteria.Count);
        Assert.Equal(5, preset.Alternatives.Count);

        // Перевірка існування та працездатності калькулятора
        var result = AhpCalculator.SolveHierarchy(
            preset.Criteria,
            preset.Alternatives,
            preset.CriteriaMatrix,
            preset.AlternativeMatrices);

        Assert.NotNull(result);
        Assert.Equal(5, result.RankedAlternatives.Count);
    }
}
