namespace MAI.Models;

/// <summary>
/// Метод розрахунку вектора локальних пріоритетів (ваг) матриці Сааті.
/// </summary>
public enum WeightCalculationMethod
{
    /// <summary>
    /// Нормалізація стовпців: a'_ij = a_ij / sum_k(a_kj), w_i = 1/n sum_j(a'_ij) (методичний підхід).
    /// </summary>
    ColumnNormalization,

    /// <summary>
    /// Середнє геометричне елементів рядків (корені n-го степеня / LLSM):
    /// r_i = n-th root(prod_j a_ij), w_i = r_i / sum_k(r_k).
    /// </summary>
    GeometricMean,

    /// <summary>
    /// Головний власний вектор за степеневим методом (класичний точний розв'язок Т. Сааті: A * w = lambda_max * w).
    /// </summary>
    PrincipalEigenvector
}

/// <summary>
/// Метод обчислення максимального власного числа lambda_max.
/// </summary>
public enum LambdaCalculationMethod
{
    /// <summary>
    /// λmax = Σ(сума j-го стовпця · wⱼ) (за методичними рекомендаціями).
    /// </summary>
    ColumnSumProduct,

    /// <summary>
    /// λmax = середнє (A·w)ᵢ / wᵢ.
    /// </summary>
    RowRatioMean,

    /// <summary>
    /// λmax — головне власне число (степеневий метод).
    /// </summary>
    ExactEigenvalue
}

/// <summary>
/// Режим порогу узгодженості для перевірки несуперечливості суджень (CR <= поріг).
/// </summary>
public enum ConsistencyThresholdMode
{
    /// <summary>
    /// Класичний адаптивний поріг Т. Сааті: для n=3 CR <= 0.05, для n=4 CR <= 0.08, для n>=5 CR <= 0.10.
    /// </summary>
    SaatyAdaptive,

    /// <summary>
    /// Фіксований поріг 10% для всіх розмірностей (CR <= 0.10) (методичний підхід).
    /// </summary>
    Uniform10Percent
}

/// <summary>
/// Повний профіль параметрів та алгоритмів математичного розрахунку МАІ.
/// </summary>
public class AhpCalculationOptions
{
    public WeightCalculationMethod WeightMethod { get; set; } = WeightCalculationMethod.ColumnNormalization;
    public LambdaCalculationMethod LambdaMethod { get; set; } = LambdaCalculationMethod.ColumnSumProduct;
    public RiCalculationMode RiMode { get; set; } = RiCalculationMode.Empirical;
    public ConsistencyThresholdMode ThresholdMode { get; set; } = ConsistencyThresholdMode.Uniform10Percent;

    public AhpCalculationOptions()
    {
    }

    public AhpCalculationOptions(
        WeightCalculationMethod weightMethod,
        LambdaCalculationMethod lambdaMethod,
        RiCalculationMode riMode,
        ConsistencyThresholdMode thresholdMode)
    {
        WeightMethod = weightMethod;
        LambdaMethod = lambdaMethod;
        RiMode = riMode;
        ThresholdMode = thresholdMode;
    }

    public AhpCalculationOptions Clone()
    {
        return new AhpCalculationOptions(WeightMethod, LambdaMethod, RiMode, ThresholdMode);
    }

    /// <summary>
    /// Профіль за замовчуванням: за методичними рекомендаціями.
    /// </summary>
    public static AhpCalculationOptions Default => MethodicalProfile;

    /// <summary>
    /// Профіль розрахунку за методичними рекомендаціями:
    /// нормалізація стовпців, колонковий добуток lambda_max, емпіричний RI, фіксований поріг 10%.
    /// </summary>
    public static AhpCalculationOptions MethodicalProfile => new(
        WeightCalculationMethod.ColumnNormalization,
        LambdaCalculationMethod.ColumnSumProduct,
        RiCalculationMode.Empirical,
        ConsistencyThresholdMode.Uniform10Percent);

    /// <summary>
    /// Класичний академічний профіль Томаса Сааті:
    /// власний вектор (степеневий метод), точне lambda_max, табличний RI Сааті, адаптивний поріг 5%/8%/10%.
    /// </summary>
    public static AhpCalculationOptions ClassicSaatyProfile => new(
        WeightCalculationMethod.PrincipalEigenvector,
        LambdaCalculationMethod.ExactEigenvalue,
        RiCalculationMode.SaatyTable,
        ConsistencyThresholdMode.SaatyAdaptive);

