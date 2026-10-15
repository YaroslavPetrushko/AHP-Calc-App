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
    public double[,] PairwiseMatrix { get; set; } = new double[0, 0];
    public double[] Weights { get; set; } = [];
    public double[,] NormalizedMatrix { get; set; } = new double[0, 0];
    public double[] ColumnSums { get; set; } = [];
    public double LambdaMax { get; set; }
    public double CI { get; set; }
    public double RITable { get; set; }
    public double RIEmpirical { get; set; }
    public double CRTable { get; set; }
    public double CREmpirical { get; set; }

    public AhpCalculationOptions Options { get; set; } = AhpCalculationOptions.Default;

    public RiCalculationMode ActiveRiMode => Options.RiMode;

    public double ActiveRI => ActiveRiMode == RiCalculationMode.Empirical ? RIEmpirical : RITable;
    public double ActiveCR => ActiveRiMode == RiCalculationMode.Empirical ? CREmpirical : CRTable;

    public double AcceptableCRThreshold => Options.GetAcceptableCR(Size);
    public double Threshold => AcceptableCRThreshold;
    public string AcceptableCRThresholdText => Options.GetThresholdDescription(Size);

    public bool IsConsistent => ActiveCR <= AcceptableCRThreshold;

    public string ConsistencyMessage =>
        IsConsistent
            ? $"Узгоджено (CR = {ActiveCR:P1} <= {AcceptableCRThresholdText})"
            : $"Порушено узгодженість (CR = {ActiveCR:P1} > {AcceptableCRThresholdText})";
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

    public AhpCalculationOptions Options { get; set; } = AhpCalculationOptions.Default;

    public RiCalculationMode ActiveRiMode => Options.RiMode;

    public double ActiveHierarchicalRI => ActiveRiMode == RiCalculationMode.Empirical ? HierarchicalRIEmpirical : HierarchicalRITable;
    public double ActiveHierarchicalCR => ActiveRiMode == RiCalculationMode.Empirical ? HierarchicalCREmpirical : HierarchicalCRTable;

    public double HierarchicalThreshold => 0.10;
    public double HierarchyThreshold => HierarchicalThreshold;
    public string HierarchicalThresholdText => "10.0%";

    public bool IsHierarchyConsistent => ActiveHierarchicalCR <= 0.1001;

    public Alternative? BestAlternative => RankedAlternatives.FirstOrDefault();

    /// <summary>
    /// Список назв матриць, у яких коефіцієнт CR перевищує власний допустимий поріг.
    /// </summary>
    public List<string> InconsistentMatrices
    {
        get
        {
            var list = new List<string>();
            if (CriteriaMatrixResult != null && !CriteriaMatrixResult.IsConsistent && !string.IsNullOrEmpty(CriteriaMatrixResult.Name))
            {
                list.Add(CriteriaMatrixResult.Name);
            }
            if (AlternativeMatrixResults != null)
            {
                foreach (var kvp in AlternativeMatrixResults)
                {
                    if (!kvp.Value.IsConsistent)
                    {
                        string name = !string.IsNullOrEmpty(kvp.Value.Name) ? kvp.Value.Name : kvp.Key;
                        list.Add(name);
                    }
                }
            }
            return list;
        }
    }

    public string HierarchyConsistencyMessage =>
        IsHierarchyConsistent
            ? $"Ієрархічна узгодженість прийнятна (CRh = {ActiveHierarchicalCR:P1} <= {HierarchicalThresholdText})"
            : $"Увага: Ієрархічна неузгодженість перевищує норму (CRh = {ActiveHierarchicalCR:P1} > {HierarchicalThresholdText})";
}
