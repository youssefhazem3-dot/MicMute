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
        test(nameof(RefreshBurstDoesNotFloodDispatcher), RefreshBurstDoesNotFloodDispatcher);
        test(nameof(LosingFocusCancelsShortcutRecording), LosingFocusCancelsShortcutRecording);
        test(nameof(MissingMicrophoneIsNotShownAsActive), MissingMicrophoneIsNotShownAsActive);
        test(nameof(TemporaryStatusCannotOverwriteNewerAudioWarning), TemporaryStatusCannotOverwriteNewerAudioWarning);
        test(nameof(AdaptiveMaxHeightAdaptsToTaskbarAndScreenSize), AdaptiveMaxHeightAdaptsToTaskbarAndScreenSize);
        test(nameof(CentersWindowInVisibleWorkAreaAboveTaskbar), CentersWindowInVisibleWorkAreaAboveTaskbar);
        test(nameof(CentersWindowWhenContentFitsWithoutClipping), CentersWindowWhenContentFitsWithoutClipping);
        test(nameof(ClampsWindowBoundsWithinWorkArea), ClampsWindowBoundsWithinWorkArea);
        test(nameof(MainWindowAppliesAdaptiveConstraintsAndScrollsOnSmallWorkArea), MainWindowAppliesAdaptiveConstraintsAndScrollsOnSmallWorkArea);
    }

    private static void ParsesDurationUsingCurrentCultureAndInvariantFallback()
    {
        var french = CultureInfo.GetCultureInfo("fr-FR");
        Check.True(UiBehavior.TryParseOsdDuration("1,5", french, out double currentCulture), "current culture decimal separator should parse");
        Check.Equal(1.5, currentCulture);
        Check.True(UiBehavior.TryParseOsdDuration("1.5", french, out double invariant), "invariant decimal fallback should parse");
        Check.Equal(1.5, invariant);
    }

    private static void RefreshBurstDoesNotFloodDispatcher()
    {
        var dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
        using var debouncer = new DispatcherDebouncer(dispatcher);
        int posted = 0;
        System.Windows.Threading.DispatcherHookEventHandler hook = (_, _) => System.Threading.Interlocked.Increment(ref posted);
        dispatcher.Hooks.OperationPosted += hook;
        try
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                for (int i = 0; i < 1000; i++) debouncer.Schedule(TimeSpan.FromMilliseconds(10), () => { });
            }).GetAwaiter().GetResult();
        }
        finally { dispatcher.Hooks.OperationPosted -= hook; }
        Console.WriteLine("METRIC refresh burst dispatcher operations: " + posted);
        Check.True(posted <= 1, "1000 refresh requests must enqueue at most one dispatcher operation; actual " + posted);
    }

    private static void LosingFocusCancelsShortcutRecording()
    {
        using var audio = new AudioController();
        var window = new RecordingWindow(audio);
        try
        {
            window.btnRecordHotkey.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
            Check.Equal("Cancel", window.btnRecordHotkey.Content as string);
            window.LoseFocus();
            Check.Equal("Record", window.btnRecordHotkey.Content as string, "deactivation must restore the saved shortcut");
        }
        finally { window.Close(); }
    }

    private sealed class RecordingWindow : MainWindow
    {
        public RecordingWindow(AudioController audio) : base(audio) { }
        public void LoseFocus() => OnDeactivated(EventArgs.Empty);
    }

    private static void MissingMicrophoneIsNotShownAsActive()
    {
        using var audio = new AudioController(); // No endpoint has been selected.
        var window = new MainWindow(audio);
        try
        {
            Check.Equal("NO MICROPHONE", window.tbStatusText.Text);
            Check.True(!window.btnStateToggle.IsEnabled, "mute must be disabled without an endpoint");
            Check.Equal(System.Windows.Visibility.Visible, window.borderWarning.Visibility);
            Check.True(window.cbDevices.SelectedItem == null, "do not imply an unbound microphone is selected");
        }
        finally { window.Close(); }
    }

    private static void TemporaryStatusCannotOverwriteNewerAudioWarning()
    {
        using var audio = new AudioController();
        var window = new MainWindow(audio);
        var previousContext = System.Threading.SynchronizationContext.Current;
        System.Threading.SynchronizationContext.SetSynchronizationContext(
            new System.Windows.Threading.DispatcherSynchronizationContext(window.Dispatcher));
        try
        {
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            typeof(MainWindow).GetMethod("ShowTemporaryStatus", flags)!.Invoke(window, new object[] { "Old status" });
            typeof(MainWindow).GetMethod("AudioController_WarningNotification", flags)!.Invoke(window, new object[] { audio, "New audio error" });
            var frame = new System.Windows.Threading.DispatcherFrame();
            var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(3300) };
            timer.Tick += (_, _) => { timer.Stop(); frame.Continue = false; };
            timer.Start();
            System.Windows.Threading.Dispatcher.PushFrame(frame);
            Check.Equal("New audio error", new System.Windows.Documents.TextRange(window.tbWarningMessage.ContentStart, window.tbWarningMessage.ContentEnd).Text);
            Check.Equal(System.Windows.Visibility.Visible, window.borderWarning.Visibility);
        }
        finally
        {
            System.Threading.SynchronizationContext.SetSynchronizationContext(previousContext);
            window.Close();
        }
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
            foreach (string name in new[] { "btnStateToggle", "cbDevices", "cbStartMinimized", "txtOsdDuration", "btnResetData", "tbStoragePath", "contentScrollViewer" })
                Check.True(window.FindName(name) != null, "missing named control: " + name);
            Check.True(window.FindName("contentScrollViewer") is System.Windows.Controls.ScrollViewer, "contentScrollViewer must be a ScrollViewer");
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

    private static void AdaptiveMaxHeightAdaptsToTaskbarAndScreenSize()
    {
        // Screen with taskbar present (e.g. 768 - 48 = 720 work area)
        double withTaskbar = UiBehavior.CalculateAdaptiveMaxHeight(720);
        Check.Equal(696.0, withTaskbar);

        // Screen with taskbar hidden (e.g. 768 full screen work area)
        double taskbarHidden = UiBehavior.CalculateAdaptiveMaxHeight(768);
        Check.Equal(744.0, taskbarHidden);

        // 1080p screen with taskbar (1080 - 48 = 1032 work area)
        double monitor1080p = UiBehavior.CalculateAdaptiveMaxHeight(1032);
        Check.Equal(1008.0, monitor1080p);

        // Very small screen: must not shrink below minimum height
        double verySmall = UiBehavior.CalculateAdaptiveMaxHeight(200);
        Check.Equal(UiBehavior.MinimumWindowHeight, verySmall);

        // Non-finite fallbacks
        Check.Equal(750.0, UiBehavior.CalculateAdaptiveMaxHeight(double.NaN));
        Check.Equal(750.0, UiBehavior.CalculateAdaptiveMaxHeight(-10));
    }

    private static void CentersWindowInVisibleWorkAreaAboveTaskbar()
    {
        // 1366x768 screen with 48px taskbar at bottom -> WorkArea: (0, 0, 1366, 720)
        var workArea = new DipRect(0, 0, 1366, 720);
        var bounds = UiBehavior.CalculateCenteredWindowBounds(workArea, 350, 770);

        // Max allowed height should be 696 (720 - 24)
        Check.Equal(696.0, bounds.Height);
        Check.Equal(350.0, bounds.Width);
        Check.Equal(508.0, bounds.Left); // (1366 - 350) / 2
        Check.Equal(12.0, bounds.Top);   // (720 - 696) / 2
        Check.Equal(708.0, bounds.Top + bounds.Height);
        Check.True(bounds.Top + bounds.Height < 720.0, "window bottom must be above taskbar (720)");
        Check.Equal(12.0, 720.0 - (bounds.Top + bounds.Height)); // 12 DIP breathing room above taskbar
    }

    private static void CentersWindowWhenContentFitsWithoutClipping()
    {
        // 1920x1080 screen with 48px taskbar -> WorkArea: (0, 0, 1920, 1032)
        var workArea = new DipRect(0, 0, 1920, 1032);
        var bounds = UiBehavior.CalculateCenteredWindowBounds(workArea, 350, 770);

        // Content fits comfortably (770 < 1008)
        Check.Equal(770.0, bounds.Height);
        Check.Equal(350.0, bounds.Width);
        Check.Equal(785.0, bounds.Left); // (1920 - 350) / 2
        Check.Equal(131.0, bounds.Top);  // (1032 - 770) / 2
        Check.True(bounds.Top + bounds.Height < 1032.0, "window bottom must be well above taskbar");
    }

    private static void ClampsWindowBoundsWithinWorkArea()
    {
        var workArea = new DipRect(0, 0, 1366, 720);

        // Window dragged too far down towards or under taskbar
        var tooLow = new DipRect(500, 500, 350, 696);
        var clamped = UiBehavior.ClampWindowBoundsToWorkArea(workArea, tooLow);
        Check.Equal(12.0, clamped.Top); // Max allowed top for 696 height is 720 - 696 - 12 = 12
        Check.Equal(708.0, clamped.Top + clamped.Height);

        // Window with NaN values
        var nanBounds = new DipRect(double.NaN, double.NaN, 350, 770);
        var recovered = UiBehavior.ClampWindowBoundsToWorkArea(workArea, nanBounds);
        Check.True(!double.IsNaN(recovered.Top));
        Check.Equal(12.0, recovered.Top);
    }

    private static void MainWindowAppliesAdaptiveConstraintsAndScrollsOnSmallWorkArea()
    {
        using var audio = new AudioController();
        var window = new MainWindow(audio);
        try
        {
            Check.True(window.contentScrollViewer != null, "contentScrollViewer must be bound");
            Check.Equal(System.Windows.Controls.ScrollBarVisibility.Auto, window.contentScrollViewer!.VerticalScrollBarVisibility);

            var primary = System.Windows.Forms.Screen.PrimaryScreen;
            double workHeight = primary != null ? primary.WorkingArea.Height : 720.0;
            double expectedMax = UiBehavior.CalculateAdaptiveMaxHeight(workHeight);
            Check.Equal(expectedMax, window.MaxHeight);

            if (workHeight == 720.0)
            {
                Check.Equal(696.0, window.MaxHeight);
                Check.Equal(12.0, window.Top);
                Check.True(window.Top + window.MaxHeight < 720.0, "Window must fit cleanly above the taskbar");
            }
        }
        finally { window.Close(); }
    }
}
