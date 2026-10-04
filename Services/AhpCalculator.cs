using System;
using System.Collections.Generic;
using System.Linq;
using MAI.Models;

namespace MAI.Services;

/// <summary>
/// Математичне ядро методу аналізу ієрархій (МАІ) Томаса Сааті.
/// </summary>
public static class AhpCalculator
{
    /// <summary>
    /// Табличні значення випадкового індексу узгодженості (RI) Сааті для розмірностей n = 1..10.
    /// </summary>
    public static readonly Dictionary<int, double> SaatyRITable = new()
    {
        { 1, 0.00 },
        { 2, 0.00 },
        { 3, 0.58 },
        { 4, 0.90 },
        { 5, 1.12 },
        { 6, 1.24 },
        { 7, 1.32 },
        { 8, 1.41 },
        { 9, 1.45 },
        { 10, 1.49 }
    };

    /// <summary>
    /// Отримання табличного значення випадкового індексу RI.
    /// </summary>
    public static double GetRITable(int n)
    {
        if (SaatyRITable.TryGetValue(n, out double ri))
            return ri;
        return n > 10 ? 1.49 + 0.03 * (n - 10) : 0.0;
    }

    /// <summary>
    /// Розрахунок випадкового індексу за емпіричною формулою Сааті: RI = 1.98 * (n - 2) / n.
    /// </summary>
    public static double GetRIEmpirical(int n)
    {
        return n > 2 ? 1.98 * (n - 2.0) / n : 0.0;
    }

    /// <summary>
    /// Обчислення локальних пріоритетів та характеристик узгодженості для однієї матриці Сааті.
    /// </summary>
    public static MatrixResult CalculateMatrix(ComparisonMatrix matrix, string matrixName = "")
    {
        int n = matrix.Size;
        var result = new MatrixResult
        {
            Name = matrixName,
            Size = n,
            Labels = new List<string>(matrix.Labels),
            Weights = new double[n],
            NormalizedMatrix = new double[n, n],
            ColumnSums = new double[n]
        };

        if (n == 0) return result;

        if (n == 1)
        {
            result.Weights[0] = 1.0;
            result.NormalizedMatrix[0, 0] = 1.0;
            result.ColumnSums[0] = 1.0;
            result.LambdaMax = 1.0;
            result.CI = 0.0;
            result.RITable = 0.0;
            result.RIEmpirical = 0.0;
            result.CRTable = 0.0;
            result.CREmpirical = 0.0;
            return result;
        }

        // 1. Обчислення сум стовпців
        for (int j = 0; j < n; j++)
        {
            double colSum = 0.0;
            for (int i = 0; i < n; i++)
            {
                colSum += matrix.Values[i, j];
            }
            result.ColumnSums[j] = colSum;
        }

        // 2. Нормалізація матриці та обчислення середніх по рядках (вектор ваг)
        for (int i = 0; i < n; i++)
        {
            double rowSumNorm = 0.0;
            for (int j = 0; j < n; j++)
            {
                double normVal = result.ColumnSums[j] > 0
                    ? matrix.Values[i, j] / result.ColumnSums[j]
                    : 1.0 / n;
                result.NormalizedMatrix[i, j] = normVal;
                rowSumNorm += normVal;
            }
            result.Weights[i] = rowSumNorm / n;
        }

        // 3. Обчислення максимального власного числа lambda_max:
        // v = A * w; lambda_i = v_i / w_i; lambda_max = average(lambda_i)
        double lambdaSum = 0.0;
        int validRows = 0;
        for (int i = 0; i < n; i++)
        {
            double vi = 0.0;
            for (int j = 0; j < n; j++)
            {
                vi += matrix.Values[i, j] * result.Weights[j];
            }

            if (result.Weights[i] > 1e-9)
            {
                lambdaSum += vi / result.Weights[i];
                validRows++;
            }
        }

        result.LambdaMax = validRows > 0 ? lambdaSum / validRows : n;

        // 4. Індекс узгодженості CI = (lambda_max - n) / (n - 1)
        double ciRaw = (result.LambdaMax - n) / (n - 1);
        result.CI = Math.Max(0.0, ciRaw);

        // 5. Випадкові індекси RI
        result.RITable = GetRITable(n);
        result.RIEmpirical = GetRIEmpirical(n);

        // 6. Коефіцієнт узгодженості CR = CI / RI
        result.CRTable = result.RITable > 0 ? result.CI / result.RITable : 0.0;
        result.CREmpirical = result.RIEmpirical > 0 ? result.CI / result.RIEmpirical : 0.0;

        return result;
    }

