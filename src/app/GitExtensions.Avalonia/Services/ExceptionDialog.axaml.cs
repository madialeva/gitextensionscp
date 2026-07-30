using Avalonia.Controls;
using Avalonia.Interactivity;

namespace GitExtensions.Avalonia.Services;

public partial class ExceptionDialog : Window
{
    public ExceptionDialog()
    {
        InitializeComponent();
    }

    public ExceptionDialog(Exception exception) : this()
    {
        ExceptionText.Text = exception.ToString();
    }

    private void OnOkClick(object? sender, RoutedEventArgs e) => Close();
}
