using GitExtensions.Extensibility.Git;
using GitExtensions.Extensibility.Settings;

namespace GitExtensions.Extensibility.Plugins;

public interface IGitPlugin
{
    Guid Id { get; }

    string? Name { get; }

    string? Description { get; }

    /// <summary>
    ///  Raw bytes of the plugin icon (a PNG image), or <see langword="null"/> if the plugin has no icon.
    ///  Kept UI-technology neutral on purpose: each shell materializes it to its native image type.
    /// </summary>
    byte[]? IconData { get; }

    IGitPluginSettingsContainer? SettingsContainer { get; set; }

    bool HasSettings { get; }

    IEnumerable<ISetting> GetSettings();

    void Register(IGitUICommands gitUiCommands);

    void Unregister(IGitUICommands gitUiCommands);

    /// <summary>
    /// Runs the plugin and returns whether the RevisionGrid should be refreshed.
    /// </summary>
    bool Execute(GitUIEventArgs args);
}
