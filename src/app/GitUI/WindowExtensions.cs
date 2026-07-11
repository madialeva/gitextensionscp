using GitExtensions.Extensibility;

namespace GitUI;

/// <summary>
///  Bridges the UI-neutral <see cref="IWindow"/> of the plugin API and the WinForms
///  <see cref="IWin32Window"/> this shell needs for dialog ownership.
/// </summary>
public static class WindowExtensions
{
    /// <summary>
    ///  Translates an <see cref="IWindow"/> back to an <see cref="IWin32Window"/>.
    ///  In this WinForms shell every <see cref="IWindow"/> implementation is a Form or Control,
    ///  so the cast only fails for foreign implementations, which are treated as "no owner".
    /// </summary>
    public static IWin32Window? AsWinFormsWindow(this IWindow? window) => window as IWin32Window;

    /// <summary>
    ///  Adapts any WinForms window (plain <see cref="Form"/>s included) to the neutral
    ///  <see cref="IWindow"/> expected by the plugin API.
    /// </summary>
    public static IWindow? AsApiWindow(this IWin32Window? window)
        => window as IWindow ?? (window is null ? null : new Win32WindowAdapter(window));

    private sealed class Win32WindowAdapter(IWin32Window window) : IWindow, IWin32Window
    {
        public IntPtr Handle => window.Handle;
    }
}
