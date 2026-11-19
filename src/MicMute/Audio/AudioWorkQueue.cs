using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace MicMute;

/// <summary>Serializes Core Audio COM access on one dedicated STA dispatcher.</summary>
internal sealed class AudioWorkQueue : IDisposable
{
    private readonly Thread _thread;
    private readonly ManualResetEventSlim _ready = new();
    private Dispatcher? _dispatcher;
    private int _disposed;

    public AudioWorkQueue()
    {
        _thread = new Thread(() =>
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            _ready.Set();
            Dispatcher.Run();
        })
        { IsBackground = true, Name = "MicMute_Audio" };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        _ready.Wait();
    }

    public Task<T> InvokeAsync<T>(Func<T> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (Volatile.Read(ref _disposed) != 0)
            return Task.FromException<T>(new ObjectDisposedException(nameof(AudioWorkQueue)));

        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            _dispatcher!.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                try { completion.TrySetResult(action()); }
                catch (Exception ex) { completion.TrySetException(ex); }
            }));
        }
        catch (InvalidOperationException ex) { completion.TrySetException(ex); }
        return completion.Task;
    }

    public Task InvokeAsync(Action action) => InvokeAsync(() => { action(); return true; });

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        try { _dispatcher?.BeginInvokeShutdown(DispatcherPriority.Normal); }
        catch (InvalidOperationException) { }
        if (Thread.CurrentThread != _thread) _thread.Join(TimeSpan.FromMilliseconds(100));
        _ready.Dispose();
    }
}
