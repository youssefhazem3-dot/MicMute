using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MicMute.Tests;

internal static class AudioCases
{
    public static void Run(Action<string, Action> test)
    {
        test(nameof(AudioWorkRunsSeriallyOnStaWithoutBlockingCaller), AudioWorkRunsSeriallyOnStaWithoutBlockingCaller);
        test(nameof(AudioWorkerDisposalDoesNotWaitForStalledDriverWork), AudioWorkerDisposalDoesNotWaitForStalledDriverWork);
        test(nameof(OldNotificationCannotReplaceNewerMuteState), OldNotificationCannotReplaceNewerMuteState);
    }

    private static void AudioWorkRunsSeriallyOnStaWithoutBlockingCaller()
    {
        using var worker = new AudioWorkQueue();
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var order = new List<int>();
        Task<int> first = worker.InvokeAsync(() =>
        {
            order.Add(1);
            entered.Set();
            release.Wait();
            return 1;
        });
        Check.True(entered.Wait(TimeSpan.FromSeconds(2)), "audio work did not start");

        try
        {
            Task<Task<ApartmentState>> queued = Task.Factory.StartNew(() => worker.InvokeAsync(() =>
            {
                order.Add(2);
                return Thread.CurrentThread.GetApartmentState();
            }), CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
            Check.True(queued.Wait(TimeSpan.FromSeconds(2)), "queueing audio work must not wait for the active COM operation");
            Check.True(!queued.Result.IsCompleted, "later audio work must wait its turn");
            release.Set();
            Check.Equal(1, first.GetAwaiter().GetResult());
            Check.Equal(ApartmentState.STA, queued.Result.GetAwaiter().GetResult());
            Check.Equal(2, order.Count);
            Check.Equal(1, order[0]);
            Check.Equal(2, order[1]);
        }
        finally { release.Set(); }
    }

    private static void OldNotificationCannotReplaceNewerMuteState()
    {
        var state = new AudioMuteState();
        state.RecordLocalChange(true);
        state.RecordLocalChange(false);
        int reads = 0;
        bool staleAccepted = state.TryApplyNotification(true, () => { reads++; return false; }, out _);
        Check.True(!staleAccepted, "an old muted notification must not replace the current live state");
        Check.Equal(1, reads);
        Check.Equal(false, state.Current);

        bool currentAccepted = state.TryApplyNotification(true, () => true, out bool reported);
        Check.True(currentAccepted, "a real external mute change must still be reported");
        Check.True(reported);
    }

    private static void AudioWorkerDisposalDoesNotWaitForStalledDriverWork()
    {
        var worker = new AudioWorkQueue();
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        Task blocked = worker.InvokeAsync(() => { entered.Set(); release.Wait(); });
        Check.True(entered.Wait(TimeSpan.FromSeconds(2)), "audio work did not start");
        Task disposal = Task.Run(worker.Dispose);
        try
        {
            Check.True(disposal.Wait(TimeSpan.FromMilliseconds(500)),
                "closing the app must not wait several seconds for a stalled audio operation");
        }
        finally
        {
            release.Set();
            disposal.Wait(TimeSpan.FromSeconds(5));
            worker.Dispose();
        }
    }
}
