using System;
using System.Linq;
using MAI.Models;
using MAI.Services;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace MAI.Tests;

/// <summary>
/// Модульні тести двомовної локалізації (Українська / English), повноти словників,
/// звітів та пресетів.
/// </summary>
public class LocalizationTests
{
    [Fact]
    public void Dictionaries_HaveIdenticalKeys_AndNoEmptyValues()
    {
        var ukKeys = LocalizationStrings.Uk.Keys.ToHashSet();
        var enKeys = LocalizationStrings.En.Keys.ToHashSet();

        Assert.Equal(ukKeys, enKeys);

        foreach (var kvp in LocalizationStrings.Uk)
        {
            Assert.False(string.IsNullOrWhiteSpace(kvp.Value), $"Uk key '{kvp.Key}' has empty value");
        }

        foreach (var kvp in LocalizationStrings.En)
        {
            Assert.False(string.IsNullOrWhiteSpace(kvp.Value), $"En key '{kvp.Key}' has empty value");
        }
    }

    [Fact]
    public void DefaultLanguage_IsUkrainian_AndUsesUkCulture()
    {
        Assert.Equal(AppLanguage.Uk, LocalizationService.CurrentLanguage);
        Assert.Equal("uk-UA", LocalizationService.ActiveCulture.Name);
        Assert.Equal(",", LocalizationService.ActiveCulture.NumberFormat.NumberDecimalSeparator);
    }

    [Fact]
    public void SwitchToEnglish_UpdatesProfileNames_AndDescriptions()
    {
        try
        {
            LocalizationService.ApplyLanguage(AppLanguage.En);
            Assert.Equal(AppLanguage.En, LocalizationService.CurrentLanguage);
            Assert.Equal(".", LocalizationService.ActiveCulture.NumberFormat.NumberDecimalSeparator);

            var methodical = AhpCalculationOptions.MethodicalProfile;
            var classic = AhpCalculationOptions.ClassicSaatyProfile;

            Assert.Equal("Methodical Guidelines", methodical.ProfileName);
            Assert.Equal("Methodical", methodical.ProfileShortName);
            Assert.Equal("Classic T. Saaty Method", classic.ProfileName);
            Assert.Equal("Classic", classic.ProfileShortName);

            string lambdaDesc = AhpCalculationOptions.GetLambdaMethodDescription(LambdaCalculationMethod.ColumnSumProduct);
            Assert.Contains("Guidelines", lambdaDesc, StringComparison.OrdinalIgnoreCase);

            var res = new MatrixResult
            {
                CREmpirical = 0.05,
                Size = 4,
                Options = AhpCalculationOptions.MethodicalProfile
            };
            Assert.Contains("Consistent", res.ConsistencyMessage);
        }
        finally
        {
            LocalizationService.ApplyLanguage(AppLanguage.Uk);
        }
    }

    [Fact]
    public void SwitchToEnglish_SensitivitySummary_AndIntersections_AreLocalized()
    {
        try
        {
            LocalizationService.ApplyLanguage(AppLanguage.En);

            var preset = PresetService.GetReferencePreset();
            var result = AhpCalculator.SolveHierarchy(
                preset.Criteria,
                preset.Alternatives,
                preset.CriteriaMatrix,
                preset.AlternativeMatrices,
                AhpCalculationOptions.MethodicalProfile
            );

            var sens = AhpCalculator.CalculateSensitivity(result, preset.Criteria[0].Id);
            Assert.NotNull(sens.StabilitySummary);
            Assert.Contains("Leader", sens.StabilitySummary);
            Assert.Contains("Stability margin", sens.StabilitySummary);
            Assert.Contains("pp", sens.StabilitySummary);

            if (sens.Intersections.Count > 0)
            {
                Assert.False(string.IsNullOrWhiteSpace(sens.Intersections[0].EventDescription));
            }
        }
        finally
        {
            LocalizationService.ApplyLanguage(AppLanguage.Uk);
        }
    }

    [Fact]
    public void SwitchToEnglish_ReportTableBuilder_AndExporters_GenerateEnglishTables()
    {
        try
        {
            LocalizationService.ApplyLanguage(AppLanguage.En);

            var preset = PresetService.GetReferencePreset();
            var result = AhpCalculator.SolveHierarchy(
                preset.Criteria,
                preset.Alternatives,
                preset.CriteriaMatrix,
                preset.AlternativeMatrices,
                AhpCalculationOptions.MethodicalProfile
            );

            var tables = ReportTableBuilder.Build(result);
            Assert.NotEmpty(tables);
            Assert.Equal("Calculation Parameters", tables[0].Title);
            Assert.Contains(tables, t => t.Title.Contains("Pairwise Comparison Matrix of Criteria"));

            string formattedNumber = ReportTableBuilder.FormatNumber(1.2345);
            Assert.Contains(".", formattedNumber);

            string html = ReportExporter.ToHtml(tables);
            Assert.Contains("<html lang=\"en\">", html);
            Assert.Contains("Calculation Parameters", html);
        }
        finally
        {
            LocalizationService.ApplyLanguage(AppLanguage.Uk);
        }
    }

    [Fact]
    public void SwitchToEnglish_GenerateFullReportText_ProducesEnglishSections()
    {
        try
        {
            LocalizationService.ApplyLanguage(AppLanguage.En);

            var preset = PresetService.GetReferencePreset();
            var result = AhpCalculator.SolveHierarchy(
                preset.Criteria,
                preset.Alternatives,
                preset.CriteriaMatrix,
                preset.AlternativeMatrices,
                AhpCalculationOptions.MethodicalProfile
            );

            string report = ProjectStorageService.GenerateFullReportText(result, "Test recommendation");
            Assert.Contains("--- 1. FINAL ALTERNATIVES RANKING ---", report);
            Assert.Contains("--- 2. CRITERIA WEIGHT COEFFICIENTS ---", report);
            Assert.Contains("--- 3. JUDGMENTS CONSISTENCY CHECK", report);
            Assert.Contains("--- 4. SYNTHESIS MATRIX", report);
            Assert.Contains("--- 5. RANKING SENSITIVITY ANALYSIS TO CRITERIA WEIGHTS ---", report);
        }
        finally
        {
            LocalizationService.ApplyLanguage(AppLanguage.Uk);
        }
    }

    [Fact]
    public void SwitchToEnglish_Presets_LoadTranslatedCriteriaAndDescriptions()
    {
        try
        {
            LocalizationService.ApplyLanguage(AppLanguage.En);

            var refPreset = PresetService.GetReferencePreset();
            var gpuPreset = PresetService.GetGpu2026Preset();

            var expectedCritNames = new[] { "Performance", "Functionality", "Relevance", "Cost" };
            Assert.Equal(expectedCritNames, refPreset.Criteria.Select(c => c.Name).ToArray());
            Assert.Equal(expectedCritNames, gpuPreset.Criteria.Select(c => c.Name).ToArray());

            Assert.Equal(4, refPreset.CriteriaMatrix.Size);
            Assert.Equal(4, gpuPreset.CriteriaMatrix.Size);
            Assert.Equal(5, refPreset.Alternatives.Count);
            Assert.Equal(5, gpuPreset.Alternatives.Count);
        }
        finally
        {
            LocalizationService.ApplyLanguage(AppLanguage.Uk);
        }
    }
}
