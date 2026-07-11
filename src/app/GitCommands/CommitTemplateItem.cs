using GitCommands.Utils;

namespace GitCommands;

public sealed class CommitTemplateItem
{
    public string Name { get; set; }
    public string Text { get; set; }

    /// <summary>Raw bytes of a PNG image, or <see langword="null"/> (UI-technology neutral).</summary>
    public byte[]? IconData { get; set; }
    public bool IsRegex { get; set; }

    public CommitTemplateItem(string name, string text, byte[]? iconData, bool isRegex)
    {
        Name = name;
        Text = text;
        IconData = iconData;
        IsRegex = isRegex;
    }

    public CommitTemplateItem()
    {
        Name = string.Empty;
        Text = string.Empty;
        IconData = null;
        IsRegex = false;
    }

    public static void SaveToSettings(CommitTemplateItem[]? items)
    {
        string strVal = SerializeCommitTemplates(items);
        AppSettings.CommitTemplates = strVal;
    }

    public static CommitTemplateItem[]? LoadFromSettings()
    {
        string? serializedString = AppSettings.CommitTemplates;
        CommitTemplateItem[]? templates = DeserializeCommitTemplates(serializedString, out bool shouldBeUpdated);
        if (shouldBeUpdated)
        {
            SaveToSettings(templates!);
        }

        return templates;
    }

    private static string SerializeCommitTemplates(CommitTemplateItem[]? items)
    {
        return JsonSerializer.Serialize(items);
    }

    private static CommitTemplateItem[]? DeserializeCommitTemplates(string serializedString, out bool shouldBeUpdated)
    {
        shouldBeUpdated = false;
        if (string.IsNullOrEmpty(serializedString))
        {
            return null;
        }

        CommitTemplateItem[]? commitTemplateItem = null;
        try
        {
            commitTemplateItem = JsonSerializer.Deserialize<CommitTemplateItem[]>(serializedString);
        }
        catch
        {
            // do nothing
        }

        return commitTemplateItem;
    }
}
