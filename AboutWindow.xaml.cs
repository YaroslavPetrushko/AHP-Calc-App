using System.Reflection;
using System.Windows;

namespace MAI;

/// <summary>
/// Діалогове вікно "Про програму" з описом можливостей, версії та платформи.
/// </summary>
public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        var ver = Assembly.GetExecutingAssembly().GetName().Version;
        string verStr = ver != null ? $"{ver.Major}.{ver.Minor}.{(ver.Build >= 0 ? ver.Build : 0)}" : "1.2.1";
        TxtAppVersion.Text = $"Версія {verStr}";
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
