using System;
using System.Collections.Generic;
using System.Linq;
using MAI.Models;

namespace MAI.Services;

/// <summary>
/// Математичне ядро методу аналізу ієрархій (МАІ) Томаса Сааті.
/// Підтримує різні профілі та методи розрахунку ваг і показників узгодженості (методичний підхід, Класичний Сааті).
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

        if (n > 10 && opts.RiMode == RiCalculationMode.SaatyTable)
        {
            throw new InvalidOperationException("Табличний RI визначений для n ≤ 10; використайте емпіричну формулу.");
        }

        var result = new MatrixResult
        {
            Name = matrixName,
            Size = n,
            Labels = new List<string>(matrix.Labels),
            PairwiseMatrix = (double[,])matrix.Values.Clone(),
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
                // Методичний підхід: lambda_max = sum(сума_стовпця_j * w_j)
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
                    var (_, lam) = CalculatePrincipalEigenvector(matrix.Values, n);
                    result.LambdaMax = lam;
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
            ? AhpCalculationOptions.MethodicalProfile
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

        if ((critCount > 10 || altCount > 10) && opts.RiMode == RiCalculationMode.SaatyTable)
        {
            throw new InvalidOperationException("Табличний RI визначений для n ≤ 10; використайте емпіричну формулу.");
        }

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

        // 4. Ранжування альтернатив з допуском 1e-9 («змагальна» нумерація: 1, 2, 2, 4)
        ahpResult.RankedAlternatives = ahpResult.Alternatives
            .OrderByDescending(a => a.GlobalPriority)
            .ToList();

        for (int rank = 0; rank < ahpResult.RankedAlternatives.Count; rank++)
        {
            var alt = ahpResult.RankedAlternatives[rank];
            double p = alt.GlobalPriority;

            int higherCount = ahpResult.RankedAlternatives.Count(other => other.GlobalPriority > p + 1e-9);
            alt.Rank = 1 + higherCount;

            bool isTied = ahpResult.RankedAlternatives.Any(other => !ReferenceEquals(other, alt) && Math.Abs(other.GlobalPriority - p) <= 1e-9);
            alt.RankText = isTied ? $"{alt.Rank}=" : alt.Rank.ToString();
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
            ? AhpCalculationOptions.MethodicalProfile
            : AhpCalculationOptions.ClassicSaatyProfile;
        return SolveHierarchy(criteria, alternatives, criteriaMatrix, alternativeMatrices, opts);
    }

    /// <summary>
    /// Розрахунок аналізу чутливості ієрархічного синтезу за обраним критерієм.
    /// Моделює пропорційне масштабування ваг решти критеріїв при зміні ваги t in [0, 1].
    /// </summary>
    public static CriterionSensitivityResult CalculateSensitivity(AhpResult result, string criterionId)
    {
        var crit = result.Criteria.FirstOrDefault(c => c.Id == criterionId)
            ?? throw new ArgumentException($"Критерій з ID '{criterionId}' не знайдено в результатах.");

        var res = new CriterionSensitivityResult
        {
            Criterion = crit,
            OriginalWeight = crit.Weight
        };

        if (result.RankedAlternatives.Count > 0)
        {
            res.OptimalAlternativeId = result.RankedAlternatives[0].Id;
            res.OptimalAlternativeName = result.RankedAlternatives[0].Name;
        }

        double wk0 = crit.Weight;
        bool isWk0Edge = Math.Abs(wk0 - 1.0) < 1e-6;

        // 1. Побудова лінійних функцій P_i(t) = alpha_i * t + beta_i
        foreach (var alt in result.Alternatives)
        {
            int altIdx = result.Alternatives.IndexOf(alt);
            double Lik = result.AlternativeMatrixResults.TryGetValue(criterionId, out var altMatrixRes) && altIdx < altMatrixRes.Weights.Length
                ? altMatrixRes.Weights[altIdx]
                : 0.0;

            double beta;
            double alpha;

            if (isWk0Edge)
            {
                beta = 0.0;
                alpha = Lik;
            }
            else
            {
                double Si = 0.0;
                foreach (var otherCrit in result.Criteria)
                {
                    if (otherCrit.Id == criterionId) continue;
                    if (result.AlternativeMatrixResults.TryGetValue(otherCrit.Id, out var otherMat) && altIdx < otherMat.Weights.Length)
                    {
                        Si += otherMat.Weights[altIdx] * otherCrit.Weight;
                    }
                }
                beta = Si / (1.0 - wk0);
                alpha = Lik - beta;
            }

            res.Lines.Add(new SensitivityLine
            {
                AlternativeId = alt.Id,
                AlternativeName = alt.Name,
                Alpha = alpha,
                Beta = beta,
                OriginalPriority = alt.GlobalPriority,
                LocalPriorityUnderCriterion = Lik
            });
        }

        // 2. Попарні точки перетину функцій: alpha_i * t + beta_i = alpha_j * t + beta_j
        var rawIntersections = new List<CriticalIntersectionPoint>();
        for (int i = 0; i < res.Lines.Count; i++)
        {
            for (int j = i + 1; j < res.Lines.Count; j++)
            {
                var line1 = res.Lines[i];
                var line2 = res.Lines[j];
                double dAlpha = line1.Alpha - line2.Alpha;
                double dBeta = line2.Beta - line1.Beta;

                if (Math.Abs(dAlpha) > 1e-6)
                {
                    double t = dBeta / dAlpha;
                    // Відкидаємо перетини з t <= 1e-6 або t >= 1 - 1e-6 (збіг на межі інтервалу)
                    if (t > 1e-6 && t < 1.0 - 1e-6)
                    {
                        double p = line1.PriorityAt(t);

                        // Точна умова вершини: p >= maxP - 1e-9
                        double maxP = res.Lines.Max(l => l.PriorityAt(t));
                        bool isOnTop = p >= maxP - 1e-9;

                        bool isLeaderChange = false;
                        string? oldLeaderId = null, oldLeaderName = null;
                        string? newLeaderId = null, newLeaderName = null;

                        if (isOnTop)
                        {
                            // Визначаємо лідера зліва та справа від t
                            double eps = 1e-6;
                            double leftT = Math.Max(0.0, t - eps);
                            double rightT = Math.Min(1.0, t + eps);

                            int bestLeftIdx = 0;
                            double bestLeftP = res.Lines[0].PriorityAt(leftT);
                            for (int k = 1; k < res.Lines.Count; k++)
                            {
                                double pk = res.Lines[k].PriorityAt(leftT);
                                if (pk > bestLeftP + 1e-9)
                                {
                                    bestLeftP = pk;
                                    bestLeftIdx = k;
                                }
                            }

                            int bestRightIdx = 0;
                            double bestRightP = res.Lines[0].PriorityAt(rightT);
                            for (int k = 1; k < res.Lines.Count; k++)
                            {
                                double pk = res.Lines[k].PriorityAt(rightT);
                                if (pk > bestRightP + 1e-9)
                                {
                                    bestRightP = pk;
                                    bestRightIdx = k;
                                }
                            }

                            if (bestLeftIdx != bestRightIdx)
                            {
                                isLeaderChange = true;
                                oldLeaderId = res.Lines[bestLeftIdx].AlternativeId;
                                oldLeaderName = res.Lines[bestLeftIdx].AlternativeName;
                                newLeaderId = res.Lines[bestRightIdx].AlternativeId;
                                newLeaderName = res.Lines[bestRightIdx].AlternativeName;
                            }
                        }

                        rawIntersections.Add(new CriticalIntersectionPoint
                        {
                            Alt1Id = line1.AlternativeId,
                            Alt1Name = line1.AlternativeName,
                            Alt2Id = line2.AlternativeId,
                            Alt2Name = line2.AlternativeName,
                            WeightT = t,
                            PriorityP = p,
                            IsLeadershipChange = isLeaderChange,
                            OldLeaderId = oldLeaderId,
                            OldLeaderName = oldLeaderName,
                            NewLeaderId = newLeaderId,
                            NewLeaderName = newLeaderName,
                            EventDescription = isLeaderChange
                                ? $"Зміна лідера (#1): «{oldLeaderName}» ➔ «{newLeaderName}»"
                                : $"Перетин рейтингів між «{line1.AlternativeName}» та «{line2.AlternativeName}»"
                        });
                    }
                }
            }
        }

        res.Intersections = rawIntersections
            .OrderBy(p => p.WeightT)
            .ThenByDescending(p => p.IsLeadershipChange)
            .ToList();

        // 3. Інтервали лідерства на відрізку [0, 1]
        // контрольні точки = {0, 1} U {t усіх перетинів, що є зміною лідера}
        var controlPoints = new List<double> { 0.0, 1.0 };
        foreach (var p in res.Intersections)
        {
            if (p.IsLeadershipChange)
            {
                controlPoints.Add(p.WeightT);
            }
        }
        controlPoints.Sort();

        var distinctPoints = new List<double>();
        foreach (var pt in controlPoints)
        {
            if (distinctPoints.Count == 0 || Math.Abs(pt - distinctPoints[^1]) > 1e-6)
            {
                distinctPoints.Add(pt);
            }
        }

        for (int k = 0; k < distinctPoints.Count - 1; k++)
        {
            double fromT = distinctPoints[k];
            double toT = distinctPoints[k + 1];
            if (toT - fromT < 1e-6) continue;

            double midT = (fromT + toT) / 2.0;

            int bestIdx = 0;
            double bestP = res.Lines[0].PriorityAt(midT);
            for (int i = 1; i < res.Lines.Count; i++)
            {
                double p = res.Lines[i].PriorityAt(midT);
                if (p > bestP + 1e-9)
                {
                    bestP = p;
                    bestIdx = i;
                }
            }
            var leader = res.Lines[bestIdx];

            if (res.LeadershipIntervals.Count > 0 &&
                res.LeadershipIntervals[^1].AlternativeId == leader.AlternativeId)
            {
                res.LeadershipIntervals[^1].ToT = toT;
            }
            else
            {
                res.LeadershipIntervals.Add(new LeadershipInterval
                {
                    AlternativeId = leader.AlternativeId,
                    AlternativeName = leader.AlternativeName,
                    FromT = fromT,
                    ToT = toT
                });
            }
        }

        // 4. Визначення діапазону стійкості для поточного оптимального вибору
        var currentOptInterval = res.LeadershipIntervals.FirstOrDefault(intvl =>
            wk0 >= intvl.FromT - 1e-6 && wk0 <= intvl.ToT + 1e-6);

        if (currentOptInterval != null)
        {
            double lo = currentOptInterval.FromT;
            double hi = currentOptInterval.ToT;

            res.StabilityIntervalMin = lo;
            res.StabilityIntervalMax = hi;

            bool isLoZero = lo <= 1e-6;
            bool isHiOne = hi >= 1.0 - 1e-6;

            if (!isLoZero)
            {
                res.LowerMarginPp = (wk0 - lo) * 100.0;
                var prevInterval = res.LeadershipIntervals.FirstOrDefault(i => Math.Abs(i.ToT - lo) < 1e-6);
                res.LowerSuccessorName = prevInterval?.AlternativeName;
            }
            else
            {
                res.LowerMarginPp = null;
                res.LowerSuccessorName = null;
            }

            if (!isHiOne)
            {
                res.UpperMarginPp = (hi - wk0) * 100.0;
                var nextInterval = res.LeadershipIntervals.FirstOrDefault(i => Math.Abs(i.FromT - hi) < 1e-6);
                res.UpperSuccessorName = nextInterval?.AlternativeName;
            }
            else
            {
                res.UpperMarginPp = null;
                res.UpperSuccessorName = null;
            }

            double loPct = lo * 100.0;
            double hiPct = hi * 100.0;
            double w0Pct = wk0 * 100.0;

            string marginPart;
            if (res.LowerMarginPp.HasValue && res.UpperMarginPp.HasValue)
            {
                marginPart = $"Запас стійкості: −{res.LowerMarginPp.Value:0.0} п.п. вниз, +{res.UpperMarginPp.Value:0.0} п.п. вгору.";
            }
            else if (res.LowerMarginPp.HasValue)
            {
                marginPart = $"Запас стійкості: −{res.LowerMarginPp.Value:0.0} п.п. вниз.";
            }
            else if (res.UpperMarginPp.HasValue)
            {
                marginPart = $"Запас стійкості: +{res.UpperMarginPp.Value:0.0} п.п. вгору.";
            }
            else
            {
                marginPart = "Запас стійкості: абсолютний на всьому інтервалі.";
            }

            string successorPart = "";
            if (!string.IsNullOrEmpty(res.LowerSuccessorName) && !string.IsNullOrEmpty(res.UpperSuccessorName))
            {
                successorPart = $" Нижче {loPct:0.0} % лідером стає «{res.LowerSuccessorName}», вище {hiPct:0.0} % — «{res.UpperSuccessorName}».";
            }
            else if (!string.IsNullOrEmpty(res.LowerSuccessorName))
            {
                successorPart = $" Нижче {loPct:0.0} % лідером стає «{res.LowerSuccessorName}».";
            }
            else if (!string.IsNullOrEmpty(res.UpperSuccessorName))
            {
                successorPart = $" Вище {hiPct:0.0} % лідером стає «{res.UpperSuccessorName}».";
            }

            res.StabilitySummary = $"Лідер «{currentOptInterval.AlternativeName}» зберігається, поки вага критерію «{crit.Name}» лежить у межах {loPct:0.0} %–{hiPct:0.0} % (поточна {w0Pct:0.0} %). {marginPart}{successorPart}";
        }
        else
        {
            res.StabilityIntervalMin = 0.0;
            res.StabilityIntervalMax = 1.0;
            res.LowerMarginPp = null;
            res.UpperMarginPp = null;
            res.LowerSuccessorName = null;
            res.UpperSuccessorName = null;
            res.StabilitySummary = $"Поточний вибір залишається лідером на всьому діапазоні ваг критерію «{crit.Name}».";
        }

        return res;
    }

    /// <summary>
    /// Знаходить пару порівнянь (i < j), яка вносить найбільше логарифмічне відхилення
    /// від узгодженого співвідношення ваг w_i / w_j.
    /// Метрика: |ln(a_ij * w_j / w_i)| лише для i < j.
    /// Implied = w_i / w_j.
    /// Recommended — значення шкали Сааті, найближче до Implied у логарифмічному масштабі.
    /// </summary>
    public static (int Row, int Col, double Current, double Implied, double Recommended)? FindMostInconsistentCell(
        ComparisonMatrix matrix,
        double[] weights)
    {
        if (matrix == null || weights == null || matrix.Size < 3 || weights.Length != matrix.Size)
            return null;

        int n = matrix.Size;
        double maxError = -1.0;
        int bestI = -1;
        int bestJ = -1;

        for (int i = 0; i < n; i++)
        {
            for (int j = i + 1; j < n; j++)
            {
                double aij = matrix.Values[i, j];
                double wi = weights[i];
                double wj = weights[j];

                if (aij <= 0.0 || wi <= 0.0 || wj <= 0.0)
                    continue;

                // Метрика: |ln(a_ij * w_j / w_i)|
                double error = Math.Abs(Math.Log(aij * wj / wi));
                if (error > maxError)
                {
                    maxError = error;
                    bestI = i;
                    bestJ = j;
                }
            }
        }

        if (bestI < 0 || bestJ < 0 || maxError < 1e-6)
            return null;

        double current = matrix.Values[bestI, bestJ];
        double implied = weights[bestI] / weights[bestJ];
        double recommended = GetClosestSaatyScaleValueLog(implied);

        return (bestI, bestJ, current, implied, recommended);
    }

    /// <summary>
    /// Знаходить значення шкали Сааті, найближче до x у логарифмічному масштабі: min |ln(x) - ln(s)|.
    /// </summary>
    public static double GetClosestSaatyScaleValueLog(double implied)
    {
        if (implied <= 0.0) return 1.0;

        double logVal = Math.Log(implied);
        double closest = ComparisonMatrix.SaatyScale[0];
        double minDiff = Math.Abs(logVal - Math.Log(closest));

        for (int i = 1; i < ComparisonMatrix.SaatyScale.Length; i++)
        {
            double s = ComparisonMatrix.SaatyScale[i];
            double diff = Math.Abs(logVal - Math.Log(s));
            if (diff < minDiff)
            {
                minDiff = diff;
                closest = s;
            }
        }

        return closest;
    }
}
