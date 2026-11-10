using System.Reflection;
using System.Windows;
using MAI.Models;
using MAI.Services;

namespace MAI;

/// <summary>
/// Діалогове вікно "Про програму" з описом можливостей, версії та платформи.
/// </summary>
public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        UpdateVersionText();
        LocalizationService.LanguageChanged += OnLanguageChanged;
        Closed += (s, e) => LocalizationService.LanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(AppLanguage lang)
    {
        UpdateVersionText();
    }

    private void UpdateVersionText()
    {
        var ver = Assembly.GetExecutingAssembly().GetName().Version;
        string verStr = ver != null ? $"{ver.Major}.{ver.Minor}.{(ver.Build >= 0 ? ver.Build : 0)}" : "1.3.2";
        TxtAppVersion.Text = LocalizationService.Format("Str_About_VersionFmt", verStr);
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
