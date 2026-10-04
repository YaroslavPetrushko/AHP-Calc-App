using System.Collections.Generic;

namespace MAI.Models;

/// <summary>
/// Представляє альтернативу вибору у методі аналізу ієрархій.
/// </summary>
public class Alternative
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Глобальний пріоритет (частка від 0 до 1).
    /// </summary>
    public double GlobalPriority { get; set; }

    /// <summary>
    /// Ранг за підсумковим пріоритетом (1 - найкращий).
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// Локальні пріоритети альтернативи за окремими критеріями.
    /// </summary>
    public Dictionary<string, double> LocalPriorities { get; set; } = new();

    /// <summary>
    /// Зважені бали за кожним критерієм: LocalPriority * CriterionWeight.
    /// </summary>
    public Dictionary<string, double> WeightedScores { get; set; } = new();

    public Alternative() { }

    public Alternative(string id, string name, string description = "")
    {
        Id = id;
        Name = name;
        Description = description;
    }

    public override string ToString() => string.IsNullOrWhiteSpace(Description) ? Name : $"{Name} ({Description})";
}
