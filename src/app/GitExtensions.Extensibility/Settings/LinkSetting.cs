namespace GitExtensions.Extensibility.Settings;

/// <summary>
///  Not a real setting (it saves no value): displays a clickable link in a settings page and
///  invokes a callback when activated. Pure data — the UI layer decides how to render it
///  (a link label in WinForms).
/// </summary>
public class LinkSetting : ISetting
{
    public LinkSetting(string text, Action activated, string caption = "    ")
    {
        Text = text;
        Activated = activated;
        Caption = caption;
    }

    public string Name { get; } = "LinkSetting";
    public string Caption { get; }

    /// <summary>The link text to display.</summary>
    public string Text { get; }

    /// <summary>Invoked when the user activates (clicks) the link.</summary>
    public Action Activated { get; }
}
