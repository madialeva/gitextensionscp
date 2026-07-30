using Avalonia.Controls;
using GitExtensions.Extensibility;

namespace GitExtensions.Avalonia.Services;

internal sealed class AvaloniaWindowAdapter(Window window) : IWindow
{
    internal Window Window { get; } = window;
}

internal static class WindowAdapterHelper
{
    internal static Window? ResolveOwner(IWindow? window) =>
        (window as AvaloniaWindowAdapter)?.Window;
}
