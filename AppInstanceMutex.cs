using System;
using System.Threading;

namespace MicMute;

internal sealed class AppInstanceMutex : IDisposable
{
    private Mutex? _mutex;

    private AppInstanceMutex(Mutex mutex) => _mutex = mutex;

    public bool IsOwned => _mutex != null;

    public static AppInstanceMutex? TryAcquire(string name)
    {
        Mutex mutex = new(initiallyOwned: true, name, out bool createdNew);
        if (createdNew) return new AppInstanceMutex(mutex);
        mutex.Dispose();
        return null;
    }

    public void Dispose()
    {
        Mutex? mutex = _mutex;
        if (mutex == null) return;
        _mutex = null;
        try { mutex.ReleaseMutex(); }
        finally { mutex.Dispose(); }
    }
}
