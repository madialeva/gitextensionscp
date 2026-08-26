using System.Collections.Concurrent;

namespace CommonTestUtils;

/// <summary>
///  A single-threaded <see cref="SynchronizationContext"/> that emulates a UI message loop
///  without WinForms: posted callbacks run on a dedicated background thread, so
///  <c>SwitchToMainThreadAsync</c> can marshal continuations back to that thread during tests.
/// </summary>
internal sealed class SingleThreadSynchronizationContext : SynchronizationContext, IDisposable
{
    private readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> _queue = new();
    private readonly Thread _thread;

    public SingleThreadSynchronizationContext()
    {
        _thread = new Thread(Run)
        {
            IsBackground = true,
            Name = "CommonTestUtils.SynchronizationContext"
        };
        _thread.Start();
    }

    /// <summary>The thread on which posted callbacks are executed (the emulated UI thread).</summary>
    public Thread MainThread => _thread;

    public override void Post(SendOrPostCallback d, object? state)
        => _queue.Add((d, state));

    public override void Send(SendOrPostCallback d, object? state)
    {
        if (Thread.CurrentThread == _thread)
        {
            d(state);
            return;
        }

        using ManualResetEventSlim completed = new();
        _queue.Add((
            s =>
            {
                try
                {
                    d(s);
                }
                finally
                {
                    completed.Set();
                }
            },
            state));
        completed.Wait();
    }

    public void Dispose()
        => _queue.CompleteAdding();

    private void Run()
    {
        SetSynchronizationContext(this);
        foreach ((SendOrPostCallback callback, object? state) in _queue.GetConsumingEnumerable())
        {
            callback(state);
        }
    }
}
