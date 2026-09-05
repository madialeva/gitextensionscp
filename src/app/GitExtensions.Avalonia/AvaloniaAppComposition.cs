using Avalonia.Controls;
using Avalonia.Platform.Storage;
using GitCommands;
using GitExtensions.Avalonia.Localization;
using GitExtensions.Avalonia.Services;
using GitExtUtils;
using GitUI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Threading;

namespace GitExtensions.Avalonia;

internal sealed class AvaloniaAppComposition
{
    private readonly Func<Window?> _mainWindowProvider;

    public AvaloniaAppComposition(Func<Window?> mainWindowProvider)
    {
        ArgumentNullException.ThrowIfNull(mainWindowProvider);
        _mainWindowProvider = mainWindowProvider;
    }

    public IServiceProvider BuildServiceProvider()
    {
        ServiceCollection services = new();
        services.AddAvaloniaServices();
        return services.BuildServiceProvider();
    }

    public void InstallPlatformDelegates(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        AvaloniaLocalizationService localization = serviceProvider.GetRequiredService<AvaloniaLocalizationService>();

        TaskManager.ExceptionReporter = exception =>
        {
            Window? mainWindow = _mainWindowProvider();
            if (mainWindow is null)
            {
                return;
            }

            ExceptionDialog dialog = new(exception, localization);
            dialog.ShowDialog(mainWindow);
        };

        UserMessageHandler.ShowError = (owner, text, caption) =>
        {
            Window? parent = WindowAdapterHelper.ResolveOwner(owner) ?? _mainWindowProvider();
            if (parent is null)
            {
                return;
            }

            ErrorDialog dialog = new(caption ?? localization.Resolve(AvaloniaLocalizationKeys.Error), text ?? "", localization);
            dialog.ShowDialog(parent);
        };

        OsShellUtil.PickFolder = (owner, selectedPath) =>
        {
            Window? parent = WindowAdapterHelper.ResolveOwner(owner) ?? _mainWindowProvider();
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
                            Title = localization.Resolve(AvaloniaLocalizationKeys.SelectFolder),
                            AllowMultiple = false,
                            SuggestedStartLocation = suggestedStart
                        });

                return folders.Count > 0 ? folders[0].Path.LocalPath : null;
            });
        };
    }
}
