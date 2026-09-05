using GitCommands.UserRepositoryHistory;
using GitExtensions.Avalonia.Services;

namespace GitExtensions.Avalonia.Tests;

public sealed class RepositoryOpeningServiceTests
{
    [Test]
    public async Task OpenAsync_should_register_valid_repository_as_most_recent()
    {
        FakeHistory history = new();
        RepositoryPresentation presentation = new("/repo", "main", ["origin"], 2);
        RepositoryOpeningService service = new(history, new FakePicker("/repo"), new FakeReader(presentation));

        RepositoryPresentation result = await service.OpenAsync("/repo", CancellationToken.None);

        result.Should().Be(presentation);
        history.AddedPaths.Should().ContainSingle().Which.Should().Be("/repo");
    }

    [Test]
    public async Task PickFolderAsync_should_return_null_when_selection_is_cancelled()
    {
        FakeHistory history = new();
        FakeReader reader = new(new RepositoryPresentation("/repo", "main", [], 0));
        RepositoryOpeningService service = new(history, new FakePicker(null), reader);

        string? result = await service.PickFolderAsync(null, CancellationToken.None);

        result.Should().BeNull();
        reader.ReadCount.Should().Be(0);
        history.AddedPaths.Should().BeEmpty();
    }

    [Test]
    public async Task OpenAsync_should_not_register_repository_when_reading_fails()
    {
        FakeHistory history = new();
        RepositoryOpeningService service = new(history, new FakePicker(null), new ThrowingReader());

        Func<Task> action = () => service.OpenAsync("/invalid", CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        history.AddedPaths.Should().BeEmpty();
    }

    private sealed class FakeHistory : IRepositoryHistoryPort
    {
        public List<string> AddedPaths { get; } = [];

        public Task<IList<Repository>> AddAsMostRecentAsync(string path)
        {
            AddedPaths.Add(path);
            return Task.FromResult<IList<Repository>>([]);
        }

        public Task<IList<Repository>> LoadRecentHistoryAsync()
            => Task.FromResult<IList<Repository>>([]);
    }

    private sealed class FakePicker(string? path) : IRepositoryFolderPicker
    {
        public Task<string?> PickFolderAsync(string? selectedPath, CancellationToken cancellationToken)
            => Task.FromResult(path);
    }

    private sealed class FakeReader(RepositoryPresentation presentation) : IRepositoryReader
    {
        public int ReadCount { get; private set; }

        public Task<RepositoryPresentation> ReadAsync(string path, CancellationToken cancellationToken)
        {
            ReadCount++;
            return Task.FromResult(presentation);
        }
    }

    private sealed class ThrowingReader : IRepositoryReader
    {
        public Task<RepositoryPresentation> ReadAsync(string path, CancellationToken cancellationToken)
            => throw new InvalidOperationException("read failed");
    }
}
