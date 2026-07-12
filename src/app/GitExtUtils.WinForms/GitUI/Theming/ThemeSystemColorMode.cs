namespace GitExtUtils.GitUI.Theming;

/// <summary>
///  Bridges the UI-neutral theme data (<see cref="Theme.IsDark"/>) with the WinForms
///  color-mode concepts (<see cref="SystemColorMode"/>, <see cref="Application.SystemColorMode"/>).
/// </summary>
public static class ThemeSystemColorMode
{
    /// <summary>
    /// Get the Windows SystemColorMode for this theme, based on the background color.
    /// </summary>
    public static SystemColorMode GetSystemColorMode(this Theme theme)
        => theme.IsDark ? SystemColorMode.Dark : SystemColorMode.Classic;

    /// <summary>
    /// Get the default ThemeId for the current Windows SystemColorMode.
    /// </summary>
    public static ThemeId ColorModeThemeId
        => Application.SystemColorMode == SystemColorMode.Dark
            ? ThemeId.DefaultDark
            : ThemeId.DefaultLight;
}
