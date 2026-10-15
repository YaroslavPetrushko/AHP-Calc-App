using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using MAI.Models;

namespace MAI.Services;

/// <summary>
/// Опис повного набору даних задачі для МАІ.
/// </summary>
public class AhpProblemPreset
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<Criterion> Criteria { get; set; } = new();
    public List<Alternative> Alternatives { get; set; } = new();
    public ComparisonMatrix CriteriaMatrix { get; set; } = new(1);
    public Dictionary<string, ComparisonMatrix> AlternativeMatrices { get; set; } = new();
}

/// <summary>
/// Сервіс попередньо налаштованих наборів даних для швидкого тестування та перевірки алгоритму.
/// </summary>
public static class PresetService
{
    /// <summary>
    /// Контрольний набір даних: відеокарти попереднього покоління.
    /// </summary>
    public static AhpProblemPreset GetReferencePreset()
    {
        var criteria = new List<Criterion>
        {
            new("P", "Продуктивність", "Чиста обчислювальна потужність, FPS у 3D та растровому рендерингу"),
            new("F", "Функціональність", "Обсяг VRAM, тензорні блоки, екосистема CUDA під AI/ML"),
            new("A", "Актуальність", "Актуальність архітектури, апаратна підтримка DLSS/FSR, енкодери AV1"),
            new("C", "Вартість", "Цінова доступність за даними роздробу України")
        };

        var alternatives = new List<Alternative>
        {
            new("A1", "NVIDIA RTX 3050 8GB", "Базовий рівень минулого покоління, низька ціна"),
            new("A2", "NVIDIA RTX 3070 8GB", "Висока растрово-графічна потужність, обмеження 8GB"),
            new("A3", "NVIDIA RTX 4060 Ti 16GB", "Збалансована модель з 16GB VRAM та DLSS 3"),
            new("A4", "NVIDIA RTX 5060 Ti 16GB", "Нове покоління Blackwell, 16GB VRAM, оптимум для AI/ML"),
            new("A5", "Radeon RX 7700 XT 12GB", "Альтернатива AMD, 12GB пам'яті, високий чистий FPS")
        };

        var critLabels = criteria.Select(c => c.Name).ToList();
        var altLabels = alternatives.Select(a => a.Name).ToList();

        // Матриця критеріїв (4x4)
        var critMatrix = new ComparisonMatrix(4, critLabels);
        critMatrix.SetComparison(0, 1, 2.0);    // P vs F = 2
        critMatrix.SetComparison(0, 2, 3.0);    // P vs A = 3
        critMatrix.SetComparison(0, 3, 1.0);    // P vs C = 1
        critMatrix.SetComparison(1, 2, 2.0);    // F vs A = 2
        critMatrix.SetComparison(1, 3, 1.0);    // F vs C = 1
        critMatrix.SetComparison(2, 3, 0.5);    // A vs C = 1/2 (C vs A = 2)

        var altMatrices = new Dictionary<string, ComparisonMatrix>();

        // 1. Productivity (Ap)
        var pMatrix = new ComparisonMatrix(5, altLabels);
        pMatrix.SetComparison(0, 1, 0.2);     // A1 vs A2 = 1/5
        pMatrix.SetComparison(0, 2, 0.2);     // A1 vs A3 = 1/5
        pMatrix.SetComparison(0, 3, 0.2);     // A1 vs A4 = 1/5
        pMatrix.SetComparison(0, 4, 1.0 / 6); // A1 vs A5 = 1/6
        pMatrix.SetComparison(1, 2, 0.5);     // A2 vs A3 = 1/2
        pMatrix.SetComparison(1, 3, 0.5);     // A2 vs A4 = 1/2
        pMatrix.SetComparison(1, 4, 0.5);     // A2 vs A5 = 1/2
        pMatrix.SetComparison(2, 3, 1.0);     // A3 vs A4 = 1
        pMatrix.SetComparison(2, 4, 0.5);     // A3 vs A5 = 1/2
        pMatrix.SetComparison(3, 4, 0.5);     // A4 vs A5 = 1/2
        altMatrices["P"] = pMatrix;

        // 2. Functionality (Af)
        var fMatrix = new ComparisonMatrix(5, altLabels);
        fMatrix.SetComparison(0, 1, 0.5);     // A1 vs A2 = 1/2
        fMatrix.SetComparison(0, 2, 0.2);     // A1 vs A3 = 1/5
        fMatrix.SetComparison(0, 3, 0.2);     // A1 vs A4 = 1/5
        fMatrix.SetComparison(0, 4, 0.25);    // A1 vs A5 = 1/4
        fMatrix.SetComparison(1, 2, 0.2);     // A2 vs A3 = 1/5
        fMatrix.SetComparison(1, 3, 0.2);     // A2 vs A4 = 1/5
        fMatrix.SetComparison(1, 4, 0.5);     // A2 vs A5 = 1/2
        fMatrix.SetComparison(2, 3, 0.5);     // A3 vs A4 = 1/2
        fMatrix.SetComparison(2, 4, 3.0);     // A3 vs A5 = 3
        fMatrix.SetComparison(3, 4, 3.0);     // A4 vs A5 = 3
        altMatrices["F"] = fMatrix;

        // 3. Actuality (Aa)
        var aMatrix = new ComparisonMatrix(5, altLabels);
        aMatrix.SetComparison(0, 1, 1.0);     // A1 vs A2 = 1
        aMatrix.SetComparison(0, 2, 1.0 / 3); // A1 vs A3 = 1/3
        aMatrix.SetComparison(0, 3, 1.0 / 3); // A1 vs A4 = 1/3
        aMatrix.SetComparison(0, 4, 0.5);     // A1 vs A5 = 1/2
        aMatrix.SetComparison(1, 2, 1.0 / 3); // A2 vs A3 = 1/3
        aMatrix.SetComparison(1, 3, 1.0 / 3); // A2 vs A4 = 1/3
        aMatrix.SetComparison(1, 4, 0.5);     // A2 vs A5 = 1/2
        aMatrix.SetComparison(2, 3, 1.0);     // A3 vs A4 = 1
        aMatrix.SetComparison(2, 4, 2.0);     // A3 vs A5 = 2
        aMatrix.SetComparison(3, 4, 2.0);     // A4 vs A5 = 2
        altMatrices["A"] = aMatrix;

        // 4. Cost (Ac)
        var cMatrix = new ComparisonMatrix(5, altLabels);
        cMatrix.SetComparison(0, 1, 2.0);     // A1 vs A2 = 2
        cMatrix.SetComparison(0, 2, 5.0);     // A1 vs A3 = 5
        cMatrix.SetComparison(0, 3, 7.0);     // A1 vs A4 = 7
        cMatrix.SetComparison(0, 4, 5.0);     // A1 vs A5 = 5
        cMatrix.SetComparison(1, 2, 3.0);     // A2 vs A3 = 3
        cMatrix.SetComparison(1, 3, 5.0);     // A2 vs A4 = 5
        cMatrix.SetComparison(1, 4, 3.0);     // A2 vs A5 = 3
        cMatrix.SetComparison(2, 3, 3.0);     // A3 vs A4 = 3
        cMatrix.SetComparison(2, 4, 1.0);     // A3 vs A5 = 1
        cMatrix.SetComparison(3, 4, 1.0 / 3); // A4 vs A5 = 1/3
        altMatrices["C"] = cMatrix;

        return new AhpProblemPreset
        {
            Name = "Контрольний набір №1: відеокарти (попереднє покоління)",
            Description = "Вибір дискретної відеокарти для студента-програміста: 4 критерії, 5 альтернатив.",
            Criteria = criteria,
            Alternatives = alternatives,
            CriteriaMatrix = critMatrix,
            AlternativeMatrices = altMatrices
        };
    }

