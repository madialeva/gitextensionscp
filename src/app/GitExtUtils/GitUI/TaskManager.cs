using System.Diagnostics;
using Microsoft.VisualStudio.Threading;

namespace GitUI;

public class TaskManager
{
    private static readonly CancellationTokenSequence _switchToMainThreadCancellationTokenSequence = new();

    private static CancellationToken _switchToMainThreadCancellationToken = _switchToMainThreadCancellationTokenSequence.Next();

    /// <summary>
    ///  Receives the (demystified) exceptions of fire-and-forget operations. This assembly is
    ///  UI-technology neutral and cannot report to the UI itself: the WinForms shell installs
    ///  <c>Application.OnThreadException</c> here at startup (Program.cs, BugReporter, test
    ///  infrastructure). The default merely traces.
    /// </summary>
    public static Action<Exception> ExceptionReporter { get; set; } = ex => Trace.TraceError(ex.ToString());

    internal static CancellationToken SwitchToMainThreadCancellationToken => _switchToMainThreadCancellationToken;

    private readonly JoinableTaskCollection _joinableTaskCollection;

    public TaskManager(JoinableTaskContext joinableTaskContext)
    {
        JoinableTaskContext = joinableTaskContext;
        _joinableTaskCollection = joinableTaskContext.CreateCollection();
        JoinableTaskFactory = joinableTaskContext.CreateFactory(_joinableTaskCollection);
    }

    public JoinableTaskContext JoinableTaskContext { get; init; }

    public JoinableTaskFactory JoinableTaskFactory { get; init; }

    /// <summary>
    /// Handle all exceptions from asynchronous execution of <paramref name="asyncAction"/> by calling <paramref name="handleExceptionAsync"/> except for <see cref="OperationCanceledException"/>, which is ignored.
    /// </summary>
    internal static async Task HandleExceptionsAsync(Func<Task> asyncAction, Func<Exception, Task> handleExceptionAsync)
    {
        try
        {
            await asyncAction();
        }
        catch (OperationCanceledException)
        {
            // Do not rethrow these
        }
        catch (Exception ex)
        {
            await handleExceptionAsync(ex);
        }
    }

    /// <summary>
    /// Handle all exceptions from synchronous execution of <paramref name="action"/> by calling <paramref name="handleException"/> except for <see cref="OperationCanceledException"/>, which is ignored.
    /// </summary>
    public static void HandleExceptions(Action action, Action<Exception> handleException)
    {
        try
        {
            action();
        }
        catch (OperationCanceledException)
        {
            // Do not rethrow these
        }
        catch (Exception ex)
        {
            handleException(ex);
        }
    }

    internal static Func<Task> AsyncAction(Action action)
    {
        return () =>
            {
                action();
                return Task.CompletedTask;
            };
    }

    internal static void CancelSwitchToMainThread()
    {
        _switchToMainThreadCancellationToken = _switchToMainThreadCancellationTokenSequence.Next();
    }

    /// <summary>
    /// Asynchronously run <paramref name="asyncAction"/> on a background thread and forward all exceptions to <see cref="ExceptionReporter"/> except for <see cref="OperationCanceledException"/>, which is ignored.
    /// </summary>
    public void FileAndForget(Func<Task> asyncAction)
    {
        _ = JoinableTaskFactory.RunAsync(async () =>
            {
                await TaskScheduler.Default;
                await HandleExceptionsAsync(asyncAction, ReportExceptionOnMainThreadAsync);
            });
    }

    /// <summary>
    /// Asynchronously run <paramref name="action"/> on a background thread and forward all exceptions to <see cref="ExceptionReporter"/> except for <see cref="OperationCanceledException"/>, which is ignored.
    /// </summary>
    public void FileAndForget(Action action)
    {
        FileAndForget(AsyncAction(action));
    }

    /// <summary>
    /// Asynchronously run <paramref name="task"/> on a background thread and forward all exceptions to <see cref="ExceptionReporter"/> except for <see cref="OperationCanceledException"/>, which is ignored.
    /// </summary>
    public void FileAndForget(Task task)
    {
        TimeSpan infiniteTimeout = new(-TimeSpan.TicksPerMillisecond);
        FileAndForget(() => task.WaitAsync(infiniteTimeout));
    }

    public async Task JoinPendingOperationsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _joinableTaskCollection.JoinTillEmptyAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex);
        }
    }

    public void JoinPendingOperations()
    {
        const int maxWaitMilliseconds = 60_000;
        using CancellationTokenSource cancellationTokenSource = new(maxWaitMilliseconds);

        // Note that JoinableTaskContext.Factory must be used to bypass the default behavior of JoinableTaskFactory
        // since the latter adds new tasks to the collection and would therefore never complete.
        JoinableTaskContext.Factory.Run(() => JoinPendingOperationsAsync(cancellationTokenSource.Token));
    }

    /// <summary>
    /// Forward the exception <paramref name="ex"/> to <see cref="ExceptionReporter"/> on the main thread.
    /// </summary>
    /// The readability of the callstack is improved by calling <c>ExceptionExtensions.Demystify</c>.
    internal async Task ReportExceptionOnMainThreadAsync(Exception ex)
    {
        try
        {
            if (!JoinableTaskContext.IsOnMainThread)
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync(_switchToMainThreadCancellationToken);
            }

            ExceptionReporter(ex.Demystify());
        }
        catch (Exception exceptionWhileReporting)
        {
            try
            {
                Trace.TraceError(exceptionWhileReporting.ToString());
                Trace.TraceError(ex.ToString());
                Trace.TraceError(ex.StackTrace);
            }
            catch
            {
                // Give up
            }
        }
    }
}
