using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using GitCommands;
using GitExtensions.Avalonia.Services;
using GitExtUtils;
using GitUI;
using Microsoft.Extensions.DependencyInjection;
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
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            ThreadHelper.JoinableTaskContext = new JoinableTaskContext();

            ServiceCollection services = new();
            services.AddAvaloniaServices();
            ServiceProvider = services.BuildServiceProvider();

            TaskManager.ExceptionReporter = ex =>
            {
                ExceptionDialog dialog = new(ex);
                dialog.ShowDialog(MainWindow!);
            };

            UserMessageHandler.ShowError = (owner, text, caption) =>
            {
                ErrorDialog dialog = new(caption ?? "Error", text ?? "");
                Window? parent = WindowAdapterHelper.ResolveOwner(owner) ?? MainWindow;
                dialog.ShowDialog(parent!);
            };

            OsShellUtil.PickFolder = (owner, selectedPath) =>
            {
                Window? parent = WindowAdapterHelper.ResolveOwner(owner) ?? MainWindow;
                if (parent is null)
                {
                    return null;
                }

                return ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    IStorageFolder? suggestedStart = selectedPath is not null
                        ? await parent.StorageProvider.TryGetFolderFromPathAsync(selectedPath)
                        : null;

                    IReadOnlyList<IStorageFolder> folders =
                        await parent.StorageProvider.OpenFolderPickerAsync(
                            new FolderPickerOpenOptions
                            {
                                Title = "Select folder",
                                AllowMultiple = false,
                                SuggestedStartLocation = suggestedStart
                            });

                    return folders.Count > 0 ? folders[0].Path.LocalPath : null;
                });
            };

            desktop.MainWindow = new MainWindow();
            MainWindow = desktop.MainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