    /// <summary>
    /// Набір №2: відеокарти 2026 (RTX 50 / RX 9000) — завантажується з вбудованого ресурсу gpu_2026.ahp.json.
    /// </summary>
    public static AhpProblemPreset GetGpu2026Preset()
    {
        return LoadPresetFromResource("gpu_2026.ahp.json");
    }

    /// <summary>
    /// Завантаження пресету з вбудованого ресурсу збірки (.ahp.json).
    /// </summary>
    public static AhpProblemPreset LoadPresetFromResource(string resourceFileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        string[] allResourceNames = assembly.GetManifestResourceNames();

        string? matchedName = allResourceNames.FirstOrDefault(n =>
            n.Equals(resourceFileName, StringComparison.OrdinalIgnoreCase) ||
            n.EndsWith("." + resourceFileName, StringComparison.OrdinalIgnoreCase));

        if (matchedName == null)
        {
            throw new InvalidOperationException($"Вбудований ресурс пресету «{resourceFileName}» не знайдено в збірці. Доступні ресурси: {string.Join(", ", allResourceNames)}");
        }

        using var stream = assembly.GetManifestResourceStream(matchedName);
        if (stream == null)
        {
            throw new InvalidOperationException($"Не вдалося відкрити потік вбудованого ресурсу «{matchedName}».");
        }

        using var reader = new StreamReader(stream, Encoding.UTF8);
        string json = reader.ReadToEnd();

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var data = JsonSerializer.Deserialize<AhpProjectData>(json, jsonOptions);
        if (data == null)
        {
            throw new InvalidOperationException($"Не вдалося десеріалізувати дані пресету з ресурсу «{matchedName}». Файл пошкоджений або порожній.");
        }

        var errors = ProjectStorageService.ValidateProject(data);
        if (errors.Count > 0)
        {
            throw new InvalidOperationException($"Помилка валідації пресету «{matchedName}»:\n" + string.Join("\n", errors.Select(e => $"• {e}")));
        }

        var critLabels = data.Criteria.Select(c => c.Name).ToList();
        var altLabels = data.Alternatives.Select(a => a.Name).ToList();

        var critMatrix = new ComparisonMatrix(data.Criteria.Count, critLabels);
        if (data.CriteriaMatrix != null && data.CriteriaMatrix.Length > 0)
        {
            critMatrix.LoadFromJaggedArray(data.CriteriaMatrix);
        }

        var altMatrices = new Dictionary<string, ComparisonMatrix>();
        foreach (var c in data.Criteria)
        {
            var altMat = new ComparisonMatrix(data.Alternatives.Count, altLabels);
            if (data.AlternativeMatrices != null && data.AlternativeMatrices.TryGetValue(c.Id, out var jaggedMat))
            {
                altMat.LoadFromJaggedArray(jaggedMat);
            }
            altMatrices[c.Id] = altMat;
        }

        return new AhpProblemPreset
        {
            Name = data.Title,
            Description = data.Description,
            Criteria = data.Criteria,
            Alternatives = data.Alternatives,
            CriteriaMatrix = critMatrix,
            AlternativeMatrices = altMatrices
        };
    }

