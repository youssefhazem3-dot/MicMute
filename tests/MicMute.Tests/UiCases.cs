using System;
using System.Globalization;

namespace MicMute.Tests;

static class UiCases
{
    public static void Run(Action<string, Action> test)
    {
        test(nameof(ParsesDurationUsingCurrentCultureAndInvariantFallback), ParsesDurationUsingCurrentCultureAndInvariantFallback);
        test(nameof(RejectsNonFiniteAndOutOfRangeDurations), RejectsNonFiniteAndOutOfRangeDurations);
        test(nameof(FormatsDurationUsingTheRequestedCulture), FormatsDurationUsingTheRequestedCulture);
        test(nameof(BoundsTrayTooltipToShellLimit), BoundsTrayTooltipToShellLimit);
        test(nameof(CentersPhysicalWindowRectInPhysicalScreenBounds), CentersPhysicalWindowRectInPhysicalScreenBounds);
        test(nameof(ExplicitStartupArgumentsOverrideStoredPreference), ExplicitStartupArgumentsOverrideStoredPreference);
        test(nameof(RestartArgumentsIdentifyParentAndRequestVisibleWindow), RestartArgumentsIdentifyParentAndRequestVisibleWindow);
        test(nameof(RefreshGenerationInvalidatesOlderCallbacks), RefreshGenerationInvalidatesOlderCallbacks);
        test(nameof(OsdResourceLoadsUsingProductionConstructor), OsdResourceLoadsUsingProductionConstructor);
        test(nameof(MainPanelEmbeddedXamlParses), MainPanelEmbeddedXamlParses);
        test(nameof(RestartWaitsUntilParentActuallyExits), RestartWaitsUntilParentActuallyExits);
        test(nameof(DispatcherCoalescesRefreshesAndCancelsDisposedWork), DispatcherCoalescesRefreshesAndCancelsDisposedWork);
    }

    private static void ParsesDurationUsingCurrentCultureAndInvariantFallback()
    {
        var french = CultureInfo.GetCultureInfo("fr-FR");
        Check.True(UiBehavior.TryParseOsdDuration("1,5", french, out double currentCulture), "current culture decimal separator should parse");
        Check.Equal(1.5, currentCulture);
        Check.True(UiBehavior.TryParseOsdDuration("1.5", french, out double invariant), "invariant decimal fallback should parse");
        Check.Equal(1.5, invariant);
    }

    private static void OsdResourceLoadsUsingProductionConstructor()
    {
        var window = new OsdWindow();
        try
        {
            Check.True(window.Content != null, "OSD content must be embedded and parseable");
            Check.True(window.borderPanel != null && window.tbStatus != null, "named OSD controls must bind");
        }
        finally { window.Close(); }
    }

    private static void MainPanelEmbeddedXamlParses()
    {
        using var stream = typeof(MainWindow).Assembly.GetManifestResourceStream("MicMute.MainWindow.xaml");
        Check.True(stream != null, "main panel resource must be embedded");
        using var reader = new System.IO.StreamReader(stream!);
        string xaml = System.Text.RegularExpressions.Regex.Replace(reader.ReadToEnd(), @"\s+x:Class=""[^""]+""", "");
        xaml = System.Text.RegularExpressions.Regex.Replace(xaml, @"\s+(Click|MouseLeftButtonDown|SelectionChanged|Checked|Unchecked|ValueChanged|LostFocus|KeyDown|TextChanged)=""[^""]+""", "");
        var window = (System.Windows.Window)System.Windows.Markup.XamlReader.Parse(xaml);
        try
        {
            foreach (string name in new[] { "btnStateToggle", "cbDevices", "cbStartMinimized", "txtOsdDuration", "btnResetData", "tbStoragePath" })
                Check.True(window.FindName(name) != null, "missing named control: " + name);
        }
        finally { window.Close(); }
    }

