using Microsoft.UI.Xaml;

namespace LinkNest.Mobile.WinUI;

/// <summary>
/// Windows platform entry point for the MAUI application.
/// </summary>
public partial class App : MauiWinUIApplication
{
    public App()
    {
        InitializeComponent();
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
