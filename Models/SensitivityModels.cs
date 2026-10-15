using System;
using System.Collections.Generic;

namespace MAI.Models;

/// <summary>
/// Представляє лінійну функцію пріоритету альтернативи: P_i(t) = Alpha * t + Beta,
/// де t in [0, 1] — гіпотетична вага досліджуваного критерію.
/// </summary>
public class SensitivityLine
{
    public string AlternativeId { get; set; } = string.Empty;
    public string AlternativeName { get; set; } = string.Empty;
    public double Alpha { get; set; } // Кутовий коефіцієнт (slope): L_ik - beta
    public double Beta { get; set; }  // Вільний член (intercept): S_i / (1 - w_k^0)
    public double OriginalPriority { get; set; } // Базовий бал P_i(w_k^0)
    public double LocalPriorityUnderCriterion { get; set; } // L_ik (значення при t = 1)

    /// <summary>
    /// Розрахунок пріоритету альтернативи для заданого значення ваги критерію t in [0, 1].
    /// </summary>
    public double PriorityAt(double t)
    {
        return Alpha * t + Beta;
    }
}

/// <summary>
/// Точка перетину функцій двох альтернатив: alpha_1 * t + beta_1 = alpha_2 * t + beta_2.
/// </summary>
public class CriticalIntersectionPoint
{
    public string Alt1Id { get; set; } = string.Empty;
    public string Alt1Name { get; set; } = string.Empty;
    public string Alt2Id { get; set; } = string.Empty;
    public string Alt2Name { get; set; } = string.Empty;
    public double WeightT { get; set; }   // Критичне значення ваги критерію t*
    public double PriorityP { get; set; } // Пріоритет у точці перетину P(t*)
    public bool IsInDomain => WeightT >= 0.0 && WeightT <= 1.0;
    public bool IsLeadershipChange { get; set; } // Чи відбувається зміна переможця (#1)
    public string? OldLeaderId { get; set; }
    public string? OldLeaderName { get; set; }
    public string? NewLeaderId { get; set; }
    public string? NewLeaderName { get; set; }
    public string EventDescription { get; set; } = string.Empty;
}

/// <summary>
/// Інтервал значень ваги критерію [FromT, ToT], на якому певна альтернатива посідає 1-ше місце.
/// </summary>
public class LeadershipInterval
{
    public string AlternativeId { get; set; } = string.Empty;
    public string AlternativeName { get; set; } = string.Empty;
    public double FromT { get; set; }
    public double ToT { get; set; }

    public string IntervalText => $"[{FromT:F3}; {ToT:F3}] ({FromT:P1} – {ToT:P1})";
    public string DisplayText => $"{IntervalText} ➔ {AlternativeId} ({AlternativeName})";
}

/// <summary>
/// Повний результат аналізу чутливості за обраним критерієм.
/// </summary>
public class CriterionSensitivityResult
{
    public Criterion Criterion { get; set; } = new();
    public double OriginalWeight { get; set; }
    public List<SensitivityLine> Lines { get; set; } = new();
    public List<CriticalIntersectionPoint> Intersections { get; set; } = new();
    public List<LeadershipInterval> LeadershipIntervals { get; set; } = new();

    public string OptimalAlternativeId { get; set; } = string.Empty;
    public string OptimalAlternativeName { get; set; } = string.Empty;
    public double StabilityIntervalMin { get; set; }
    public double StabilityIntervalMax { get; set; }
    public double? LowerMarginPp { get; set; }
    public double? UpperMarginPp { get; set; }
    public string? LowerSuccessorName { get; set; }
    public string? UpperSuccessorName { get; set; }

    public bool IsStable => OriginalWeight >= StabilityIntervalMin && OriginalWeight <= StabilityIntervalMax;
    public double RobustnessMarginLower => Math.Max(0.0, OriginalWeight - StabilityIntervalMin);
    public double RobustnessMarginUpper => Math.Max(0.0, StabilityIntervalMax - OriginalWeight);
    public string StabilitySummary { get; set; } = string.Empty;
}

/// <summary>
/// Елемент динамічного списку альтернатив при переміщенні повзунка ваги t.
/// </summary>
public class SensitivityDynamicRankingItem
{
    public int Rank { get; set; }
    private string _rankText = string.Empty;
    public string RankText
    {
        get => string.IsNullOrEmpty(_rankText) ? $"#{Rank}" : _rankText;
        set => _rankText = value;
    }
    public string AlternativeId { get; set; } = string.Empty;
    public string AlternativeName { get; set; } = string.Empty;
    public double CurrentPriority { get; set; }
    public double OriginalPriority { get; set; }
    public double DeltaPriority => CurrentPriority - OriginalPriority;

    public string PriorityPercentText => $"{CurrentPriority:P1} ({CurrentPriority:F4})";
    public string DeltaText => DeltaPriority switch
    {
        > 0.0005 => $"▲ +{DeltaPriority * 100:F1} п.п.",
        < -0.0005 => $"▼ {DeltaPriority * 100:F1} п.п.",
        _ => "— 0.0 п.п."
    };

    public bool IsWinner => Rank == 1;
}
