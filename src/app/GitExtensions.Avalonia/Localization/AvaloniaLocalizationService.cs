using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace GitExtensions.Avalonia.Localization;

internal sealed class AvaloniaLocalizationService : INotifyPropertyChanged
{
    public const string CultureEnvironmentVariable = "GITEXTENSIONS_CULTURE";

    private const string EnglishCultureName = "en";
    private const string MissingKeyFormat = "[Missing translation: {0}]";
    private readonly IReadOnlyDictionary<string, XliffCatalog> _catalogs;
    private CultureInfo _activeCulture = CultureInfo.GetCultureInfo(EnglishCultureName);

    public static AvaloniaLocalizationService FromAssembly(Assembly assembly, string? initialCultureName = null)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        Dictionary<string, XliffCatalog> catalogs = new(StringComparer.OrdinalIgnoreCase);
        foreach (string resourceName in assembly.GetManifestResourceNames().Where(name => name.EndsWith(".xlf", StringComparison.OrdinalIgnoreCase)))
        {
            using Stream? stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                continue;
            }

            XliffCatalog catalog = XliffCatalog.Load(stream);
            string cultureName = catalog.TargetLanguage
                ?? (resourceName.EndsWith(".English.xlf", StringComparison.OrdinalIgnoreCase) ? EnglishCultureName : string.Empty);
            if (cultureName.Length > 0)
            {
                catalogs[cultureName] = catalog;
            }
        }

        AvaloniaLocalizationService service = new(catalogs);
        if (initialCultureName is not null)
        {
            service.SetCulture(initialCultureName);
        }

        return service;
    }

    public AvaloniaLocalizationService(IReadOnlyDictionary<string, XliffCatalog> catalogs)
    {
        ArgumentNullException.ThrowIfNull(catalogs);
        _catalogs = new ReadOnlyDictionary<string, XliffCatalog>(
            new Dictionary<string, XliffCatalog>(catalogs, StringComparer.OrdinalIgnoreCase));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public CultureInfo ActiveCulture
    {
        get => _activeCulture;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (Equals(_activeCulture, value))
            {
                return;
            }

            _activeCulture = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }
    }

    public string this[string key] => Resolve(key);

    public string Resolve(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        string sourceKey = AvaloniaLocalizationKeys.SourceKeys.TryGetValue(key, out string? mappedKey)
            ? mappedKey
            : key;

        if (TryGetCatalog(_activeCulture, out XliffCatalog? activeCatalog)
            && activeCatalog is not null
            && activeCatalog.TryGet(sourceKey, out string activeValue))
        {
            return activeValue;
        }

        if (_activeCulture.Name is not ("" or EnglishCultureName)
            && _catalogs.TryGetValue(EnglishCultureName, out XliffCatalog? englishCatalog)
            && englishCatalog.TryGet(sourceKey, out string englishValue))
        {
            return englishValue;
        }

        return AvaloniaLocalizationKeys.Defaults.TryGetValue(sourceKey, out string? defaultValue)
            ? defaultValue
            : string.Format(CultureInfo.InvariantCulture, MissingKeyFormat, key);
    }

    private bool TryGetCatalog(CultureInfo culture, out XliffCatalog? catalog)
    {
        if (_catalogs.TryGetValue(culture.Name, out catalog))
        {
            return true;
        }

        if (_catalogs.TryGetValue(culture.TwoLetterISOLanguageName, out catalog))
        {
            return true;
        }

        catalog = null;
        return false;
    }

    public bool SetCulture(string cultureName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cultureName);

        try
        {
            CultureInfo culture = CultureInfo.GetCultureInfo(cultureName);
            if (!CultureInfo.GetCultures(CultureTypes.AllCultures)
                .Any(availableCulture => string.Equals(availableCulture.Name, culture.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            ActiveCulture = culture;
            return true;
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
