using System.Collections.Generic;
using System.Linq;

namespace MAI.Models;

/// <summary>
/// Результати обчислень для окремої матриці попарних порівнянь.
/// </summary>
public class MatrixResult
{
    public string Name { get; set; } = string.Empty;
    public int Size { get; set; }
    public List<string> Labels { get; set; } = new();
    public double[] Weights { get; set; } = [];
    public double[,] NormalizedMatrix { get; set; } = new double[0, 0];
    public double[] ColumnSums { get; set; } = [];
    public double LambdaMax { get; set; }
    public double CI { get; set; }
    public double RITable { get; set; }
    public double RIEmpirical { get; set; }
    public double CRTable { get; set; }
    public double CREmpirical { get; set; }

    public bool IsConsistent => CRTable <= 0.1001;

    public string ConsistencyMessage =>
        IsConsistent
            ? $"Узгоджено (CR = {CRTable:P1} <= 10%)"
            : $"Порушено узгодженість (CR = {CRTable:P1} > 10%)";
}

/// <summary>
/// Повний результат ієрархічного аналізу Сааті для всієї системи.
/// </summary>
public class AhpResult
{
    public List<Criterion> Criteria { get; set; } = new();
    public List<Alternative> Alternatives { get; set; } = new();
    public List<Alternative> RankedAlternatives { get; set; } = new();

    public MatrixResult CriteriaMatrixResult { get; set; } = new();
    public Dictionary<string, MatrixResult> AlternativeMatrixResults { get; set; } = new();

    // Показники композиції ієрархії
    public double HierarchicalCI { get; set; }
    public double HierarchicalRITable { get; set; }
    public double HierarchicalRIEmpirical { get; set; }
    public double HierarchicalCRTable { get; set; }
    public double HierarchicalCREmpirical { get; set; }

    public bool IsHierarchyConsistent => HierarchicalCRTable <= 0.1001;

    public Alternative? BestAlternative => RankedAlternatives.FirstOrDefault();

    public string HierarchyConsistencyMessage =>
        IsHierarchyConsistent
            ? $"Ієрархічна узгодженість прийнятна (CRh = {HierarchicalCRTable:P1} <= 10%)"
            : $"Увага: Ієрархічна неузгодженість перевищує норму (CRh = {HierarchicalCRTable:P1} > 10%)";
}
