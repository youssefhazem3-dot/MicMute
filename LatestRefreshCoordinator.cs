using System;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace MicMute;

/// <summary>Applies only the newest asynchronous refresh and retries one transient failure.</summary>
internal sealed class LatestRefreshCoordinator<T> : IDisposable
{
    private readonly Dispatcher _dispatcher;
    private readonly Func<Task<T>> _load;
    private readonly Action<T> _apply;
    private readonly Action<Exception>? _onFailure;
    private readonly TimeSpan _retryDelay;
    private long _requestedVersion;
    private bool _running;
    private bool _disposed;

    public LatestRefreshCoordinator(Func<Task<T>> load, Action<T> apply,
        TimeSpan retryDelay, Action<Exception>? onFailure = null, Dispatcher? dispatcher = null)
    {
        _load = load ?? throw new ArgumentNullException(nameof(load));
        _apply = apply ?? throw new ArgumentNullException(nameof(apply));
        _retryDelay = retryDelay;
        _onFailure = onFailure;
        _dispatcher = dispatcher ?? Dispatcher.CurrentDispatcher;
    }

    public void Request()
    {
        if (_disposed || _dispatcher.HasShutdownStarted) return;
        if (!_dispatcher.CheckAccess())
        {
            try { _dispatcher.BeginInvoke(new Action(Request)); }
            catch (InvalidOperationException) { }
            return;
        }
        _requestedVersion++;
        if (_running) return;
        _running = true;
        _ = LoadAsync(_requestedVersion, attempt: 0);
    }

    private async Task LoadAsync(long version, int attempt)
    {
        T? result = default;
        Exception? failure = null;
        try { result = await _load().ConfigureAwait(false); }
        catch (Exception ex) { failure = ex; }

        try
        {
            await _dispatcher.InvokeAsync(() => Complete(version, attempt, result, failure));
        }
        catch (InvalidOperationException) { }
    }

    private void Complete(long version, int attempt, T? result, Exception? failure)
    {
        if (_disposed) { _running = false; return; }
        if (version != _requestedVersion)
        {
            _ = LoadAsync(_requestedVersion, attempt: 0);
            return;
        }
        if (failure != null && attempt == 0)
        {
            _ = RetryAfterDelay(version);
            return;
        }

        _running = false;
        if (failure != null) { _onFailure?.Invoke(failure); return; }
        try { _apply(result!); }
        catch (Exception ex) { _onFailure?.Invoke(ex); }
    }

    private async Task RetryAfterDelay(long version)
    {
        await Task.Delay(_retryDelay).ConfigureAwait(false);
        try
        {
            await _dispatcher.InvokeAsync(() =>
            {
                if (_disposed) { _running = false; return; }
                _ = LoadAsync(_requestedVersion, version == _requestedVersion ? 1 : 0);
            });
        }
        catch (InvalidOperationException) { }
    }

    public void Dispose()
    {
        _disposed = true;
        _requestedVersion++;
    }
}
