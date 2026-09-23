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
        test(nameof(OsdSliderCoversEveryAcceptedDuration), OsdSliderCoversEveryAcceptedDuration);
        test(nameof(BoundsTrayTooltipToShellLimit), BoundsTrayTooltipToShellLimit);
        test(nameof(CentersPhysicalWindowRectInPhysicalScreenBounds), CentersPhysicalWindowRectInPhysicalScreenBounds);
        test(nameof(ExplicitStartupArgumentsOverrideStoredPreference), ExplicitStartupArgumentsOverrideStoredPreference);
        test(nameof(RestartArgumentsIdentifyParentAndRequestVisibleWindow), RestartArgumentsIdentifyParentAndRequestVisibleWindow);
        test(nameof(RefreshGenerationInvalidatesOlderCallbacks), RefreshGenerationInvalidatesOlderCallbacks);
        test(nameof(OsdResourceLoadsUsingProductionConstructor), OsdResourceLoadsUsingProductionConstructor);
        test(nameof(OsdPaletteFollowsLightMode), OsdPaletteFollowsLightMode);
        test(nameof(MainPanelEmbeddedXamlParses), MainPanelEmbeddedXamlParses);
        test(nameof(BrokenLooseXamlFallsBackToEmbeddedPanel), BrokenLooseXamlFallsBackToEmbeddedPanel);
        test(nameof(IncompleteLooseXamlFallsBackToEmbeddedPanel), IncompleteLooseXamlFallsBackToEmbeddedPanel);
        test(nameof(RestartWaitsUntilParentActuallyExits), RestartWaitsUntilParentActuallyExits);
        test(nameof(DispatcherCoalescesRefreshesAndCancelsDisposedWork), DispatcherCoalescesRefreshesAndCancelsDisposedWork);
        test(nameof(RefreshBurstDoesNotFloodDispatcher), RefreshBurstDoesNotFloodDispatcher);
        test(nameof(LosingFocusCancelsShortcutRecording), LosingFocusCancelsShortcutRecording);
        test(nameof(PressingEscapeCancelsShortcutRecording), PressingEscapeCancelsShortcutRecording);
        test(nameof(MissingMicrophoneIsNotShownAsActive), MissingMicrophoneIsNotShownAsActive);
        test(nameof(TemporaryStatusCannotOverwriteNewerAudioWarning), TemporaryStatusCannotOverwriteNewerAudioWarning);
        test(nameof(StatusBoxFollowsSystemThemeAndMonochromeIcons), StatusBoxFollowsSystemThemeAndMonochromeIcons);
        test(nameof(HeroButtonMatchesAppLogoGeometryAndStateTriggers), HeroButtonMatchesAppLogoGeometryAndStateTriggers);
        test(nameof(AudioFeedbackLoadsDiscordChimesAndSynthesizesFallback), AudioFeedbackLoadsDiscordChimesAndSynthesizesFallback);
        test(nameof(AdaptiveMaxHeightAdaptsToTaskbarAndScreenSize), AdaptiveMaxHeightAdaptsToTaskbarAndScreenSize);
        test(nameof(CentersWindowInVisibleWorkAreaAboveTaskbar), CentersWindowInVisibleWorkAreaAboveTaskbar);
        test(nameof(CentersWindowWhenContentFitsWithoutClipping), CentersWindowWhenContentFitsWithoutClipping);
        test(nameof(ClampsWindowBoundsWithinWorkArea), ClampsWindowBoundsWithinWorkArea);
        test(nameof(NarrowWorkAreaResizesWindowAndAllowsHorizontalAccess), NarrowWorkAreaResizesWindowAndAllowsHorizontalAccess);
        test(nameof(MainWindowAppliesAdaptiveConstraintsAndScrollsOnSmallWorkArea), MainWindowAppliesAdaptiveConstraintsAndScrollsOnSmallWorkArea);
        test(nameof(ApplicationIconIntegrityAndNoLegacyIconsRemain), ApplicationIconIntegrityAndNoLegacyIconsRemain);
        test(nameof(TrayContextMenuHasCompactDimensionsAndRoundedConfiguration), TrayContextMenuHasCompactDimensionsAndRoundedConfiguration);
        test(nameof(MutedIndicatorUsesCommonlyUsedRedAndEliminatesBloodTones), MutedIndicatorUsesCommonlyUsedRedAndEliminatesBloodTones);
        test(nameof(MainWindowCornersHaveNoClippedDropShadowArtifacts), MainWindowCornersHaveNoClippedDropShadowArtifacts);
        test(nameof(SettingsLocationOptionsArePlacedUnderHeaderInEvenRow), SettingsLocationOptionsArePlacedUnderHeaderInEvenRow);
        test(nameof(MainWindowLaunchesAtExactCenterOfScreenAcrossResolutions), MainWindowLaunchesAtExactCenterOfScreenAcrossResolutions);
        test(nameof(SoundVolumeSliderBoundsAndFormatting), SoundVolumeSliderBoundsAndFormatting);
        test(nameof(AudioFeedbackVolumeScaling), AudioFeedbackVolumeScaling);
        test(nameof(ToggleSwitchHoverTriggersOnKnobOnly), ToggleSwitchHoverTriggersOnKnobOnly);
        test(nameof(SoundVolumeSliderDimmingReflectsSoundFeedbackToggle), SoundVolumeSliderDimmingReflectsSoundFeedbackToggle);
        test(nameof(InputBoxesSupportEscapeKeyToRevertAndLoseFocus), InputBoxesSupportEscapeKeyToRevertAndLoseFocus);
        test(nameof(OsdWindowTargetingAndTopmostPersistence), OsdWindowTargetingAndTopmostPersistence);
    }

    private static void ParsesDurationUsingCurrentCultureAndInvariantFallback()
    {
        var french = CultureInfo.GetCultureInfo("fr-FR");
        Check.True(UiBehavior.TryParseOsdDuration("1,5", french, out double currentCulture), "current culture decimal separator should parse");
        Check.Equal(1.5, currentCulture);
        Check.True(UiBehavior.TryParseOsdDuration("1.5", french, out double invariant), "invariant decimal fallback should parse");
        Check.Equal(1.5, invariant);
        Check.True(UiBehavior.TryParseOsdDuration("30sec", CultureInfo.InvariantCulture, out double secVal), "sec suffix should parse");
        Check.Equal(30.0, secVal);
        Check.True(UiBehavior.TryParseOsdDuration("30s", CultureInfo.InvariantCulture, out double sVal), "s suffix should parse");
        Check.Equal(30.0, sVal);
        Check.True(UiBehavior.TryParseOsdDuration("30 seconds", CultureInfo.InvariantCulture, out double secondsVal), "seconds suffix should parse");
        Check.Equal(30.0, secondsVal);
    }

    private static void OsdSliderCoversEveryAcceptedDuration()
    {
        using var audio = new AudioController();
        var window = new MainWindow(audio);
        try
        {
            Check.Equal(UiBehavior.MinimumOsdDuration, window.sliderOsdDuration.Minimum);
            Check.Equal(UiBehavior.MaximumOsdDuration, window.sliderOsdDuration.Maximum);
            Check.Equal(30.0, window.sliderOsdDuration.Maximum);
            window.sliderOsdDuration.Value = 30.0;
            Check.Equal(30.0, window.sliderOsdDuration.Value);
        }
        finally { window.Close(); }
    }

    private static void NarrowWorkAreaResizesWindowAndAllowsHorizontalAccess()
    {
        using var audio = new AudioController();
        var window = new MainWindow(audio);
        try
        {
            window.ApplyAdaptiveBounds(new DipRect(0, 0, 400, 720), isInitialPlacement: true);
            Check.Equal(400.0, window.Width);
            Check.Equal(System.Windows.Controls.ScrollBarVisibility.Auto,
                window.contentScrollViewer.HorizontalScrollBarVisibility);
            window.ApplyAdaptiveBounds(new DipRect(0, 0, 1200, 900), isInitialPlacement: false);
            Check.Equal(412.0, window.Width, "the preferred width must return on a larger monitor");
        }
        finally { window.Close(); }
    }

    private static void BrokenLooseXamlFallsBackToEmbeddedPanel()
    {
        string localFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "micmute-invalid-xaml-" + Guid.NewGuid().ToString("N") + ".xaml");
        System.IO.File.WriteAllText(localFile, "<Window><broken");
        try
        {
            var root = MainWindow.LoadWindowRoot(localFile);
            try { Check.True(root.FindName("btnStateToggle") != null, "the embedded panel must load when the loose file is invalid"); }
            finally { root.Close(); }
        }
        finally { System.IO.File.Delete(localFile); }
    }

    private static void IncompleteLooseXamlFallsBackToEmbeddedPanel()
    {
        string localFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "micmute-incomplete-xaml-" + Guid.NewGuid().ToString("N") + ".xaml");
        System.IO.File.WriteAllText(localFile,
            "<Window xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Grid/></Window>");
        try
        {
            var root = MainWindow.LoadWindowRoot(localFile);
            try { Check.True(root.FindName("btnStateToggle") != null, "an incompatible loose panel must not replace the embedded panel"); }
            finally { root.Close(); }
        }
        finally { System.IO.File.Delete(localFile); }
    }

    private static void OsdPaletteFollowsLightMode()
    {
        var window = new OsdWindow();
        try
        {
            window.ApplyTheme(true);
            var light = (System.Windows.Media.SolidColorBrush)window.borderPanel.Background;
            Check.True(light.Color.R > 200, "light OSD should use a light surface");
            var lightGlyph = (System.Windows.Media.SolidColorBrush)window.activeGlyph.Fill;
            Check.True(lightGlyph.Color.R < 100, "active glyph must contrast with the light surface");
            window.ApplyTheme(false);
            var dark = (System.Windows.Media.SolidColorBrush)window.borderPanel.Background;
            Check.True(dark.Color.R < 60, "dark OSD should use a dark surface");
            var darkGlyph = (System.Windows.Media.SolidColorBrush)window.activeGlyph.Fill;
            Check.True(darkGlyph.Color.R > 180, "active glyph must contrast with the dark surface");
        }
        finally { window.Close(); }
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

    private static void PressingEscapeCancelsShortcutRecording()
    {
        using var audio = new AudioController();
        var window = new RecordingWindow(audio);
        try
        {
            window.btnRecordHotkey.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
            Check.Equal("Cancel", window.btnRecordHotkey.Content as string);
            var source = System.Windows.PresentationSource.FromVisual(window) ?? new System.Windows.Interop.HwndSource(0, 0, 0, 0, 0, "", IntPtr.Zero);
            var keyEvent = new System.Windows.Input.KeyEventArgs(
                System.Windows.Input.Keyboard.PrimaryDevice,
                source,
                0,
                System.Windows.Input.Key.Escape)
            {
                RoutedEvent = System.Windows.Input.Keyboard.PreviewKeyDownEvent
            };
            window.RaiseEvent(keyEvent);
            Check.Equal("Record", window.btnRecordHotkey.Content as string, "Escape must cancel recording without modifying shortcut");
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
            Check.True(window.btnStateToggle.Opacity < 0.6, "unavailable mute control must look disabled");
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

    private static void StatusBoxFollowsSystemThemeAndMonochromeIcons()
    {
        using var audio = new AudioController();
        var window = new MainWindow(audio);
        try
        {
            Check.True(window.borderWarningIcon != null, "borderWarningIcon squircle tile must be present");
            Check.True(window.pathWarningIcon != null, "pathWarningIcon must be present");

            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            
            // Success message should set checkmark icon
            typeof(MainWindow).GetMethod("ShowTemporaryStatus", flags)!.Invoke(window, new object[] { "Shortcut changed successfully." });
            Check.Equal("Shortcut changed successfully.", new System.Windows.Documents.TextRange(window.tbWarningMessage.ContentStart, window.tbWarningMessage.ContentEnd).Text);
            Check.True(window.pathWarningIcon!.Data != null, "pathWarningIcon data must be set");

            // Warning message should set warning triangle icon
            typeof(MainWindow).GetMethod("ShowWarningMessage", flags)!.Invoke(window, new object[] { "No active audio capture devices found.", "" });
            Check.True(window.pathWarningIcon!.Data != null, "pathWarningIcon data must be set on warning");

            // Verify dark mode brushes are monochrome
            typeof(MainWindow).GetMethod("SetLightMode", flags)!.Invoke(window, new object[] { false });
            var darkBg = (System.Windows.Media.SolidColorBrush)window.Resources["WarningBgBrush"];
            var darkBorder = (System.Windows.Media.SolidColorBrush)window.Resources["WarningBorderBrush"];
            var darkText = (System.Windows.Media.SolidColorBrush)window.Resources["WarningTextBrush"];
            Check.True(darkBg.Color.R == darkBg.Color.G && darkBg.Color.G == darkBg.Color.B, "dark WarningBgBrush must be neutral monochrome");
            Check.True(darkBorder.Color.R == darkBorder.Color.G && darkBorder.Color.G == darkBorder.Color.B, "dark WarningBorderBrush must be neutral monochrome");
            Check.True(darkText.Color.R > 200 && darkText.Color.G > 200 && darkText.Color.B > 200, "dark WarningTextBrush must be white/near-white");
            Check.True(Math.Abs(darkText.Color.R - darkText.Color.B) <= 2, "dark WarningTextBrush must be neutral");

            // Verify light mode brushes are monochrome
            typeof(MainWindow).GetMethod("SetLightMode", flags)!.Invoke(window, new object[] { true });
            var lightBg = (System.Windows.Media.SolidColorBrush)window.Resources["WarningBgBrush"];
            var lightBorder = (System.Windows.Media.SolidColorBrush)window.Resources["WarningBorderBrush"];
            var lightText = (System.Windows.Media.SolidColorBrush)window.Resources["WarningTextBrush"];
            Check.True(lightBg.Color.R == lightBg.Color.G && lightBg.Color.G == lightBg.Color.B, "light WarningBgBrush must be neutral monochrome");
            Check.True(lightBorder.Color.R == lightBorder.Color.G && lightBorder.Color.G == lightBorder.Color.B, "light WarningBorderBrush must be neutral monochrome");
            Check.True(lightText.Color.R < 100 && lightText.Color.G < 100 && lightText.Color.B < 100, "light WarningTextBrush must be dark neutral");
            Check.True(lightText.Color.R != 239 && lightText.Color.R != 220, "light WarningTextBrush must not be red");
        }
        finally { window.Close(); }
    }

    private static void AudioFeedbackLoadsDiscordChimesAndSynthesizesFallback()
    {
        AudioFeedback.Initialize();

        string[] resourceNames = typeof(AudioFeedback).Assembly.GetManifestResourceNames();
        Check.True(Array.Exists(resourceNames, r => r == "MicMute.sounds.mute.wav"), "MicMute.sounds.mute.wav must be embedded");
        Check.True(Array.Exists(resourceNames, r => r == "MicMute.sounds.unmute.wav"), "MicMute.sounds.unmute.wav must be embedded");

        using var fallbackMute = AudioFeedback.CreateDiscordChimePlayer(isMuted: true);
        fallbackMute.Load();
        Check.True(fallbackMute.IsLoadCompleted, "fallback mute chime must load into SoundPlayer");

        using var fallbackUnmute = AudioFeedback.CreateDiscordChimePlayer(isMuted: false);
        fallbackUnmute.Load();
        Check.True(fallbackUnmute.IsLoadCompleted, "fallback unmute chime must load into SoundPlayer");

        AudioFeedback.Play(isMuted: true);
        AudioFeedback.Play(isMuted: false);
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
            foreach (string name in new[] { "btnStateToggle", "cbDevices", "cbStartMinimized", "txtOsdDuration", "sliderSoundVolume", "txtSoundVolume", "btnResetData", "tbStoragePath", "contentScrollViewer" })
                Check.True(window.FindName(name) != null, "missing named control: " + name);
            Check.True(window.FindName("contentScrollViewer") is System.Windows.Controls.ScrollViewer, "contentScrollViewer must be a ScrollViewer");
        }
        finally { window.Close(); }
    }

    private static void HeroButtonMatchesAppLogoGeometryAndStateTriggers()
    {
        using var stream = typeof(MainWindow).Assembly.GetManifestResourceStream("MicMute.MainWindow.xaml");
        Check.True(stream != null, "main panel resource must be embedded");
        using var reader = new System.IO.StreamReader(stream!);
        string rawXaml = reader.ReadToEnd();

        // Verify the hero button style and titlebar use the studio condenser app logo vector geometry
        const string appLogoVectorPrefix = "F1M12,14.6207084655762L11.859619140625,14.9596195220947";
        Check.True(rawXaml.Contains(appLogoVectorPrefix), "MainWindow.xaml must contain the app logo vector geometry");

        // Verify 32x32 icon sizing in hero button for prominent glass presence
        Check.True(rawXaml.Contains(@"<Grid Width=""32"" Height=""32"""), "hero button microphone must use 32x32 dimensions");

        // Verify muted trigger in MicToggleButtonStyle sets icon to White to match app logo
        Check.True(rawXaml.Contains(@"<Setter TargetName=""iconPath"" Property=""Fill"" Value=""#FFFFFF"" />"),
            "Muted state must set hero icon to white matching the app logo");

        // Verify blood-red coagulated dark tones are eliminated in favor of standard balanced UI red
        Check.True(!rawXaml.Contains("#FF420D0D"), "Dark coagulated blood tone #FF420D0D must not exist in MainWindow.xaml");
        Check.True(!rawXaml.Contains("#FF6E1515"), "Dark blood tone #FF6E1515 must not exist in MainWindow.xaml");
        Check.True(!rawXaml.Contains("#35801818"), "Dark blood ambient tone #35801818 must not exist in MainWindow.xaml");
        Check.True(rawXaml.Contains("#FFEF4444") && rawXaml.Contains("#FFDC2626"),
            "Hero lens muted state must use standard balanced UI red (#FFEF4444, #FFDC2626)");

        // Verify old dead generic clipart path is completely removed
        Check.True(!rawXaml.Contains("M12,2.5 C9.93,2.5 8.25,4.18"), "Old dead generic microphone path must not exist in MainWindow.xaml");
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

    private static void ApplicationIconIntegrityAndNoLegacyIconsRemain()
    {
        // 1. Verify embedded resource MicMute.app.ico in App assembly
        var assembly = typeof(App).Assembly;
        using var stream = assembly.GetManifestResourceStream("MicMute.app.ico");
        Check.True(stream != null, "MicMute.app.ico embedded resource must exist");
        Check.Equal(21305, stream!.Length);

        // 2. Read resource stream bytes and verify ICO header and 7 frames
        byte[] icoBytes = new byte[stream.Length];
        stream.Seek(0, System.IO.SeekOrigin.Begin);
        stream.Read(icoBytes, 0, icoBytes.Length);

        ushort reserved = BitConverter.ToUInt16(icoBytes, 0);
        ushort type = BitConverter.ToUInt16(icoBytes, 2);
        ushort count = BitConverter.ToUInt16(icoBytes, 4);
        Check.Equal(0, (int)reserved);
        Check.Equal(1, (int)type);
        Check.Equal(7, (int)count);

        byte[] expectedSizes = [16, 24, 32, 48, 64, 128, 0];
        for (int i = 0; i < 7; i++)
        {
            int entryOffset = 6 + i * 16;
            byte w = icoBytes[entryOffset];
            byte h = icoBytes[entryOffset + 1];
            Check.Equal((int)expectedSizes[i], (int)w);
            Check.Equal((int)expectedSizes[i], (int)h);
        }

        // 3. Ensure no stale 116,602-byte legacy icon file exists in the current output directory
        string baseDir = AppContext.BaseDirectory;
        string localIco = System.IO.Path.Combine(baseDir, "app.ico");
        if (System.IO.File.Exists(localIco))
        {
            long len = new System.IO.FileInfo(localIco).Length;
            Check.True(len != 116602, "Legacy 116,602-byte gold icon must not exist in output directory");
            Check.Equal(21305, (int)len);
        }
    }

    private static void TrayContextMenuHasCompactDimensionsAndRoundedConfiguration()
    {
        using var menu = new App.LiquidGlassContextMenu();
        menu.Renderer = new App.LiquidGlassMenuRenderer();
        try
        {
            menu.Font = new System.Drawing.Font("Segoe UI Variable Text", 9.5f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        }
        catch
        {
            menu.Font = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        }

        var item1 = new System.Windows.Forms.ToolStripMenuItem("Toggle Mute") { AutoSize = false, Height = 30, Margin = new System.Windows.Forms.Padding(0), Padding = new System.Windows.Forms.Padding(0) };
        var item2 = new System.Windows.Forms.ToolStripMenuItem("Open App") { AutoSize = false, Height = 30, Margin = new System.Windows.Forms.Padding(0), Padding = new System.Windows.Forms.Padding(0) };
        var item3 = new System.Windows.Forms.ToolStripMenuItem("Quit") { AutoSize = false, Height = 30, Margin = new System.Windows.Forms.Padding(0), Padding = new System.Windows.Forms.Padding(0) };

        menu.Items.Add(item1);
        menu.Items.Add(item2);
        menu.Items.Add(item3);

        Check.True(!menu.ShowImageMargin, "ShowImageMargin must be false");
        Check.True(!menu.ShowCheckMargin, "ShowCheckMargin must be false");
        Check.True(menu.Renderer is App.LiquidGlassMenuRenderer, "Renderer must be LiquidGlassMenuRenderer");
        Check.Equal(3, menu.Items.Count);
        Check.Equal("Toggle Mute", menu.Items[0].Text);
        Check.Equal("Open App", menu.Items[1].Text);
        Check.Equal("Quit", menu.Items[2].Text);

        var preferredSize = menu.GetPreferredSize(System.Drawing.Size.Empty);
        // Compact width: should comfortably fit the items without excessive empty gap (between 145 and 180)
        Check.True(preferredSize.Width >= 145 && preferredSize.Width <= 180, $"Menu preferred width {preferredSize.Width} must be compact");
        // Compact height: 3 items (30px each) + padding should be between 85px and 105px (previously bloated to >135px)
        Check.True(preferredSize.Height >= 85 && preferredSize.Height <= 105, $"Menu preferred height {preferredSize.Height} must be compact and under 105px");
    }

    private static void MutedIndicatorUsesCommonlyUsedRedAndEliminatesBloodTones()
    {
        // Check MainWindow.xaml
        using var mwStream = typeof(MainWindow).Assembly.GetManifestResourceStream("MicMute.MainWindow.xaml");
        Check.True(mwStream != null, "MainWindow.xaml must be embedded");
        using var mwReader = new System.IO.StreamReader(mwStream!);
        string mwXaml = mwReader.ReadToEnd();

        // Check OsdWindow.xaml
        using var osdStream = typeof(OsdWindow).Assembly.GetManifestResourceStream("MicMute.OsdWindow.xaml");
        Check.True(osdStream != null, "OsdWindow.xaml must be embedded");
        using var osdReader = new System.IO.StreamReader(osdStream!);
        string osdXaml = osdReader.ReadToEnd();

        // Assert no coagulated/blood-red color hexes remain
        string[] bloodColors = { "#420D0D", "#6E1515", "#801818", "#E28585" };
        foreach (var c in bloodColors)
        {
            Check.True(!mwXaml.Contains(c), $"MainWindow.xaml must not contain blood color {c}");
            Check.True(!osdXaml.Contains(c), $"OsdWindow.xaml must not contain blood color {c}");
        }

        // Assert standard UI red colors are present
        Check.True(mwXaml.Contains("#FFEF4444"), "MainWindow.xaml must use standard UI red #FFEF4444");
        Check.True(mwXaml.Contains("#FFF87171"), "MainWindow.xaml must use standard UI red highlight #FFF87171");
        Check.True(osdXaml.Contains("#F87171"), "OsdWindow.xaml must use standard UI red #F87171");

        // Assert OsdWindow runtime brushes use standard UI red and not legacy muddy crimson
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;
        var lightMutedField = typeof(OsdWindow).GetField("BrushLightMuted", flags);
        Check.True(lightMutedField != null, "BrushLightMuted field must exist on OsdWindow");
        var lightMuted = (System.Windows.Media.SolidColorBrush)lightMutedField!.GetValue(null)!;
        Check.Equal(System.Windows.Media.Color.FromRgb(0xDC, 0x26, 0x26), lightMuted.Color, "BrushLightMuted must be #DC2626");

        var mutedBorderField = typeof(OsdWindow).GetField("BrushMutedBorder", flags);
        Check.True(mutedBorderField != null, "BrushMutedBorder field must exist on OsdWindow");
        var mutedBorder = (System.Windows.Media.SolidColorBrush)mutedBorderField!.GetValue(null)!;
        Check.Equal(System.Windows.Media.Color.FromRgb(0xDC, 0x26, 0x26), mutedBorder.Color, "BrushMutedBorder must be #DC2626");

        var mutedTextField = typeof(OsdWindow).GetField("BrushMutedText", flags);
        Check.True(mutedTextField != null, "BrushMutedText field must exist on OsdWindow");
        var mutedText = (System.Windows.Media.SolidColorBrush)mutedTextField!.GetValue(null)!;
        Check.Equal(System.Windows.Media.Color.FromRgb(0xF8, 0x71, 0x71), mutedText.Color, "BrushMutedText must be #F87171");
    }

    private static void MainWindowCornersHaveNoClippedDropShadowArtifacts()
    {
        using var stream = typeof(MainWindow).Assembly.GetManifestResourceStream("MicMute.MainWindow.xaml");
        Check.True(stream != null, "MainWindow.xaml resource must be embedded");
        using var reader = new System.IO.StreamReader(stream!);
        string rawXaml = reader.ReadToEnd();

        // The outer window border must not have a DropShadowEffect which causes light-black sharp edge artifacts in the 4 corners
        Check.True(!rawXaml.Contains(@"<DropShadowEffect BlurRadius=""28"""),
            "Outer window border must not have BlurRadius=28 DropShadowEffect");

        // Verify root Border has CornerRadius 20 and no Effect
        int borderIdx = rawXaml.IndexOf("<!-- Outer Window Frame (iOS Liquid Glass Shell) -->", StringComparison.Ordinal);
        Check.True(borderIdx >= 0, "Outer window frame comment must exist");
        string borderSection = rawXaml.Substring(borderIdx, 300);
        Check.True(!borderSection.Contains("<Border.Effect>"), "Outer window border must not have <Border.Effect>");
    }

    private static void SettingsLocationOptionsArePlacedUnderHeaderInEvenRow()
    {
        using var stream = typeof(MainWindow).Assembly.GetManifestResourceStream("MicMute.MainWindow.xaml");
        Check.True(stream != null, "MainWindow.xaml resource must be embedded");
        using var reader = new System.IO.StreamReader(stream!);
        string rawXaml = reader.ReadToEnd();

        // 1. Verify Settings Location and description exist
        int groupStart = rawXaml.IndexOf("Settings Location", StringComparison.Ordinal);
        Check.True(groupStart >= 0, "Settings Location must exist in MainWindow.xaml");

        int descIdx = rawXaml.IndexOf("Configuration file and data directory", groupStart, StringComparison.Ordinal);
        Check.True(descIdx > groupStart, "Configuration file and data directory must exist");

        // 2. Verify btnOpenFolder, btnChangeFolder, btnResetData are placed AFTER description text
        int btnOpenIdx = rawXaml.IndexOf(@"x:Name=""btnOpenFolder""", descIdx, StringComparison.Ordinal);
        Check.True(btnOpenIdx > descIdx, "btnOpenFolder must be placed after the description text");

        int btnChangeIdx = rawXaml.IndexOf(@"x:Name=""btnChangeFolder""", btnOpenIdx, StringComparison.Ordinal);
        Check.True(btnChangeIdx > btnOpenIdx, "btnChangeFolder must follow btnOpenFolder");

        int btnResetIdx = rawXaml.IndexOf(@"x:Name=""btnResetData""", btnChangeIdx, StringComparison.Ordinal);
        Check.True(btnResetIdx > btnChangeIdx, "btnResetData must follow btnChangeFolder");

        // 3. Verify all 3 buttons are in a grid with even spacing (* columns and 8px gaps)
        Check.True(rawXaml.Contains(@"<ColumnDefinition Width=""*"" />"), "Must contain * width columns for buttons");
        Check.True(rawXaml.Contains(@"<ColumnDefinition Width=""8"" />"), "Must contain even 8px gaps between buttons");
    }

    private static void MainWindowLaunchesAtExactCenterOfScreenAcrossResolutions()
    {
        // 1. Theoretical centering calculations across common resolutions
        // 1080p (1920x1080, 48px taskbar -> 1032 work area)
        var wa1080p = new DipRect(0, 0, 1920, 1032);
        var bounds1080p = UiBehavior.CalculateCenteredWindowBounds(wa1080p, 412, 849);
        Check.Equal(412.0, bounds1080p.Width);
        Check.Equal(849.0, bounds1080p.Height);
        Check.Equal(754.0, bounds1080p.Left);
        Check.Equal(91.5, bounds1080p.Top);
        double topSpace1080 = bounds1080p.Top - wa1080p.Top;
        double bottomSpace1080 = (wa1080p.Top + wa1080p.Height) - (bounds1080p.Top + bounds1080p.Height);
        Check.Equal(topSpace1080, bottomSpace1080, "1080p top and bottom margins must be identical");

        // 1440p (2560x1440, 48px taskbar -> 1392 work area)
        var wa1440p = new DipRect(0, 0, 2560, 1392);
        var bounds1440p = UiBehavior.CalculateCenteredWindowBounds(wa1440p, 412, 849);
        Check.Equal(1074.0, bounds1440p.Left);
        Check.Equal(271.5, bounds1440p.Top);
        double topSpace1440 = bounds1440p.Top - wa1440p.Top;
        double bottomSpace1440 = (wa1440p.Top + wa1440p.Height) - (bounds1440p.Top + bounds1440p.Height);
        Check.Equal(topSpace1440, bottomSpace1440, "1440p top and bottom margins must be identical");

        // 4K (3840x2160, 48px taskbar -> 2112 work area)
        var wa4K = new DipRect(0, 0, 3840, 2112);
        var bounds4K = UiBehavior.CalculateCenteredWindowBounds(wa4K, 412, 849);
        Check.Equal(1714.0, bounds4K.Left);
        Check.Equal(631.5, bounds4K.Top);
        double topSpace4K = bounds4K.Top - wa4K.Top;
        double bottomSpace4K = (wa4K.Top + wa4K.Height) - (bounds4K.Top + bounds4K.Height);
        Check.Equal(topSpace4K, bottomSpace4K, "4K top and bottom margins must be identical");

        // Offset multi-monitor work area (e.g. secondary monitor at X: 1920, Y: 60, Size: 1920x1020)
        var waSecondary = new DipRect(1920, 60, 1920, 1020);
        var boundsSecondary = UiBehavior.CalculateCenteredWindowBounds(waSecondary, 412, 849);
        Check.Equal(1920.0 + 754.0, boundsSecondary.Left);
        double topSpaceSec = boundsSecondary.Top - waSecondary.Top;
        double bottomSpaceSec = (waSecondary.Top + waSecondary.Height) - (boundsSecondary.Top + boundsSecondary.Height);
        Check.Equal(topSpaceSec, bottomSpaceSec, "Secondary monitor top and bottom margins must be identical");

        // 2. Real window instantiation and rendering test
        using var audio = new AudioController();
        var window = new MainWindow(audio);
        try
        {
            var primary = System.Windows.Forms.Screen.PrimaryScreen;
            double scaleX = 1.0, scaleY = 1.0;
            var dpi = System.Windows.Media.VisualTreeHelper.GetDpi(window);
            if (dpi.DpiScaleX > 0) scaleX = dpi.DpiScaleX;
            if (dpi.DpiScaleY > 0) scaleY = dpi.DpiScaleY;

            double workLeft = (primary?.WorkingArea.Left ?? 0) / scaleX;
            double workTop = (primary?.WorkingArea.Top ?? 0) / scaleY;
            double workWidth = (primary?.WorkingArea.Width ?? 1920) / scaleX;
            double workHeight = (primary?.WorkingArea.Height ?? 1032) / scaleY;
            var currentWorkArea = new DipRect(workLeft, workTop, workWidth, workHeight);

            // On initialization before Show, window must already be centered, not pinned to top
            if (workHeight > 750)
            {
                Check.True(window.Top > 20.0, $"Window Top ({window.Top}) must not be stuck at top (12px) on workHeight {workHeight}");
            }

            // Show window and verify final rendered centering
            window.Show();
            window.UpdateLayout();

            double topMargin = window.Top - currentWorkArea.Top;
            double bottomMargin = (currentWorkArea.Top + currentWorkArea.Height) - (window.Top + window.ActualHeight);
            Check.True(Math.Abs(topMargin - bottomMargin) <= 2.0,
                $"Window must be vertically centered: topMargin={topMargin}, bottomMargin={bottomMargin}");

            double leftMargin = window.Left - currentWorkArea.Left;
            double rightMargin = (currentWorkArea.Left + currentWorkArea.Width) - (window.Left + window.ActualWidth);
            Check.True(Math.Abs(leftMargin - rightMargin) <= 2.0,
                $"Window must be horizontally centered: leftMargin={leftMargin}, rightMargin={rightMargin}");
        }
        finally
        {
            window.Close();
        }
    }

    private static void SoundVolumeSliderBoundsAndFormatting()
    {
        // 1. Check constants
        Check.Equal(0, UiBehavior.MinimumSoundVolume);
        Check.Equal(100, UiBehavior.MaximumSoundVolume);
        Check.Equal(100, UiBehavior.DefaultSoundVolume);

        // 2. Formatting
        Check.Equal("100", UiBehavior.FormatSoundVolume(100));
        Check.Equal("50", UiBehavior.FormatSoundVolume(50));
        Check.Equal("0", UiBehavior.FormatSoundVolume(0));
        Check.Equal("0", UiBehavior.FormatSoundVolume(-10));
        Check.Equal("100", UiBehavior.FormatSoundVolume(120));

        // 3. Parsing
        Check.True(UiBehavior.TryParseSoundVolume("100", out int v1) && v1 == 100);
        Check.True(UiBehavior.TryParseSoundVolume("50", out int v2) && v2 == 50);
        Check.True(UiBehavior.TryParseSoundVolume("0", out int v3) && v3 == 0);
        Check.True(UiBehavior.TryParseSoundVolume(" 75% ", out int v4) && v4 == 75);
        Check.True(UiBehavior.TryParseSoundVolume("25 %", out int v5) && v5 == 25);
        Check.True(!UiBehavior.TryParseSoundVolume("-5", out _));
        Check.True(!UiBehavior.TryParseSoundVolume("105", out _));
        Check.True(!UiBehavior.TryParseSoundVolume("abc", out _));
        Check.True(!UiBehavior.TryParseSoundVolume("", out _));
        Check.True(!UiBehavior.TryParseSoundVolume("   ", out _));

        // 4. Verify slider control bindings from embedded XAML
        using var stream = typeof(MainWindow).Assembly.GetManifestResourceStream("MicMute.MainWindow.xaml");
        Check.True(stream != null, "MainWindow.xaml must be embedded");
        using var reader = new System.IO.StreamReader(stream!);
        string xaml = System.Text.RegularExpressions.Regex.Replace(reader.ReadToEnd(), @"\s+x:Class=""[^""]+""", "");
        xaml = System.Text.RegularExpressions.Regex.Replace(xaml, @"\s+(Click|MouseLeftButtonDown|SelectionChanged|Checked|Unchecked|ValueChanged|LostFocus|KeyDown|TextChanged)=""[^""]+""", "");
        var window = (System.Windows.Window)System.Windows.Markup.XamlReader.Parse(xaml);
        try
        {
            var slider = window.FindName("sliderSoundVolume") as System.Windows.Controls.Slider;
            Check.True(slider != null, "sliderSoundVolume must exist");
            Check.Equal(0.0, slider!.Minimum);
            Check.Equal(100.0, slider.Maximum);
            Check.True(slider.IsSnapToTickEnabled);
            Check.Equal(1.0, slider.TickFrequency);

            var txt = window.FindName("txtSoundVolume") as System.Windows.Controls.TextBox;
            Check.True(txt != null, "txtSoundVolume must exist");
        }
        finally
        {
            window.Close();
        }
    }

    private static void AudioFeedbackVolumeScaling()
    {
        AudioFeedback.Initialize();
        AudioFeedback.SetVolume(75);
        Check.Equal(75, AudioFeedback.CurrentVolume);

        AudioFeedback.SetVolume(0);
        Check.Equal(0, AudioFeedback.CurrentVolume);

        // Verify Play at 0% does not throw and skips execution
        AudioFeedback.Play(isMuted: true);
        AudioFeedback.Play(isMuted: false);

        // Verify ScaleWavVolume scales PCM samples linearly
        byte[] testPcmWav = AudioFeedback.SynthesizeDiscordChimeBytes(isMuted: true, volume: 1.0);
        byte[] halfPcmWav = AudioFeedback.ScaleWavVolume(testPcmWav, 0.5f);
        byte[] zeroPcmWav = AudioFeedback.ScaleWavVolume(testPcmWav, 0.0f);

        Check.True(testPcmWav.Length == halfPcmWav.Length, "WAV length must be preserved");
        Check.True(testPcmWav.Length == zeroPcmWav.Length, "WAV length must be preserved");

        // Verify maximum amplitude of halfPcm is roughly half of testPcm
        short maxTest = 0;
        short maxHalf = 0;
        for (int i = 44; i + 1 < testPcmWav.Length; i += 2)
        {
            short s1 = Math.Abs(BitConverter.ToInt16(testPcmWav, i));
            short s2 = Math.Abs(BitConverter.ToInt16(halfPcmWav, i));
            if (s1 > maxTest) maxTest = s1;
            if (s2 > maxHalf) maxHalf = s2;
        }
        Check.True(maxTest > 5000, "original chime must have audible amplitude");
        Check.True(Math.Abs(maxHalf - (maxTest / 2)) < 50, "50% volume must scale samples to half");

        // Verify zeroPcm is completely silent
        short maxZero = 0;
        for (int i = 44; i + 1 < zeroPcmWav.Length; i += 2)
        {
            short s0 = Math.Abs(BitConverter.ToInt16(zeroPcmWav, i));
            if (s0 > maxZero) maxZero = s0;
        }
        Check.Equal((short)0, maxZero);

        // Reset volume to 100%
        AudioFeedback.SetVolume(100);
        Check.Equal(100, AudioFeedback.CurrentVolume);
    }

    private static void ToggleSwitchHoverTriggersOnKnobOnly()
    {
        using var audio = new AudioController();
        var window = new MainWindow(audio);
        try
        {
            var style = (System.Windows.Style)window.FindResource("ToggleSwitchStyle");
            Check.True(style != null, "ToggleSwitchStyle must exist in resources");

            foreach (var setter in style!.Setters)
            {
                if (setter is System.Windows.Setter s && s.Property == System.Windows.FrameworkElement.CursorProperty)
                {
                    Check.True(false, "ToggleSwitchStyle should not force Cursor=Hand across the entire button");
                }
            }

            var template = window.cbEnableOsd.Template;
            Check.True(template != null, "ToggleSwitchStyle must have a ControlTemplate");

            bool foundKnobHoverTrigger = false;
            bool foundRootHoverTrigger = false;
            foreach (var triggerBase in template!.Triggers)
            {
                if (triggerBase is System.Windows.Trigger trigger &&
                    trigger.Property == System.Windows.UIElement.IsMouseOverProperty)
                {
                    if (trigger.SourceName == "knob")
                    {
                        foundKnobHoverTrigger = true;
                    }
                    else if (string.IsNullOrEmpty(trigger.SourceName))
                    {
                        foundRootHoverTrigger = true;
                    }
                }
            }

            Check.True(foundKnobHoverTrigger, "ToggleSwitchStyle hover trigger must target the knob element");
            Check.True(!foundRootHoverTrigger, "ToggleSwitchStyle must not trigger hover on the entire root CheckBox");
        }
        finally
        {
            window.Close();
        }
    }

    private static void SoundVolumeSliderDimmingReflectsSoundFeedbackToggle()
    {
        using var audio = new AudioController();
        var window = new MainWindow(audio);
        try
        {
            Check.True(window.panelSoundVolume != null, "panelSoundVolume must be bound");

            // Enabled state
            window.cbSoundFeedback.IsChecked = true;
            Check.True(window.panelSoundVolume!.IsEnabled, "panelSoundVolume must be enabled when SoundFeedback is checked");
            Check.Equal(1.0, window.panelSoundVolume.Opacity, "panelSoundVolume opacity must be 1.0 when enabled");

            // Disabled state
            window.cbSoundFeedback.IsChecked = false;
            Check.True(!window.panelSoundVolume.IsEnabled, "panelSoundVolume must be disabled when SoundFeedback is unchecked");
            Check.Equal(0.45, window.panelSoundVolume.Opacity, "panelSoundVolume opacity must be 0.45 when dimmed");

            // Re-enabled state
            window.cbSoundFeedback.IsChecked = true;
            Check.True(window.panelSoundVolume.IsEnabled, "panelSoundVolume must re-enable when SoundFeedback is checked");
            Check.Equal(1.0, window.panelSoundVolume.Opacity, "panelSoundVolume opacity must restore to 1.0");
        }
        finally
        {
            window.Close();
        }
    }

    private static void InputBoxesSupportEscapeKeyToRevertAndLoseFocus()
    {
        using var audio = new AudioController();
        var window = new MainWindow(audio);
        try
        {
            var initialSettings = SettingsManager.Load();

            // Test txtOsdDuration Escape handling
            window.txtOsdDuration.Text = "99";
            var source = System.Windows.PresentationSource.FromVisual(window) ?? new System.Windows.Interop.HwndSource(0, 0, 0, 0, 0, "", IntPtr.Zero);
            var escOsd = new System.Windows.Input.KeyEventArgs(
                System.Windows.Input.Keyboard.PrimaryDevice,
                source,
                0,
                System.Windows.Input.Key.Escape)
            {
                RoutedEvent = System.Windows.UIElement.KeyDownEvent
            };
            window.txtOsdDuration.RaiseEvent(escOsd);
            Check.Equal(UiBehavior.FormatOsdDuration(initialSettings.OsdDuration, CultureInfo.CurrentCulture), window.txtOsdDuration.Text);

            // Test txtSoundVolume Escape handling
            window.txtSoundVolume.Text = "99";
            var escVol = new System.Windows.Input.KeyEventArgs(
                System.Windows.Input.Keyboard.PrimaryDevice,
                source,
                0,
                System.Windows.Input.Key.Escape)
            {
                RoutedEvent = System.Windows.UIElement.KeyDownEvent
            };
            window.txtSoundVolume.RaiseEvent(escVol);
            Check.Equal(UiBehavior.FormatSoundVolume(initialSettings.SoundVolume), window.txtSoundVolume.Text);
        }
        finally
        {
            window.Close();
        }
    }

    private static void OsdWindowTargetingAndTopmostPersistence()
    {
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;
        var swpFrameChanged = typeof(OsdWindow).GetField("SWP_FRAMECHANGED", flags);
        Check.True(swpFrameChanged != null, "SWP_FRAMECHANGED must be defined");
        Check.Equal((uint)0x0020, (uint)swpFrameChanged!.GetValue(null)!);

        var swpNoOwnerZOrder = typeof(OsdWindow).GetField("SWP_NOOWNERZORDER", flags);
        Check.True(swpNoOwnerZOrder != null, "SWP_NOOWNERZORDER must be defined");
        Check.Equal((uint)0x0200, (uint)swpNoOwnerZOrder!.GetValue(null)!);

        var getFgMethod = typeof(OsdWindow).GetMethod("GetForegroundWindow", flags);
        Check.True(getFgMethod != null, "GetForegroundWindow P/Invoke must be defined on OsdWindow");

        var window = new OsdWindow();
        try
        {
            Check.True(window.Topmost, "OsdWindow must be configured as Topmost");
            Check.True(!window.ShowActivated, "OsdWindow must not show activated so games keep focus");
            Check.Equal(System.Windows.WindowStartupLocation.Manual, window.WindowStartupLocation);

            IntPtr handle = new System.Windows.Interop.WindowInteropHelper(window).EnsureHandle();
            Check.True(handle != IntPtr.Zero, "OsdWindow handle must be valid");

            var positionMethod = typeof(OsdWindow).GetMethod("PositionOnActiveScreen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Check.True(positionMethod != null, "PositionOnActiveScreen must exist");
            positionMethod!.Invoke(window, new object[] { handle });

            Check.True(!double.IsNaN(window.Left), "window.Left must be synchronized with DIP coordinates (not NaN)");
            Check.True(!double.IsNaN(window.Top), "window.Top must be synchronized with DIP coordinates (not NaN)");
            Check.True(double.IsFinite(window.Left), "window.Left must be finite");
            Check.True(double.IsFinite(window.Top), "window.Top must be finite");
        }
        finally
        {
            window.Close();
        }
    }
}
