using GitCommands;
using GitCommands.UserRepositoryHistory;

namespace GitExtensions.Avalonia.Services;

internal sealed class RepositoryOpeningService(
    IRepositoryHistoryPort history,
    IRepositoryFolderPicker folderPicker,
    IRepositoryReader reader)
{
    public Task<IList<Repository>> LoadRecentHistoryAsync()
        => history.LoadRecentHistoryAsync();

    public Task<string?> PickFolderAsync(
        string? selectedPath,
        CancellationToken cancellationToken)
        => folderPicker.PickFolderAsync(selectedPath, cancellationToken);

    public async Task<RepositoryPresentation> OpenAsync(string path, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        cancellationToken.ThrowIfCancellationRequested();

        RepositoryPresentation presentation = await reader.ReadAsync(path, cancellationToken);
        await history.AddAsMostRecentAsync(presentation.Path);
        return presentation;
    }
}