    private static void DispatcherCoalescesRefreshesAndCancelsDisposedWork()
    {
        var dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
        using var debouncer = new DispatcherDebouncer(dispatcher);
        using var disposed = new DispatcherDebouncer(dispatcher);
        int calls = 0, value = 0;
        for (int i = 0; i < 100; i++)
        {
            int captured = i;
            debouncer.Schedule(TimeSpan.FromMilliseconds(10), () => { calls++; value = captured; });
        }
        disposed.Schedule(TimeSpan.FromMilliseconds(10), () => calls += 1000);
        disposed.Dispose();
        var frame = new System.Windows.Threading.DispatcherFrame();
        var timeout = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        timeout.Tick += (_, _) => { timeout.Stop(); frame.Continue = false; };
        timeout.Start();
        System.Windows.Threading.Dispatcher.PushFrame(frame);
        Check.Equal(1, calls);
        Check.Equal(99, value);
    }

    private static void RestartWaitsUntilParentActuallyExits()
    {
        var info = new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, CreateNoWindow = true };
        info.ArgumentList.Add("--wait-fixture");
        using var child = System.Diagnostics.Process.Start(info)!;
        Check.True(!UiBehavior.WaitForParentExit(child.Id, TimeSpan.FromMilliseconds(5)), "must not continue while parent runs");
        Check.True(UiBehavior.WaitForParentExit(child.Id, TimeSpan.FromSeconds(5)), "must continue after parent exits");
    }

    private static void RejectsNonFiniteAndOutOfRangeDurations()
    {
        CultureInfo culture = CultureInfo.InvariantCulture;
        Check.True(!UiBehavior.TryParseOsdDuration("NaN", culture, out _), "NaN must not enter settings");
        Check.True(!UiBehavior.TryParseOsdDuration("Infinity", culture, out _), "Infinity must not enter settings");
        Check.True(!UiBehavior.TryParseOsdDuration("30.1", culture, out _), "duration above the UI limit must be rejected");
        Check.True(!UiBehavior.TryParseOsdDuration("0.09", culture, out _), "duration below the UI limit must be rejected");
    }

    private static void FormatsDurationUsingTheRequestedCulture()
    {
        Check.Equal("1,5", UiBehavior.FormatOsdDuration(1.5, CultureInfo.GetCultureInfo("fr-FR")));
        Check.Equal("1.5", UiBehavior.FormatOsdDuration(1.5, CultureInfo.InvariantCulture));
    }

    private static void BoundsTrayTooltipToShellLimit()
    {
        string tooltip = UiBehavior.LimitTooltip(new string('x', 200));
        Check.True(tooltip.Length <= 127, "notify icon tooltip must stay below the shell's 128 character limit");
        Check.Equal("", UiBehavior.LimitTooltip(null));
    }

    private static void CentersPhysicalWindowRectInPhysicalScreenBounds()
    {
        PixelRect centered = UiBehavior.CenterInPixels(new PixelRect(-1920, 0, 1920, 1080), new PixelSize(360, 120));
        Check.Equal(-1140, centered.Left);
        Check.Equal(480, centered.Top);
        Check.Equal(360, centered.Width);
        Check.Equal(120, centered.Height);
    }

    private static void ExplicitStartupArgumentsOverrideStoredPreference()
    {
        Check.True(UiBehavior.ShouldStartMinimized(true, Array.Empty<string>()), "stored minimized preference should apply without arguments");
        Check.True(!UiBehavior.ShouldStartMinimized(true, new[] { "--show" }), "show argument should override stored preference");
        Check.True(UiBehavior.ShouldStartMinimized(false, new[] { "--minimized" }), "minimized argument should override stored preference");
    }

    private static void RestartArgumentsIdentifyParentAndRequestVisibleWindow()
    {
        string args = UiBehavior.BuildRestartArguments(1234, showWindow: true);
        Check.True(args.Contains("--wait-for-parent 1234", StringComparison.Ordinal), "child must know which parent to wait for");
        Check.True(args.Contains("--show", StringComparison.Ordinal), "restart should explicitly restore the window");
    }

    private static void RefreshGenerationInvalidatesOlderCallbacks()
    {
        var gate = new RefreshGeneration();
        long first = gate.Next();
        long second = gate.Next();
        Check.True(!gate.IsCurrent(first), "a newer refresh request must cancel the older callback");
        Check.True(gate.IsCurrent(second), "the newest refresh request remains runnable");
        gate.Dispose();
        Check.True(!gate.IsCurrent(second), "disposed refresh work must not update UI");
    }
}
