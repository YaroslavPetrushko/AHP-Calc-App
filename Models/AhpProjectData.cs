using System;
using System.Collections.Generic;

namespace MAI.Models;

/// <summary>
/// Повна структура задачі МАІ для збереження та завантаження у форматі JSON.
/// </summary>
public class AhpProjectData
{
    public string Title { get; set; } = "Задача багатокритеріального аналізу";
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = typeof(AhpProjectData).Assembly.GetName().Version is { } v
        ? $"{v.Major}.{v.Minor}.{(v.Build >= 0 ? v.Build : 0)}"
        : "1.2.0";
    public DateTime SavedAt { get; set; } = DateTime.Now;
    private AhpCalculationOptions? _options;

    public AhpCalculationOptions Options
    {
        get => _options ??= AhpCalculationOptions.Default.Clone();
        set => _options = value;
    }

    /// <summary>
    /// Для зворотної сумісності з файлами версії 1.1.
    /// </summary>
    public RiCalculationMode RiMode
    {
        get => Options.RiMode;
        set
        {
            if (_options == null)
            {
                _options = value == RiCalculationMode.SaatyTable
                    ? AhpCalculationOptions.ClassicSaatyProfile.Clone()
                    : AhpCalculationOptions.MethodicalProfile.Clone();
            }
            else
            {
                _options.RiMode = value;
            }
        }
    }

    public List<Criterion> Criteria { get; set; } = new();
    public List<Alternative> Alternatives { get; set; } = new();

    /// <summary>
    /// Матриця попарних порівнянь критеріїв (NxN зубчастий масив).
    /// </summary>
    public double[][] CriteriaMatrix { get; set; } = Array.Empty<double[]>();

    /// <summary>
    /// Матриці попарних порівнянь альтернатив за кожним критерієм (ID критерію -> MxM зубчастий масив).
    /// </summary>
    public Dictionary<string, double[][]> AlternativeMatrices { get; set; } = new();
}
