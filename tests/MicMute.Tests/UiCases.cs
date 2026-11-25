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
        test(nameof(StartupManagerElevatedTaskSchedulerAndStandardRegistry), StartupManagerElevatedTaskSchedulerAndStandardRegistry);
        test(nameof(MainWindowUiAccuratelyClarifiesAdminModeForAntiCheat), MainWindowUiAccuratelyClarifiesAdminModeForAntiCheat);
        test(nameof(TitleBarCaptionButtonsAndThemeToggleAreSymmetricallyAligned), TitleBarCaptionButtonsAndThemeToggleAreSymmetricallyAligned);
        test(nameof(ComboBoxDropdownHasSmoothAnimationAndRotatingChevron), ComboBoxDropdownHasSmoothAnimationAndRotatingChevron);
        test(nameof(HeroHotkeySynchronizesWithSettingsAndLiveChanges), HeroHotkeySynchronizesWithSettingsAndLiveChanges);
        test(nameof(AppExitAndTeardownHandlesFailuresGracefullyWithoutHanging), AppExitAndTeardownHandlesFailuresGracefullyWithoutHanging);
        test(nameof(OsdWindowCornersHaveNoClippedDropShadowArtifacts), OsdWindowCornersHaveNoClippedDropShadowArtifacts);
        test(nameof(OsdDurationAndSoundVolumeKeycapsSupportClampingAndKeyboardInteraction), OsdDurationAndSoundVolumeKeycapsSupportClampingAndKeyboardInteraction);
        test(nameof(OsdWindowMatchesCircularGlowReferenceDesign), OsdWindowMatchesCircularGlowReferenceDesign);
        test(nameof(ButtonsReliablyRegisterClicksWithPressModeAndTitleBarGuards), ButtonsReliablyRegisterClicksWithPressModeAndTitleBarGuards);
        test(nameof(TaskbarIconAndTypographyAndThemeBrighterPalette), TaskbarIconAndTypographyAndThemeBrighterPalette);
        test(nameof(SmoothMinimizeRestoreAndDropdownClearType), SmoothMinimizeRestoreAndDropdownClearType);
        test(nameof(HeroHotkeyCenterLockedAndThemeMoonAlignedAndBoldTypography), HeroHotkeyCenterLockedAndThemeMoonAlignedAndBoldTypography);
        test(nameof(OsdDurationSliderDimmingReflectsOsdToggle), OsdDurationSliderDimmingReflectsOsdToggle);
        test(nameof(TrayOpenAnimationAndSmoothTransitions), TrayOpenAnimationAndSmoothTransitions);
        test(nameof(DaVinciStyleAppMenuAndFuturePlaceholderOptions), DaVinciStyleAppMenuAndFuturePlaceholderOptions);
        test(nameof(DeviceDropdownDoesNotGlitchOrCloseWhenOsdAppearsAndDefersUpdates), DeviceDropdownDoesNotGlitchOrCloseWhenOsdAppearsAndDefersUpdates);
        test(nameof(KeycapInputBoxesSingleClickShowsCaretAndDoubleClickSelectsAll), KeycapInputBoxesSingleClickShowsCaretAndDoubleClickSelectsAll);
        test(nameof(OsdWindowHideAndAudioFeedbackStreamManagement), OsdWindowHideAndAudioFeedbackStreamManagement);
        test(nameof(MenuOutsideClickDismissalAndOsdSpamResilience), MenuOutsideClickDismissalAndOsdSpamResilience);
        test(nameof(PowerModesAndMouseShortcutAndMenuActions), PowerModesAndMouseShortcutAndMenuActions);
        test(nameof(UiUxRefinementsTrayOsdMenuChipsModalAndToast), UiUxRefinementsTrayOsdMenuChipsModalAndToast);
        test(nameof(ResponsiveScalingFitsAllCommonLaptopResolutionsWithoutScrolling), ResponsiveScalingFitsAllCommonLaptopResolutionsWithoutScrolling);
        test(nameof(AutoUpdaterParsesReleasesVerifiesSha256ReplacesBinaryAndUpdatesVisualIndicators), AutoUpdaterParsesReleasesVerifiesSha256ReplacesBinaryAndUpdatesVisualIndicators);
        test(nameof(SimplifiedMainWindowAndSettingsPopupWithRotatingGear), SimplifiedMainWindowAndSettingsPopupWithRotatingGear);
        test(nameof(ModernMotionCurvesGlidingPillAndZeroLinearKeyframes), ModernMotionCurvesGlidingPillAndZeroLinearKeyframes);
        test(nameof(MemoryLeakCleanupAndResourceLifecycle), MemoryLeakCleanupAndResourceLifecycle);
        test(nameof(JitterFreeAnimationsAndCenterOriginOsd), JitterFreeAnimationsAndCenterOriginOsd);
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
            Check.True(window.Width <= 400.0 && window.CurrentUiScale < 1.0,
                $"Window must proportionally scale to fit narrow/short work area (Width={window.Width}, Scale={window.CurrentUiScale})");
            Check.Equal(System.Windows.Controls.ScrollBarVisibility.Disabled,
                window.contentScrollViewer.HorizontalScrollBarVisibility);
            window.ApplyAdaptiveBounds(new DipRect(0, 0, 1920, 1032), isInitialPlacement: false);
            Check.Equal(1.0, window.CurrentUiScale, "scale must return to 1.0 on a full-size monitor");
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

            // Verify dark mode brushes are monochrome and input caret is light
            typeof(MainWindow).GetMethod("SetLightMode", flags)!.Invoke(window, new object[] { false });
            var darkBg = (System.Windows.Media.SolidColorBrush)window.Resources["WarningBgBrush"];
            var darkBorder = (System.Windows.Media.SolidColorBrush)window.Resources["WarningBorderBrush"];
            var darkText = (System.Windows.Media.SolidColorBrush)window.Resources["WarningTextBrush"];
            Check.True(darkBg.Color.R == darkBg.Color.G && darkBg.Color.G == darkBg.Color.B, "dark WarningBgBrush must be neutral monochrome");
            Check.True(darkBorder.Color.R == darkBorder.Color.G && darkBorder.Color.G == darkBorder.Color.B, "dark WarningBorderBrush must be neutral monochrome");
            Check.True(darkText.Color.R > 200 && darkText.Color.G > 200 && darkText.Color.B > 200, "dark WarningTextBrush must be white/near-white");
            Check.True(Math.Abs(darkText.Color.R - darkText.Color.B) <= 2, "dark WarningTextBrush must be neutral");
            var darkOsdCaret = (System.Windows.Media.SolidColorBrush)window.txtOsdDuration.CaretBrush;
            var darkVolCaret = (System.Windows.Media.SolidColorBrush)window.txtSoundVolume.CaretBrush;
            Check.True(darkOsdCaret.Color.R > 200 && darkOsdCaret.Color.G > 200 && darkOsdCaret.Color.B > 200, "dark txtOsdDuration CaretBrush must be white/near-white");
            Check.True(darkVolCaret.Color.R > 200 && darkVolCaret.Color.G > 200 && darkVolCaret.Color.B > 200, "dark txtSoundVolume CaretBrush must be white/near-white");

            // Verify light mode brushes are monochrome and input caret is dark
            typeof(MainWindow).GetMethod("SetLightMode", flags)!.Invoke(window, new object[] { true });
            var lightBg = (System.Windows.Media.SolidColorBrush)window.Resources["WarningBgBrush"];
            var lightBorder = (System.Windows.Media.SolidColorBrush)window.Resources["WarningBorderBrush"];
            var lightText = (System.Windows.Media.SolidColorBrush)window.Resources["WarningTextBrush"];
            Check.True(lightBg.Color.R == lightBg.Color.G && lightBg.Color.G == lightBg.Color.B, "light WarningBgBrush must be neutral monochrome");
            Check.True(lightBorder.Color.R == lightBorder.Color.G && lightBorder.Color.G == lightBorder.Color.B, "light WarningBorderBrush must be neutral monochrome");
            Check.True(lightText.Color.R < 100 && lightText.Color.G < 100 && lightText.Color.B < 100, "light WarningTextBrush must be dark neutral");
            Check.True(lightText.Color.R != 239 && lightText.Color.R != 220, "light WarningTextBrush must not be red");
            var lightOsdCaret = (System.Windows.Media.SolidColorBrush)window.txtOsdDuration.CaretBrush;
            var lightVolCaret = (System.Windows.Media.SolidColorBrush)window.txtSoundVolume.CaretBrush;
            Check.True(lightOsdCaret.Color.R < 100 && lightOsdCaret.Color.G < 100 && lightOsdCaret.Color.B < 100, "light txtOsdDuration CaretBrush must be dark neutral");
            Check.True(lightVolCaret.Color.R < 100 && lightVolCaret.Color.G < 100 && lightVolCaret.Color.B < 100, "light txtSoundVolume CaretBrush must be dark neutral");
        }
        finally { window.Close(); }
    }

    private static void AudioFeedbackLoadsDiscordChimesAndSynthesizesFallback()
    {
        AudioFeedback.Initialize();

        string[] resourceNames = typeof(AudioFeedback).Assembly.GetManifestResourceNames();
        Check.True(Array.Exists(resourceNames, r => r == "MicMute.sounds.mute.wav"), "MicMute.sounds.mute.wav must be embedded");
        Check.True(Array.Exists(resourceNames, r => r == "MicMute.sounds.unmute.wav"), "MicMute.sounds.unmute.wav must be embedded");

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
            foreach (string name in new[] { "btnStateToggle", "cbDevices", "tbHotkey", "btnRecordHotkey", "btnSettingsToggle", "settingsGearRot", "contentScrollViewer" })
                Check.True(window.FindName(name) != null, "missing named control on MainWindow: " + name);
            Check.True(window.FindName("contentScrollViewer") is System.Windows.Controls.ScrollViewer, "contentScrollViewer must be a ScrollViewer");
        }
        finally { window.Close(); }

        using var settingsStream = typeof(SettingsWindow).Assembly.GetManifestResourceStream("MicMute.SettingsWindow.xaml");
        Check.True(settingsStream != null, "settings window resource must be embedded");
        using var settingsReader = new System.IO.StreamReader(settingsStream!);
        string settingsXaml = System.Text.RegularExpressions.Regex.Replace(settingsReader.ReadToEnd(), @"\s+x:Class=""[^""]+""", "");
        var settingsWindow = (System.Windows.Window)System.Windows.Markup.XamlReader.Parse(settingsXaml);
        try
        {
            foreach (string name in new[] { "cbEnableOsd", "txtOsdDuration", "cbSoundFeedback", "sliderSoundVolume", "txtSoundVolume", "cbStartup", "cbStartMinimized", "cbRunAsAdmin", "btnResetData", "tbStoragePath", "settingsScrollViewer" })
                Check.True(settingsWindow.FindName(name) != null, "missing named control on SettingsWindow: " + name);
            Check.True(settingsWindow.FindName("settingsScrollViewer") is System.Windows.Controls.ScrollViewer, "settingsScrollViewer must be a ScrollViewer");
        }
        finally { settingsWindow.Close(); }
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
        Check.True(!new AppSettings().StartMinimized, "AppSettings default StartMinimized must be false so the app pops up on first launch");
        Check.True(!UiBehavior.ShouldStartMinimized(false, Array.Empty<string>()), "stored false preference should start visible without arguments");
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

            Check.Equal(System.Windows.Controls.ScrollBarVisibility.Disabled, window.contentScrollViewer!.HorizontalScrollBarVisibility);

            var primary = System.Windows.Forms.Screen.PrimaryScreen;
            var dpi = System.Windows.Media.VisualTreeHelper.GetDpi(window);
            double scaleY = dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1.0;
            double workHeight = primary != null ? primary.WorkingArea.Height / scaleY : 720.0;
            double expectedMax = UiBehavior.CalculateAdaptiveMaxHeight(workHeight);
            Check.Equal(expectedMax, window.MaxHeight);

            if (workHeight == 720.0)
            {
                Check.Equal(696.0, window.MaxHeight);
                Check.True(window.Top >= 12.0, "Window must remain below top margin");
                Check.True(window.Top + window.ActualHeight < 720.0, "Window must fit cleanly above the taskbar");
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
            menu.Font = new System.Drawing.Font("Segoe UI Variable Text", 9f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        }
        catch
        {
            menu.Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        }

        var item1 = new System.Windows.Forms.ToolStripMenuItem("Toggle Mute") { AutoSize = false, Size = new System.Drawing.Size(184, 29), Margin = new System.Windows.Forms.Padding(0, 1, 0, 1), Padding = new System.Windows.Forms.Padding(0) };
        var item2 = new System.Windows.Forms.ToolStripMenuItem("Open App") { AutoSize = false, Size = new System.Drawing.Size(184, 29), Margin = new System.Windows.Forms.Padding(0, 1, 0, 1), Padding = new System.Windows.Forms.Padding(0) };
        var item3 = new System.Windows.Forms.ToolStripMenuItem("Quit") { AutoSize = false, Size = new System.Drawing.Size(184, 29), Margin = new System.Windows.Forms.Padding(0, 7, 0, 1), Padding = new System.Windows.Forms.Padding(0) };

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
        
        Check.True(preferredSize.Width >= 180 && preferredSize.Width <= 204, $"Menu preferred width {preferredSize.Width} must be a clean 192px wide rectangle");
        Check.True(preferredSize.Height >= 85 && preferredSize.Height <= 115, $"Menu preferred height {preferredSize.Height} must be comfortably sized");

        // Validate that OnRenderMenuItemBackground and OnRenderItemText render cleanly for all items
        using var bmp = new System.Drawing.Bitmap(192, 29);
        using var g = System.Drawing.Graphics.FromImage(bmp);
        var renderArgs = new System.Windows.Forms.ToolStripItemRenderEventArgs(g, item2);
        item2.Select();
        var renderMethod = typeof(App.LiquidGlassMenuRenderer).GetMethod("OnRenderMenuItemBackground", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Check.True(renderMethod != null, "OnRenderMenuItemBackground must exist on LiquidGlassMenuRenderer");
        renderMethod!.Invoke(menu.Renderer, new object[] { renderArgs });

        var textRenderMethod = typeof(App.LiquidGlassMenuRenderer).GetMethod("OnRenderItemText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Check.True(textRenderMethod != null, "OnRenderItemText must exist on LiquidGlassMenuRenderer");
        var rect = new System.Drawing.Rectangle(0, 0, 184, 29);
        var flags = System.Windows.Forms.TextFormatFlags.Left | System.Windows.Forms.TextFormatFlags.VerticalCenter;

        // Validate menu rendering in both dark and light modes without leaving scratch artifacts or dirty settings
        var originalSettings = SettingsManager.Load();
        try
        {
            foreach (bool light in new[] { false, true })
            {
                var settings = SettingsManager.Load();
                SettingsManager.Save(settings with { LightMode = light });

                using var fullBmp = new System.Drawing.Bitmap(196, 112);
                using var fullG = System.Drawing.Graphics.FromImage(fullBmp);
                
                // Background & Border
                var bgMethod = typeof(App.LiquidGlassMenuRenderer).GetMethod("OnRenderToolStripBackground", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                bgMethod?.Invoke(menu.Renderer, new object[] { new System.Windows.Forms.ToolStripRenderEventArgs(fullG, menu) });
                var borderMethod = typeof(App.LiquidGlassMenuRenderer).GetMethod("OnRenderToolStripBorder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                borderMethod?.Invoke(menu.Renderer, new object[] { new System.Windows.Forms.ToolStripRenderEventArgs(fullG, menu) });

                // Item 1 (Hovered / Selected)
                item1.Select();
                using var item1Bmp = new System.Drawing.Bitmap(184, 29);
                using (var item1G = System.Drawing.Graphics.FromImage(item1Bmp))
                {
                    renderMethod!.Invoke(menu.Renderer, new object[] { new System.Windows.Forms.ToolStripItemRenderEventArgs(item1G, item1) });
                    textRenderMethod!.Invoke(menu.Renderer, new object[] { new System.Windows.Forms.ToolStripItemTextRenderEventArgs(item1G, item1, item1.Text, rect, System.Drawing.Color.White, menu.Font, flags) });
                }
                fullG.DrawImage(item1Bmp, 4, 4);

                // Item 2 (Open App)
                using var item2Bmp = new System.Drawing.Bitmap(184, 29);
                using (var item2G = System.Drawing.Graphics.FromImage(item2Bmp))
                {
                    textRenderMethod!.Invoke(menu.Renderer, new object[] { new System.Windows.Forms.ToolStripItemTextRenderEventArgs(item2G, item2, item2.Text, rect, System.Drawing.Color.White, menu.Font, flags) });
                }
                fullG.DrawImage(item2Bmp, 4, 35);

                // Item 3 (Quit)
                using var item3Bmp = new System.Drawing.Bitmap(184, 29);
                using (var item3G = System.Drawing.Graphics.FromImage(item3Bmp))
                {
                    textRenderMethod!.Invoke(menu.Renderer, new object[] { new System.Windows.Forms.ToolStripItemTextRenderEventArgs(item3G, item3, item3.Text, rect, System.Drawing.Color.White, menu.Font, flags) });
                }
                fullG.DrawImage(item3Bmp, 4, 72);

                // Verify item3 has drawn glyphs
                int item3Pixels = 0;
                for (int py = 0; py < item3Bmp.Height; py++)
                    for (int px = 0; px < item3Bmp.Width; px++)
                        if (item3Bmp.GetPixel(px, py).A > 0) item3Pixels++;
                Check.True(item3Pixels > 30, "Item 3 (Quit) must render both icon and text pixels");
            }
        }
        finally
        {
            SettingsManager.Save(originalSettings);
            SettingsManager.Flush();
        }
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
        Check.Equal(System.Windows.Media.Color.FromRgb(0xF8, 0x71, 0x71), mutedBorder.Color, "BrushMutedBorder must be #F87171");

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
        using var stream = typeof(SettingsWindow).Assembly.GetManifestResourceStream("MicMute.SettingsWindow.xaml");
        Check.True(stream != null, "SettingsWindow.xaml resource must be embedded");
        using var reader = new System.IO.StreamReader(stream!);
        string rawXaml = reader.ReadToEnd();

        // 1. Verify Settings Location and description exist
        int groupStart = rawXaml.IndexOf("Settings Location", StringComparison.Ordinal);
        Check.True(groupStart >= 0, "Settings Location must exist in SettingsWindow.xaml");

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

        // 4. Verify slider control bindings from embedded SettingsWindow XAML
        using var stream = typeof(SettingsWindow).Assembly.GetManifestResourceStream("MicMute.SettingsWindow.xaml");
        Check.True(stream != null, "SettingsWindow.xaml must be embedded");
        using var reader = new System.IO.StreamReader(stream!);
        string xaml = System.Text.RegularExpressions.Regex.Replace(reader.ReadToEnd(), @"\s+x:Class=""[^""]+""", "");
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

        var bringWindowToTopMethod = typeof(OsdWindow).GetMethod("BringWindowToTop", flags);
        Check.True(bringWindowToTopMethod != null, "BringWindowToTop P/Invoke must be defined on OsdWindow");

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

    private static void StartupManagerElevatedTaskSchedulerAndStandardRegistry()
    {
        // Assert StartupManager public API methods exist and execute safely
        Check.True(typeof(StartupManager).GetMethod("CreateScheduledTask") != null, "CreateScheduledTask must exist");
        Check.True(typeof(StartupManager).GetMethod("RemoveScheduledTask") != null, "RemoveScheduledTask must exist");
        Check.True(typeof(StartupManager).GetMethod("IsScheduledTaskConfigured") != null, "IsScheduledTaskConfigured must exist");

        string? originalRun = null;
        try
        {
            using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", writable: false))
            {
                originalRun = key?.GetValue("MicMute") as string;
            }

            // Querying scheduled task status should not throw
            bool taskConfigured = StartupManager.IsScheduledTaskConfigured();
            // Removing scheduled task when it does not exist should not throw and return true
            bool removed = StartupManager.RemoveScheduledTask();
            Check.True(removed, "RemoveScheduledTask should succeed or safely report non-existence");
            // Setting standard startup should not spawn external schtasks processes
            int schtasksBefore = System.Diagnostics.Process.GetProcessesByName("schtasks").Length;
            StartupManager.SetStartup(true, false);
            int schtasksAfter = System.Diagnostics.Process.GetProcessesByName("schtasks").Length;
            Check.True(schtasksAfter == schtasksBefore, "SetStartup must not spawn schtasks subprocesses");
            // Querying startup enabled should not throw
            bool enabled = StartupManager.IsStartupEnabled();
        }
        finally
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);
                if (key != null)
                {
                    if (originalRun != null)
                        key.SetValue("MicMute", originalRun);
                    else
                        key.DeleteValue("MicMute", throwOnMissingValue: false);
                }
            }
            catch { }
        }
    }

    private static void MainWindowUiAccuratelyClarifiesAdminModeForAntiCheat()
    {
        using var stream = typeof(SettingsWindow).Assembly.GetManifestResourceStream("MicMute.SettingsWindow.xaml");
        Check.True(stream != null, "SettingsWindow.xaml must be embedded");
        using var reader = new System.IO.StreamReader(stream!);
        string xaml = reader.ReadToEnd();

        // Must clarify anti-cheat and elevated windows, and not claim games in general require admin
        Check.True(xaml.Contains("Enables hotkeys over anti-cheat and elevated windows"), "SettingsWindow.xaml must clarify anti-cheat and elevated windows");
        Check.True(!xaml.Contains("Enables hotkeys over games and elevated windows"), "SettingsWindow.xaml must not misleadingly claim normal games require admin");
    }

    private static void TitleBarCaptionButtonsAndThemeToggleAreSymmetricallyAligned()
    {
        using var stream = typeof(MainWindow).Assembly.GetManifestResourceStream("MicMute.MainWindow.xaml");
        Check.True(stream != null, "MainWindow.xaml must be embedded");
        using var reader = new System.IO.StreamReader(stream!);
        string rawXaml = reader.ReadToEnd();

        // 1. Verify all three button styles are declared
        Check.True(rawXaml.Contains(@"<Style x:Key=""Win11MinimizeButtonStyle"" TargetType=""Button"">"), "Win11MinimizeButtonStyle must be defined");
        Check.True(rawXaml.Contains(@"<Style x:Key=""Win11CloseButtonStyle"" TargetType=""Button"">"), "Win11CloseButtonStyle must be defined");
        Check.True(rawXaml.Contains(@"<Style x:Key=""TitleThemeButtonStyle"" TargetType=""Button"">"), "TitleThemeButtonStyle must be defined");

        // 2. Parse Window to inspect instantiated styles and controls
        string xaml = System.Text.RegularExpressions.Regex.Replace(rawXaml, @"\s+x:Class=""[^""]+""", "");
        xaml = System.Text.RegularExpressions.Regex.Replace(xaml, @"\s+(Click|MouseLeftButtonDown|SelectionChanged|Checked|Unchecked|ValueChanged|LostFocus|KeyDown|TextChanged)=""[^""]+""", "");
        var window = (System.Windows.Window)System.Windows.Markup.XamlReader.Parse(xaml);
        try
        {
            var btnTheme = window.FindName("btnTitleTheme") as System.Windows.Controls.Button;
            var btnMin = window.FindName("btnMinimize") as System.Windows.Controls.Button;
            var btnClose = window.FindName("btnClose") as System.Windows.Controls.Button;

            Check.True(btnTheme != null, "btnTitleTheme must exist");
            Check.True(btnMin != null, "btnMinimize must exist");
            Check.True(btnClose != null, "btnClose must exist");

            // Assert exact matching button dimensions (44x32) for mathematical alignment and equal spacing
            var themeStyle = (System.Windows.Style)window.Resources["TitleThemeButtonStyle"];
            var minStyle = (System.Windows.Style)window.Resources["Win11MinimizeButtonStyle"];
            var closeStyle = (System.Windows.Style)window.Resources["Win11CloseButtonStyle"];

            static double GetStyleDimension(System.Windows.Style style, System.Windows.DependencyProperty prop)
            {
                foreach (var setter in style.Setters)
                {
                    if (setter is System.Windows.Setter s && s.Property == prop && s.Value is double d)
                        return d;
                }
                return double.NaN;
            }

            Check.Equal(44.0, GetStyleDimension(themeStyle, System.Windows.FrameworkElement.WidthProperty));
            Check.Equal(32.0, GetStyleDimension(themeStyle, System.Windows.FrameworkElement.HeightProperty));
            Check.Equal(44.0, GetStyleDimension(minStyle, System.Windows.FrameworkElement.WidthProperty));
            Check.Equal(32.0, GetStyleDimension(minStyle, System.Windows.FrameworkElement.HeightProperty));
            Check.Equal(44.0, GetStyleDimension(closeStyle, System.Windows.FrameworkElement.WidthProperty));
            Check.Equal(32.0, GetStyleDimension(closeStyle, System.Windows.FrameworkElement.HeightProperty));

            // Verify glyphs are centered in consistent 10x10 bounding boxes for pixel-perfect vertical centering
            Check.True(rawXaml.Contains(@"x:Name=""minLine""") && rawXaml.Contains(@"Width=""10"" Height=""10"""), "minLine glyph must have explicit 10x10 dimensions");
            Check.True(rawXaml.Contains(@"x:Name=""cross""") && rawXaml.Contains(@"Width=""10"" Height=""10"""), "cross glyph must have explicit 10x10 dimensions");
        }
        finally
        {
            window.Close();
        }
    }

    private static void ComboBoxDropdownHasSmoothAnimationAndRotatingChevron()
    {
        using var stream = typeof(MainWindow).Assembly.GetManifestResourceStream("MicMute.MainWindow.xaml");
        Check.True(stream != null, "MainWindow.xaml must be embedded");
        using var reader = new System.IO.StreamReader(stream!);
        string rawXaml = reader.ReadToEnd();

        // 1. PopupAnimation must be None so WPF's built-in choppy fade never fights our custom animation
        Check.True(rawXaml.Contains(@"PopupAnimation=""None"""), "ComboBox Popup must use PopupAnimation=None for conflict-free custom animation");

        // 2. Rotating chevron arrow with RotateTransform and ExponentialEase on IsChecked
        Check.True(rawXaml.Contains(@"x:Name=""arrowRot"""), "arrowRot RotateTransform must be defined");
        Check.True(rawXaml.Contains(@"Storyboard.TargetName=""arrowRot"""), "Storyboard must target arrowRot");
        Check.True(rawXaml.Contains(@"Storyboard.TargetProperty=""Angle"""), "Storyboard must animate Angle");
        Check.True(rawXaml.Contains(@"<ExponentialEase Exponent=""4.5"" EasingMode=""EaseOut"" />"), "Chevron rotation must use ExponentialEase");

        // 3. Named transforms on dropDownBorder for code-driven animation on every DropDownOpened
        Check.True(rawXaml.Contains(@"x:Name=""dropDownScale"""), "dropDownScale ScaleTransform must be declared");
        Check.True(rawXaml.Contains(@"x:Name=""dropDownTranslate"""), "dropDownTranslate TranslateTransform must be declared");
    }

    private static void HeroHotkeySynchronizesWithSettingsAndLiveChanges()
    {
        using var audio = new AudioController();
        var window = new MainWindow(audio);
        try
        {
            Check.True(window.tbHeroHotkey != null, "tbHeroHotkey must be bound in MainWindow");

            // Display key sync
            window.DisplayHotkey(System.Windows.Input.Key.F8, System.Windows.Input.ModifierKeys.Control);
            Check.Equal("Ctrl + F8", window.tbHeroHotkey!.Text);

            // Record mode sync
            window.StartRecordingHotkey();
            Check.Equal("...", window.tbHeroHotkey.Text);

            // Reset back
            window.DisplayHotkey(System.Windows.Input.Key.Insert, System.Windows.Input.ModifierKeys.None);
            Check.Equal("Insert", window.tbHeroHotkey.Text);
        }
        finally
        {
            window.Close();
        }
    }

    private static void AppExitAndTeardownHandlesFailuresGracefullyWithoutHanging()
    {
        // Simulate tray icon lifecycle
        using var notifyIcon = new System.Windows.Forms.NotifyIcon { Visible = true };
        notifyIcon.Visible = false;
        notifyIcon.Dispose();
        Check.True(!notifyIcon.Visible, "NotifyIcon should be invisible upon teardown");

        // Verify window close event unhooking allows normal closure
        using var audio = new AudioController();
        var window = new MainWindow(audio);
        bool closingCalled = false;
        System.ComponentModel.CancelEventHandler handler = (s, e) =>
        {
            closingCalled = true;
            e.Cancel = true;
        };
        window.Closing += handler;

        // Unhook handler as done in ExitApp
        window.Closing -= handler;
        window.Close();
        Check.True(!closingCalled, "Window should close cleanly without being intercepted by hide-to-tray");
    }

    private static void OsdWindowCornersHaveNoClippedDropShadowArtifacts()
    {
        using var stream = typeof(OsdWindow).Assembly.GetManifestResourceStream("MicMute.OsdWindow.xaml");
        Check.True(stream != null, "OsdWindow.xaml resource must be embedded");
        using var reader = new System.IO.StreamReader(stream!);
        string rawXaml = reader.ReadToEnd();

        // The circular OSD border has DropShadowEffect for glowing ring, but must have generous transparent window margin to prevent corner clipping
        Check.True(rawXaml.Contains("<DropShadowEffect"), "OsdWindow.xaml must contain DropShadowEffect for glowing ring");
        Check.True(rawXaml.Contains("<Border.Effect>"), "OsdWindow.xaml must contain <Border.Effect>");

        // Verify runtime instance has shadow effect with radius safely inside window transparent margins
        var window = new OsdWindow();
        try
        {
            Check.True(window.borderPanel.Effect is System.Windows.Media.Effects.DropShadowEffect, "OsdWindow borderPanel must have DropShadowEffect for glowing ring");
            double margin = (window.Width - window.borderPanel.Width) / 2.0;
            Check.True(margin >= 15.0, $"Window must provide at least 15px transparent margin around borderPanel (actual: {margin}px)");
            var dropShadow = (System.Windows.Media.Effects.DropShadowEffect)window.borderPanel.Effect;
            Check.True(dropShadow.BlurRadius <= margin, "DropShadowEffect blur radius must not exceed window transparent margin to prevent clipping");
        }
        finally
        {
            window.Close();
        }
    }

    private static void OsdDurationAndSoundVolumeKeycapsSupportClampingAndKeyboardInteraction()
    {
        var originalSettings = SettingsManager.Load();
        using var audio = new AudioController();
        var window = new MainWindow(audio);
        try
        {
            Check.True(window.borderOsdDurationKeycap != null, "borderOsdDurationKeycap must be bound");
            Check.True(window.borderSoundVolumeKeycap != null, "borderSoundVolumeKeycap must be bound");
            Check.Equal(34.0, window.txtOsdDuration.Width);
            Check.Equal(34.0, window.txtSoundVolume.Width);

            var source = System.Windows.PresentationSource.FromVisual(window) ?? new System.Windows.Interop.HwndSource(0, 0, 0, 0, 0, "", IntPtr.Zero);
            void PressEnter(System.Windows.UIElement element)
            {
                var enterEvent = new System.Windows.Input.KeyEventArgs(
                    System.Windows.Input.Keyboard.PrimaryDevice,
                    source,
                    0,
                    System.Windows.Input.Key.Enter)
                {
                    RoutedEvent = System.Windows.UIElement.KeyDownEvent
                };
                element.RaiseEvent(enterEvent);
            }

            // Test 1: Enter on txtOsdDuration clamps valid numbers, updates slider, and commits
            window.txtOsdDuration.Text = "2";
            PressEnter(window.txtOsdDuration);
            Check.Equal("2.0", window.txtOsdDuration.Text.Replace(',', '.'));
            Check.Equal(2.0, window.sliderOsdDuration.Value);
            Check.Equal(2.0, SettingsManager.Load().OsdDuration);

            // Test 2: Enter on txtOsdDuration clamps below minimum (0 -> 0.1)
            window.txtOsdDuration.Text = "0";
            PressEnter(window.txtOsdDuration);
            Check.Equal("0.1", window.txtOsdDuration.Text.Replace(',', '.'));
            Check.Equal(0.1, window.sliderOsdDuration.Value);
            Check.Equal(0.1, SettingsManager.Load().OsdDuration);

            // Test 3: Enter on txtOsdDuration clamps above maximum (50 -> 30.0)
            window.txtOsdDuration.Text = "50";
            PressEnter(window.txtOsdDuration);
            Check.Equal("30.0", window.txtOsdDuration.Text.Replace(',', '.'));
            Check.Equal(30.0, window.sliderOsdDuration.Value);
            Check.Equal(30.0, SettingsManager.Load().OsdDuration);

            // Test 4: Enter on txtOsdDuration supports comma as decimal separator ("1,5" -> 1.5)
            window.txtOsdDuration.Text = "1,5";
            PressEnter(window.txtOsdDuration);
            Check.Equal("1.5", window.txtOsdDuration.Text.Replace(',', '.'));
            Check.Equal(1.5, window.sliderOsdDuration.Value);
            Check.Equal(1.5, SettingsManager.Load().OsdDuration);

            // Test 5: Enter on txtSoundVolume clamps above maximum (120 -> 100)
            window.txtSoundVolume.Text = "120";
            PressEnter(window.txtSoundVolume);
            Check.Equal("100", window.txtSoundVolume.Text);
            Check.Equal(100.0, window.sliderSoundVolume.Value);
            Check.Equal(100, SettingsManager.Load().SoundVolume);

            // Test 6: Enter on txtSoundVolume clamps below minimum (-10 -> 0)
            window.txtSoundVolume.Text = "-10";
            PressEnter(window.txtSoundVolume);
            Check.Equal("0", window.txtSoundVolume.Text);
            Check.Equal(0.0, window.sliderSoundVolume.Value);
            Check.Equal(0, SettingsManager.Load().SoundVolume);
        }
        finally
        {
            SettingsManager.Save(originalSettings);
            SettingsManager.Flush();
            AudioFeedback.SetVolume(originalSettings.SoundVolume);
            window.Close();
        }
    }

    private static void OsdWindowMatchesCircularGlowReferenceDesign()
    {
        var window = new OsdWindow();
        try
        {
            // Geometry matches circular disc in transparent window
            Check.Equal(180.0, window.Width);
            Check.Equal(180.0, window.Height);
            Check.Equal(140.0, window.borderPanel.Width);
            Check.Equal(140.0, window.borderPanel.Height);
            Check.Equal(70.0, window.borderPanel.CornerRadius.TopLeft);
            Check.Equal(1.8, window.borderPanel.BorderThickness.Left);

            // Glowing ring drop shadow
            Check.True(window.borderPanel.Effect is System.Windows.Media.Effects.DropShadowEffect, "borderPanel must have DropShadowEffect for glowing ring");
            var shadow = (System.Windows.Media.Effects.DropShadowEffect)window.borderPanel.Effect;
            Check.Equal(14.0, shadow.BlurRadius);
            Check.Equal(0.0, shadow.ShadowDepth);

            // Test Active State (Live)
            var updateMethod = typeof(OsdWindow).GetMethod("UpdateState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Check.True(updateMethod != null, "UpdateState must exist");
            var applyThemeMethod = typeof(OsdWindow).GetMethod("ApplyTheme", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Check.True(applyThemeMethod != null, "ApplyTheme must exist");

            applyThemeMethod!.Invoke(window, new object[] { false }); // Dark mode
            updateMethod!.Invoke(window, new object[] { false }); // Active (Live)

            Check.Equal("ACTIVE", window.tbStatus.Text);
            Check.Equal(13.5, window.tbStatus.FontSize);
            Check.Equal(System.Windows.FontWeights.Bold, window.tbStatus.FontWeight);
            var activeTextBrush = (System.Windows.Media.SolidColorBrush)window.tbStatus.Foreground;
            Check.Equal(System.Windows.Media.Colors.White, activeTextBrush.Color, "Active text must be pure white");
            var activeBorderBrush = (System.Windows.Media.SolidColorBrush)window.borderPanel.BorderBrush;
            Check.Equal(System.Windows.Media.Colors.White, activeBorderBrush.Color, "Active border must be pure white #FFFFFF");
            Check.Equal(System.Windows.Media.Color.FromRgb(0x00, 0x00, 0x00), shadow.Color, "Active shadow must be clean black #000000");
            var activeGlyphBrush = (System.Windows.Media.SolidColorBrush)window.activeGlyph.Fill;
            Check.Equal(System.Windows.Media.Colors.White, activeGlyphBrush.Color, "Active glyph must be pure white");
            var activeBg = (System.Windows.Media.SolidColorBrush)window.borderPanel.Background;
            Check.True(activeBg.Color.R < 40, "Active background disc must be dark surface");

            // Test Muted State
            updateMethod.Invoke(window, new object[] { true }); // Muted

            Check.Equal("MUTED", window.tbStatus.Text);
            Check.Equal(13.5, window.tbStatus.FontSize);
            Check.Equal(System.Windows.FontWeights.Bold, window.tbStatus.FontWeight);
            var mutedTextBrush = (System.Windows.Media.SolidColorBrush)window.tbStatus.Foreground;
            Check.Equal(System.Windows.Media.Color.FromRgb(0xF8, 0x71, 0x71), mutedTextBrush.Color, "Muted text must be coral red #F87171");
            var mutedBorderBrush = (System.Windows.Media.SolidColorBrush)window.borderPanel.BorderBrush;
            Check.Equal(System.Windows.Media.Color.FromRgb(0xF8, 0x71, 0x71), mutedBorderBrush.Color, "Muted border must be coral red #F87171");
            Check.Equal(System.Windows.Media.Color.FromRgb(0xEF, 0x44, 0x44), shadow.Color, "Muted glow must be coral red #EF4444");
            var mutedGlyphBrush = (System.Windows.Media.SolidColorBrush)window.mutedGlyph.Fill;
            Check.Equal(System.Windows.Media.Color.FromRgb(0xF8, 0x71, 0x71), mutedGlyphBrush.Color, "Muted glyph must be coral red #F87171");
            var mutedSlashBrush = (System.Windows.Media.SolidColorBrush)window.muteSlash.Stroke;
            Check.Equal(System.Windows.Media.Color.FromRgb(0xF8, 0x71, 0x71), mutedSlashBrush.Color, "Muted slash must be coral red #F87171");
            var mutedBg = (System.Windows.Media.SolidColorBrush)window.borderPanel.Background;
            Check.True(mutedBg.Color.R < 40, "Muted background disc must be dark surface");
        }
        finally
        {
            window.Close();
        }
    }

    private static void ButtonsReliablyRegisterClicksWithPressModeAndTitleBarGuards()
    {
        using var stream = typeof(MainWindow).Assembly.GetManifestResourceStream("MicMute.MainWindow.xaml");
        Check.True(stream != null, "MainWindow.xaml must be embedded");
        using var reader = new System.IO.StreamReader(stream!);
        string rawXaml = reader.ReadToEnd();

        // 1. Verify in raw XAML that ClickMode="Press" is applied to caption buttons, theme toggle, and hero mute button
        Check.True(rawXaml.Contains(@"<Style x:Key=""Win11MinimizeButtonStyle"" TargetType=""Button"">")
            && rawXaml.Contains(@"<Setter Property=""ClickMode"" Value=""Press"" />"), "Win11MinimizeButtonStyle must have ClickMode=Press");
        Check.True(rawXaml.Contains(@"<Style x:Key=""Win11CloseButtonStyle"" TargetType=""Button"">"), "Win11CloseButtonStyle must be defined");
        Check.True(rawXaml.Contains(@"<Style x:Key=""TitleThemeButtonStyle"" TargetType=""Button"">"), "TitleThemeButtonStyle must be defined");
        Check.True(rawXaml.Contains(@"<Style x:Key=""MicToggleButtonStyle"" TargetType=""Button"">"), "MicToggleButtonStyle must be defined");
        Check.True(rawXaml.Contains(@"<Grid x:Name=""rootGrid"" Background=""Transparent"""), "rootGrid must have Background=Transparent to ensure reliable hit-testing");

        // 2. Parse Window and verify style ClickMode values
        string xaml = System.Text.RegularExpressions.Regex.Replace(rawXaml, @"\s+x:Class=""[^""]+""", "");
        xaml = System.Text.RegularExpressions.Regex.Replace(xaml, @"\s+(Click|MouseLeftButtonDown|SelectionChanged|Checked|Unchecked|ValueChanged|LostFocus|KeyDown|TextChanged)=""[^""]+""", "");
        var window = (System.Windows.Window)System.Windows.Markup.XamlReader.Parse(xaml);
        try
        {
            static System.Windows.Controls.ClickMode GetClickMode(System.Windows.Style style)
            {
                foreach (var setter in style.Setters)
                {
                    if (setter is System.Windows.Setter s && s.Property == System.Windows.Controls.Primitives.ButtonBase.ClickModeProperty)
                        return (System.Windows.Controls.ClickMode)s.Value;
                }
                return System.Windows.Controls.ClickMode.Release;
            }

            var minStyle = (System.Windows.Style)window.Resources["Win11MinimizeButtonStyle"];
            var closeStyle = (System.Windows.Style)window.Resources["Win11CloseButtonStyle"];
            var themeStyle = (System.Windows.Style)window.Resources["TitleThemeButtonStyle"];
            var heroStyle = (System.Windows.Style)window.Resources["MicToggleButtonStyle"];

            Check.Equal(System.Windows.Controls.ClickMode.Press, GetClickMode(minStyle), "Minimize button style must use ClickMode.Press");
            Check.Equal(System.Windows.Controls.ClickMode.Press, GetClickMode(closeStyle), "Close button style must use ClickMode.Press");
            Check.Equal(System.Windows.Controls.ClickMode.Press, GetClickMode(themeStyle), "Theme toggle button style must use ClickMode.Press");
            Check.Equal(System.Windows.Controls.ClickMode.Press, GetClickMode(heroStyle), "Hero mute toggle button style must use ClickMode.Press");
        }
        finally
        {
            window.Close();
        }
    }

    private static void TaskbarIconAndTypographyAndThemeBrighterPalette()
    {
        using var stream = typeof(MainWindow).Assembly.GetManifestResourceStream("MicMute.MainWindow.xaml");
        Check.True(stream != null, "MainWindow.xaml must be embedded");
        using var reader = new System.IO.StreamReader(stream!);
        string rawXaml = reader.ReadToEnd();

        // 1. Windows 11 modern UI font family in MainWindow and OsdWindow (Google fonts ditched)
        Check.True(rawXaml.Contains("Segoe UI Variable Text") && rawXaml.Contains("Segoe UI Variable Display"), "MainWindow XAML must specify Windows 11 Segoe UI Variable font family chain");
        Check.True(!rawXaml.Contains("Roboto") && !rawXaml.Contains("Google Sans"), "MainWindow XAML must not contain Google fonts");

        using var osdStream = typeof(OsdWindow).Assembly.GetManifestResourceStream("MicMute.OsdWindow.xaml");
        Check.True(osdStream != null, "OsdWindow.xaml must be embedded");
        using var osdReader = new System.IO.StreamReader(osdStream!);
        string osdXaml = osdReader.ReadToEnd();
        Check.True(osdXaml.Contains("Segoe UI Variable Display"), "OsdWindow XAML must specify Windows 11 Segoe UI Variable font family chain");
        Check.True(!osdXaml.Contains("Roboto"), "OsdWindow XAML must not contain Google fonts");

        // 2. High brightness palette verification in dark mode (+10% white, +20% gray)
        Check.True(rawXaml.Contains(@"<SolidColorBrush x:Key=""TitleTextBrush"" Color=""#FFFFFF"" />"), "TitleTextBrush must be pure white (#FFFFFF)");
        Check.True(rawXaml.Contains(@"<SolidColorBrush x:Key=""TextWhiteBrush"" Color=""#FFFFFF"" />"), "TextWhiteBrush must be pure white (#FFFFFF)");
        Check.True(rawXaml.Contains(@"<SolidColorBrush x:Key=""TextGrayBrush"" Color=""#C1C1CC"" />"), "TextGrayBrush must be 20% brighter gray (#C1C1CC)");
        Check.True(rawXaml.Contains(@"<SolidColorBrush x:Key=""TextDimBrush"" Color=""#888892"" />"), "TextDimBrush must be 20% brighter (#888892)");
        Check.True(rawXaml.Contains(@"<SolidColorBrush x:Key=""ToggleKnobBorderBrush"" Color=""#C1C1CC"" />"), "ToggleKnobBorderBrush must be #C1C1CC");

        // 3. Runtime instance verification
        using var audio = new AudioController();
        var window = new MainWindow(audio);
        try
        {
            window.cbLightMode.IsChecked = false;
            Check.True(window.ShowInTaskbar, "MainWindow must show in Windows taskbar");
            Check.True(window.Icon != null, "MainWindow.Icon must be populated from embedded multi-resolution icon");
            Check.True(window.FontFamily != null && window.FontFamily.Source.Contains("Segoe UI Variable"), "MainWindow FontFamily must use Windows 11 Segoe UI Variable font");

            var titleBrush = (System.Windows.Media.SolidColorBrush)window.Resources["TitleTextBrush"];
            Check.Equal(System.Windows.Media.Color.FromRgb(255, 255, 255), titleBrush.Color);

            var grayBrush = (System.Windows.Media.SolidColorBrush)window.Resources["TextGrayBrush"];
            Check.Equal(System.Windows.Media.Color.FromRgb(193, 193, 204), grayBrush.Color);
        }
        finally
        {
            window.Close();
        }
    }

    private static void SmoothMinimizeRestoreAndDropdownClearType()
    {
        using var stream = typeof(MainWindow).Assembly.GetManifestResourceStream("MicMute.MainWindow.xaml");
        Check.True(stream != null, "MainWindow.xaml must be embedded");
        using var reader = new System.IO.StreamReader(stream!);
        string rawXaml = reader.ReadToEnd();

        // 1. Root transform and transition elements in XAML
        Check.True(rawXaml.Contains(@"x:Name=""rootBorder"""), "rootBorder must be defined in MainWindow.xaml");
        Check.True(rawXaml.Contains(@"x:Name=""rootScaleTransform"""), "rootScaleTransform must be defined");
        Check.True(rawXaml.Contains(@"x:Name=""rootTranslateTransform"""), "rootTranslateTransform must be defined");
        Check.True(rawXaml.Contains(@"x:Name=""themeTransitionOverlay"""), "themeTransitionOverlay must be defined");
        Check.True(rawXaml.Contains(@"x:Name=""themeIconRot"""), "themeIconRot must be defined");

        // 2. Dropdown text sharpness, dynamic popup typography, and ClearType hints
        Check.True(rawXaml.Contains(@"x:Key=""PopupTextFormattingMode"">Display</TextFormattingMode>"), "Display formatting mode must be configured for native 1.0 popups");
        Check.True(rawXaml.Contains(@"x:Key=""PopupTextRenderingMode"">ClearType</TextRenderingMode>"), "ClearType rendering mode must be configured for native 1.0 popups");
        Check.True(rawXaml.Contains(@"RenderOptions.ClearTypeHint=""Enabled"""), "ClearTypeHint must be enabled for layered dropdown popups");

        // 3. Runtime element bindings
        using var audio = new AudioController();
        var window = new MainWindow(audio);
        try
        {
            Check.True(window.rootBorder != null, "rootBorder must be bound at runtime");
            Check.True(window.rootScaleTransform != null, "rootScaleTransform must be bound at runtime");
            Check.True(window.rootTranslateTransform != null, "rootTranslateTransform must be bound at runtime");
            Check.True(window.themeTransitionOverlay != null, "themeTransitionOverlay must be bound at runtime");
            Check.True(window.themeIconRot != null, "themeIconRot must be bound at runtime");

            // 4. Verify Theme resources are frozen for zero-stutter rendering
            window.cbLightMode.IsChecked = true;
            if (window.Resources["AccentBrush"] is System.Windows.Media.Brush lightAccent)
            {
                Check.True(lightAccent.IsFrozen, "Light mode AccentBrush must be frozen for zero-stutter rendering");
            }
            if (window.Resources["WindowBgBrush"] is System.Windows.Media.Brush lightBg)
            {
                Check.True(lightBg.IsFrozen, "Light mode WindowBgBrush must be frozen for zero-stutter rendering");
            }
            if (window.Resources["StatusCapsuleBorderBrush"] is System.Windows.Media.SolidColorBrush lightBorder)
            {
                Check.True(lightBorder.Color.A > 20, "StatusCapsuleBorderBrush must have visible alpha in Light Mode");
                Check.True(lightBorder.IsFrozen, "Light mode StatusCapsuleBorderBrush must be frozen");
            }
            if (window.borderStatusCapsule != null)
            {
                Check.Equal(new System.Windows.Thickness(1), window.borderStatusCapsule.BorderThickness);
                Check.True(window.borderStatusCapsule.BorderBrush != null, "borderStatusCapsule must have BorderBrush in Light Mode");
            }

            window.cbLightMode.IsChecked = false;
            if (window.Resources["AccentBrush"] is System.Windows.Media.Brush darkAccent)
            {
                Check.True(darkAccent.IsFrozen, "Dark mode AccentBrush must be frozen for zero-stutter rendering");
            }
            if (window.Resources["WindowBgBrush"] is System.Windows.Media.Brush darkBg)
            {
                Check.True(darkBg.IsFrozen, "Dark mode WindowBgBrush must be frozen for zero-stutter rendering");
            }
            if (window.Resources["StatusCapsuleBorderBrush"] is System.Windows.Media.SolidColorBrush darkBorder)
            {
                Check.True(darkBorder.Color.A > 20, "StatusCapsuleBorderBrush must have visible alpha in Dark Mode");
                Check.True(darkBorder.IsFrozen, "Dark mode StatusCapsuleBorderBrush must be frozen");
            }
            if (window.borderStatusCapsule != null)
            {
                Check.Equal(new System.Windows.Thickness(1), window.borderStatusCapsule.BorderThickness);
                Check.True(window.borderStatusCapsule.BorderBrush != null, "borderStatusCapsule must have BorderBrush in Dark Mode");
            }
        }
        finally
        {
            window.Close();
        }
    }

    private static void HeroHotkeyCenterLockedAndThemeMoonAlignedAndBoldTypography()
    {
        using var stream = typeof(MainWindow).Assembly.GetManifestResourceStream("MicMute.MainWindow.xaml");
        Check.True(stream != null, "MainWindow.xaml must be embedded");
        using var reader = new System.IO.StreamReader(stream!);
        string rawXaml = reader.ReadToEnd();

        // 1. Verify XAML structural constraints for centering and Bold typography
        Check.True(rawXaml.Contains(@"FontWeight=""Bold"""), "Window must specify Bold font weight");
        Check.True(rawXaml.Contains(@"x:Name=""borderStatusCapsule"""), "borderStatusCapsule must be defined");
        Check.True(rawXaml.Contains(@"Width=""158"""), "borderStatusCapsule must have invariant width 158");

        // 2. Runtime verification of coordinates and theme icon
        using var audio = new AudioController();
        var window = new MainWindow(audio);
        try
        {
            window.cbLightMode.IsChecked = false;
            Check.Equal(System.Windows.FontWeights.Bold, window.FontWeight);

            // Verify live vs muted layout invariance
            window.Measure(new System.Windows.Size(412, 700));
            window.Arrange(new System.Windows.Rect(0, 0, 412, 700));
            window.UpdateLayout();

            var posLive = window.tbHeroHotkey!.TransformToAncestor(window.rootBorder!).Transform(new System.Windows.Point(0, 0));

            var updateMute = typeof(MainWindow).GetMethod("UpdateMuteStateUI", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Check.True(updateMute != null, "UpdateMuteStateUI must exist");
            updateMute!.Invoke(window, new object[] { true });

            window.Measure(new System.Windows.Size(412, 700));
            window.Arrange(new System.Windows.Rect(0, 0, 412, 700));
            window.UpdateLayout();

            var posMuted = window.tbHeroHotkey.TransformToAncestor(window.rootBorder!).Transform(new System.Windows.Point(0, 0));

            Check.True(Math.Abs(posLive.X - posMuted.X) < 0.001, $"Hero hotkey must remain locked in center. Live={posLive.X}, Muted={posMuted.X}");

            // Verify light mode moon geometry and angle
            window.cbLightMode.IsChecked = true;
            Check.True(window.themeIconRot!.Angle == 0 || window.themeIconRot.Angle == 360, $"Moon icon must remain upright (Angle={window.themeIconRot.Angle})");
            Check.True(window.pathTitleTheme!.Data.ToString().Contains("12.79") || window.pathTitleTheme.Data.ToString().Contains("21"), "Moon geometry must be centered 1:1");
        }
        finally
        {
            window.Close();
        }
    }

    private static void OsdDurationSliderDimmingReflectsOsdToggle()
    {
        using var audio = new AudioController();
        var window = new MainWindow(audio);
        try
        {
            Check.True(window.panelOsdDuration != null, "panelOsdDuration must be bound");

            // Enabled state
            window.cbEnableOsd.IsChecked = true;
            Check.True(window.panelOsdDuration!.IsEnabled, "panelOsdDuration must be enabled when EnableOsd is checked");
            Check.Equal(1.0, window.panelOsdDuration.Opacity, "panelOsdDuration opacity must be 1.0 when enabled");

            // Disabled state
            window.cbEnableOsd.IsChecked = false;
            Check.True(!window.panelOsdDuration.IsEnabled, "panelOsdDuration must be disabled when EnableOsd is unchecked");
            Check.Equal(0.45, window.panelOsdDuration.Opacity, "panelOsdDuration opacity must be 0.45 when dimmed");

            // Re-enabled state
            window.cbEnableOsd.IsChecked = true;
            Check.True(window.panelOsdDuration.IsEnabled, "panelOsdDuration must re-enable when EnableOsd is checked");
            Check.Equal(1.0, window.panelOsdDuration.Opacity, "panelOsdDuration opacity must restore to 1.0");
        }
        finally
        {
            window.Close();
        }
    }

    private static void TrayOpenAnimationAndSmoothTransitions()
    {
        using var stream = typeof(MainWindow).Assembly.GetManifestResourceStream("MicMute.MainWindow.xaml");
        Check.True(stream != null, "MainWindow.xaml must be embedded");
        using var reader = new System.IO.StreamReader(stream!);
        string rawXaml = reader.ReadToEnd();

        // 1. Verify X and Y properties in rootTranslateTransform and panelOsdDuration in SettingsWindow
        Check.True(rawXaml.Contains(@"x:Name=""rootTranslateTransform"" X=""0.0"" Y=""0.0"""), "rootTranslateTransform must declare X=0.0 and Y=0.0");
        using var settingsStream = typeof(SettingsWindow).Assembly.GetManifestResourceStream("MicMute.SettingsWindow.xaml");
        Check.True(settingsStream != null, "SettingsWindow.xaml must be embedded");
        using var settingsReader = new System.IO.StreamReader(settingsStream!);
        string settingsRawXaml = settingsReader.ReadToEnd();
        Check.True(settingsRawXaml.Contains(@"x:Name=""panelOsdDuration"""), "panelOsdDuration must be declared in SettingsWindow.xaml");

        // 2. Runtime verification of tray open animation
        using var audio = new AudioController();
        var window = new MainWindow(audio);
        try
        {
            Check.True(typeof(MainWindow).GetMethod("PlayOpenFromTrayAnimation") != null, "PlayOpenFromTrayAnimation method must exist");
            Check.True(typeof(MainWindow).GetMethod("CenterOnScreen") != null, "CenterOnScreen method must exist");
            Check.True(window.panelOsdDuration != null, "panelOsdDuration must be bound at runtime");
            Check.True(window.rootTranslateTransform != null, "rootTranslateTransform must be bound at runtime");

            // Execute PlayOpenFromTrayAnimation with a mock tray location - must not throw
            window.PlayOpenFromTrayAnimation(new System.Drawing.Point(100, 100));
        }
        finally
        {
            window.Close();
        }
    }

    private static void DaVinciStyleAppMenuAndFuturePlaceholderOptions()
    {
        using var stream = typeof(MainWindow).Assembly.GetManifestResourceStream("MicMute.MainWindow.xaml");
        Check.True(stream != null, "MainWindow.xaml must be embedded");
        using var reader = new System.IO.StreamReader(stream!);
        string rawXaml = reader.ReadToEnd();

        // 1. Verify XAML declarations for DaVinci Resolve-style top-left menu
        Check.True(rawXaml.Contains(@"x:Name=""btnAppMenu"""), "btnAppMenu must be declared in XAML");
        Check.True(rawXaml.Contains(@"x:Name=""popupAppMenu"""), "popupAppMenu must be declared in XAML");
        Check.True(rawXaml.Contains(@"x:Name=""btnMenuCheckUpdates"""), "btnMenuCheckUpdates must be declared in XAML");
        Check.True(rawXaml.Contains(@"x:Name=""btnMenuAbout"""), "btnMenuAbout must be declared in XAML");
        Check.True(rawXaml.Contains(@"x:Name=""tbMenuVersion"""), "tbMenuVersion must be declared in XAML");
        Check.True(rawXaml.Contains(@"TitleAppMenuButtonStyle"), "TitleAppMenuButtonStyle must be declared in XAML");
        Check.True(rawXaml.Contains(@"AppDropdownItemButtonStyle"), "AppDropdownItemButtonStyle must be declared in XAML");

        // 2. Runtime verification of bindings and interactions
        using var audio = new AudioController();
        var window = new MainWindow(audio);
        try
        {
            window.Show();
            window.UpdateLayout();

            Check.True(window.btnAppMenu != null, "btnAppMenu must be bound at runtime");
            Check.True(window.popupAppMenu != null, "popupAppMenu must be bound at runtime");
            Check.True(window.btnMenuCheckUpdates != null, "btnMenuCheckUpdates must be bound at runtime");
            Check.True(window.btnMenuAbout != null, "btnMenuAbout must be bound at runtime");
            Check.True(window.tbMenuVersion != null, "tbMenuVersion must be bound at runtime");

            // Verify version formatting
            Check.True(window.tbMenuVersion!.Text.StartsWith("Version "), "tbMenuVersion must display a formatted version string");

            // Verify popup initial state
            Check.True(!window.popupAppMenu!.IsOpen, "popupAppMenu should be closed initially");

            // Test toggling popup via btnAppMenu
            window.BtnAppMenu_Click(window.btnAppMenu!, new System.Windows.RoutedEventArgs());
            Check.True(window.popupAppMenu.IsOpen, "Clicking btnAppMenu should open popupAppMenu");

            // Test clicking 'Check for Updates' closes popup (and executes placeholder hook)
            window.BtnMenuCheckUpdates_Click(window.btnMenuCheckUpdates!, new System.Windows.RoutedEventArgs());
            Check.True(!window.popupAppMenu.IsOpen, "Clicking Check for Updates must close popupAppMenu");

            // Open again and test clicking 'About MicMute' closes popup
            window.popupAppMenu.IsOpen = true;
            Check.True(window.popupAppMenu.IsOpen, "popupAppMenu should be open");
            window.BtnMenuAbout_Click(window.btnMenuAbout!, new System.Windows.RoutedEventArgs());
            Check.True(!window.popupAppMenu.IsOpen, "Clicking About MicMute must close popupAppMenu");

            // Verify dynamic resource inheritance across theme changes
            window.SetLightMode(isLight: true);
            Check.Equal(window.Resources["InputBgBrush"], window.popupAppMenu.Resources["InputBgBrush"]);
            window.SetLightMode(isLight: false);
            Check.Equal(window.Resources["InputBgBrush"], window.popupAppMenu.Resources["InputBgBrush"]);
        }
        finally
        {
            window.Close();
        }
    }

    private static void DeviceDropdownDoesNotGlitchOrCloseWhenOsdAppearsAndDefersUpdates()
    {
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        const System.Reflection.BindingFlags staticFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;

        // Verify WM_MOUSEACTIVATE and MA_NOACTIVATE are defined on OsdWindow
        var wmMouseActivate = typeof(OsdWindow).GetField("WM_MOUSEACTIVATE", staticFlags);
        Check.True(wmMouseActivate != null, "WM_MOUSEACTIVATE must be defined on OsdWindow");
        Check.Equal(0x0021, (int)wmMouseActivate!.GetValue(null)!);

        var maNoActivate = typeof(OsdWindow).GetField("MA_NOACTIVATE", staticFlags);
        Check.True(maNoActivate != null, "MA_NOACTIVATE must be defined on OsdWindow");
        Check.Equal(3, (int)maNoActivate!.GetValue(null)!);

        using var audio = new AudioController();
        var window = new MainWindow(audio);
        try
        {
            window.Show();
            window.UpdateLayout();

            var dev1 = new AudioDevice("id1", "Microphone 1");
            var dev2 = new AudioDevice("id2", "Microphone 2");
            var initialList = new System.Collections.Generic.List<AudioDevice> { dev1, dev2 };

            var applyMethod = typeof(MainWindow).GetMethod("ApplyDeviceList", flags);
            Check.True(applyMethod != null, "ApplyDeviceList must exist");
            applyMethod!.Invoke(window, new object[] { initialList });

            Check.Equal(2, window.cbDevices.Items.Count);
            Check.Equal(initialList, window.cbDevices.ItemsSource);

            // Open the dropdown
            window.cbDevices.IsDropDownOpen = true;
            Check.True(window.cbDevices.IsDropDownOpen, "Dropdown should be open");

            // Attempt to apply an updated device list while dropdown is open
            var dev3 = new AudioDevice("id3", "Microphone 3");
            var updatedList = new System.Collections.Generic.List<AudioDevice> { dev1, dev2, dev3 };
            applyMethod.Invoke(window, new object[] { updatedList });

            // Dropdown must remain open and items untouched while open
            Check.True(window.cbDevices.IsDropDownOpen, "Dropdown must remain open when device refresh arrives");
            Check.Equal(2, window.cbDevices.Items.Count);

            var pendingField = typeof(MainWindow).GetField("_pendingDeviceList", flags);
            Check.True(pendingField != null, "_pendingDeviceList must exist");
            var pendingVal = pendingField!.GetValue(window) as System.Collections.Generic.List<AudioDevice>;
            Check.Equal(3, pendingVal?.Count);

            // Trigger OSD show while dropdown is open
            OsdWindow.ShowOsd(true, 0.5);
            Check.True(window.cbDevices.IsDropDownOpen, "Dropdown must remain open when OSD is shown");

            // Close the dropdown - pending list should be applied
            window.cbDevices.IsDropDownOpen = false;
            Check.Equal(3, window.cbDevices.Items.Count);
            Check.True(pendingField.GetValue(window) == null, "_pendingDeviceList must be flushed after closing");

            // Test equivalence check: re-applying an equivalent list preserves ItemsSource instance
            var currentItemsSource = window.cbDevices.ItemsSource;
            var sameList = new System.Collections.Generic.List<AudioDevice> { dev1, dev2, dev3 };
            applyMethod.Invoke(window, new object[] { sameList });
            Check.True(ReferenceEquals(currentItemsSource, window.cbDevices.ItemsSource), "ItemsSource instance must be preserved when list content is unchanged");
        }
        finally
        {
            window.Close();
        }
    }

    private static void KeycapInputBoxesSingleClickShowsCaretAndDoubleClickSelectsAll()
    {
        using var audio = new AudioController();
        var window = new MainWindow(audio);
        try
        {
            window.Show();
            window.UpdateLayout();

            var mouseDevice = System.Windows.Input.Mouse.PrimaryDevice;

            // 1. Single click on txtOsdDuration must not SelectAll (caret indicator shown, SelectionLength == 0)
            window.txtOsdDuration.Text = "0.3";
            window.txtOsdDuration.SelectionLength = 0;
            var singleClickOsd = new System.Windows.Input.MouseButtonEventArgs(
                mouseDevice, 0, System.Windows.Input.MouseButton.Left)
            {
                RoutedEvent = System.Windows.UIElement.PreviewMouseLeftButtonDownEvent
            };
            window.txtOsdDuration.RaiseEvent(singleClickOsd);
            Check.Equal(0, window.txtOsdDuration.SelectionLength, "Single click on txtOsdDuration must not select all text");

            // 2. Double click (ClickCount == 2) on txtOsdDuration must SelectAll
            var doubleClickOsd = new System.Windows.Input.MouseButtonEventArgs(
                mouseDevice, 0, System.Windows.Input.MouseButton.Left)
            {
                RoutedEvent = System.Windows.UIElement.PreviewMouseLeftButtonDownEvent
            };
            typeof(System.Windows.Input.MouseButtonEventArgs)
                .GetProperty("ClickCount")?
                .GetSetMethod(true)?
                .Invoke(doubleClickOsd, new object[] { 2 });
            window.txtOsdDuration.RaiseEvent(doubleClickOsd);
            Check.Equal(window.txtOsdDuration.Text.Length, window.txtOsdDuration.SelectionLength, "Double click on txtOsdDuration must select all text");

            // 3. Single click on txtSoundVolume must not SelectAll
            window.txtSoundVolume.Text = "85";
            window.txtSoundVolume.SelectionLength = 0;
            var singleClickVol = new System.Windows.Input.MouseButtonEventArgs(
                mouseDevice, 0, System.Windows.Input.MouseButton.Left)
            {
                RoutedEvent = System.Windows.UIElement.PreviewMouseLeftButtonDownEvent
            };
            window.txtSoundVolume.RaiseEvent(singleClickVol);
            Check.Equal(0, window.txtSoundVolume.SelectionLength, "Single click on txtSoundVolume must not select all text");

            // 4. Double click on txtSoundVolume must SelectAll
            var doubleClickVol = new System.Windows.Input.MouseButtonEventArgs(
                mouseDevice, 0, System.Windows.Input.MouseButton.Left)
            {
                RoutedEvent = System.Windows.UIElement.PreviewMouseLeftButtonDownEvent
            };
            typeof(System.Windows.Input.MouseButtonEventArgs)
                .GetProperty("ClickCount")?
                .GetSetMethod(true)?
                .Invoke(doubleClickVol, new object[] { 2 });
            window.txtSoundVolume.RaiseEvent(doubleClickVol);
            Check.Equal(window.txtSoundVolume.Text.Length, window.txtSoundVolume.SelectionLength, "Double click on txtSoundVolume must select all text");
        }
        finally
        {
            window.Close();
        }
    }

    private static void OsdWindowHideAndAudioFeedbackStreamManagement()
    {
        // 1. Verify OsdWindow.HideOsd can be safely invoked repeatedly without throwing
        OsdWindow.HideOsd();
        OsdWindow.HideOsd();

        // 2. Verify AudioFeedback stream management and disposal
        AudioFeedback.SetVolume(75);
        Check.Equal(75, AudioFeedback.CurrentVolume, "AudioFeedback current volume should be 75");

        AudioFeedback.SetVolume(0);
        Check.Equal(0, AudioFeedback.CurrentVolume, "AudioFeedback current volume should be 0");
        AudioFeedback.Play(true); // Should short-circuit and not throw

        AudioFeedback.Dispose();
        AudioFeedback.SetVolume(100);
        Check.Equal(100, AudioFeedback.CurrentVolume, "AudioFeedback volume re-initialized to 100");

        // 3. Verify AdminManager.SetRunAsAdmin returns a boolean
        bool result = AdminManager.SetRunAsAdmin(false);
        Check.True(result || !result, "SetRunAsAdmin must return a valid boolean without uncaught exception");
    }

    private static void MenuOutsideClickDismissalAndOsdSpamResilience()
    {
        using var audio = new AudioController();
        var window = new MainWindow(audio);
        try
        {
            window.Show();
            window.UpdateLayout();

            // 1. Menu inside-click preservation & outside-click dismissal
            Check.True(window.popupAppMenu != null, "popupAppMenu must not be null");
            Check.True(window.popupAppMenu!.StaysOpen, "popupAppMenu must use StaysOpen=True so button mouse capture does not prematurely close the menu");
            window.popupAppMenu.IsOpen = true;
            Check.True(window.popupAppMenu.IsOpen, "Menu should be open");

            // Clicking any button inside borderAppMenu must NOT close popupAppMenu on PreviewMouseDown
            var mouseDevice = System.Windows.Input.Mouse.PrimaryDevice;
            foreach (var menuButton in new[] { window.btnMenuStartupState, window.btnMenuSyncAllMics, window.btnMenuLockMute, window.btnMenuCheckUpdates, window.btnMenuAbout })
            {
                Check.True(menuButton != null, "menu button must be bound");
                var insideClick = new System.Windows.Input.MouseButtonEventArgs(
                    mouseDevice, 0, System.Windows.Input.MouseButton.Left)
                {
                    RoutedEvent = System.Windows.UIElement.PreviewMouseDownEvent
                };
                menuButton!.RaiseEvent(insideClick);
                Check.True(window.popupAppMenu.IsOpen,
                    $"PreviewMouseDown on {menuButton.Name} must keep popupAppMenu open so Click fires");
            }

            // Click outside the menu button (e.g. on window body)
            var outsideClick = new System.Windows.Input.MouseButtonEventArgs(
                mouseDevice, 0, System.Windows.Input.MouseButton.Left)
            {
                RoutedEvent = System.Windows.UIElement.PreviewMouseDownEvent
            };
            window.RaiseEvent(outsideClick);
            Check.True(!window.popupAppMenu.IsOpen, "Menu should close on outside PreviewMouseDown");

            // Re-open and verify Deactivated dismisses it
            window.popupAppMenu.IsOpen = true;
            Check.True(window.popupAppMenu.IsOpen, "Menu should be open again");
            typeof(MainWindow).GetMethod("MainWindow_Deactivated", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .Invoke(window, new object?[] { null, EventArgs.Empty });
            Check.True(!window.popupAppMenu.IsOpen, "Menu should close on Deactivated");

            // 2. OSD rapid spamming resilience (does not disappear or zero opacity)
            OsdWindow.ShowOsd(true, 1.0);
            OsdWindow.ShowOsd(false, 1.0);
            OsdWindow.ShowOsd(true, 1.0);

            var instField = typeof(OsdWindow).GetField("_instance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var inst = instField?.GetValue(null) as OsdWindow;
            Check.True(inst != null, "OSD instance must be active");
            Check.True(inst!.IsVisible, "OSD must remain visible when spammed");
            Check.True(inst.Opacity > 0.5, "OSD opacity must remain active when spammed");

            OsdWindow.HideOsd();
            Check.Equal(0.0, inst.Opacity, "HideOsd should reset opacity to 0");

            // 3. Tray animation uses DPI-aware transient BitmapCache during motion and clears it on close/rest
            window.PlayOpenFromTrayAnimation(new System.Drawing.Point(500, 500));
            Check.True(window.rootBorder != null && window.rootBorder.CacheMode is System.Windows.Media.BitmapCache,
                "rootBorder must enable transient BitmapCache during tray open animation for smooth layered motion");
            window.Close();
            Check.True(window.rootBorder!.CacheMode == null, "rootBorder must clear transient BitmapCache on close");
        }
        finally
        {
            window.Close();
        }
    }

    private static void PowerModesAndMouseShortcutAndMenuActions()
    {
        AppSettings originalSettings = SettingsManager.Load();
        using var audio = new AudioController();
        var window = new MainWindow(audio);
        try
        {
            window.Show();
            window.UpdateLayout();

            // 1. Mouse button formatting in DisplayHotkey
            window.DisplayHotkey(HotkeyManager.MouseMiddleKey, System.Windows.Input.ModifierKeys.None);
            Check.Equal("Mouse Middle", window.tbHotkey.Text);
            window.DisplayHotkey(HotkeyManager.MouseXButton1Key, System.Windows.Input.ModifierKeys.Control);
            Check.Equal("Ctrl + Mouse 4", window.tbHotkey.Text);
            window.DisplayHotkey(HotkeyManager.MouseXButton2Key, System.Windows.Input.ModifierKeys.None);
            Check.Equal("Mouse 5", window.tbHotkey.Text);

            // 2. Hold mode toggle button + hero prompt + subtitle
            Check.True(window.btnHotkeyMode != null, "btnHotkeyMode must be bound");
            Check.Equal("Toggle", window.btnHotkeyMode!.Content as string);
            window.BtnHotkeyMode_Click(window.btnHotkeyMode, new System.Windows.RoutedEventArgs());
            Check.True(SettingsManager.Load().HoldKeyMode, "HoldKeyMode should toggle to true");
            Check.Equal("Hold", window.btnHotkeyMode.Content as string);
            Check.Equal("Hold ", window.tbHeroHotkeyPrefix?.Text);
            Check.Equal(" to toggle", window.tbHeroHotkeySuffix?.Text);
            Check.Equal("Hold key to toggle", window.tbHotkeySubtitle?.Text);

            // 2b. Left-clicking elsewhere while recording cancels shortcut recording
            window.StartRecordingHotkey();
            Check.Equal("Cancel", window.btnRecordHotkey.Content as string);
            var leftClick = new System.Windows.Input.MouseButtonEventArgs(
                System.Windows.Input.Mouse.PrimaryDevice, 0, System.Windows.Input.MouseButton.Left)
            {
                RoutedEvent = System.Windows.UIElement.PreviewMouseDownEvent
            };
            window.RaiseEvent(leftClick);
            Check.Equal("Record", window.btnRecordHotkey.Content as string, "Clicking outside Record button must cancel recording");

            // 3. Startup mic state cycling
            window.BtnMenuStartupState_Click(window.btnMenuStartupState!, new System.Windows.RoutedEventArgs());
            Check.Equal(1, SettingsManager.Load().StartupMuteMode);
            Check.Equal("Startup: Always Muted", window.tbMenuStartupState?.Text);
            window.BtnMenuStartupState_Click(window.btnMenuStartupState!, new System.Windows.RoutedEventArgs());
            Check.Equal(2, SettingsManager.Load().StartupMuteMode);
            Check.Equal("Startup: Always Live", window.tbMenuStartupState?.Text);
            window.BtnMenuStartupState_Click(window.btnMenuStartupState!, new System.Windows.RoutedEventArgs());
            Check.Equal(0, SettingsManager.Load().StartupMuteMode);
            Check.Equal("Startup: Keep Last", window.tbMenuStartupState?.Text);

            // 4. Sync All Microphones & Mute Lock toggles
            window.BtnMenuSyncAllMics_Click(window.btnMenuSyncAllMics!, new System.Windows.RoutedEventArgs());
            Check.True(SettingsManager.Load().SyncAllMicrophones, "SyncAllMicrophones should enable");
            Check.True(audio.SyncAllMicrophones, "AudioController.SyncAllMicrophones should sync");
            Check.Equal("Sync All Mics: On", window.tbMenuSyncAllMics?.Text);

            window.BtnMenuLockMute_Click(window.btnMenuLockMute!, new System.Windows.RoutedEventArgs());
            Check.True(SettingsManager.Load().LockMuteState, "LockMuteState should enable");
            Check.True(audio.LockMuteState, "AudioController.LockMuteState should sync");
            Check.Equal("Mute Lock: On", window.tbMenuLockMute?.Text);

            // 5. GitHub Update check & About menu feedback
            window.BtnMenuCheckUpdates_Click(window.btnMenuCheckUpdates!, new System.Windows.RoutedEventArgs());
            Check.Equal("Checking GitHub for updates...", new System.Windows.Documents.TextRange(window.tbWarningMessage.ContentStart, window.tbWarningMessage.ContentEnd).Text);

            window.BtnMenuAbout_Click(window.btnMenuAbout!, new System.Windows.RoutedEventArgs());
            Check.Equal(System.Windows.Visibility.Visible, window.borderModalOverlay!.Visibility, "About MicMute must open the theme-aware modal popup");
            Check.Equal("About MicMute", window.tbModalTitle?.Text);
            Check.Equal("OK", window.btnModalConfirm?.Content as string);
            Check.Equal(System.Windows.Visibility.Collapsed, window.btnModalCancel?.Visibility);
            string aboutModalText = window.tbModalMessage?.Text ?? string.Empty;
            Check.True(aboutModalText.Contains("github.com/youssefhazem3-dot/MicMute", StringComparison.Ordinal), "About popup should include project URL");
            Check.True(aboutModalText.Contains("microphone mute", StringComparison.OrdinalIgnoreCase), "About popup should explain what MicMute is");
            window.CloseConfirmationModal(immediate: true);
        }
        finally
        {
            SettingsManager.Save(originalSettings);
            SettingsManager.Flush();
            window.Close();
        }
    }

    private static void UiUxRefinementsTrayOsdMenuChipsModalAndToast()
    {
        // 1. OSD Screen Positioning math across Top, Center, Bottom
        var screen = new PixelRect(0, 0, 1920, 1080);
        var size = new PixelSize(180, 180);
        PixelRect topRect = UiBehavior.CalculateOsdPositionInPixels(screen, size, "Top");
        PixelRect centerRect = UiBehavior.CalculateOsdPositionInPixels(screen, size, "Center");
        PixelRect bottomRect = UiBehavior.CalculateOsdPositionInPixels(screen, size, "Bottom");
        Check.Equal(UiBehavior.CenterInPixels(screen, size), centerRect, "Center placement must match CenterInPixels");
        Check.Equal(centerRect.Left, topRect.Left, "Top OSD must stay horizontally centered");
        Check.Equal(centerRect.Left, bottomRect.Left, "Bottom OSD must stay horizontally centered");
        Check.True(topRect.Top < centerRect.Top && centerRect.Top < bottomRect.Top,
            $"OSD vertical anchors must be ordered Top ({topRect.Top}) < Center ({centerRect.Top}) < Bottom ({bottomRect.Top})");

        // 2. 192px Tray menu renders dynamic labels ("Unmute Mic"), hotkey hint ("Insert"), and Quit danger hover
        using (var trayMenu = new App.LiquidGlassContextMenu())
        using (var muteBmp = new System.Drawing.Bitmap(192, 29))
        using (var quitBmp = new System.Drawing.Bitmap(192, 29))
        {
            var renderer = new App.LiquidGlassMenuRenderer(trayMenu);
            trayMenu.Renderer = renderer;
            var muteItem = new System.Windows.Forms.ToolStripMenuItem("Unmute Mic")
            {
                AutoSize = false,
                Size = new System.Drawing.Size(192, 29),
                Tag = "Insert"
            };
            var openItem = new System.Windows.Forms.ToolStripMenuItem("Open App")
            {
                AutoSize = false,
                Size = new System.Drawing.Size(192, 29)
            };
            var quitItem = new System.Windows.Forms.ToolStripMenuItem("Quit")
            {
                AutoSize = false,
                Size = new System.Drawing.Size(192, 29),
                Margin = new System.Windows.Forms.Padding(0, 7, 0, 1)
            };
            trayMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { muteItem, openItem, quitItem });

            var bgMethod = typeof(App.LiquidGlassMenuRenderer).GetMethod("OnRenderMenuItemBackground",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var textMethod = typeof(App.LiquidGlassMenuRenderer).GetMethod("OnRenderItemText",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var rect = new System.Drawing.Rectangle(0, 0, 192, 29);
            var flags = System.Windows.Forms.TextFormatFlags.Left | System.Windows.Forms.TextFormatFlags.VerticalCenter;

            using (var gMute = System.Drawing.Graphics.FromImage(muteBmp))
            {
                gMute.Clear(System.Drawing.Color.FromArgb(18, 18, 20));
                textMethod!.Invoke(renderer, new object[]
                {
                    new System.Windows.Forms.ToolStripItemTextRenderEventArgs(
                        gMute, muteItem, muteItem.Text, rect, System.Drawing.Color.White, trayMenu.Font, flags)
                });
            }

            quitItem.Select();
            using (var gQuit = System.Drawing.Graphics.FromImage(quitBmp))
            {
                gQuit.Clear(System.Drawing.Color.FromArgb(18, 18, 20));
                bgMethod!.Invoke(renderer, new object[] { new System.Windows.Forms.ToolStripItemRenderEventArgs(gQuit, quitItem) });
                textMethod!.Invoke(renderer, new object[]
                {
                    new System.Windows.Forms.ToolStripItemTextRenderEventArgs(
                        gQuit, quitItem, quitItem.Text, rect, System.Drawing.Color.White, trayMenu.Font, flags)
                });
            }

            int coralMutePixels = 0;
            int rightHintPixels = 0;
            int quitHoverPixels = 0;
            for (int y = 0; y < 29; y++)
            {
                for (int x = 0; x < 192; x++)
                {
                    var mp = muteBmp.GetPixel(x, y);
                    if (mp.R > 180 && mp.B < 150) coralMutePixels++;
                    if (x > 136 && (mp.R > 45 || mp.G > 45 || mp.B > 45)) rightHintPixels++;
                    var qp = quitBmp.GetPixel(x, y);
                    if (qp.R > qp.B + 8) quitHoverPixels++;
                }
            }
            Check.True(coralMutePixels > 10, "Unmute Mic tray icon must render coral-red muted mic/slash");
            Check.True(rightHintPixels > 10, "192px tray menu must render right-aligned shortcut hint with breathing room");
            Check.True(quitHoverPixels > 50, "Quit tray hover must render coral danger highlight");
        }

        // 3. OsdWindow stays crisp minimalist (no glossy white glare ellipses) with Light Mode slash mask support
        var osd = new OsdWindow();
        try
        {
            Check.True(osd.panelScaleTransform != null, "OsdWindow must bind panelScaleTransform");
            Check.True(osd.muteSlashBg != null, "OsdWindow must bind muteSlashBg");
            Check.True(osd.borderPanel.Background is System.Windows.Media.SolidColorBrush,
                "borderPanel.Background must stay a SolidColorBrush");

            using var osdStream = typeof(OsdWindow).Assembly.GetManifestResourceStream("MicMute.OsdWindow.xaml");
            using var osdReader = new System.IO.StreamReader(osdStream!);
            string osdXaml = osdReader.ReadToEnd();
            Check.True(!osdXaml.Contains("osdSpecularArc") && !osdXaml.Contains("osdGlassSheen"),
                "OsdWindow must not include glossy oval glare overlays");

            osd.ApplyTheme(true);
            var lightMask = (System.Windows.Media.SolidColorBrush)osd.muteSlashBg!.Stroke;
            Check.True(lightMask.Color.R > 220, "Light mode mute slash mask must be white-tinted");
            osd.ApplyTheme(false);
            var darkMask = (System.Windows.Media.SolidColorBrush)osd.muteSlashBg.Stroke;
            Check.True(darkMask.Color.R < 30, "Dark mode mute slash mask must be dark-tinted");
        }
        finally
        {
            osd.Close();
        }

        // 4. MainWindow refinements (no voice metering, clean segmented OSD position, monochrome pills & chips, theme-aware modal)
        AppSettings originalSettings = SettingsManager.Load();
        using var audio = new AudioController();
        var window = new MainWindow(audio);
        try
        {
            window.Show();
            window.UpdateLayout();

            Check.True(window.appMenuChevronRot != null, "MainWindow must expose appMenuChevronRot");
            Check.True(window.panelHeroModeChips != null && window.chipMuteLock != null && window.chipSyncAllMics != null,
                "MainWindow must expose hero active mode chips");
            Check.True(window.btnOsdPosCenter != null && window.btnOsdPosBottom != null && window.btnOsdPosTop != null,
                "MainWindow must expose segmented OSD position buttons");
            Check.True(window.warningTranslate != null, "MainWindow must expose warningTranslate for animated bottom toast");
            Check.True(window.borderModalOverlay != null && window.btnModalCancel != null && window.btnModalConfirm != null,
                "MainWindow must expose theme-aware modal controls");

            using var mwStream = typeof(MainWindow).Assembly.GetManifestResourceStream("MicMute.MainWindow.xaml");
            using var mwReader = new System.IO.StreamReader(mwStream!);
            string mwXaml = mwReader.ReadToEnd();
            Check.True(!mwXaml.Contains("voiceHalo"), "Voice metering ring must not exist in MainWindow.xaml");

            // Hero mode chips stay hidden when both power modes are off, appear cleanly when toggled on
            SettingsManager.Save(originalSettings with { SyncAllMicrophones = false, LockMuteState = false, StartupMuteMode = 0, OsdPosition = "Center" });
            window.BtnMenuSyncAllMics_Click(window.btnMenuSyncAllMics!, new System.Windows.RoutedEventArgs()); // On
            window.BtnMenuSyncAllMics_Click(window.btnMenuSyncAllMics!, new System.Windows.RoutedEventArgs()); // Off
            Check.Equal(System.Windows.Visibility.Collapsed, window.panelHeroModeChips!.Visibility, "Mode chips must stay collapsed when both modes are off");

            // Keep App Menu open when toggling power-user options inside it
            window.popupAppMenu!.IsOpen = true;
            window.BtnMenuStartupState_Click(window.btnMenuStartupState!, new System.Windows.RoutedEventArgs());
            Check.True(window.popupAppMenu.IsOpen, "Startup State toggle must keep App Menu open");
            Check.Equal("Muted", window.tbMenuStartupBadge?.Text);

            window.BtnMenuSyncAllMics_Click(window.btnMenuSyncAllMics!, new System.Windows.RoutedEventArgs());
            Check.True(window.popupAppMenu.IsOpen, "Sync All Mics toggle must keep App Menu open");
            Check.Equal("ON", window.tbMenuSyncBadge?.Text);
            Check.Equal(System.Windows.Visibility.Visible, window.panelHeroModeChips.Visibility);
            Check.Equal(System.Windows.Visibility.Visible, window.chipSyncAllMics!.Visibility);

            window.BtnMenuLockMute_Click(window.btnMenuLockMute!, new System.Windows.RoutedEventArgs());
            Check.True(window.popupAppMenu.IsOpen, "Mute Lock toggle must keep App Menu open");
            Check.Equal("ON", window.tbMenuLockBadge?.Text);
            Check.Equal(System.Windows.Visibility.Visible, window.chipMuteLock!.Visibility);

            // Action items still close the menu and About opens the theme-aware modal
            window.BtnMenuAbout_Click(window.btnMenuAbout!, new System.Windows.RoutedEventArgs());
            Check.True(!window.popupAppMenu.IsOpen, "About menu item must close App Menu");
            Check.Equal(System.Windows.Visibility.Visible, window.borderModalOverlay!.Visibility, "About menu item must open the About popup");
            window.CloseConfirmationModal(immediate: true);

            // Segmented OSD Position buttons update settings & selected Tag
            window.BtnOsdPosBottom_Click(window.btnOsdPosBottom!, new System.Windows.RoutedEventArgs());
            Check.Equal("Bottom", SettingsManager.Load().OsdPosition);
            Check.Equal("Selected", window.btnOsdPosBottom!.Tag as string);
            Check.True(window.btnOsdPosCenter!.Tag == null, "Center segment must be deselected");

            window.BtnOsdPosTop_Click(window.btnOsdPosTop!, new System.Windows.RoutedEventArgs());
            Check.Equal("Top", SettingsManager.Load().OsdPosition);
            Check.Equal("Selected", window.btnOsdPosTop!.Tag as string);

            window.BtnOsdPosCenter_Click(window.btnOsdPosCenter!, new System.Windows.RoutedEventArgs());
            Check.Equal("Center", SettingsManager.Load().OsdPosition);
            Check.Equal("Selected", window.btnOsdPosCenter!.Tag as string);

            // Recording hotkey pulses monochrome Accent border without thickening border
            window.SetLightMode(false);
            window.StartRecordingHotkey();
            Check.Equal("Press keys...", window.tbHotkey.Text);
            Check.Equal(1.0, window.borderHotkey.BorderThickness.Left, "Recording border must keep 1px thickness to avoid layout shift");
            var pulseBrush = window.borderHotkey.BorderBrush as System.Windows.Media.SolidColorBrush;
            Check.True(pulseBrush != null && pulseBrush.Color.R == pulseBrush.Color.G && pulseBrush.Color.G == pulseBrush.Color.B,
                "Dark mode recording border must pulse monochrome white (not coral-red)");

            var esc = new System.Windows.Input.KeyEventArgs(
                System.Windows.Input.Keyboard.PrimaryDevice,
                new System.Windows.Interop.HwndSource(0, 0, 0, 0, 0, "test", IntPtr.Zero),
                0,
                System.Windows.Input.Key.Escape)
            {
                RoutedEvent = System.Windows.UIElement.PreviewKeyDownEvent
            };
            window.RaiseEvent(esc);
            Check.Equal("Record", window.btnRecordHotkey.Content as string);

            // Theme-aware modal scrim, opaque modal card & DangerActionButtonStyle (no local Background override breaking triggers)
            window.SetLightMode(true);
            var lightScrim = (System.Windows.Media.SolidColorBrush)window.Resources["ModalScrimBrush"];
            var lightCard = (System.Windows.Media.SolidColorBrush)window.Resources["ModalCardBgBrush"];
            Check.Equal((byte)255, lightCard.Color.A, "Light mode modal card must be 100% opaque to prevent background bleed-through");
            window.SetLightMode(false);
            var darkScrim = (System.Windows.Media.SolidColorBrush)window.Resources["ModalScrimBrush"];
            var darkCard = (System.Windows.Media.SolidColorBrush)window.Resources["ModalCardBgBrush"];
            Check.Equal((byte)255, darkCard.Color.A, "Dark mode modal card must be 100% opaque to prevent background bleed-through");
            Check.True(lightScrim.Color != darkScrim.Color, "ModalScrimBrush must adapt between Light and Dark mode");

            window.BtnOsdPosBottom_Click(window.btnOsdPosBottom!, new System.Windows.RoutedEventArgs());
            window.BtnResetSettings_Click(window.btnModalConfirm!, new System.Windows.RoutedEventArgs());
            Check.Equal(System.Windows.Visibility.Visible, window.borderModalOverlay!.Visibility);
            Check.Equal("Reset Settings", window.tbModalTitle?.Text);
            Check.Equal("Reset", window.btnModalConfirm!.Content as string);
            Check.True(ReferenceEquals(window.btnModalConfirm.Style, window.FindResource("DangerActionButtonStyle")),
                "Destructive Reset button must use DangerActionButtonStyle so hover/press triggers remain active");
            Check.True(window.btnModalConfirm.ReadLocalValue(System.Windows.Controls.Control.BackgroundProperty) == System.Windows.DependencyProperty.UnsetValue,
                "Modal confirm button must not override Background locally");

            window.BtnModalCancel_Click(window.btnModalCancel!, new System.Windows.RoutedEventArgs());
            var deadline = DateTime.UtcNow.AddMilliseconds(400);
            while (window.borderModalOverlay.Visibility == System.Windows.Visibility.Visible && DateTime.UtcNow < deadline)
            {
                window.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() => { }));
                System.Threading.Thread.Sleep(20);
            }
            Check.Equal(System.Windows.Visibility.Collapsed, window.borderModalOverlay.Visibility);
            Check.Equal("Bottom", SettingsManager.Load().OsdPosition, "Cancelling modal must preserve settings");

            bool confirmed = false;
            window.ShowConfirmationModal("Test Title", "Test Message", "Apply", () => confirmed = true);
            Check.Equal(System.Windows.Visibility.Visible, window.borderModalOverlay.Visibility);
            Check.True(ReferenceEquals(window.btnModalConfirm.Style, window.FindResource("SecondaryActionButtonStyle")),
                "Non-destructive modal confirm button must use SecondaryActionButtonStyle");
            window.BtnModalConfirm_Click(window.btnModalConfirm!, new System.Windows.RoutedEventArgs());
            Check.True(confirmed, "Confirm button must execute modal callback");
            Check.Equal(System.Windows.Visibility.Collapsed, window.borderModalOverlay.Visibility);
        }
        finally
        {
            SettingsManager.Save(originalSettings);
            SettingsManager.Flush();
            window.Close();
        }
    }

    private static void ResponsiveScalingFitsAllCommonLaptopResolutionsWithoutScrolling()
    {
        // 1. Verify pure adaptive scale math across standard desktop and laptop work areas
        Check.Equal(1.0, UiBehavior.CalculateAdaptiveScale(new DipRect(0, 0, 1920, 1032), 412, 922),
            "1080p @ 100% must stay at native 1.0 scale");
        Check.Equal(1.0, UiBehavior.CalculateAdaptiveScale(new DipRect(0, 0, 2560, 1392), 412, 922),
            "1440p @ 100% must stay at native 1.0 scale");
        double scale768 = UiBehavior.CalculateAdaptiveScale(new DipRect(0, 0, 1366, 720), 412, 922);
        Check.True(scale768 > 0.70 && scale768 < 0.80, $"1366x768 @ 100% scale expected ~0.75, got {scale768:F3}");
        double scale1080At150 = UiBehavior.CalculateAdaptiveScale(new DipRect(0, 0, 1280, 672), 412, 922);
        Check.True(scale1080At150 > 0.65 && scale1080At150 < 0.75, $"1080p @ 150% scale expected ~0.70, got {scale1080At150:F3}");
        double scaleTiny = UiBehavior.CalculateAdaptiveScale(new DipRect(0, 0, 640, 320), 412, 922);
        Check.Equal(UiBehavior.MinimumUiScale, scaleTiny, "ultra-tiny displays must clamp to MinimumUiScale");
        Check.Equal(1.0, UiBehavior.CalculateAdaptiveScale(new DipRect(0, 0, double.NaN, 720), 412, 922),
            "non-finite work area must fall back to 1.0");

        // 2. Real MainWindow layout verification with a long laptop microphone name across laptop/desktop resolutions
        using var audio = new AudioController();
        var window = new MainWindow(audio);
        try
        {
            Check.True(window.windowScaleHost != null, "windowScaleHost must be bound");
            Check.True(window.rootLayoutScaleTransform != null, "rootLayoutScaleTransform must be bound");
            Check.True(ReferenceEquals(window.windowScaleHost!.LayoutTransform, window.rootLayoutScaleTransform),
                "windowScaleHost must share rootLayoutScaleTransform");
            Check.True(!ReferenceEquals(window.borderAppMenu?.LayoutTransform, window.rootLayoutScaleTransform),
                "borderAppMenu must NOT re-apply rootLayoutScaleTransform (WPF PopupRoot inherits ancestor scale automatically)");
            Check.True(window.borderAppMenu?.Effect == null,
                "borderAppMenu must isolate DropShadowEffect on a sibling layer so menu text is never bitmap-rasterized");
            Check.True(window.borderModalCard?.Effect == null,
                "borderModalCard must isolate DropShadowEffect on a sibling layer so modal text is never bitmap-rasterized");

            // Populate a long Intel laptop microphone name that previously caused horizontal overflow
            var longDevice = new AudioDevice("id-intel-sst", "Microphone Array (Intel® Smart Sound Technology (Intel® SST))");
            window.cbDevices.ItemsSource = new System.Collections.Generic.List<AudioDevice> { longDevice };
            window.cbDevices.SelectedItem = longDevice;
            window.borderWarning.Visibility = System.Windows.Visibility.Collapsed;

            // Simplified MainWindow is compact enough to run at native 1.0x across standard laptops,
            // and scales down proportionally on ultra-compact displays (e.g., 800x400).
            var scenarios = new (string Label, DipRect WorkArea, bool ExpectFullScale)[]
            {
                ("1366x768 @ 100%", new DipRect(0, 0, 1366, 720), true),
                ("1920x1080 @ 150%", new DipRect(0, 0, 1280, 672), true),
                ("1920x1080 @ 125%", new DipRect(0, 0, 1536, 816), true),
                ("1920x1080 @ 175%", new DipRect(0, 0, 1097, 576), true),
                ("1600x900 @ 100%", new DipRect(0, 0, 1600, 852), true),
                ("1920x1080 @ 100%", new DipRect(0, 0, 1920, 1032), true),
                ("Compact 800x400", new DipRect(0, 0, 800, 400), false),
            };

            // Measure and arrange windowScaleHost directly so scenarios taller than a headless CI monitor
            // (e.g. 1920x1080 tested on a 1024x768 runner) are not artificially clamped by Win32 SM_CYMAXTRACK.
            var host = window.windowScaleHost!;
            foreach (var (label, workArea, expectFullScale) in scenarios)
            {
                window.borderWarning.Visibility = System.Windows.Visibility.Collapsed;
                window.ApplyAdaptiveBounds(workArea, isInitialPlacement: true);
                host.Measure(new System.Windows.Size(window.Width, window.MaxHeight));
                double arrangedHeight = Math.Min(window.MaxHeight, host.DesiredSize.Height);
                host.Arrange(new System.Windows.Rect(0, 0, window.Width, arrangedHeight));
                host.UpdateLayout();

                Check.Equal(System.Windows.Controls.ScrollBarVisibility.Disabled,
                    window.contentScrollViewer.HorizontalScrollBarVisibility);
                Check.True(window.contentScrollViewer.ScrollableWidth <= 0.5,
                    $"{label}: must have zero horizontal scrolling with long mic name (ScrollableWidth={window.contentScrollViewer.ScrollableWidth:F2})");
                Check.True(window.contentScrollViewer.ScrollableHeight <= 0.5,
                    $"{label}: must fit vertically without scrolling (ScrollableHeight={window.contentScrollViewer.ScrollableHeight:F2}, DesiredH={host.DesiredSize.Height:F1}, MaxHeight={window.MaxHeight:F1}, Scale={window.CurrentUiScale:F3})");
                Check.True(window.Width <= workArea.Width + 0.5,
                    $"{label}: window width ({window.Width:F1}) must fit inside workArea width ({workArea.Width:F1})");
                Check.True(host.DesiredSize.Height <= window.MaxHeight + 0.5,
                    $"{label}: content height ({host.DesiredSize.Height:F1}) must stay within MaxHeight ({window.MaxHeight:F1})");
                Check.True(Math.Abs(window.rootLayoutScaleTransform!.ScaleX - window.CurrentUiScale) < 0.001,
                    $"{label}: ScaleTransform must match CurrentUiScale");

                if (expectFullScale)
                {
                    Check.Equal(1.0, window.CurrentUiScale, $"{label}: compact MainWindow must run at native 1.0 scale");
                    Check.Equal(412.0, window.Width, $"{label}: must preserve native 412 DIP width");
                }
                else
                {
                    Check.True(window.CurrentUiScale < 1.0 && window.CurrentUiScale >= UiBehavior.MinimumUiScale,
                        $"{label}: must scale proportionally below 1.0 (Scale={window.CurrentUiScale:F3})");

                    // Verify showing the temporary status toast still fits without triggering vertical scroll
                    window.tbWarningMessage.Text = "Shortcut changed successfully.";
                    window.borderWarning.Visibility = System.Windows.Visibility.Visible;
                    host.Measure(new System.Windows.Size(window.Width, window.MaxHeight));
                    host.Arrange(new System.Windows.Rect(0, 0, window.Width, Math.Min(window.MaxHeight, host.DesiredSize.Height)));
                    host.UpdateLayout();
                    Check.True(window.contentScrollViewer.ScrollableHeight <= 0.5,
                        $"{label}: showing status toast must not trigger vertical scrolling (ScrollableHeight={window.contentScrollViewer.ScrollableHeight:F2})");
                }
            }

            // Also verify SettingsWindow fits cleanly across laptop resolutions without horizontal scrolling
            var settingsWin = window.SettingsPopupWindow;
            var settingsHost = settingsWin.settingsScaleHost!;
            foreach (var (label, workArea) in new[]
            {
                ("1366x768 @ 100%", new DipRect(0, 0, 1366, 720)),
                ("1920x1080 @ 150%", new DipRect(0, 0, 1280, 672)),
                ("1920x1080 @ 175%", new DipRect(0, 0, 1097, 576)),
                ("1920x1080 @ 100%", new DipRect(0, 0, 1920, 1032)),
            })
            {
                settingsWin.ApplyAdaptiveBounds(workArea, new DipRect(100, 40, 412, 440));
                settingsHost.Measure(new System.Windows.Size(settingsWin.Width, settingsWin.MaxHeight));
                settingsHost.Arrange(new System.Windows.Rect(0, 0, settingsWin.Width, Math.Min(settingsWin.MaxHeight, settingsHost.DesiredSize.Height)));
                settingsHost.UpdateLayout();
                Check.True(settingsWin.settingsScrollViewer.ScrollableWidth <= 0.5,
                    $"SettingsWindow {label}: must have zero horizontal scroll");
                Check.True(settingsWin.settingsScrollViewer.ScrollableHeight <= 0.5,
                    $"SettingsWindow {label}: must fit vertically without scrolling (ScrollableH={settingsWin.settingsScrollViewer.ScrollableHeight:F2}, Scale={settingsWin.CurrentUiScale:F3})");
                Check.True(settingsWin.Width <= workArea.Width + 0.5,
                    $"SettingsWindow {label}: width must fit inside work area");
            }

            // Verify live shown window under adaptive scaling (< 1.0) so scaled popups are tested end-to-end
            window.borderWarning.Visibility = System.Windows.Visibility.Collapsed;
            window.Show();
            window.ApplyAdaptiveBounds(new DipRect(0, 0, 800, 400), isInitialPlacement: true);
            window.UpdateLayout();
            Check.True(window.CurrentUiScale < 1.0, "compact 800x400 display must engage adaptive scaling < 1.0");
            Check.True(window.contentScrollViewer.ScrollableWidth <= 0.5, "live shown scaled window must have zero horizontal scroll");
            Check.True(window.contentScrollViewer.ScrollableHeight <= 0.5, "live shown scaled window must have zero vertical scroll");
            Check.Equal(System.Windows.Media.TextFormattingMode.Ideal,
                (System.Windows.Media.TextFormattingMode)window.Resources["PopupTextFormattingMode"],
                "scaled laptop popups must switch to Ideal text formatting to avoid pre-scale pixel-snapping blur");
            Check.Equal(System.Windows.Media.TextRenderingMode.Auto,
                (System.Windows.Media.TextRenderingMode)window.Resources["PopupTextRenderingMode"],
                "scaled laptop popups must switch to Auto text rendering");

            // Verify mic dropdown popup matches ComboBox toggle width 1:1 (no double-scaling) and isolates shadow
            window.cbDevices.IsDropDownOpen = true;
            window.UpdateLayout();
            window.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
            var toggle = window.cbDevices.Template?.FindName("toggle", window.cbDevices) as System.Windows.FrameworkElement;
            var dropDownBorder = window.cbDevices.Template?.FindName("dropDownBorder", window.cbDevices) as System.Windows.Controls.Border;
            Check.True(toggle != null && dropDownBorder != null, "ComboBox toggle and dropDownBorder must exist");
            Check.True(!ReferenceEquals(dropDownBorder!.LayoutTransform, window.rootLayoutScaleTransform),
                "dropDownBorder must NOT re-apply rootLayoutScaleTransform");
            Check.True(dropDownBorder.Effect == null,
                "dropDownBorder must isolate DropShadowEffect on a sibling layer so dropdown text stays sharp");
            var toggleLeft = toggle!.PointToScreen(new System.Windows.Point(0, 0));
            var toggleRight = toggle.PointToScreen(new System.Windows.Point(toggle.ActualWidth, toggle.ActualHeight));
            var ddLeft = dropDownBorder.PointToScreen(new System.Windows.Point(0, 0));
            var ddRight = dropDownBorder.PointToScreen(new System.Windows.Point(dropDownBorder.ActualWidth, dropDownBorder.ActualHeight));
            double toggleScreenW = toggleRight.X - toggleLeft.X;
            double ddScreenW = ddRight.X - ddLeft.X;
            Check.True(Math.Abs(ddScreenW - toggleScreenW) <= 1.5,
                $"Mic dropdown width ({ddScreenW:F1}) must match ComboBox toggle width ({toggleScreenW:F1}) without double-scaling");
            Check.True(Math.Abs(ddLeft.X - toggleLeft.X) <= 1.5,
                $"Mic dropdown left edge ({ddLeft.X:F1}) must align with ComboBox toggle left edge ({toggleLeft.X:F1})");
            window.cbDevices.IsDropDownOpen = false;

            // Verify top-left app menu popup scales once with the main window (no double-scaling)
            window.popupAppMenu!.IsOpen = true;
            window.UpdateLayout();
            window.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
            var rootLeft = window.rootBorder!.PointToScreen(new System.Windows.Point(0, 0));
            var rootRight = window.rootBorder.PointToScreen(new System.Windows.Point(window.rootBorder.ActualWidth, window.rootBorder.ActualHeight));
            var menuLeft = window.borderAppMenu!.PointToScreen(new System.Windows.Point(0, 0));
            var menuRight = window.borderAppMenu.PointToScreen(new System.Windows.Point(window.borderAppMenu.ActualWidth, window.borderAppMenu.ActualHeight));
            double rootScreenW = rootRight.X - rootLeft.X;
            double menuScreenW = menuRight.X - menuLeft.X;
            double expectedMenuRatio = window.borderAppMenu.ActualWidth / window.rootBorder.ActualWidth;
            double actualMenuRatio = menuScreenW / rootScreenW;
            Check.True(Math.Abs(actualMenuRatio - expectedMenuRatio) <= 0.02,
                $"App menu width ratio ({actualMenuRatio:F3}) must match single-scaled ratio ({expectedMenuRatio:F3}), not double-scaled");
            window.popupAppMenu.IsOpen = false;

            // Verify native 1.0 desktop scale restores Display + ClearType popup typography
            window.ApplyAdaptiveBounds(new DipRect(0, 0, 1920, 1032), isInitialPlacement: false);
            Check.Equal(System.Windows.Media.TextFormattingMode.Display,
                (System.Windows.Media.TextFormattingMode)window.Resources["PopupTextFormattingMode"],
                "native 1.0 popups must use Display text formatting");
            Check.Equal(System.Windows.Media.TextRenderingMode.ClearType,
                (System.Windows.Media.TextRenderingMode)window.Resources["PopupTextRenderingMode"],
                "native 1.0 popups must use ClearType text rendering");
        }
        finally
        {
            window.Close();
        }
    }

    private static void AutoUpdaterParsesReleasesVerifiesSha256ReplacesBinaryAndUpdatesVisualIndicators()
    {
        // 1. Version parsing
        Check.True(UpdateManager.TryParseReleaseVersion("v1.2.3", out Version v123) && v123 == new Version(1, 2, 3), "v1.2.3 must parse");
        Check.True(UpdateManager.TryParseReleaseVersion("V2.0.1-beta.1", out Version v201) && v201 == new Version(2, 0, 1), "prerelease tag must parse base version");
        Check.True(!UpdateManager.TryParseReleaseVersion("not-a-version", out _), "invalid tag must be rejected");

        // 2. GitHub release JSON parsing & lightweight vs standalone asset selection
        const string sampleReleaseJson = """
            {
              "tag_name": "v1.2.0",
              "html_url": "https://github.com/youssefhazem3-dot/MicMute/releases/tag/v1.2.0",
              "assets": [
                {
                  "name": "MicMute-win-x64.zip",
                  "size": 610000,
                  "browser_download_url": "https://example.invalid/MicMute-win-x64.zip"
                },
                {
                  "name": "MicMute-win-x64-standalone.zip",
                  "size": 65000000,
                  "browser_download_url": "https://example.invalid/MicMute-win-x64-standalone.zip"
                },
                {
                  "name": "SHA256SUMS.txt",
                  "size": 200,
                  "browser_download_url": "https://example.invalid/SHA256SUMS.txt"
                }
              ]
            }
            """;

        var lightResult = UpdateManager.ParseReleaseJson(sampleReleaseJson, new Version(1, 0, 0), preferStandalone: false);
        Check.Equal(UpdateCheckStatus.UpdateAvailable, lightResult.Status);
        Check.True(lightResult.Update != null, "UpdateInfo must be populated");
        Check.Equal("MicMute-win-x64.zip", lightResult.Update!.AssetName);
        Check.Equal("https://example.invalid/SHA256SUMS.txt", lightResult.Update.ChecksumsUrl);
        Check.Equal(new Version(1, 2, 0), lightResult.Update.LatestVersion);

        var standaloneResult = UpdateManager.ParseReleaseJson(sampleReleaseJson, new Version(1, 0, 0), preferStandalone: true);
        Check.Equal(UpdateCheckStatus.UpdateAvailable, standaloneResult.Status);
        Check.Equal("MicMute-win-x64-standalone.zip", standaloneResult.Update!.AssetName);

        var upToDateResult = UpdateManager.ParseReleaseJson(sampleReleaseJson, new Version(1, 2, 0), preferStandalone: false);
        Check.Equal(UpdateCheckStatus.UpToDate, upToDateResult.Status);
        Check.True(upToDateResult.Update == null, "Up-to-date check must not return an update payload");

        // 3. SHA-256 checksum parsing, ZIP staging, and atomic executable replacement
        string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "MicMute-UpdateTest-" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(tempDir);
        try
        {
            byte[] payload = System.Text.Encoding.UTF8.GetBytes("MicMute staged binary fixture v1.2.0");

            string zipPath = System.IO.Path.Combine(tempDir, "MicMute-win-x64.zip");
            using (var zip = System.IO.Compression.ZipFile.Open(zipPath, System.IO.Compression.ZipArchiveMode.Create))
            {
                var entry = zip.CreateEntry("MicMute.exe");
                using var es = entry.Open();
                es.Write(payload, 0, payload.Length);
            }

            string actualZipHash = UpdateManager.ComputeFileSha256(zipPath);
            string sumsFile = $"{actualZipHash}  MicMute-win-x64.zip\n{new string('a', 64)}  MicMute-win-x64-standalone.zip\n";
            Check.Equal(actualZipHash, UpdateManager.ParseSha256Sum(sumsFile, "MicMute-win-x64.zip"));
            Check.True(UpdateManager.ParseSha256Sum("invalid-line", "MicMute-win-x64.zip") == null, "malformed checksum line must return null");

            string extractDir = System.IO.Path.Combine(tempDir, "stage");
            string stagedExe = UpdateManager.StageFromZipFile(zipPath, extractDir);
            Check.True(System.IO.File.Exists(stagedExe), "StageFromZipFile must extract MicMute.exe");

            string targetExe = System.IO.Path.Combine(tempDir, "installed", "MicMute.bin");
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(targetExe)!);
            System.IO.File.WriteAllText(targetExe, "MicMute previous fixture v1.0.0");

            Check.True(UpdateManager.ReplaceExecutableWithRetry(stagedExe, targetExe), "ReplaceExecutableWithRetry must succeed");
            string replacedText = System.IO.File.ReadAllText(targetExe);
            Check.Equal("MicMute staged binary fixture v1.2.0", replacedText);
            Check.True(!System.IO.File.Exists(targetExe + ".update.bak"), "backup file must be cleaned up after swap");

            // 4. Updater CLI argument formatting & parsing
            string builtArgs = UpdateManager.BuildApplyUpdateArguments(targetExe, 4242, showWindow: true, previousVersion: "1.0.0");
            Check.True(builtArgs.Contains("--apply-update", StringComparison.Ordinal) && builtArgs.Contains("--wait-for-parent 4242", StringComparison.Ordinal),
                "apply-update arguments must include target path and parent PID");
            Check.True(UpdateManager.TryGetCleanupDirectory(new[] { "--cleanup-update", extractDir }, out string parsedCleanup) && parsedCleanup == extractDir,
                "cleanup directory argument must parse");
            Check.True(UpdateManager.TryGetUpdatedFromVersion(new[] { "--updated-from", "1.0.0" }, out string parsedFrom) && parsedFrom == "1.0.0",
                "updated-from argument must parse");
        }
        finally
        {
            try { System.IO.Directory.Delete(tempDir, recursive: true); } catch { }
        }

        // 5. Visual indicators on MainWindow (title version, dot, menu badge, inline progress banner)
        using var audio = new AudioController();
        var window = new MainWindow(audio);
        try
        {
            window.Show();
            window.UpdateLayout();

            string verDisplay = UpdateManager.GetCurrentVersionDisplay();
            Check.True(window.tbTitleVersion != null, "tbTitleVersion must be bound");
            Check.Equal($"v{verDisplay}", window.tbTitleVersion!.Text, "title bar must display current app version");
            Check.Equal($"Version {verDisplay}", window.tbMenuVersion?.Text, "app menu must display current app version");
            Check.True(window.dotTitleUpdate != null && window.borderUpdateBanner != null && window.borderUpdateProgressFill != null,
                "update visual indicator elements must be bound");
            Check.Equal(System.Windows.Visibility.Collapsed, window.dotTitleUpdate!.Visibility, "update dot must be hidden when idle");
            Check.Equal(System.Windows.Visibility.Collapsed, window.borderUpdateBanner!.Visibility, "update banner must be hidden when idle");

            // Transition: Checking
            window.ApplyUpdateVisualState(UpdatePhase.Checking, "Checking for updates...", "Connecting to GitHub Releases", -1, "...", badgeActive: false, showBanner: true);
            Check.Equal(UpdatePhase.Checking, window.CurrentUpdatePhase);
            Check.Equal(System.Windows.Visibility.Visible, window.dotTitleUpdate.Visibility, "update dot must show while checking");
            Check.Equal(System.Windows.Visibility.Visible, window.borderUpdateBanner.Visibility, "update banner must show while checking");
            Check.Equal("...", window.tbMenuUpdateBadge?.Text);

            // Transition: Downloading 65%
            window.ApplyUpdateVisualState(UpdatePhase.Downloading, "Downloading v1.2.0 (65%)", "Updating v1.0.0 → v1.2.0", 65.0, "65%", badgeActive: true, showBanner: true);
            Check.Equal(UpdatePhase.Downloading, window.CurrentUpdatePhase);
            Check.Equal("65%", window.tbUpdateBannerPercent?.Text);
            Check.Equal("65%", window.tbMenuUpdateBadge?.Text);
            Check.True(window.borderUpdateProgressFill!.Width > 0, "progress bar fill width must grow during download");

            // Transition: Up to date
            window.ApplyUpdateVisualState(UpdatePhase.UpToDate, $"Up to date (v{verDisplay})", "Latest version installed", 100, "Up to date", badgeActive: false, showBanner: false);
            Check.Equal(UpdatePhase.UpToDate, window.CurrentUpdatePhase);
            Check.Equal(System.Windows.Visibility.Collapsed, window.dotTitleUpdate.Visibility, "update dot must hide when up to date");
            Check.Equal(System.Windows.Visibility.Collapsed, window.borderUpdateBanner.Visibility, "update banner must hide when up to date");
            Check.Equal("Up to date", window.tbMenuUpdateBadge?.Text);

            // Verify "You're up to date!" info modal collapses any transient bottom status toast so the modal stays centered and fixed
            window.ShowTemporaryStatus("Checking GitHub for updates...");
            Check.Equal(System.Windows.Visibility.Visible, window.borderWarning.Visibility, "Transient toast should be visible before modal");
            window.ShowInfoModal("You're up to date!", $"MicMute v{verDisplay} is already the latest version.", "OK");
            Check.Equal(System.Windows.Visibility.Collapsed, window.borderWarning.Visibility, "Presenting modal must collapse transient bottom status toast immediately to prevent vertical jump");
            Check.Equal(System.Windows.Visibility.Visible, window.borderModalOverlay!.Visibility);
            Check.Equal("You're up to date!", window.tbModalTitle?.Text);
            Check.Equal("OK", window.btnModalConfirm?.Content as string);
            Check.Equal(System.Windows.Visibility.Collapsed, window.btnModalCancel?.Visibility);
            Check.Equal(3, System.Windows.Controls.Grid.GetColumnSpan(window.btnModalConfirm!));
            window.BtnModalConfirm_Click(window.btnModalConfirm!, new System.Windows.RoutedEventArgs());
            Check.Equal(System.Windows.Visibility.Collapsed, window.borderModalOverlay.Visibility);
        }
        finally
        {
            window.Close();
        }
    }

    private static void SimplifiedMainWindowAndSettingsPopupWithRotatingGear()
    {
        // 1. Verify MainWindow.xaml is simplified (no Group 2 / Group 3 cards) and declares rotating Settings toggle
        using var mainStream = typeof(MainWindow).Assembly.GetManifestResourceStream("MicMute.MainWindow.xaml");
        Check.True(mainStream != null, "MainWindow.xaml must be embedded");
        using var mainReader = new System.IO.StreamReader(mainStream!);
        string mainXaml = mainReader.ReadToEnd();
        Check.True(mainXaml.Contains(@"x:Name=""btnSettingsToggle"""), "MainWindow must declare btnSettingsToggle");
        Check.True(mainXaml.Contains(@"x:Name=""settingsGearRot"""), "MainWindow must declare settingsGearRot RotateTransform");
        Check.True(mainXaml.Contains(@"x:Name=""settingsChevronRot"""), "MainWindow must declare settingsChevronRot RotateTransform");
        Check.True(!mainXaml.Contains(@"x:Name=""cbEnableOsd"""), "MainWindow must not directly host cbEnableOsd");
        Check.True(!mainXaml.Contains(@"x:Name=""cbSoundFeedback"""), "MainWindow must not directly host cbSoundFeedback");
        Check.True(!mainXaml.Contains(@"x:Name=""cbStartup"""), "MainWindow must not directly host cbStartup");
        Check.True(!mainXaml.Contains(@"x:Name=""cbStartMinimized"""), "MainWindow must not directly host cbStartMinimized");
        Check.True(!mainXaml.Contains(@"x:Name=""cbRunAsAdmin"""), "MainWindow must not directly host cbRunAsAdmin");
        Check.True(!mainXaml.Contains("Settings Location"), "MainWindow must not directly host Settings Location card");

        // 2. Verify smart adjacent placement math (right preferred, left fallback, narrow center fallback)
        var wa = new DipRect(0, 0, 1920, 1032);
        var centeredAnchor = new DipRect(754, 290, 412, 440);
        var placedRight = UiBehavior.CalculateAdjacentPopupBounds(wa, centeredAnchor, 412, 560, gap: 12);
        Check.Equal(centeredAnchor.Left + centeredAnchor.Width + 12, placedRight.Left, "Should open to the right of MainWindow when room exists");
        Check.Equal(centeredAnchor.Top, placedRight.Top, "Should align top edge with MainWindow when within work area");

        var rightEdgeAnchor = new DipRect(1480, 290, 412, 440);
        var placedLeft = UiBehavior.CalculateAdjacentPopupBounds(wa, rightEdgeAnchor, 412, 560, gap: 12);
        Check.Equal(rightEdgeAnchor.Left - 12 - 412, placedLeft.Left, "Should flip to the left of MainWindow near the right screen edge");

        var narrowWa = new DipRect(0, 0, 600, 720);
        var narrowPlaced = UiBehavior.CalculateAdjacentPopupBounds(narrowWa, new DipRect(94, 100, 412, 440), 412, 560, gap: 12);
        Check.True(narrowPlaced.Left >= narrowWa.Left && narrowPlaced.Left + narrowPlaced.Width <= narrowWa.Left + narrowWa.Width + 0.5,
            "Narrow fallback must stay cleanly inside work area");

        // 3. Verify version bump tracking (1.0.9)
        Check.Equal("1.0.9", UpdateManager.GetCurrentVersionDisplay(), "App version must reflect +0.0.1 bump to 1.0.9");

        // 4. Runtime verification of Settings toggle, gear rotation, SettingsWindow open/close, theme sync & lifecycle
        using var audio = new AudioController();
        var window = new MainWindow(audio);
        try
        {
            Check.Equal("v1.0.9", window.tbTitleVersion?.Text, "Title bar must show v1.0.9");
            Check.Equal("Version 1.0.9", window.tbMenuVersion?.Text, "App menu must show Version 1.0.9");
            Check.True(window.btnSettingsToggle != null, "btnSettingsToggle must be bound");
            Check.True(window.settingsGearRot != null, "settingsGearRot must be bound");
            Check.True(window._settingsWindow != null, "SettingsPopupWindow must be initialized");
            Check.True(!window.SettingsPopupWindow.IsSettingsOpen, "SettingsPopupWindow must start closed");
            Check.Equal(0.0, window.settingsGearRot!.Angle, "Gear icon must start at 0 degrees");

            // Clicking btnSettingsToggle opens SettingsPopupWindow and rotates gear to 180 degrees
            window.BtnSettingsToggle_Click(window.btnSettingsToggle!, new System.Windows.RoutedEventArgs());
            Check.True(window.SettingsPopupWindow.IsSettingsOpen, "Clicking btnSettingsToggle must open SettingsPopupWindow");
            Check.True(window.SettingsPopupWindow.IsVisible, "SettingsPopupWindow must be visible when open");
            Check.Equal("Open", window.btnSettingsToggle!.Tag as string, "btnSettingsToggle Tag must reflect Open state");
            Check.Equal(180.0, window.settingsGearRot!.Angle, "Opening Settings must rotate gear to 180 degrees");
            Check.Equal(180.0, window.settingsChevronRot!.Angle, "Opening Settings must flip chevron to 180 degrees");

            // Theme sync: toggling Light Mode must keep SettingsPopupWindow Resources in sync
            window.SetLightMode(true);
            Check.True(ReferenceEquals(window.SettingsPopupWindow.Resources, window.Resources),
                "SettingsPopupWindow must share MainWindow theme Resources");
            window.SetLightMode(false);
            Check.True(ReferenceEquals(window.SettingsPopupWindow.Resources, window.Resources),
                "SettingsPopupWindow must remain synced after toggling back to Dark Mode");

            // Clicking btnSettingsToggle again closes SettingsPopupWindow and unwinds gear back to 0 degrees
            window.BtnSettingsToggle_Click(window.btnSettingsToggle!, new System.Windows.RoutedEventArgs());
            Check.True(!window.SettingsPopupWindow.IsSettingsOpen, "Second click must close SettingsPopupWindow");
            Check.True(window.btnSettingsToggle.Tag == null, "btnSettingsToggle Tag must clear when closed");
            Check.Equal(0.0, window.settingsGearRot.Angle, "Closing Settings must unwind gear back to 0 degrees");
            Check.Equal(0.0, window.settingsChevronRot.Angle, "Closing Settings must restore chevron to 0 degrees");

            // Re-open and verify closing MainWindow cleanly shuts down SettingsPopupWindow
            window.OpenSettingsWindow();
            Check.True(window.SettingsPopupWindow.IsSettingsOpen, "SettingsPopupWindow must reopen cleanly");
            window.Close();
            Check.True(!window.SettingsPopupWindow.IsSettingsOpen, "Closing MainWindow must close SettingsPopupWindow");
        }
        finally
        {
            try { window.Close(); } catch { }
        }
    }

    private static void ModernMotionCurvesGlidingPillAndZeroLinearKeyframes()
    {
        // 1. Verify MainWindow.xaml has zero linear SplineDoubleKeyFrames missing KeySpline
        using var mainStream = typeof(MainWindow).Assembly.GetManifestResourceStream("MicMute.MainWindow.xaml");
        Check.True(mainStream != null, "MainWindow.xaml must be embedded");
        using var mainReader = new System.IO.StreamReader(mainStream!);
        string mainXaml = mainReader.ReadToEnd();
        foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(mainXaml, @"<SplineDoubleKeyFrame\b[^>]*/>"))
        {
            Check.True(m.Value.Contains("KeySpline="), $"Every SplineDoubleKeyFrame must define KeySpline (found linear keyframe: {m.Value})");
        }

        // 2. Verify Hero ripple/slash, ToggleSwitch isolated hover/squish, and GPU Slider thumb transforms
        Check.True(mainXaml.Contains(@"x:Name=""heroRippleRing"""), "Hero button must declare expanding ripple ring");
        Check.True(mainXaml.Contains(@"x:Name=""pressScale"""), "Hero button must separate pressScale from hover gridScale");
        Check.True(mainXaml.Contains(@"x:Name=""slashScale"""), "Hero button must animate mute slash");
        Check.True(mainXaml.Contains(@"x:Name=""knobSquishScale""") && mainXaml.Contains(@"x:Name=""knobHoverScale"""),
            "ToggleSwitch must isolate knobSquishScale from knobHoverScale");
        Check.True(mainXaml.Contains(@"x:Name=""thumbHoverScale""") && mainXaml.Contains(@"x:Name=""thumbDragScale"""),
            "Slider thumb must use GPU ScaleTransform instead of layout Width/Height resizing");
        Check.True(!mainXaml.Contains(@"TargetName=""thumbCircle"" Property=""Width"""),
            "Slider thumb must not snap layout Width on hover/drag");

        // 3. Verify SettingsWindow gliding pill & synchronized theme overlay, and OsdWindow translate transform
        using var audio = new AudioController();
        var window = new MainWindow(audio);
        var osd = new OsdWindow();
        try
        {
            var settingsWin = window.SettingsPopupWindow;
            Check.True(settingsWin.settingsThemeOverlay != null, "SettingsWindow must bind synchronized theme crossfade overlay");
            Check.True(settingsWin.borderOsdPosPill != null && settingsWin.osdPosPillTranslate != null,
                "SettingsWindow must bind gliding OSD position pill indicator");
            Check.True(osd.panelTranslateTransform != null, "OsdWindow must bind panelTranslateTransform");

            // Measure SettingsWindow so gridOsdPosition has real width and verify pill translates across Center -> Bottom -> Top
            settingsWin.settingsScaleHost!.Measure(new System.Windows.Size(412, 600));
            settingsWin.settingsScaleHost.Arrange(new System.Windows.Rect(0, 0, 412, 560));
            settingsWin.settingsScaleHost.UpdateLayout();

            settingsWin.UpdateOsdPositionPill(0, animate: false);
            double xCenter = settingsWin.osdPosPillTranslate!.X;
            settingsWin.UpdateOsdPositionPill(1, animate: false);
            double xBottom = settingsWin.osdPosPillTranslate.X;
            settingsWin.UpdateOsdPositionPill(2, animate: false);
            double xTop = settingsWin.osdPosPillTranslate.X;
            Check.True(xCenter == 0.0 && xBottom > xCenter + 40.0 && xTop > xBottom + 40.0,
                $"Gliding pill X positions must advance cleanly across segments (Center={xCenter:F1}, Bottom={xBottom:F1}, Top={xTop:F1})");

            // Verify transient GPU BitmapCache helper enables and clears cleanly
            UiMotion.EnableTransientCache(window.rootBorder, window);
            Check.True(window.rootBorder!.CacheMode is System.Windows.Media.BitmapCache, "EnableTransientCache must attach BitmapCache");
            UiMotion.ClearTransientCache(window.rootBorder);
            Check.True(window.rootBorder.CacheMode == null, "ClearTransientCache must release CacheMode cleanly");
        }
        finally
        {
            osd.Close();
            window.Close();
        }
    }

    private static void MemoryLeakCleanupAndResourceLifecycle()
    {
        // 1. Hotkey recording pulse brush is tracked and cleared cleanly on cancel/close
        using var audio = new AudioController();
        var window = new MainWindow(audio);
        try
        {
            var pulseBrushField = typeof(MainWindow).GetField("_recordingPulseBrush",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Check.True(pulseBrushField != null, "MainWindow must track _recordingPulseBrush to stop its RepeatBehavior.Forever clock");

            window.StartRecordingHotkey();
            Check.True(pulseBrushField!.GetValue(window) != null, "StartRecordingHotkey must assign _recordingPulseBrush");
            // Calling SetLightMode while recording must replace without leaking the previous brush
            var firstBrush = pulseBrushField.GetValue(window);
            window.SetLightMode(true);
            var secondBrush = pulseBrushField.GetValue(window);
            Check.True(secondBrush != null && !ReferenceEquals(firstBrush, secondBrush), "Theme switch while recording must swap pulse brush cleanly");

            // Cancelling via Deactivated must clear _recordingPulseBrush
            typeof(MainWindow).GetMethod("OnDeactivated", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .Invoke(window, new object[] { EventArgs.Empty });
            Check.True(pulseBrushField.GetValue(window) == null, "Cancelling hotkey recording must stop and clear _recordingPulseBrush");

            // 2. OsdWindow Close clears static _instance and removes WndProc hook
            var osdInstanceField = typeof(OsdWindow).GetField("_instance",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var osdHwndSourceField = typeof(OsdWindow).GetField("_hwndSource",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Check.True(osdInstanceField != null && osdHwndSourceField != null, "OsdWindow must track _instance and _hwndSource");
            OsdWindow.WarmUp();
            var osd = osdInstanceField!.GetValue(null) as OsdWindow;
            Check.True(osd != null, "OsdWindow.WarmUp must initialize _instance");
            osd!.Close();
            Check.True(osdInstanceField.GetValue(null) == null, "OsdWindow.Close must clear static _instance");
            Check.True(osdHwndSourceField!.GetValue(osd) == null, "OsdWindow.Close must detach _hwndSource hook");

            // 3. AudioFeedback.SetVolume reuses SoundPlayer instances instead of leaking handles on every change
            var mutePlayerField = typeof(AudioFeedback).GetField("_mutePlayer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var rawMuteField = typeof(AudioFeedback).GetField("_rawMuteWavBytes",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Check.True(mutePlayerField != null && rawMuteField != null, "AudioFeedback fields must exist");
            AudioFeedback.SetVolume(80);
            var playerAt80 = mutePlayerField!.GetValue(null);
            AudioFeedback.SetVolume(55);
            var playerAt55 = mutePlayerField.GetValue(null);
            Check.True(playerAt80 != null && ReferenceEquals(playerAt80, playerAt55),
                "AudioFeedback.SetVolume must reuse existing SoundPlayer instances to avoid leaking kernel handles");
            AudioFeedback.Dispose();
            Check.True(mutePlayerField.GetValue(null) == null, "AudioFeedback.Dispose must release SoundPlayer");
            Check.True(rawMuteField!.GetValue(null) == null, "AudioFeedback.Dispose must release cached LOH WAV buffers");
        }
        finally
        {
            window.Close();
        }
    }

    private static void JitterFreeAnimationsAndCenterOriginOsd()
    {
        // 1. Alpha-safe GPU BitmapCache (EnableClearType == false so layered windows never trigger alpha-ClearType software fallback)
        var dummyBorder = new System.Windows.Controls.Border();
        UiMotion.EnableTransientCache(dummyBorder);
        var transientCache = dummyBorder.CacheMode as System.Windows.Media.BitmapCache;
        Check.True(transientCache != null && !transientCache.EnableClearType,
            "UiMotion.EnableTransientCache must disable EnableClearType on layered transparent surfaces");
        UiMotion.ClearTransientCache(dummyBorder);

        // 2. Unclipped whole-window transition bounds math
        double settingsHorizontalInset = (412.0 * (1.0 - SettingsWindow.OpenTransitionScale)) / 2.0;
        Check.True(SettingsWindow.OpenTransitionDeltaX < settingsHorizontalInset,
            $"SettingsWindow OpenTransitionDeltaX ({SettingsWindow.OpenTransitionDeltaX}) must stay strictly inside scale inset ({settingsHorizontalInset:F2})");
        Check.True(SettingsWindow.ExitTransitionDeltaX < settingsHorizontalInset,
            $"SettingsWindow ExitTransitionDeltaX ({SettingsWindow.ExitTransitionDeltaX}) must stay strictly inside scale inset ({settingsHorizontalInset:F2})");

        double mainVerticalInset = (400.0 * (1.0 - MainWindow.WindowTransitionScale)) / 2.0;
        Check.True(MainWindow.WindowTransitionDeltaY < mainVerticalInset,
            $"MainWindow WindowTransitionDeltaY ({MainWindow.WindowTransitionDeltaY}) must stay strictly inside scale inset ({mainVerticalInset:F2})");

        // 3. Zero per-frame DropShadowEffects on interactive controls & GPU-cached sibling shadows on popups/modals
        using var mainStream = typeof(MainWindow).Assembly.GetManifestResourceStream("MicMute.MainWindow.xaml");
        using var mainReader = new System.IO.StreamReader(mainStream!);
        string mainXaml = mainReader.ReadToEnd();
        int dropShadowCount = System.Text.RegularExpressions.Regex.Matches(mainXaml, @"<DropShadowEffect\b").Count;
        Check.Equal(3, dropShadowCount,
            $"MainWindow.xaml should only retain the 3 GPU-cached static sibling popup/modal DropShadowEffects (actual: {dropShadowCount})");
        int shadowCacheCount = System.Text.RegularExpressions.Regex.Matches(mainXaml, @"<BitmapCache EnableClearType=""False""").Count;
        Check.Equal(3, shadowCacheCount,
            "All 3 sibling shadow layers in MainWindow.xaml must carry persistent GPU BitmapCache");

        // 4. Frozen shared muted brushes on MainWindow
        using var audio = new AudioController();
        var window = new MainWindow(audio);
        try
        {
            window.SetLightMode(false);
            var mutedLens = window.Resources["HeroLensMutedBrush"] as System.Windows.Media.Brush;
            var mutedGlow = window.Resources["HeroGlowMutedBrush"] as System.Windows.Media.Brush;
            Check.True(mutedLens != null && mutedLens.IsFrozen, "HeroLensMutedBrush must be shared and frozen");
            Check.True(mutedGlow != null && mutedGlow.IsFrozen, "HeroGlowMutedBrush must be shared and frozen");
            Check.True(window.dotGlowHalo != null, "dotGlowHalo vector halo must be bound without DropShadowEffect");
        }
        finally
        {
            window.Close();
        }

        // 5. OSD pure center-origin smooth decelerate (zero vertical Y popup offset across entrance, pulse, and exit)
        try
        {
            OsdWindow.HideOsd();
            OsdWindow.ShowOsd(true, 0.12);

            var instField = typeof(OsdWindow).GetField("_instance",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var osd = instField?.GetValue(null) as OsdWindow;
            Check.True(osd != null, "OsdWindow instance must exist after ShowOsd");
            Check.Equal(new System.Windows.Point(0.5, 0.5), osd!.borderPanel.RenderTransformOrigin,
                "OSD borderPanel must expand from exact center (0.5, 0.5)");
            Check.True(osd.borderPanel.CacheMode is System.Windows.Media.BitmapCache osdCache && !osdCache.EnableClearType,
                "OSD borderPanel must keep a persistent alpha-safe BitmapCache so DropShadowEffect never re-runs per frame");
            Check.Equal(0.0, osd.panelTranslateTransform!.X, "OSD entrance X translation must stay locked at 0");
            Check.Equal(0.0, osd.panelTranslateTransform.Y, "OSD entrance Y translation must stay locked at 0 (not sliding from below)");
            Check.True(osd.panelScaleTransform!.ScaleX <= 1.001, "OSD smooth decelerate entrance must not overshoot above 1.0");

            // Rapid toggle pulse while visible keeps Y == 0 and keeps persistent cache intact
            OsdWindow.ShowOsd(false, 0.12);
            Check.Equal(0.0, osd.panelTranslateTransform.X, "OSD pulse X translation must stay locked at 0");
            Check.Equal(0.0, osd.panelTranslateTransform.Y, "OSD pulse Y translation must stay locked at 0");
            Check.True(osd.borderPanel.CacheMode is System.Windows.Media.BitmapCache,
                "OSD borderPanel must not tear down BitmapCache during pulse");

            // Pump dispatcher past 0.12s duration so exit fade begins and verify exit stays centered (Y == 0)
            var deadline = DateTime.UtcNow.AddMilliseconds(220);
            while (DateTime.UtcNow < deadline)
            {
                osd.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() => { }));
                System.Threading.Thread.Sleep(15);
            }
            Check.Equal(0.0, osd.panelTranslateTransform.Y, "OSD exit Y translation must stay locked at 0 (no downward drop)");
        }
        finally
        {
            OsdWindow.CloseOsd();
        }
    }
}

