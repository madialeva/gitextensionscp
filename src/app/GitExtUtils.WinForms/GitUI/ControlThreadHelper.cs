using Microsoft.VisualStudio.Threading;

namespace GitUI;

/// <summary>
///  WinForms companions of <see cref="ThreadHelper"/>/<see cref="TaskManager"/>: fire-and-forget
///  that first switches to the UI thread of a specific <see cref="Control"/>. Extracted from
///  those classes so that they (a dependency of the GitCommands core) stay free of WinForms.
/// </summary>
public static class ControlThreadHelper
{
    /// <summary>
    /// Asynchronously run <paramref name="asyncAction"/> on the UI thread and forward all exceptions to <see cref="TaskManager.ExceptionReporter"/> except for <see cref="OperationCanceledException"/>, which is ignored.
    /// </summary>
    public static void InvokeAndForget(this TaskManager taskManager, Control control, Func<Task> asyncAction, CancellationToken cancellationToken = default)
    {
        _ = taskManager.JoinableTaskFactory.RunAsync(() =>
            TaskManager.HandleExceptionsAsync(async () =>
                {
                    if (!taskManager.JoinableTaskContext.IsOnMainThread)
                    {
                        await control.SwitchToMainThreadAsync(cancellationToken.CombineWith(TaskManager.SwitchToMainThreadCancellationToken).Token);
                    }

                    await asyncAction();
                },
                taskManager.ReportExceptionOnMainThreadAsync));
    }

    /// <summary>
    /// Asynchronously run <paramref name="action"/> on the UI thread and forward all exceptions to <see cref="TaskManager.ExceptionReporter"/> except for <see cref="OperationCanceledException"/>, which is ignored.
    /// </summary>
    public static void InvokeAndForget(this TaskManager taskManager, Control control, Action action, CancellationToken cancellationToken = default)
        => taskManager.InvokeAndForget(control, TaskManager.AsyncAction(action), cancellationToken);

    /// <summary>
    /// Asynchronously run <paramref name="asyncAction"/> on the UI thread and forward all exceptions to <see cref="TaskManager.ExceptionReporter"/> except for <see cref="OperationCanceledException"/>, which is ignored.
    /// </summary>
    public static void InvokeAndForget(this Control control, Func<Task> asyncAction, TaskManager? taskManager = null, CancellationToken cancellationToken = default)
        => (taskManager ?? ThreadHelper.DefaultTaskManager).InvokeAndForget(control, asyncAction, cancellationToken);

    /// <summary>
    /// Asynchronously run <paramref name="action"/> on the UI thread and forward all exceptions to <see cref="TaskManager.ExceptionReporter"/> except for <see cref="OperationCanceledException"/>, which is ignored.
    /// </summary>
    public static void InvokeAndForget(this Control control, Action action, TaskManager? taskManager = null, CancellationToken cancellationToken = default)
        => InvokeAndForget(control, TaskManager.AsyncAction(action), taskManager, cancellationToken);
}
