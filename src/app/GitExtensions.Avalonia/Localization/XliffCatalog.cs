using System.Xml.Linq;

namespace GitExtensions.Avalonia.Localization;

internal sealed class XliffCatalog
{
    private readonly IReadOnlyDictionary<string, string> _entries;

    private XliffCatalog(string? targetLanguage, IReadOnlyDictionary<string, string> entries)
    {
        TargetLanguage = targetLanguage;
        _entries = entries;
    }

    public string? TargetLanguage { get; }

    public static XliffCatalog Load(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        XDocument document = XDocument.Load(stream, LoadOptions.PreserveWhitespace);
        Dictionary<string, string> entries = new(StringComparer.Ordinal);
        string? targetLanguage = document
            .Descendants()
            .FirstOrDefault(element => element.Name.LocalName == "file")?
            .Attribute("target-language")?
            .Value;

        foreach (XElement unit in document.Descendants().Where(element => element.Name.LocalName == "trans-unit"))
        {
            string? source = unit.Elements().FirstOrDefault(element => element.Name.LocalName == "source")?.Value;
            if (string.IsNullOrWhiteSpace(source))
            {
                continue;
            }

            string? target = unit.Elements().FirstOrDefault(element => element.Name.LocalName == "target")?.Value;
            entries[source] = string.IsNullOrWhiteSpace(target) ? source : target;
        }

        return new XliffCatalog(targetLanguage, entries);
    }

    public bool TryGet(string key, out string value)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _entries.TryGetValue(key, out value!);
    }
}
