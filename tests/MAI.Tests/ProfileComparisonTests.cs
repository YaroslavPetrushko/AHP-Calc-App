using System;
using System.Linq;
using MAI.Models;
using MAI.Services;
using Xunit;

namespace MAI.Tests;

public class ProfileComparisonTests
{
    [Fact]
    public void ProfileComparison_Reference_WinnerIsA4InAllProfiles()
    {
        var preset = PresetService.GetReferencePreset();
        var result = ProfileComparisonService.Compare(
            preset.Criteria,
            preset.Alternatives,
            preset.CriteriaMatrix,
            preset.AlternativeMatrices
        );

        Assert.NotEmpty(result.Profiles);
        Assert.True(result.Profiles.Count >= 3, "Expected at least 3 profiles (Classic, Methodical, GeometricMean)");

        foreach (var p in result.Profiles)
        {
            Assert.Equal("A4", p.WinnerId);
            Assert.Contains("5060", p.WinnerName);
        }
    }

    [Fact]
    public void ProfileComparison_Reference_RankingIsIdenticalAcrossAllProfiles()
    {
        var preset = PresetService.GetReferencePreset();
        var result = ProfileComparisonService.Compare(
            preset.Criteria,
            preset.Alternatives,
            preset.CriteriaMatrix,
            preset.AlternativeMatrices
        );

        Assert.True(result.AllRankingsMatchBenchmark, "Ranking should match benchmark across all profiles");

        var expectedOrder = new[] { "A4", "A3", "A5", "A1", "A2" };
        foreach (var p in result.Profiles)
        {
            var actualOrder = p.Result.RankedAlternatives.Select(a => a.Id).ToArray();
            Assert.Equal(expectedOrder, actualOrder);
        }
    }

    [Fact]
    public void ProfileComparison_Reference_MaxDeltaPriorityBetweenClassicAndMethodical_IsLessThan0002()
    {
        var preset = PresetService.GetReferencePreset();
        var result = ProfileComparisonService.Compare(
            preset.Criteria,
            preset.Alternatives,
            preset.CriteriaMatrix,
            preset.AlternativeMatrices
        );

        var methodicalProfile = result.Profiles.FirstOrDefault(p => p.Options.IsMethodicalProfile);
        Assert.NotNull(methodicalProfile);

        // Ground truth max |delta P| between Classic and Methodical is approx 0.0011 (for A4: 0.2298 vs 0.2287)
        Assert.True(methodicalProfile.MaxDeltaPriorityFromBenchmark < 0.002,
            $"Expected max |ΔP| < 0.002, got {methodicalProfile.MaxDeltaPriorityFromBenchmark}");
        Assert.True(methodicalProfile.MaxDeltaPriorityFromBenchmark > 0.0005,
            $"Expected max |ΔP| > 0.0005, got {methodicalProfile.MaxDeltaPriorityFromBenchmark}");
    }

    [Fact]
    public void ProfileComparison_Generates3ReportTables_WithValidExport()
    {
        var preset = PresetService.GetReferencePreset();
        var result = ProfileComparisonService.Compare(
            preset.Criteria,
            preset.Alternatives,
            preset.CriteriaMatrix,
            preset.AlternativeMatrices
        );

        var tables = result.ToReportTables();
        Assert.Equal(3, tables.Count);

        Assert.Contains(tables, t => t.Title.Contains("Порівняння ваг критеріїв за профілями"));
        Assert.Contains(tables, t => t.Title.Contains("Порівняння глобальних пріоритетів та рангів альтернатив за профілями"));
        Assert.Contains(tables, t => t.Title.Contains("Порівняння показників узгодженості за профілями"));

        string csv = ReportExporter.ToCsv(tables);
        Assert.StartsWith("\uFEFF", csv);
        Assert.Contains(";", csv);

        string md = ReportExporter.ToMarkdown(tables);
        Assert.Contains("|---|", md);

        string html = ReportExporter.ToHtml(tables);
        Assert.Contains("<!DOCTYPE html>", html);
        Assert.Equal(3, System.Text.RegularExpressions.Regex.Matches(html, "<table\\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Count);
    }

    [Fact]
    public void ProfileComparison_ValueEquals_IdentifiesMatchingOptions()
    {
        var classic1 = AhpCalculationOptions.ClassicSaatyProfile;
        var classic2 = AhpCalculationOptions.ClassicSaatyProfile.Clone();
        var methodical = AhpCalculationOptions.MethodicalProfile;

        Assert.True(classic1.ValueEquals(classic2));
        Assert.Equal(classic1, classic2);
        Assert.False(classic1.ValueEquals(methodical));
        Assert.NotEqual(classic1, methodical);
    }

    [Fact]
    public void ProfileComparison_CompareFromProjectData_ProducesSameResults()
    {
        var preset = PresetService.GetReferencePreset();
        var projectData = new AhpProjectData
        {
            Title = "Тестовий проект",
            Criteria = preset.Criteria,
            Alternatives = preset.Alternatives,
            CriteriaMatrix = preset.CriteriaMatrix.ToJaggedArray(),
            AlternativeMatrices = preset.AlternativeMatrices.ToDictionary(k => k.Key, v => v.Value.ToJaggedArray()),
            Options = AhpCalculationOptions.ClassicSaatyProfile
        };

        var result = ProfileComparisonService.Compare(projectData);
        Assert.NotNull(result);
        Assert.True(result.Profiles.Count >= 3);
        Assert.Equal("A4", result.Profiles[0].WinnerId);
    }

    [Fact]
    public void ProfileNames_AndShortNames_MatchExpectedSpecifications()
    {
        var methodical = AhpCalculationOptions.MethodicalProfile;
        Assert.Equal("Метод. рекомендації", methodical.ProfileName);
        Assert.Equal("Методичний", methodical.ProfileShortName);

        var classic = AhpCalculationOptions.ClassicSaatyProfile;
        Assert.Equal("Класичний метод Т. Сааті", classic.ProfileName);
        Assert.Equal("Класичний", classic.ProfileShortName);

        var geometric = AhpCalculationOptions.GeometricMeanProfile;
        Assert.Equal("Класичний, геометричне середнє", geometric.ProfileName);
        Assert.Equal("Геом. середнє", geometric.ProfileShortName);

        var custom = new AhpCalculationOptions(
            WeightCalculationMethod.GeometricMean,
            LambdaCalculationMethod.RowRatioMean,
            RiCalculationMode.Empirical,
            ConsistencyThresholdMode.Uniform10Percent);
        Assert.Equal("Користувацький профіль", custom.ProfileName);
        Assert.Equal("Користувацький", custom.ProfileShortName);
    }
}
