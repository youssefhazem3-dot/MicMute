using System;

namespace MicMute.Tests;

internal static class LifecycleCases
{
    public static void Run(Action<string, Action> test)
    {
        test(nameof(OccupiedInstanceDoesNotTakeOwnershipOrTerminateOwner), OccupiedInstanceDoesNotTakeOwnershipOrTerminateOwner);
    }

    private static void OccupiedInstanceDoesNotTakeOwnershipOrTerminateOwner()
    {
        string name = "Local\\MicMuteTests_" + Guid.NewGuid().ToString("N");
        using var first = AppInstanceMutex.TryAcquire(name);
        Check.True(first != null, "first instance must acquire the mutex");

        using var occupied = AppInstanceMutex.TryAcquire(name);
        Check.True(occupied == null, "second instance must report the occupied mutex");
        Check.True(first!.IsOwned, "the first instance must retain ownership");

        first.Dispose();
        using var replacement = AppInstanceMutex.TryAcquire(name);
        Check.True(replacement != null, "a new instance must acquire after the owner exits");
    }
}
