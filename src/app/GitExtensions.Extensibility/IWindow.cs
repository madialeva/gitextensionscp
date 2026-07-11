namespace GitExtensions.Extensibility;

/// <summary>
///  UI-technology-neutral reference to a window that can own dialogs (modality, centering).
///  Replaces <c>System.Windows.Forms.IWin32Window</c> in the public API: each shell implements
///  it on its own window/control types and translates it back internally
///  (e.g. the WinForms shell casts to <c>IWin32Window</c> before calling <c>ShowDialog</c>).
/// </summary>
public interface IWindow
{
}
