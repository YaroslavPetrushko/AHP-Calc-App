using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MAI.Models;
using MAI.Services;
using Xunit;

namespace MAI.Tests;

public class ProjectStorageTests
{
    [Fact]
    public void RoundtripSerialization_PreservesAllDataWithoutLoss()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"ahp_test_{Guid.NewGuid():N}.ahp.json");
        try
        {
            var original = new AhpProjectData
            {
                Title = "Тестовий проєкт",
                Description = "Опис тестового проєкту",
                Version = "1.2",
                Options = AhpCalculationOptions.MethodicalProfile,
                Criteria = new List<Criterion>
                {
                    new("C1", "Ціна", "Вартість у гривнях"),
                    new("C2", "Якість", "Рівень надійності")
                },
                Alternatives = new List<Alternative>
                {
                    new("A1", "Варіант А", "Перший вибір"),
                    new("A2", "Варіант Б", "Другий вибір")
                },
                CriteriaMatrix = new double[][]
                {
                    new double[] { 1.0, 3.0 },
                    new double[] { 1.0 / 3.0, 1.0 }
                },
                AlternativeMatrices = new Dictionary<string, double[][]>
                {
                    ["C1"] = new double[][]
                    {
                        new double[] { 1.0, 2.0 },
                        new double[] { 0.5, 1.0 }
                    },
                    ["C2"] = new double[][]
                    {
                        new double[] { 1.0, 4.0 },
                        new double[] { 0.25, 1.0 }
                    }
                }
            };

            // Збереження
            ProjectStorageService.SaveProject(tempFile, original);

            // Завантаження
            var loaded = ProjectStorageService.LoadProject(tempFile);

            Assert.Equal(original.Title, loaded.Title);
            Assert.Equal(original.Description, loaded.Description);
            Assert.Equal(original.Version, loaded.Version);
            Assert.Equal(original.Options.WeightMethod, loaded.Options.WeightMethod);
            Assert.Equal(original.Options.LambdaMethod, loaded.Options.LambdaMethod);
            Assert.Equal(original.Options.RiMode, loaded.Options.RiMode);
            Assert.Equal(original.Options.ThresholdMode, loaded.Options.ThresholdMode);

            Assert.Equal(original.Criteria.Count, loaded.Criteria.Count);
            for (int i = 0; i < original.Criteria.Count; i++)
            {
                Assert.Equal(original.Criteria[i].Id, loaded.Criteria[i].Id);
                Assert.Equal(original.Criteria[i].Name, loaded.Criteria[i].Name);
                Assert.Equal(original.Criteria[i].Description, loaded.Criteria[i].Description);
            }

            Assert.Equal(original.Alternatives.Count, loaded.Alternatives.Count);
            for (int i = 0; i < original.Alternatives.Count; i++)
            {
                Assert.Equal(original.Alternatives[i].Id, loaded.Alternatives[i].Id);
                Assert.Equal(original.Alternatives[i].Name, loaded.Alternatives[i].Name);
                Assert.Equal(original.Alternatives[i].Description, loaded.Alternatives[i].Description);
            }

            // Матриця критеріїв
            Assert.Equal(2, loaded.CriteriaMatrix.Length);
            Assert.Equal(1.0, loaded.CriteriaMatrix[0][0]);
            Assert.Equal(3.0, loaded.CriteriaMatrix[0][1]);
            Assert.Equal(1.0 / 3.0, loaded.CriteriaMatrix[1][0]);
            Assert.Equal(1.0, loaded.CriteriaMatrix[1][1]);

            // Матриці альтернатив
            Assert.True(loaded.AlternativeMatrices.ContainsKey("C1"));
            Assert.True(loaded.AlternativeMatrices.ContainsKey("C2"));
            Assert.Equal(2.0, loaded.AlternativeMatrices["C1"][0][1]);
            Assert.Equal(4.0, loaded.AlternativeMatrices["C2"][0][1]);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void GenerateFullReportText_GeneratesExpectedSections()
    {
        var preset = PresetService.GetReferencePreset();
        var result = AhpCalculator.SolveHierarchy(
            preset.Criteria,
            preset.Alternatives,
            preset.CriteriaMatrix,
            preset.AlternativeMatrices,
            AhpCalculationOptions.MethodicalProfile);

        string report = ProjectStorageService.GenerateFullReportText(result);

        Assert.Contains("ЗВІТ ЗА МЕТОДОМ АНАЛІЗУ ІЄРАРХІЙ", report);
        Assert.Contains("--- 1. ПІДСУМКОВИЙ РЕЙТИНГ АЛЬТЕРНАТИВ ---", report);
        Assert.Contains("--- 2. ВАГОВІ КОЕФІЦІЄНТИ КРИТЕРІЇВ ---", report);
        Assert.Contains("--- 3. ПЕРЕВІРКА УЗГОДЖЕНОСТІ СУДЖЕНЬ (CI, RI, CR) ---", report);
        Assert.Contains("--- 4. МАТРИЦЯ СИНТЕЗУ (Lij · wj) ---", report);
        Assert.Contains("--- 5. АНАЛІЗ ЧУТЛИВОСТІ РЕЙТИНГУ", report);
    }

    [Fact]
    public void LoadProject_PreviousVersionJson_LoadsSuccessfully()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"ahp_v11_{Guid.NewGuid():N}.ahp.json");
        try
        {
            string v11Json = """
            {
              "title": "Проєкт версії 1.1",
              "description": "Сумісність зі старою версією",
              "version": "1.1",
              "riMode": "SaatyTable",
              "criteria": [
                { "id": "C1", "name": "Критерій 1", "description": "" },
                { "id": "C2", "name": "Критерій 2", "description": "" }
              ],
              "alternatives": [
                { "id": "A1", "name": "Альтернатива 1", "description": "" },
                { "id": "A2", "name": "Альтернатива 2", "description": "" }
              ],
              "criteriaMatrix": [
                [ 1.0, 2.0 ],
                [ 0.5, 1.0 ]
              ],
              "alternativeMatrices": {
                "C1": [
                  [ 1.0, 3.0 ],
                  [ 0.3333333333333333, 1.0 ]
                ],
                "C2": [
                  [ 1.0, 0.5 ],
                  [ 2.0, 1.0 ]
                ]
              }
            }
            """;
            File.WriteAllText(tempFile, v11Json);

            var loaded = ProjectStorageService.LoadProject(tempFile);

            Assert.NotNull(loaded);
            Assert.Equal("1.1", loaded.Version);
            Assert.Equal("Проєкт версії 1.1", loaded.Title);
            Assert.Equal(RiCalculationMode.SaatyTable, loaded.RiMode);
            Assert.Equal(2, loaded.Criteria.Count);
            Assert.Equal(2, loaded.Alternatives.Count);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void LoadProject_V120Json_LoadsSuccessfully()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"ahp_v120_{Guid.NewGuid():N}.ahp.json");
        try
        {
            string v120Json = """
            {
              "title": "Проєкт версії 1.2.0",
              "description": "Сумісність із форматом v1.2.0",
              "version": "1.2.0",
              "savedAt": "2026-10-06T15:00:00",
              "options": {
                "weightMethod": "ColumnNormalization",
                "lambdaMethod": "ColumnSumProduct",
                "riMode": "Empirical",
                "thresholdMode": "Uniform10Percent"
              },
              "criteria": [
                { "id": "C1", "name": "К1", "description": "" },
                { "id": "C2", "name": "К2", "description": "" }
              ],
              "alternatives": [
                { "id": "A1", "name": "А1", "description": "" },
                { "id": "A2", "name": "А2", "description": "" }
              ],
              "criteriaMatrix": [
                [ 1.0, 2.0 ],
                [ 0.5, 1.0 ]
              ],
              "alternativeMatrices": {
                "C1": [
                  [ 1.0, 3.0 ],
                  [ 0.3333333333333333, 1.0 ]
                ],
                "C2": [
                  [ 1.0, 0.5 ],
                  [ 2.0, 1.0 ]
                ]
              }
            }
            """;
            File.WriteAllText(tempFile, v120Json);

            var loaded = ProjectStorageService.LoadProject(tempFile);

            Assert.NotNull(loaded);
            Assert.Equal("1.2.0", loaded.Version);
            Assert.Equal("Проєкт версії 1.2.0", loaded.Title);
            Assert.Equal(WeightCalculationMethod.ColumnNormalization, loaded.Options.WeightMethod);
            Assert.Equal(LambdaCalculationMethod.ColumnSumProduct, loaded.Options.LambdaMethod);
            Assert.Equal(RiCalculationMode.Empirical, loaded.Options.RiMode);
            Assert.Equal(ConsistencyThresholdMode.Uniform10Percent, loaded.Options.ThresholdMode);
            Assert.True(loaded.Options.IsMethodicalProfile);
            Assert.Equal(2, loaded.Criteria.Count);
            Assert.Equal(2, loaded.Alternatives.Count);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void GenerateFullReportText_UsesUkCultureComma_RegardlessOfCurrentThreadCulture()
    {
        var currentCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
        try
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");

            var preset = PresetService.GetReferencePreset();
            var options = AhpCalculationOptions.ClassicSaatyProfile;
            var result = AhpCalculator.SolveHierarchy(preset.Criteria, preset.Alternatives, preset.CriteriaMatrix, preset.AlternativeMatrices, options);

            string report = ProjectStorageService.GenerateFullReportText(result);

            // Report header does NOT contain (.NET 10)
            Assert.DoesNotContain("(.NET 10)", report);
            Assert.Contains("ЗВІТ ЗА МЕТОДОМ АНАЛІЗУ ІЄРАРХІЙ (МАІ) ТОМАСА СААТІ", report);

            // Check that decimal values use comma
            Assert.Contains("0,", report);
            // Check that percentages use comma
            Assert.Contains(",0%)", report);
        }
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = currentCulture;
        }
    }
}
