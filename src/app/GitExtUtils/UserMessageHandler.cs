using System.Diagnostics;
using GitExtensions.Extensibility;

namespace GitExtUtils;

/// <summary>
///  Provides a UI-neutral error notification channel for the core libraries
///  (<c>GitCommands</c>, <c>GitExtUtils</c>). The shell installs a handler that
///  shows the message in its own UI modality (WinForms <c>MessageBox</c>, Avalonia
///  popup, etc.). The default implementation is a no-op trace.
/// </summary>
/// <remarks>
///  Pattern: same as <c>TaskManager.ExceptionReporter</c> (change 0.3).
///  The core raises notifications; the shell subscribes to them. This decoupling
///  allows <c>GitCommands</c> to target <c>net10.0</c> without referencing WinForms.
/// </remarks>
public static class UserMessageHandler
{
    /// <summary>
    ///  Shows an error or warning message to the user. The shell MUST install
    ///  this before the first call site executes.
    /// </summary>
    public static Action<IWindow?, string, string?> ShowError { get; set; } =
        (owner, text, caption) => Trace.TraceWarning($"[UserMessage] {caption ?? "Error"}: {text}");
}
