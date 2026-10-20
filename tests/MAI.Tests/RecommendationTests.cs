using System.Collections.Generic;
using MAI;
using MAI.Models;
using Xunit;

namespace MAI.Tests;

public class RecommendationTests
{
    [Fact]
    public void BuildRecommendationText_SingleWinner_ProducesStandardRecommendation()
    {
        var result = new AhpResult
        {
            RankedAlternatives = new List<Alternative>
            {
                new() { Id = "A1", Name = "Alpha", GlobalPriority = 0.60, Rank = 1, RankText = "1" },
                new() { Id = "A2", Name = "Beta", GlobalPriority = 0.40, Rank = 2, RankText = "2" }
            },
            Criteria = new List<Criterion>
            {
                new() { Id = "C1", Name = "Performance", Weight = 1.0 }
            },
            Options = new AhpCalculationOptions
            {
                RiMode = RiCalculationMode.Empirical,
                ThresholdMode = ConsistencyThresholdMode.Uniform10Percent
            },
            HierarchicalCRTable = 0.02,
            HierarchicalCREmpirical = 0.02
        };

        string uk = ResultsWindow.BuildRecommendationText(result, isEn: false);
        string en = ResultsWindow.BuildRecommendationText(result, isEn: true);

        Assert.Contains("Оптимальною альтернативою", uk);
        Assert.Contains("«Alpha»", uk);
        Assert.Contains("Друге місце посіла альтернатива «Beta»", uk);

        Assert.Contains("The optimal alternative identified", en);
        Assert.Contains("\"Alpha\"", en);
        Assert.Contains("Runner-up is alternative \"Beta\"", en);
    }

    [Fact]
    public void BuildRecommendationText_TiedLeaders_ProducesJointFirstPlaceMessage()
    {
        var result = new AhpResult
        {
            RankedAlternatives = new List<Alternative>
            {
                new() { Id = "A1", Name = "Alpha", GlobalPriority = 0.45, Rank = 1, RankText = "1=" },
                new() { Id = "A2", Name = "Beta", GlobalPriority = 0.45, Rank = 1, RankText = "1=" },
                new() { Id = "A3", Name = "Gamma", GlobalPriority = 0.10, Rank = 3, RankText = "3" }
            },
            Criteria = new List<Criterion>
            {
                new() { Id = "C1", Name = "Cost", Weight = 1.0 }
            },
            Options = new AhpCalculationOptions
            {
                RiMode = RiCalculationMode.Empirical,
                ThresholdMode = ConsistencyThresholdMode.Uniform10Percent
            },
            HierarchicalCRTable = 0.03,
            HierarchicalCREmpirical = 0.03
        };

        string uk = ResultsWindow.BuildRecommendationText(result, isEn: false);
        string en = ResultsWindow.BuildRecommendationText(result, isEn: true);

        Assert.Contains("Виявлено рівність лідерів (спільне 1-ше місце)", uk);
        Assert.Contains("«Alpha»", uk);
        Assert.Contains("«Beta»", uk);
        Assert.Contains("не виявив переваги", uk);
        Assert.Contains("Наступне місце (3) посіли: «Gamma»", uk);

        Assert.Contains("Tied leaders detected (joint 1st place)", en);
        Assert.Contains("\"Alpha\"", en);
        Assert.Contains("\"Beta\"", en);
        Assert.Contains("Next ranking (3)", en);
    }

    [Fact]
    public void BuildRecommendationText_NarrowMargin_ShowsWarningNote()
    {
        var result = new AhpResult
        {
            RankedAlternatives = new List<Alternative>
            {
                new() { Id = "A1", Name = "Alpha", GlobalPriority = 0.505, Rank = 1, RankText = "1" },
                new() { Id = "A2", Name = "Beta", GlobalPriority = 0.495, Rank = 2, RankText = "2" }
            },
            Criteria = new List<Criterion>
            {
                new() { Id = "C1", Name = "Speed", Weight = 1.0 }
            },
            Options = new AhpCalculationOptions
            {
                RiMode = RiCalculationMode.Empirical,
                ThresholdMode = ConsistencyThresholdMode.Uniform10Percent
            }
        };

        string uk = ResultsWindow.BuildRecommendationText(result, isEn: false);
        string en = ResultsWindow.BuildRecommendationText(result, isEn: true);

        Assert.Contains("відрив між лідер", uk);
        Assert.Contains("The margin between leaders is narrow (< 2%)", en);
    }
}
