using Avalonia.Controls;
using Avalonia.Platform.Storage;
using GitCommands;
using GitCommands.Git;
using GitCommands.UserRepositoryHistory;
using GitExtensions.Extensibility.Git;

namespace GitExtensions.Avalonia.Services;

internal sealed class ProductionRepositoryHistoryPort : IRepositoryHistoryPort
{
    public Task<IList<Repository>> LoadRecentHistoryAsync()
        => RepositoryHistoryManager.Locals.LoadRecentHistoryAsync();

    public Task<IList<Repository>> AddAsMostRecentAsync(string path)
        => RepositoryHistoryManager.Locals.AddAsMostRecentAsync(path);
}

internal sealed class ProductionRepositoryFolderPicker(Func<Window?> mainWindowProvider) : IRepositoryFolderPicker
{
    public async Task<string?> PickFolderAsync(string? selectedPath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Window? window = mainWindowProvider();
        if (window is null)
        {
            return null;
        }

        IStorageFolder? suggestedStart = selectedPath is not null
            ? await window.StorageProvider.TryGetFolderFromPathAsync(selectedPath)
            : null;
        IReadOnlyList<IStorageFolder> folders = await window.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = "Select folder",
                AllowMultiple = false,
                SuggestedStartLocation = suggestedStart
            });

        cancellationToken.ThrowIfCancellationRequested();
        return folders.Count > 0 ? folders[0].Path.LocalPath : null;
    }
}

internal sealed class GitRepositoryReader(IGitExecutorProvider executorProvider) : IRepositoryReader
{
    public async Task<RepositoryPresentation> ReadAsync(string path, CancellationToken cancellationToken)
    {
        string normalizedPath = Path.GetFullPath(path);
        GitModule module = new(executorProvider, normalizedPath);
        cancellationToken.ThrowIfCancellationRequested();

        if (!module.IsValidGitWorkingDir())
        {
            throw new InvalidOperationException($"The selected folder is not a Git repository: {normalizedPath}");
        }

        string branch = await Task.Run(module.GetCurrentBranchName, cancellationToken);
        IReadOnlyList<Remote> remotes = await module.GetRemotesAsync();
        int workingTreeChanges = await Task.Run(() => module.GetWorkTreeFiles().Count, cancellationToken);

        return new RepositoryPresentation(
            normalizedPath,
            string.IsNullOrEmpty(branch) ? "(detached HEAD)" : branch,
            remotes.Select(remote => remote.Name).Distinct(StringComparer.Ordinal).ToArray(),
            workingTreeChanges);
    }
}
