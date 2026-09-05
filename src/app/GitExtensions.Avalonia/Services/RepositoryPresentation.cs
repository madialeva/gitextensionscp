namespace GitExtensions.Avalonia.Services;

internal sealed record RepositoryPresentation(
    string Path,
    string Branch,
    IReadOnlyList<string> Remotes,
    int WorkingTreeChanges)
{
    public string RemoteSummary => Remotes.Count == 0 ? "No remotes" : string.Join(", ", Remotes);

    public string WorkingTreeSummary => WorkingTreeChanges == 0
        ? "Working tree clean"
        : $"{WorkingTreeChanges} working tree change(s)";
}

internal interface IRepositoryHistoryPort
{
    Task<IList<GitCommands.UserRepositoryHistory.Repository>> LoadRecentHistoryAsync();

    Task<IList<GitCommands.UserRepositoryHistory.Repository>> AddAsMostRecentAsync(string path);
}

internal interface IRepositoryFolderPicker
{
    Task<string?> PickFolderAsync(string? selectedPath, CancellationToken cancellationToken);
}

internal interface IRepositoryReader
{
    Task<RepositoryPresentation> ReadAsync(string path, CancellationToken cancellationToken);
}
