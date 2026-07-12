using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using GitExtensions.Extensibility.Settings;

namespace GitExtUtils;

/// <summary>
///  Font persistence for <see cref="SettingsSource"/>. Lives outside the plugin API because
///  <see cref="Font"/> is GDI+ (Windows-only) while the API stays UI-technology neutral.
/// </summary>
public static class SettingsSourceFontExtensions
{
    [return: NotNullIfNotNull(nameof(defaultValue))]
    public static Font? GetFont(this SettingsSource settings, string name, Font? defaultValue)
        => FontParser.Parse(settings.GetValue(name), defaultValue!);

    public static void SetFont(this SettingsSource settings, string name, Font? value)
        => settings.SetValue(name, value?.AsString());
}
