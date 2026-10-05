using System;
using System.Collections.Generic;
using System.Linq;
using MAI.Models;

namespace MAI.Services;

/// <summary>
/// Математичне ядро методу аналізу ієрархій (МАІ) Томаса Сааті.
/// Підтримує різні профілі та методи розрахунку ваг і показників узгодженості (ЛР №4, Класичний Сааті).
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
    /// Отримання табличного значення випадкового індексу RI (визначено для n = 1..10).
    /// </summary>
    public static double GetRITable(int n)
    {
        if (SaatyRITable.TryGetValue(n, out double ri))
            return ri;
        return 0.0;
    }

    /// <summary>
    /// Розрахунок випадкового індексу за емпіричною формулою Сааті: RI = 1.98 * (n - 2) / n.
    /// </summary>
    public static double GetRIEmpirical(int n)
    {
        return n > 2 ? 1.98 * (n - 2.0) / n : 0.0;
    }

    /// <summary>
    /// Степеневий метод (Power Iteration) для знаходження головного власного вектора (пріоритетів)
    /// та відповідного найбільшого власного числа lambda_max згідно з класичною теорією Т. Сааті.
    /// A * w = lambda_max * w.
    /// </summary>
    public static (double[] Weights, double LambdaMax) CalculatePrincipalEigenvector(
        double[,] a, int n, int maxIterations = 1000, double tolerance = 1e-12)
    {
        if (n == 0) return ([], 0.0);
        if (n == 1) return ([1.0], 1.0);

        var w = Enumerable.Repeat(1.0 / n, n).ToArray();
        double lambda = n;

        for (int it = 0; it < maxIterations; it++)
        {
            var v = new double[n];
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    v[i] += a[i, j] * w[j];
                }
            }

            double s = v.Sum();
            if (s <= 0 || double.IsNaN(s) || double.IsInfinity(s)) break;

            var next = new double[n];
            for (int i = 0; i < n; i++) next[i] = v[i] / s;

            double diff = 0.0;
            for (int i = 0; i < n; i++)
            {
                double d = Math.Abs(next[i] - w[i]);
                if (d > diff) diff = d;
            }

            w = next;
            lambda = s;

            if (diff < tolerance) break;
        }

        return (w, lambda);
    }

    /// <summary>
    /// Розрахунок ваг методом середнього геометричного рядків (коренів n-го степеня / LLSM):
    /// r_i = n-th root(prod_j a_ij), w_i = r_i / sum_k(r_k).
    /// </summary>
    public static double[] CalculateGeometricMeanWeights(double[,] a, int n)
    {
        if (n == 0) return [];
        if (n == 1) return [1.0];

        var w = new double[n];
        double sum = 0.0;

        for (int i = 0; i < n; i++)
        {
            double prod = 1.0;
            for (int j = 0; j < n; j++)
            {
                prod *= a[i, j];
            }
            w[i] = Math.Pow(prod, 1.0 / n);
            sum += w[i];
        }

        if (sum > 0)
        {
            for (int i = 0; i < n; i++) w[i] /= sum;
        }

        return w;
    }

    /// <summary>
    /// Обчислення локальних пріоритетів та характеристик узгодженості для однієї матриці Сааті
    /// згідно із заданими опціями розрахунку (AhpCalculationOptions).
    /// </summary>
    public static MatrixResult CalculateMatrix(
        ComparisonMatrix matrix,
        string matrixName = "",
        AhpCalculationOptions? options = null)
    {
        var opts = options ?? AhpCalculationOptions.ClassicSaatyProfile;
        int n = matrix.Size;

        var result = new MatrixResult
        {
            Name = matrixName,
            Size = n,
            Labels = new List<string>(matrix.Labels),
            Weights = new double[n],
            NormalizedMatrix = new double[n, n],
            ColumnSums = new double[n],
            Options = opts
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

        // 2. Нормалізація матриці (завжди виконується для візуалізації в таблицях)
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                result.NormalizedMatrix[i, j] = result.ColumnSums[j] > 0
                    ? matrix.Values[i, j] / result.ColumnSums[j]
                    : 1.0 / n;
            }
        }

        // 3. Розрахунок вектора пріоритетів (ваг) згідно з обраним методом
        double eigenLambda = n;
        switch (opts.WeightMethod)
        {
            case WeightCalculationMethod.ColumnNormalization:
                for (int i = 0; i < n; i++)
                {
                    double rowSum = 0.0;
                    for (int j = 0; j < n; j++) rowSum += result.NormalizedMatrix[i, j];
                    result.Weights[i] = rowSum / n;
                }
                break;

            case WeightCalculationMethod.GeometricMean:
                result.Weights = CalculateGeometricMeanWeights(matrix.Values, n);
                break;

            case WeightCalculationMethod.PrincipalEigenvector:
            default:
                var (eigenW, lam) = CalculatePrincipalEigenvector(matrix.Values, n);
                result.Weights = eigenW;
                eigenLambda = lam;
                break;
        }

        // 4. Обчислення найбільшого власного числа lambda_max
        switch (opts.LambdaMethod)
        {
            case LambdaCalculationMethod.ColumnSumProduct:
                // Метод Лабораторної роботи №4: lambda_max = sum(сума_стовпця_j * w_j)
                double colProductSum = 0.0;
                for (int j = 0; j < n; j++)
                {
                    colProductSum += result.ColumnSums[j] * result.Weights[j];
                }
                result.LambdaMax = colProductSum;
                break;

            case LambdaCalculationMethod.RowRatioMean:
                // Середнє відношень векторів: 1/n sum_i ((Aw)_i / w_i)
                double ratioSum = 0.0;
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
                        ratioSum += vi / result.Weights[i];
                        validRows++;
                    }
                }
                result.LambdaMax = validRows > 0 ? ratioSum / validRows : n;
                break;

            case LambdaCalculationMethod.ExactEigenvalue:
            default:
                if (opts.WeightMethod == WeightCalculationMethod.PrincipalEigenvector)
                {
                    result.LambdaMax = eigenLambda;
                }
                else
                {
                    // Для sum(w)=1 сума (Aw)_i є точною оцінкою
                    double vSum = 0.0;
                    for (int i = 0; i < n; i++)
                    {
                        for (int j = 0; j < n; j++)
                        {
                            vSum += matrix.Values[i, j] * result.Weights[j];
                        }
                    }
                    result.LambdaMax = vSum;
                }
                break;
        }

        // Захист: lambda_max не може бути строго менше n для зворотно-симетричної матриці Сааті
        if (result.LambdaMax < n)
        {
            result.LambdaMax = n;
        }

        // 5. Індекс узгодженості CI = (lambda_max - n) / (n - 1)
        double ciRaw = (result.LambdaMax - n) / (n - 1);
        result.CI = Math.Max(0.0, ciRaw);

        // 6. Випадкові індекси RI
        result.RITable = GetRITable(n);
        result.RIEmpirical = GetRIEmpirical(n);

        // 7. Коефіцієнти узгодженості CR = CI / RI
        result.CRTable = result.RITable > 0 ? result.CI / result.RITable : 0.0;
        result.CREmpirical = result.RIEmpirical > 0 ? result.CI / result.RIEmpirical : 0.0;

        return result;
    }

    /// <summary>
    /// Перевантаження для зворотної сумісності (за RiCalculationMode).
    /// </summary>
    public static MatrixResult CalculateMatrix(
        ComparisonMatrix matrix,
        string matrixName,
        RiCalculationMode riMode)
    {
        var opts = riMode == RiCalculationMode.Empirical
            ? AhpCalculationOptions.Lab4Profile
            : AhpCalculationOptions.ClassicSaatyProfile;
        return CalculateMatrix(matrix, matrixName, opts);
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
        Dictionary<string, ComparisonMatrix> alternativeMatrices,
        AhpCalculationOptions? options = null)
    {
        var opts = options ?? AhpCalculationOptions.ClassicSaatyProfile;

        var ahpResult = new AhpResult
        {
            Criteria = criteria.Select(c => new Criterion(c.Id, c.Name, c.Description)).ToList(),
            Alternatives = alternatives.Select(a => new Alternative(a.Id, a.Name, a.Description)).ToList(),
            Options = opts
        };

        int critCount = criteria.Count;
        int altCount = alternatives.Count;

        if (critCount == 0 || altCount == 0)
            return ahpResult;

        // 1. Розрахунок матриці критеріїв
        var critResult = CalculateMatrix(criteriaMatrix, "Матриця порівняння критеріїв", opts);
        ahpResult.CriteriaMatrixResult = critResult;

        for (int i = 0; i < critCount; i++)
        {
            ahpResult.Criteria[i].Weight = critResult.Weights[i];
        }

        // 2. Розрахунок локальних пріоритетів альтернатив для кожного критерію
        double[,] localPriorities = new double[altCount, critCount];

        for (int j = 0; j < critCount; j++)
        {
            var crit = criteria[j];
            if (!alternativeMatrices.TryGetValue(crit.Id, out var altMatrix) || altMatrix == null)
            {
                throw new InvalidOperationException($"Матриця попарних порівнянь альтернатив для критерію «{crit.Name}» ({crit.Id}) відсутня.");
            }

            if (altMatrix.Size != altCount)
            {
                throw new InvalidOperationException($"Розмірність матриці альтернатив для критерію «{crit.Name}» ({altMatrix.Size}) не відповідає кількості альтернатив ({altCount}).");
            }

            var altResult = CalculateMatrix(altMatrix, $"Матриця альтернатив за критерієм: {crit.Name}", opts);
            ahpResult.AlternativeMatrixResults[crit.Id] = altResult;

            for (int i = 0; i < altCount; i++)
            {
                localPriorities[i, j] = altResult.Weights[i];
            }
        }

        // 3. Ієрархічний синтез глобальних пріоритетів: P = L * w
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

    /// <summary>
    /// Перевантаження для зворотної сумісності (за RiCalculationMode).
    /// </summary>
    public static AhpResult SolveHierarchy(
        List<Criterion> criteria,
        List<Alternative> alternatives,
        ComparisonMatrix criteriaMatrix,
        Dictionary<string, ComparisonMatrix> alternativeMatrices,
        RiCalculationMode riMode)
    {
        var opts = riMode == RiCalculationMode.Empirical
            ? AhpCalculationOptions.Lab4Profile
            : AhpCalculationOptions.ClassicSaatyProfile;
        return SolveHierarchy(criteria, alternatives, criteriaMatrix, alternativeMatrices, opts);
    }
}
