using System;
using System.Globalization;
using System.Windows;
using MAI.Models;

namespace MAI.Services;

/// <summary>
/// Сервіс керування та динамічного перемикання мови інтерфейсу та звітів (Українська / English).
/// Оновлює ресурси рядків у Application.Current.Resources для DynamicResource прив'язок.
/// </summary>
public static class LocalizationService
{
    private static readonly CultureInfo UkCulture = new("uk-UA");
    private static readonly CultureInfo EnCulture = new("en-US");

    /// <summary>
    /// Поточна активна мова інтерфейсу (за замовчуванням завжди Українська).
    /// </summary>
    public static AppLanguage CurrentLanguage { get; private set; } = AppLanguage.Uk;

    /// <summary>
    /// Активна культура форматування чисел та дат відповідно до обраної мови.
    /// </summary>
    public static CultureInfo ActiveCulture => CurrentLanguage == AppLanguage.En ? EnCulture : UkCulture;

    /// <summary>
    /// Подія зміни мови інтерфейсу.
    /// </summary>
    public static event Action<AppLanguage>? LanguageChanged;

    /// <summary>
    /// Застосування обраної мови: оновлення Application.Current.Resources та сповіщення підписників.
    /// </summary>
    public static void ApplyLanguage(AppLanguage language)
    {
        CurrentLanguage = language;

        var res = Application.Current?.Resources;
        if (res != null)
        {
            var dict = LocalizationStrings.GetDictionary(language);
            foreach (var (key, value) in dict)
            {
                res[key] = value;
            }
        }

        LanguageChanged?.Invoke(language);
    }

    /// <summary>
    /// Отримання локалізованого рядка за ключем для поточної мови з безпечним поверненням до Uk та самого ключа.
    /// Безпечно працює в юніт-тестах без Application.Current.
    /// </summary>
    public static string Get(string key)
    {
        var dict = LocalizationStrings.GetDictionary(CurrentLanguage);
        if (dict.TryGetValue(key, out var value))
        {
            return value;
        }

        if (LocalizationStrings.Uk.TryGetValue(key, out var ukValue))
        {
            return ukValue;
        }

        return key;
    }

    /// <summary>
    /// Форматування локалізованого рядка з аргументами з урахуванням активної культури.
    /// </summary>
    public static string Format(string key, params object?[] args)
    {
        return string.Format(ActiveCulture, Get(key), args);
    }

    /// <summary>
    /// Форматування інтерпольованого рядка FormattableString за активною культурою.
    /// </summary>
    public static string Fmt(FormattableString fs) => fs.ToString(ActiveCulture);
}
