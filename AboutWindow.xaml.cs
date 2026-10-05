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
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
