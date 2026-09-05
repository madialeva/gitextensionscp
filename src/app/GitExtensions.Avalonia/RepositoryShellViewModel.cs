using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitCommands.UserRepositoryHistory;
using GitExtensions.Avalonia.Services;

namespace GitExtensions.Avalonia;

internal sealed partial class RepositoryShellViewModel(RepositoryOpeningService openingService) : ObservableObject
{
    private CancellationTokenSource? _openingCancellation;
    private string? _lastPath;

    public ObservableCollection<Repository> RecentRepositories { get; } = [];

    [ObservableProperty]
    private RepositoryPresentation? _activeRepository;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isWelcome = true;

    [ObservableProperty]
    private string _statusMessage = "No repository open";

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public bool HasRepository => ActiveRepository is not null;

    public async Task InitializeAsync()
    {
        RecentRepositories.Clear();
        foreach (Repository repository in await openingService.LoadRecentHistoryAsync())
        {
            RecentRepositories.Add(repository);
        }
    }

    partial void OnActiveRepositoryChanged(RepositoryPresentation? value)
        => OnPropertyChanged(nameof(HasRepository));

    partial void OnErrorMessageChanged(string? value)
        => OnPropertyChanged(nameof(HasError));

    [RelayCommand]
    private Task OpenFolderAsync()
        => OpenPickedFolderAsync();

    [RelayCommand]
    private Task OpenRecentAsync(Repository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        return OpenPathAsync(repository.Path);
    }

    [RelayCommand]
    private Task RetryAsync()
        => _lastPath is null ? OpenPickedFolderAsync() : OpenPathAsync(_lastPath);

    [RelayCommand]
    private void ShowWelcome()
    {
        ActiveRepository = null;
        IsWelcome = true;
        ErrorMessage = null;
        StatusMessage = "No repository open";
    }

    [RelayCommand]
    private void ShowRepositoryInformation()
    {
        if (ActiveRepository is null)
        {
            return;
        }

        IsWelcome = false;
        ErrorMessage = null;
        StatusMessage = ActiveRepository.Path;
    }

    private async Task OpenPickedFolderAsync()
    {
        await OpenPathAsync(await openingService.PickFolderAsync(null, CancellationToken.None));
    }

    private async Task OpenPathAsync(string? path)
    {
        if (path is null)
        {
            return;
        }

        if (_openingCancellation is not null)
        {
            await _openingCancellation.CancelAsync();
        }

        _openingCancellation?.Dispose();
        _openingCancellation = new CancellationTokenSource();
        CancellationToken cancellationToken = _openingCancellation.Token;
        _lastPath = path;
        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = "Opening repository...";

        try
        {
            ActiveRepository = await openingService.OpenAsync(path, cancellationToken);
            IsWelcome = false;
            StatusMessage = ActiveRepository.Path;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            StatusMessage = ActiveRepository is null ? "No repository open" : ActiveRepository.Path;
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
            StatusMessage = "Unable to open repository";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
