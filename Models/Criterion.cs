using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MAI.Models;

/// <summary>
/// Представляє критерій оцінювання у методі аналізу ієрархій.
/// </summary>
public class Criterion : INotifyPropertyChanged
{
    private string _id = string.Empty;
    private string _name = string.Empty;
    private string _description = string.Empty;
    private double _weight;

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

    public double Weight
    {
        get => _weight;
        set => SetField(ref _weight, value);
    }

    public Criterion() { }

    public Criterion(string id, string name, string description = "")
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
