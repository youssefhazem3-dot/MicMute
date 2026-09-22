using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace MicMute.Tests;

internal static class RefreshCases
{
    public static void Run(Action<string, Action> test)
    {
        test(nameof(LaterRefreshWinsAfterAnEarlierLoad), LaterRefreshWinsAfterAnEarlierLoad);
        test(nameof(TransientFailureRetriesWithoutClearingExistingUi), TransientFailureRetriesWithoutClearingExistingUi);
    }

    private static void LaterRefreshWinsAfterAnEarlierLoad()
    {
        WithDispatcher(() =>
        {
            var first = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var second = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var applied = new List<string>();
            int loads = 0;
            using var refresh = new LatestRefreshCoordinator<string>(
                () => ++loads == 1 ? first.Task : second.Task,
                applied.Add,
                retryDelay: TimeSpan.Zero);

            refresh.Request();
            refresh.Request();
            first.SetResult("old devices");
            PumpUntil(() => loads == 2);
            Check.Equal(0, applied.Count);
            second.SetResult("new devices");
            PumpUntil(() => applied.Count == 1);
            Check.Equal("new devices", applied[0]);
        });
    }

    private static void TransientFailureRetriesWithoutClearingExistingUi()
    {
        WithDispatcher(() =>
        {
            int loads = 0;
            var applied = new List<string>();
            using var refresh = new LatestRefreshCoordinator<string>(
                () => ++loads == 1
                    ? Task.FromException<string>(new InvalidOperationException("device is unplugging"))
                    : Task.FromResult("recovered devices"),
                applied.Add,
                retryDelay: TimeSpan.FromMilliseconds(1));

            refresh.Request();
            PumpUntil(() => applied.Count == 1);
            Check.Equal(2, loads);
            Check.Equal("recovered devices", applied[0]);
        });
    }

    private static void WithDispatcher(Action action)
    {
        SynchronizationContext? previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
        try { action(); }
        finally { SynchronizationContext.SetSynchronizationContext(previous); }
    }

    private static void PumpUntil(Func<bool> done)
    {
        var frame = new DispatcherFrame();
        var clock = Stopwatch.StartNew();
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(5) };
        timer.Tick += (_, _) =>
        {
            if (!done() && clock.Elapsed < TimeSpan.FromSeconds(2)) return;
            timer.Stop();
            frame.Continue = false;
        };
        timer.Start();
        Dispatcher.PushFrame(frame);
        Check.True(done(), "refresh did not finish within the test deadline");
    }
}