    /// <summary>
    /// Повний ієрархічний синтез за методом Сааті:
    /// 1. Розрахунок ваг критеріїв.
    /// 2. Розрахунок пріоритетів альтернатив за кожним критерієм.
    /// 3. Синтез глобальних пріоритетів: P = L * w.
    /// 4. Перевірка композиційної узгодженості ієрархії CRh.
    /// </summary>
    public static AhpResult SolveHierarchy(
        List<Criterion> criteria,
        List<Alternative> alternatives,
        ComparisonMatrix criteriaMatrix,
        Dictionary<string, ComparisonMatrix> alternativeMatrices)
    {
        var ahpResult = new AhpResult
        {
            Criteria = criteria.Select(c => new Criterion(c.Id, c.Name, c.Description)).ToList(),
            Alternatives = alternatives.Select(a => new Alternative(a.Id, a.Name, a.Description)).ToList()
        };

        int critCount = criteria.Count;
        int altCount = alternatives.Count;

        if (critCount == 0 || altCount == 0)
            return ahpResult;

        // 1. Розрахунок матриці критеріїв
        var critResult = CalculateMatrix(criteriaMatrix, "Матриця порівняння критеріїв");
        ahpResult.CriteriaMatrixResult = critResult;

        for (int i = 0; i < critCount; i++)
        {
            ahpResult.Criteria[i].Weight = critResult.Weights[i];
        }

        // 2. Розрахунок локальних пріоритетів альтернатив для кожного критерію
        // L[altIndex, critIndex]
        double[,] localPriorities = new double[altCount, critCount];

        for (int j = 0; j < critCount; j++)
        {
            var crit = criteria[j];
            if (alternativeMatrices.TryGetValue(crit.Id, out var altMatrix))
            {
                var altResult = CalculateMatrix(altMatrix, $"Матриця альтернатив за критерієм: {crit.Name}");
                ahpResult.AlternativeMatrixResults[crit.Id] = altResult;

                for (int i = 0; i < altCount; i++)
                {
                    localPriorities[i, j] = altResult.Weights[i];
                }
            }
            else
            {
                // Якщо матриця для цього критерію відсутня, беремо рівні пріоритети 1/altCount
                for (int i = 0; i < altCount; i++)
                {
                    localPriorities[i, j] = 1.0 / altCount;
                }
            }
        }

        // 3. Ієрархічний синтез глобальних пріоритетів
        for (int i = 0; i < altCount; i++)
        {
            var alt = ahpResult.Alternatives[i];
            double globalScore = 0.0;

            for (int j = 0; j < critCount; j++)
            {
                var crit = criteria[j];
                double locPri = localPriorities[i, j];
                double weight = critResult.Weights[j];
                double weightedScore = locPri * weight;

                alt.LocalPriorities[crit.Id] = locPri;
                alt.WeightedScores[crit.Id] = weightedScore;
                globalScore += weightedScore;
            }

            alt.GlobalPriority = globalScore;
        }

        // 4. Ранжування альтернатив
        ahpResult.RankedAlternatives = ahpResult.Alternatives
            .OrderByDescending(a => a.GlobalPriority)
            .ToList();

        for (int rank = 0; rank < ahpResult.RankedAlternatives.Count; rank++)
        {
            ahpResult.RankedAlternatives[rank].Rank = rank + 1;
        }

        // 5. Композиційна узгодженість ієрархії
        // CI_h = CI_crit + sum(w_j * CI_alt_j)
        // RI_h = RI_crit + sum(w_j * RI_alt_j)
        // CR_h = CI_h / RI_h
        double sumWeightedCIAlt = 0.0;
        double sumWeightedRITableAlt = 0.0;
        double sumWeightedRIEmpAlt = 0.0;

        for (int j = 0; j < critCount; j++)
        {
            var critId = criteria[j].Id;
            double wj = critResult.Weights[j];

            if (ahpResult.AlternativeMatrixResults.TryGetValue(critId, out var altMatrixRes))
            {
                sumWeightedCIAlt += wj * altMatrixRes.CI;
                sumWeightedRITableAlt += wj * altMatrixRes.RITable;
                sumWeightedRIEmpAlt += wj * altMatrixRes.RIEmpirical;
            }
        }

        ahpResult.HierarchicalCI = critResult.CI + sumWeightedCIAlt;
        ahpResult.HierarchicalRITable = critResult.RITable + sumWeightedRITableAlt;
        ahpResult.HierarchicalRIEmpirical = critResult.RIEmpirical + sumWeightedRIEmpAlt;

        ahpResult.HierarchicalCRTable = ahpResult.HierarchicalRITable > 0
            ? ahpResult.HierarchicalCI / ahpResult.HierarchicalRITable
            : 0.0;

        ahpResult.HierarchicalCREmpirical = ahpResult.HierarchicalRIEmpirical > 0
            ? ahpResult.HierarchicalCI / ahpResult.HierarchicalRIEmpirical
            : 0.0;

        return ahpResult;
    }
}