    /// <summary>
    /// Створення чистого шаблону для довільної задачі користувача.
    /// </summary>
    public static AhpProblemPreset CreateEmptyPreset(int criteriaCount = 3, int alternativesCount = 3)
    {
        int cCount = Math.Clamp(criteriaCount, 2, 10);
        int aCount = Math.Clamp(alternativesCount, 2, 10);

        var criteria = new List<Criterion>();
        for (int i = 0; i < cCount; i++)
        {
            criteria.Add(new Criterion($"C{i + 1}", $"Критерій {i + 1}", ""));
        }

        var alternatives = new List<Alternative>();
        for (int i = 0; i < aCount; i++)
        {
            alternatives.Add(new Alternative($"A{i + 1}", $"Альтернатива {i + 1}", ""));
        }

        var critLabels = criteria.Select(c => c.Name).ToList();
        var altLabels = alternatives.Select(a => a.Name).ToList();

        var critMatrix = new ComparisonMatrix(cCount, critLabels);
        var altMatrices = new Dictionary<string, ComparisonMatrix>();

        foreach (var c in criteria)
        {
            altMatrices[c.Id] = new ComparisonMatrix(aCount, altLabels);
        }

        return new AhpProblemPreset
        {
            Name = "Нова задача",
            Description = "Чистий шаблон для власної постановки задачі багатокритеріального вибору.",
            Criteria = criteria,
            Alternatives = alternatives,
            CriteriaMatrix = critMatrix,
            AlternativeMatrices = altMatrices
        };
    }
}
