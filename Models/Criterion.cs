namespace MAI.Models;

/// <summary>
/// Представляє критерій оцінювання у методі аналізу ієрархій.
/// </summary>
public class Criterion
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Weight { get; set; }

    public Criterion() { }

    public Criterion(string id, string name, string description = "")
    {
        Id = id;
        Name = name;
        Description = description;
    }

    public override string ToString() => string.IsNullOrWhiteSpace(Description) ? Name : $"{Name} ({Description})";
}
