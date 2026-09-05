using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GitExtUtils;
using GitUI;
using Microsoft.VisualStudio.Threading;

namespace GitExtensions.Avalonia;

public partial class App : Application
{
    internal static IServiceProvider ServiceProvider { get; private set; } = null!;

    internal static Window? MainWindow { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        ThreadHelper.JoinableTaskContext = new JoinableTaskContext();

        AvaloniaAppComposition composition = new(() => MainWindow);
        ServiceProvider = composition.BuildServiceProvider();
        composition.InstallPlatformDelegates(ServiceProvider);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
            MainWindow = desktop.MainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
