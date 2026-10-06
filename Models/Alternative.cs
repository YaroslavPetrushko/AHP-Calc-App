using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MAI.Models;

/// <summary>
/// Представляє альтернативу вибору у методі аналізу ієрархій.
/// </summary>
public class Alternative : INotifyPropertyChanged
{
    private string _id = string.Empty;
    private string _name = string.Empty;
    private string _description = string.Empty;
    private double _globalPriority;
    private int _rank;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Id
    {
        get => _id;
        set => SetField(ref _id, value);
    }

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    public string Description
    {
        get => _description;
        set => SetField(ref _description, value);
    }

    /// <summary>
    /// Глобальний пріоритет (частка від 0 до 1).
    /// </summary>
    public double GlobalPriority
    {
        get => _globalPriority;
        set => SetField(ref _globalPriority, value);
    }

    /// <summary>
    /// Ранг за підсумковим пріоритетом (1 - найкращий).
    /// </summary>
    public int Rank
    {
        get => _rank;
        set => SetField(ref _rank, value);
    }

    private string _rankText = string.Empty;

    /// <summary>
    /// Текстове представлення рангу (наприклад "1", або "2=" при рівності балів з іншою альтернативою).
    /// </summary>
    public string RankText
    {
        get => string.IsNullOrEmpty(_rankText) ? _rank.ToString() : _rankText;
        set => SetField(ref _rankText, value);
    }

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

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    public override string ToString() => string.IsNullOrWhiteSpace(Description) ? Name : $"{Name} ({Description})";
}
