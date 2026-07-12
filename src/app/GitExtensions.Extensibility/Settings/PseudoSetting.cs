namespace GitExtensions.Extensibility.Settings;

/// <summary>
///  Not a real setting (it saves no value): displays a block of read-only text in a settings
///  page. Pure data — the UI layer decides how to render it (a read-only text box in WinForms).
/// </summary>
public class PseudoSetting : ISetting
{
    public PseudoSetting(string text, string caption = "    ", int? height = null)
    {
        Text = text;
        Caption = caption;
        Height = height;
    }

    public string Name { get; } = "PseudoSetting";
    public string Caption { get; }

    /// <summary>The text to display.</summary>
    public string Text { get; }

    /// <summary>Fixed height in pixels for multi-line text, or <see langword="null"/> for a single line.</summary>
    public int? Height { get; }
}
