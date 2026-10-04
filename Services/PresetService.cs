using System.Collections.Generic;
using System.Linq;
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
    /// Контрольний набір даних із Лабораторних робіт №3–4 (Вибір відеокарти для студента CS).
    /// </summary>
    public static AhpProblemPreset GetLab34Preset()
    {
        var criteria = new List<Criterion>
        {
            new("P", "Productivity (P)", "Чиста обчислювальна потужність, FPS у 3D та растровому рендерингу"),
            new("F", "Functionality (F)", "Обсяг VRAM, тензорні блоки, екосистема CUDA під AI/ML"),
            new("A", "Actuality & Features (A)", "Актуальність архітектури, апаратна підтримка DLSS/FSR, енкодери AV1"),
            new("C", "Cost / Value (C)", "Цінова доступність за даними роздробу України")
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
            Name = "Лабораторні роботи №3–4 (Відеокарти)",
            Description = "Вибір відеокарти для студента спеціальності Комп'ютерні науки: 4 критерії, 5 альтернатив.",
            Criteria = criteria,
            Alternatives = alternatives,
            CriteriaMatrix = critMatrix,
            AlternativeMatrices = altMatrices
        };
    }

    /// <summary>
    /// Пресет із Курсової роботи (Актуалізований набір відеокарт 2026).
    /// </summary>
    public static AhpProblemPreset GetCourseworkPreset()
    {
        var criteria = new List<Criterion>
        {
            new("P", "Продуктивність (P)", "Швидкодія в растровому рендерингу (FPS у тестах та іграх)"),
            new("F", "Функціональність (F)", "Обсяг VRAM, CUDA/ROCm, тензорні блоки для AI/ML"),
            new("A", "Актуальність (A)", "Новизна архітектури (Blackwell / RDNA 4), DLSS/FSR, строк підтримки"),
            new("C", "Вартість (C)", "Цінова доступність нової карти в роздробі України")
        };

        var alternatives = new List<Alternative>
        {
            new("A1", "NVIDIA RTX 5060 8GB", "Базовий рівень Blackwell, доступна ціна"),
            new("A2", "NVIDIA RTX 5060 Ti 16GB", "Оптимум пам'яті 16GB для локального навчання нейромереж"),
            new("A3", "NVIDIA RTX 5070 12GB", "Вища продуктивність за меншого обсягу пам'яті"),
            new("A4", "AMD Radeon RX 9060 XT 16GB", "Конкурент A2 на новій платформі RDNA 4, 16GB"),
            new("A5", "AMD Radeon RX 9070 XT 16GB", "Флагманська продуктивність сегмента, 16GB VRAM")
        };

        var critLabels = criteria.Select(c => c.Name).ToList();
        var altLabels = alternatives.Select(a => a.Name).ToList();

        var critMatrix = new ComparisonMatrix(4, critLabels);
        critMatrix.SetComparison(0, 1, 2.0);
        critMatrix.SetComparison(0, 2, 3.0);
        critMatrix.SetComparison(0, 3, 1.0);
        critMatrix.SetComparison(1, 2, 2.0);
        critMatrix.SetComparison(1, 3, 1.0);
        critMatrix.SetComparison(2, 3, 0.5);

        var altMatrices = new Dictionary<string, ComparisonMatrix>();

        // Ap
        var pMatrix = new ComparisonMatrix(5, altLabels);
        pMatrix.SetComparison(0, 1, 0.25);
        pMatrix.SetComparison(0, 2, 0.2);
        pMatrix.SetComparison(0, 3, 0.25);
        pMatrix.SetComparison(0, 4, 1.0 / 6);
        pMatrix.SetComparison(1, 2, 0.5);
        pMatrix.SetComparison(1, 3, 1.0);
        pMatrix.SetComparison(1, 4, 0.333);
        pMatrix.SetComparison(2, 3, 2.0);
        pMatrix.SetComparison(2, 4, 0.5);
        pMatrix.SetComparison(3, 4, 0.333);
        altMatrices["P"] = pMatrix;

        // Af
        var fMatrix = new ComparisonMatrix(5, altLabels);
        fMatrix.SetComparison(0, 1, 0.25);
        fMatrix.SetComparison(0, 2, 0.5);
        fMatrix.SetComparison(0, 3, 0.333);
        fMatrix.SetComparison(0, 4, 0.2);
        fMatrix.SetComparison(1, 2, 2.0);
        fMatrix.SetComparison(1, 3, 2.0);
        fMatrix.SetComparison(1, 4, 1.0);
        fMatrix.SetComparison(2, 3, 1.0);
        fMatrix.SetComparison(2, 4, 0.5);
        fMatrix.SetComparison(3, 4, 0.5);
        altMatrices["F"] = fMatrix;

        // Aa
        var aMatrix = new ComparisonMatrix(5, altLabels);
        aMatrix.SetComparison(0, 1, 0.5);
        aMatrix.SetComparison(0, 2, 0.5);
        aMatrix.SetComparison(0, 3, 0.5);
        aMatrix.SetComparison(0, 4, 0.5);
        aMatrix.SetComparison(1, 2, 1.0);
        aMatrix.SetComparison(1, 3, 1.0);
        aMatrix.SetComparison(1, 4, 1.0);
        aMatrix.SetComparison(2, 3, 1.0);
        aMatrix.SetComparison(2, 4, 1.0);
        aMatrix.SetComparison(3, 4, 1.0);
        altMatrices["A"] = aMatrix;

        // Ac
        var cMatrix = new ComparisonMatrix(5, altLabels);
        cMatrix.SetComparison(0, 1, 3.0);
        cMatrix.SetComparison(0, 2, 5.0);
        cMatrix.SetComparison(0, 3, 3.0);
        cMatrix.SetComparison(0, 4, 7.0);
        cMatrix.SetComparison(1, 2, 2.0);
        cMatrix.SetComparison(1, 3, 1.0);
        cMatrix.SetComparison(1, 4, 3.0);
        cMatrix.SetComparison(2, 3, 0.5);
        cMatrix.SetComparison(2, 4, 2.0);
        cMatrix.SetComparison(3, 4, 3.0);
        altMatrices["C"] = cMatrix;

        return new AhpProblemPreset
        {
            Name = "Курсова робота (Відеокарти 2026)",
            Description = "Актуалізований модельний ряд (Blackwell / RDNA 4): 4 критерії, 5 альтернатив.",
            Criteria = criteria,
            Alternatives = alternatives,
            CriteriaMatrix = critMatrix,
            AlternativeMatrices = altMatrices
        };
    }

    /// <summary>
    /// Створення чистого шаблону для довільної задачі користувача.
    /// </summary>
    public static AhpProblemPreset CreateEmptyPreset(int criteriaCount = 3, int alternativesCount = 3)
    {
        int cCount = Math.Clamp(criteriaCount, 2, 8);
        int aCount = Math.Clamp(alternativesCount, 2, 8);

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
