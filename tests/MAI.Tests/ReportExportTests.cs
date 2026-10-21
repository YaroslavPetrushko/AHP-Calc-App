using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MAI.Models;
using MAI.Services;
using Xunit;

namespace MAI.Tests;

public class ReportExportTests
{
    private static List<ReportTable> CreateSampleTables()
    {
        return new List<ReportTable>
        {
            new ReportTable(
                "Тестова таблиця 1",
                new[] { "Назва", "Значення", "Опис; з крапкою з комою" },
                new List<string[]>
                {
                    new[] { "Елемент A", "0,3645", "Текст зі спеціальним символом \"кавички\"" },
                    new[] { "Елемент B | спец", "12,50 %", "Простий текст; з роздільником" }
                },
                "Примітка до таблиці 1"
            ),
            new ReportTable(
                "Тестова таблиця 2",
                new[] { "Критерій", "Вага" },
                new List<string[]>
                {
                    new[] { "Ціна", "0,2778" },
                    new[] { "Якість", "0,7222" }
                }
            )
        };
    }

    [Fact]
    public void Csv_StartsWithBom_ContainsSemicolons_AndNumbersWithComma()
    {
        var tables = CreateSampleTables();
        string csv = ReportExporter.ToCsv(tables);

        Assert.StartsWith("\uFEFF", csv);
        Assert.Contains(";", csv);
        Assert.Contains("0,3645", csv);
        Assert.Contains("12,50 %", csv);

        string tempFile = Path.Combine(Path.GetTempPath(), $"ahp_test_{Guid.NewGuid():N}.csv");
        try
        {
            ReportExporter.SaveCsv(tempFile, tables);
            byte[] bytes = File.ReadAllBytes(tempFile);
            Assert.True(bytes.Length >= 3);
            Assert.Equal(0xEF, bytes[0]);
            Assert.Equal(0xBB, bytes[1]);
            Assert.Equal(0xBF, bytes[2]);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void Csv_EscapesQuotesAndSemicolons()
    {
        var tables = CreateSampleTables();
        string csv = ReportExporter.ToCsv(tables);

        // Header with semicolon must be quoted
        Assert.Contains("\"Опис; з крапкою з комою\"", csv);
        // Cell with quotes must have quotes escaped as ""
        Assert.Contains("\"Текст зі спеціальним символом \"\"кавички\"\"\"", csv);
        // Cell with semicolon must be quoted
        Assert.Contains("\"Простий текст; з роздільником\"", csv);
    }

    [Fact]
    public void Markdown_ContainsPipeSeparator_AndEscapesPipeInCells()
    {
        var tables = CreateSampleTables();
        string md = ReportExporter.ToMarkdown(tables);

        // Pipe table separator line
        Assert.Contains("|---|", md);
        // Table headers and cells
        Assert.Contains("### Тестова таблиця 1", md);
        // Cell with pipe '|' must be escaped as '\|'
        Assert.Contains("Елемент B \\| спец", md);
        Assert.DoesNotContain("Елемент B | спец", md);
    }

    [Fact]
    public void Html_ContainsDoctype_AndTableCountEqualsTableCount()
    {
        var tables = CreateSampleTables();
        string html = ReportExporter.ToHtml(tables);

        Assert.Contains("<!DOCTYPE html>", html);
        Assert.Contains("Times New Roman", html);

        int tableCount = Regex.Matches(html, "<table\\b", RegexOptions.IgnoreCase).Count;
        Assert.Equal(tables.Count, tableCount);
    }

    [Fact]
    public void ReportTableBuilder_BuildsAllRequiredTables_ForReferencePreset()
    {
        var preset = PresetService.GetReferencePreset();
        var options = AhpCalculationOptions.ClassicSaatyProfile;
        var result = AhpCalculator.SolveHierarchy(preset.Criteria, preset.Alternatives, preset.CriteriaMatrix, preset.AlternativeMatrices, options);

        var tables = ReportTableBuilder.Build(result, options);

        Assert.NotNull(tables);
        Assert.True(tables.Count >= 8, $"Expected at least 8 tables, got {tables.Count}");

        // 1. Параметри розрахунку
        Assert.Contains(tables, t => t.Title.Contains("Параметри розрахунку"));
        // 2. Матриця парних порівнянь критеріїв
        Assert.Contains(tables, t => t.Title.Contains("Матриця парних порівнянь критеріїв"));
        // 3. Нормалізована матриця критеріїв
        Assert.Contains(tables, t => t.Title.Contains("Нормалізована матриця критеріїв"));
        // 4. Показники узгодженості матриці критеріїв
        Assert.Contains(tables, t => t.Title.Contains("Показники узгодженості матриці критеріїв"));
        // 5. Таблиці альтернатив для кожного критерію
        foreach (var c in preset.Criteria)
        {
            Assert.Contains(tables, t => t.Title.Contains($"за критерієм «{c.Name}»"));
        }
        // 6. Зведення локальних пріоритетів
        Assert.Contains(tables, t => t.Title.Contains("Зведення локальних пріоритетів"));
        // 7. Узгодженість ієрархії
        Assert.Contains(tables, t => t.Title.Contains("Узгодженість ієрархії"));
        // 8. Чутливість
        Assert.Contains(tables, t => t.Title.Contains("Чутливість"));

        // Check CSV, Markdown and HTML outputs on real calculation result
        string csv = ReportExporter.ToCsv(tables);
        Assert.StartsWith("\uFEFF", csv);
        Assert.Contains(";", csv);

        string md = ReportExporter.ToMarkdown(tables);
        Assert.Contains("|---|", md);

        string html = ReportExporter.ToHtml(tables);
        int htmlTableCount = Regex.Matches(html, "<table\\b", RegexOptions.IgnoreCase).Count;
        Assert.Equal(tables.Count, htmlTableCount);
    }

    [Fact]
    public void ReportTableBuilder_PairwiseMatrix_FormatsSaatyFractions_InCsvMarkdownAndHtml()
    {
        var preset = PresetService.GetReferencePreset();
        var options = AhpCalculationOptions.ClassicSaatyProfile;
        var result = AhpCalculator.SolveHierarchy(preset.Criteria, preset.Alternatives, preset.CriteriaMatrix, preset.AlternativeMatrices, options);
        var tables = ReportTableBuilder.Build(result, options);

        var critPwTable = tables.First(t => t.Title.Contains("Матриця парних порівнянь критеріїв"));
        bool hasFractionInCrit = critPwTable.Rows.Any(r => r.Contains("1/3"));
        Assert.True(hasFractionInCrit, "Pairwise table should contain '1/3' formatted Saaty fraction");

        string csv = ReportExporter.ToCsv(tables);
        Assert.True(csv.Contains(";=\"1/3\";") || csv.Contains(";1/3;"), "CSV should contain Excel-safe or raw Saaty fraction 1/3");

        string md = ReportExporter.ToMarkdown(tables);
        Assert.Contains("| 1/3 |", md);

        string html = ReportExporter.ToHtml(tables);
        Assert.Contains(">1/3<", html);
        Assert.Contains("mso-number-format", html);
    }
}