    /// <summary>
    /// Профіль геометричного середнього (LLSM / Logarithmic Least Squares):
    /// середнє геометричне рядків, степеневе точне lambda_max, табличний RI Сааті, адаптивний поріг.
    /// </summary>
    public static AhpCalculationOptions GeometricMeanProfile => new(
        WeightCalculationMethod.GeometricMean,
        LambdaCalculationMethod.ExactEigenvalue,
        RiCalculationMode.SaatyTable,
        ConsistencyThresholdMode.SaatyAdaptive);

    /// <summary>
    /// Перевірка чи відповідає поточна конфігурація методичному профілю.
    /// </summary>
    public bool IsMethodicalProfile =>
        WeightMethod == WeightCalculationMethod.ColumnNormalization &&
        LambdaMethod == LambdaCalculationMethod.ColumnSumProduct &&
        RiMode == RiCalculationMode.Empirical &&
        ThresholdMode == ConsistencyThresholdMode.Uniform10Percent;

    /// <summary>
    /// Перевірка чи відповідає поточна конфігурація класичному профілю Сааті.
    /// </summary>
    public bool IsClassicSaatyProfile =>
        WeightMethod == WeightCalculationMethod.PrincipalEigenvector &&
        LambdaMethod == LambdaCalculationMethod.ExactEigenvalue &&
        RiMode == RiCalculationMode.SaatyTable &&
        ThresholdMode == ConsistencyThresholdMode.SaatyAdaptive;

    /// <summary>
    /// Перевірка чи відповідає поточна конфігурація профілю геометричного середнього.
    /// </summary>
    public bool IsGeometricMeanProfile =>
        WeightMethod == WeightCalculationMethod.GeometricMean &&
        LambdaMethod == LambdaCalculationMethod.ExactEigenvalue &&
        RiMode == RiCalculationMode.SaatyTable &&
        ThresholdMode == ConsistencyThresholdMode.SaatyAdaptive;

    public bool ValueEquals(AhpCalculationOptions? other)
    {
        if (other is null) return false;
        return WeightMethod == other.WeightMethod &&
               LambdaMethod == other.LambdaMethod &&
               RiMode == other.RiMode &&
               ThresholdMode == other.ThresholdMode;
    }

    public override bool Equals(object? obj) => obj is AhpCalculationOptions other && ValueEquals(other);

    public override int GetHashCode() => System.HashCode.Combine(WeightMethod, LambdaMethod, RiMode, ThresholdMode);

    public string ProfileName
    {
        get
        {
            if (IsMethodicalProfile) return "Метод. рекомендації";
            if (IsClassicSaatyProfile) return "Класичний метод Т. Сааті";
            if (IsGeometricMeanProfile) return "Класичний, геометричне середнє";
            return "Користувацький профіль";
        }
    }

    /// <summary>
    /// Скорочена назва розрахункового профілю для компактних таблиць.
    /// </summary>
    public string ProfileShortName
    {
        get
        {
            if (IsMethodicalProfile) return "Методичний";
            if (IsClassicSaatyProfile) return "Класичний";
            if (IsGeometricMeanProfile) return "Геом. середнє";
            return "Користувацький";
        }
    }

    public double GetAcceptableCR(int n)
    {
        if (ThresholdMode == ConsistencyThresholdMode.Uniform10Percent) return 0.1001;
        return n switch
        {
            <= 2 => 0.0001,
            3 => 0.0501,
            4 => 0.0801,
            _ => 0.1001
        };
    }

    public string GetThresholdDescription(int n)
    {
        if (ThresholdMode == ConsistencyThresholdMode.Uniform10Percent) return "CR <= 10%";
        return n switch
        {
            <= 2 => "CR = 0% (ідеально)",
            3 => "CR <= 5%",
            4 => "CR <= 8%",
            _ => "CR <= 10%"
        };
    }

    public static string GetLambdaMethodDescription(LambdaCalculationMethod method) => method switch
    {
        LambdaCalculationMethod.ColumnSumProduct => "λmax = Σ(сума j-го стовпця · wⱼ) (за метод. реком.)",
        LambdaCalculationMethod.RowRatioMean => "λmax = середнє (A·w)ᵢ / wᵢ",
        LambdaCalculationMethod.ExactEigenvalue => "λmax — головне власне число (степеневий метод)",
        _ => method.ToString()
    };
}
