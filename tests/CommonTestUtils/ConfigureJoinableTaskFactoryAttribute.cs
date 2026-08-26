using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using GitUI;
using Microsoft.VisualStudio.Threading;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace CommonTestUtils;

[AttributeUsage(AttributeTargets.Assembly)]
public sealed class ConfigureJoinableTaskFactoryAttribute : Attribute, ITestAction
{
    private SingleThreadSynchronizationContext? _synchronizationContext;
    private HangReporter? _hangReporter;
    private ExceptionDispatchInfo? _threadException;

    public ActionTargets Targets => ActionTargets.Test;

    public ConfigureJoinableTaskFactoryAttribute()
    {
        // TaskManager is UI-neutral and only traces by default; route fire-and-forget
        // exceptions to the test-failure capture so that they fail the owning test.
        TaskManager.ExceptionReporter = StoreThreadException;
    }

    public void BeforeTest(ITest test)
    {
        ThreadHelper.HasJoinableTaskContext.Should().BeFalse("Tests with joinable tasks must not be run in parallel!");

        _synchronizationContext = new SingleThreadSynchronizationContext();
        ThreadHelper.JoinableTaskContext = new JoinableTaskContext(_synchronizationContext.MainThread, _synchronizationContext);
        _hangReporter = new HangReporter(ThreadHelper.JoinableTaskContext);
    }

    public void AfterTest(ITest test)
    {
        try
        {
            try
            {
                // Wait for eventual pending operations triggered by the test.
                using CancellationTokenSource cts = new(AsyncTestHelper.UnexpectedTimeout);
                try
                {
                    ThreadHelper.CancelSwitchToMainThread();

                    // Note that ThreadHelper.JoinableTaskContext.Factory must be used to bypass the default behavior of
                    // ThreadHelper.JoinableTaskFactory since the latter adds new tasks to the collection and would therefore
                    // never complete.
                    ThreadHelper.JoinableTaskContext.Factory.Run(() => ThreadHelper.JoinPendingOperationsAsync(cts.Token));
                }
                catch (OperationCanceledException) when (cts.IsCancellationRequested)
                {
                    if (int.TryParse(Environment.GetEnvironmentVariable("GE_TEST_SLEEP_SECONDS_ON_HANG"), out int sleepSeconds) && sleepSeconds > 0)
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(sleepSeconds));
                    }

                    throw;
                }
            }
            finally
            {
                ThreadHelper.JoinableTaskContext = null!;
                _synchronizationContext?.Dispose();
            }
        }
        catch (Exception ex) when (_threadException is not null)
        {
            StoreThreadException(ex);
        }
        finally
        {
            // Reset _threadException to null, and throw if it was set during the current test.
            Interlocked.Exchange(ref _threadException, null)?.Throw();
        }
    }

    private void StoreThreadException(Exception ex)
    {
        if (_threadException is not null)
        {
            ex = new AggregateException([_threadException.SourceException, ex]);
        }

        _threadException = ExceptionDispatchInfo.Capture(ex);
    }

    private sealed class HangReporter : JoinableTaskContextNode
    {
        public HangReporter(JoinableTaskContext context)
            : base(context)
        {
            RegisterOnHangDetected();
        }

        protected override void OnHangDetected(TimeSpan hangDuration, int notificationCount, Guid hangId)
        {
            if (notificationCount > 1)
            {
                return;
            }

            StringBuilder output = new();
            output.AppendLine();
            output.AppendLine($"HANG DETECTED: guid {hangId}");

            HangReportContribution report = ((IHangReportContributor)Context).GetHangReport();
            if (report.ContentName!.EndsWith("dgml", StringComparison.InvariantCultureIgnoreCase))
            {
                try
                {
                    string assemblyLocation = Assembly.GetExecutingAssembly().Location;
                    string? reportLocation = Path.GetDirectoryName(assemblyLocation);
                    string reportFileName = Path.Combine(reportLocation!, $"{hangId}.dgml");

                    File.WriteAllText(reportFileName, report.Content);
                    output.AppendLine($"HANG report: {reportFileName}");
                }
                catch
                {
                    /* no-op */
                }
            }
            else
            {
                output.AppendLine(report.ContentName);
                output.AppendLine(report.ContentType);
                output.AppendLine(report.Content);
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(output.ToString());
            Console.ResetColor();

            // Allow seeing the output in Release builds
            Trace.WriteLine(output.ToString());

            if (Environment.GetEnvironmentVariable("GE_TEST_LAUNCH_DEBUGGER_ON_HANG") != "1")
            {
                return;
            }

            Console.WriteLine("launching debugger...");

            Debugger.Launch();
            Debugger.Break();
        }
    }
}
