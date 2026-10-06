using System;
using System.Collections.Generic;
using System.Linq;
using MAI.Models;
using MAI.Services;
using Xunit;

namespace MAI.Tests;

public class ProjectValidationTests
{
    private static AhpProjectData CreateValidProjectData()
    {
        var criteria = new List<Criterion>
        {
            new("C1", "Ціна"),
            new("C2", "Якість")
        };
        var alternatives = new List<Alternative>
        {
            new("A1", "Варіант 1"),
            new("A2", "Варіант 2")
        };
        var critMat = new double[][]
        {
            new double[] { 1.0, 2.0 },
            new double[] { 0.5, 1.0 }
        };
        var altMat = new double[][]
        {
            new double[] { 1.0, 3.0 },
            new double[] { 1.0 / 3.0, 1.0 }
        };

        return new AhpProjectData
        {
            Criteria = criteria,
            Alternatives = alternatives,
            CriteriaMatrix = critMat,
            AlternativeMatrices = new Dictionary<string, double[][]>
            {
                ["C1"] = altMat,
                ["C2"] = altMat
            }
        };
    }

    [Fact]
    public void ValidProject_PassesValidation()
    {
        var project = CreateValidProjectData();
        var errors = ProjectStorageService.ValidateProject(project);
        Assert.Empty(errors);
    }

    [Fact]
    public void DuplicateCriterionId_FailsValidation()
    {
        var project = CreateValidProjectData();
        project.Criteria[1].Id = project.Criteria[0].Id; // Duplicate ID

        var errors = ProjectStorageService.ValidateProject(project);
        Assert.Contains(errors, e => e.Contains("дублікати ID"));
    }

    [Fact]
    public void DuplicateCriterionName_FailsValidation()
    {
        var project = CreateValidProjectData();
        project.Criteria[1].Name = project.Criteria[0].Name.ToLowerInvariant(); // Case-insensitive duplicate name

        var errors = ProjectStorageService.ValidateProject(project);
        Assert.Contains(errors, e => e.Contains("повторювані назви"));
    }

    [Fact]
    public void MismatchedMatrixSize_FailsValidation()
    {
        var project = CreateValidProjectData();
        project.CriteriaMatrix = new double[][]
        {
            new double[] { 1.0 }
        }; // Size 1 instead of 2

        var errors = ProjectStorageService.ValidateProject(project);
        Assert.Contains(errors, e => e.Contains("Розмір матриці критеріїв"));
    }

    [Fact]
    public void DimensionGreaterThan10_FailsValidation()
    {
        var project = CreateValidProjectData();
        project.Criteria = Enumerable.Range(1, 11).Select(i => new Criterion($"C{i}", $"Критерій {i}")).ToList();

        var errors = ProjectStorageService.ValidateProject(project);
        Assert.Contains(errors, e => e.Contains("межах від 2 до 10"));
    }

    [Fact]
    public void ValueOutOfRange_FailsValidation()
    {
        var project = CreateValidProjectData();
        project.CriteriaMatrix[0][1] = 10.0; // Outside [1/9; 9]

        var errors = ProjectStorageService.ValidateProject(project);
        Assert.Contains(errors, e => e.Contains("виходить за межі"));
    }

    [Fact]
    public void LoadFromJaggedArray_RestoresSymmetryAndUnitDiagonal()
    {
        var mat = new ComparisonMatrix(3, new[] { "A", "B", "C" });
        // Несиметричний вхідний масив із порушеною діагоналлю
        var jagged = new double[][]
        {
            new double[] { 99.0, 3.0, 5.0 },
            new double[] { 77.0, 88.0, 2.0 },
            new double[] { 66.0, 55.0, 44.0 }
        };

        mat.LoadFromJaggedArray(jagged);

        // Перевірка одиничної діагоналі
        for (int i = 0; i < 3; i++)
        {
            Assert.Equal(1.0, mat.Values[i, i]);
        }

        // Перевірка верхнього трикутника та зворотної симетрії
        Assert.Equal(3.0, mat.Values[0, 1]);
        Assert.Equal(1.0 / 3.0, mat.Values[1, 0]);
        Assert.Equal(5.0, mat.Values[0, 2]);
        Assert.Equal(1.0 / 5.0, mat.Values[2, 0]);
        Assert.Equal(2.0, mat.Values[1, 2]);
        Assert.Equal(1.0 / 2.0, mat.Values[2, 1]);

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                Assert.InRange(Math.Abs(mat.Values[i, j] * mat.Values[j, i] - 1.0), 0.0, 1e-12);
            }
        }
    }

    [Fact]
    public void SolveHierarchy_DimensionGreaterThan10_WithTableRi_ThrowsException()
    {
        var criteria = Enumerable.Range(1, 11).Select(i => new Criterion($"C{i}", $"Критерій {i}")).ToList();
        var alternatives = new List<Alternative> { new("A1", "A1"), new("A2", "A2") };
        var critMat = new ComparisonMatrix(11, criteria.Select(c => c.Name).ToList());
        var altMatrices = criteria.ToDictionary(
            c => c.Id,
            c => new ComparisonMatrix(2, alternatives.Select(a => a.Name).ToList())
        );

        var opts = new AhpCalculationOptions { RiMode = RiCalculationMode.SaatyTable };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            AhpCalculator.SolveHierarchy(criteria, alternatives, critMat, altMatrices, opts));

        Assert.Contains("Табличний RI визначений для n ≤ 10; використайте емпіричну формулу", ex.Message);
    }

    [Fact]
    public void Transactionality_WhenValidationFails_ExistingStateRemainsUntouched()
    {
        var existingCriteria = new List<Criterion>
        {
            new("C1", "Оригінальний 1"),
            new("C2", "Оригінальний 2")
        };

        var invalidProject = CreateValidProjectData();
        invalidProject.Criteria.Add(new Criterion("C1", "Дублікат")); // duplicate ID

        var errors = ProjectStorageService.ValidateProject(invalidProject);
        if (errors.Count == 0)
        {
            // Не повинно виконуватися
            existingCriteria.Clear();
            existingCriteria.AddRange(invalidProject.Criteria);
        }

        Assert.NotEmpty(errors);
        Assert.Equal(2, existingCriteria.Count);
        Assert.Equal("Оригінальний 1", existingCriteria[0].Name);
    }
}
