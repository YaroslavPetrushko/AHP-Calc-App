using System.Windows;
using MAI.Models;
using MAI.Services;

namespace MAI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        LocalizationService.ApplyLanguage(AppLanguage.Uk);
        base.OnStartup(e);
    }
}