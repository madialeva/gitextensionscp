using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using GitUI;
using Microsoft.Extensions.DependencyInjection;

namespace GitExtensions.Avalonia;

public partial class MainWindow : Window
{
    private readonly RepositoryShellViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = App.ServiceProvider.GetRequiredService<RepositoryShellViewModel>();
        DataContext = _viewModel;
        Opened += MainWindowOpened;
        PropertyChanged += MainWindowPropertyChanged;
    }

    private void MainWindowOpened(object? sender, EventArgs e)
    {
        _viewModel.InitializeAsync().FileAndForget();
    }

    private void WindowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is not Button && e.GetCurrentPoint(this).Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void MinimizeClick(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeClick(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        UpdateMaximizeIcon();
    }

    private void CloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void MainWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Window.WindowStateProperty)
        {
            UpdateMaximizeIcon();
        }
    }

    private void UpdateMaximizeIcon()
    {
        if (MaximizeButton.Content is PathIcon icon
            && this.FindResource(WindowState == WindowState.Maximized ? "MaximizeGeometry" : "RestoreGeometry") is Geometry geometry)
        {
            icon.Data = geometry;
        }
    }
}
