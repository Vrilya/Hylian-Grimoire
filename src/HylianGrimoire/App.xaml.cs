using Microsoft.UI.Xaml;

namespace HylianGrimoire;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        AppDiagnostics.Initialize(this);
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
