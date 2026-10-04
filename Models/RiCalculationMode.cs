namespace MAI.Models;

/// <summary>
/// Режим розрахунку випадкового індексу узгодженості (RI) за методом Сааті.
/// </summary>
public enum RiCalculationMode
{
    /// <summary>
    /// Табличне значення Сааті (стандартна таблиця емпіричних середніх значень).
    /// </summary>
    SaatyTable,

    /// <summary>
    /// Емпірична формула Сааті: RI = 1.98 * (n - 2) / n для n > 2.
    /// </summary>
    Empirical
}
