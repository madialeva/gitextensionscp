using System.Globalization;
using System.Text;
using GitExtensions.Avalonia.Localization;

namespace GitExtensions.Avalonia.Tests;

public sealed class XliffCatalogTests
{
    [Test]
    public void Load_should_use_target_when_translation_exists()
    {
        using MemoryStream stream = CreateCatalog("<source>Hello</source><target>Hola</target>");

        XliffCatalog catalog = XliffCatalog.Load(stream);

        catalog.TryGet("Hello", out string value).Should().BeTrue();
        value.Should().Be("Hola");
    }

    [Test]
    public void Load_should_fallback_to_source_when_target_is_empty()
    {
        using MemoryStream stream = CreateCatalog("<source>Hello</source><target />");

        XliffCatalog catalog = XliffCatalog.Load(stream);

        catalog.TryGet("Hello", out string value).Should().BeTrue();
        value.Should().Be("Hello");
    }

    [Test]
    public void Localization_service_should_fallback_to_english_and_mark_missing_keys()
    {
        using MemoryStream englishStream = CreateCatalog("<source>Hello</source><target />");
        using MemoryStream spanishStream = CreateCatalog(
            "<file target-language=\"es\"><body><trans-unit id=\"1\"><source>Hello</source><target>Hola</target></trans-unit></body></file>");
        XliffCatalog english = XliffCatalog.Load(englishStream);
        XliffCatalog spanish = XliffCatalog.Load(spanishStream);
        AvaloniaLocalizationService service = new(
            new Dictionary<string, XliffCatalog>(StringComparer.OrdinalIgnoreCase)
            {
                ["en"] = english,
                ["es"] = spanish
            });

        service.SetCulture("es");
        service["Hello"].Should().Be("Hola");
        service["Missing"].Should().Be("[Missing translation: Missing]");

        service.SetCulture(CultureInfo.GetCultureInfo("fr").Name);
        service["Hello"].Should().Be("Hello");
    }

    [Test]
    public void FromAssembly_should_load_the_embedded_spanish_catalog()
    {
        AvaloniaLocalizationService service = AvaloniaLocalizationService.FromAssembly(
            typeof(AvaloniaLocalizationService).Assembly);

        service.SetCulture("es");

        service["Open repository"].Should().Be("Abrir repositorio");
        service["OpenRepository"].Should().Be("Abrir repositorio");
    }

    [Test]
    public void SetCulture_should_notify_bindings_and_ignore_an_invalid_culture()
    {
        AvaloniaLocalizationService service = new(
            new Dictionary<string, XliffCatalog>
            {
                ["en"] = XliffCatalog.Load(CreateCatalog("<source>Hello</source><target>Hello</target>")),
                ["es"] = XliffCatalog.Load(CreateCatalog("<file target-language=\"es\"><body><trans-unit id=\"1\"><source>Hello</source><target>Hola</target></trans-unit></body></file>"))
            });
        int notificationCount = 0;
        service.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == string.Empty)
            {
                notificationCount++;
            }
        };

        service.SetCulture("es").Should().BeTrue();
        service["Hello"].Should().Be("Hola");
        service.SetCulture("en_US").Should().BeFalse();
        service["Hello"].Should().Be("Hola");
        notificationCount.Should().Be(1);
    }

    private static MemoryStream CreateCatalog(string unit)
    {
        string xml = $"<?xml version=\"1.0\" encoding=\"utf-8\"?><xliff version=\"1.0\"><file><body><trans-unit id=\"1\">{unit}</trans-unit></body></file></xliff>";
        return new MemoryStream(Encoding.UTF8.GetBytes(xml));
    }
}
