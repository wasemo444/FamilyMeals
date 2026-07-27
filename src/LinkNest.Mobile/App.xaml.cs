namespace LinkNest.Mobile;

/// <summary>
/// Application lifecycle entry for the MAUI host.
/// </summary>
public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        new Window(new MainPage()) { Title = "LinkNest" };
}
