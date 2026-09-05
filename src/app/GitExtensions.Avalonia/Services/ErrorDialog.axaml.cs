using Avalonia.Controls;
using Avalonia.Interactivity;
using GitExtensions.Avalonia.Localization;

namespace GitExtensions.Avalonia.Services;

public partial class ErrorDialog : Window
{
    public ErrorDialog()
    {
        InitializeComponent();
    }

    internal ErrorDialog(string title, string message, AvaloniaLocalizationService localization)
        : this()
    {
        ArgumentNullException.ThrowIfNull(localization);
        Title = title;
        MessageText.Text = message;
        OkButton.Content = localization.Resolve(AvaloniaLocalizationKeys.Ok);
    }

    private void OnOkClick(object? sender, RoutedEventArgs e) => Close();
}
