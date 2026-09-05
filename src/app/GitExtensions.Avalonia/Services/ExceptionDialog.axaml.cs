using Avalonia.Controls;
using Avalonia.Interactivity;
using GitExtensions.Avalonia.Localization;

namespace GitExtensions.Avalonia.Services;

public partial class ExceptionDialog : Window
{
    public ExceptionDialog()
    {
        InitializeComponent();
    }

    internal ExceptionDialog(Exception exception, AvaloniaLocalizationService localization)
        : this()
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(localization);
        Title = localization.Resolve(AvaloniaLocalizationKeys.Error);
        ExceptionText.Text = exception.ToString();
        OkButton.Content = localization.Resolve(AvaloniaLocalizationKeys.Ok);
    }

    private void OnOkClick(object? sender, RoutedEventArgs e) => Close();
}
