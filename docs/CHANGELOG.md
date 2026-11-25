# MicMute Version History

Every change increments the app version by `+0.0.1` (`AssemblyVersion` & `AssemblyFileVersion` in `src/MicMute/AssemblyInfo.cs`).

## v1.0.9 — 2026-11-25
- **Antivirus False-Positive Mitigation & Startup Hardening:**
  - Removed the hidden `schtasks.exe` CLI shell-out fallback from `StartupManager` (relying cleanly on the built-in Windows Task Scheduler 2.0 COM API `Schedule.Service`) to eliminate heuristic persistence triggers.
  - Guarded `AdminManager.SetRunAsAdmin` with a read-only registry check before opening `AppCompatFlags\Layers` writable, eliminating unnecessary write-mode registry access on startup.
  - Enabled `<Deterministic>true</Deterministic>` builds and added `AssemblyInformationalVersion`.
  - Added [CODE_SIGNING_POLICY.md](../CODE_SIGNING_POLICY.md) and optional [SignPath Foundation](https://signpath.org/) code-signing workflow support in `.github/workflows/release.yml`.

## v1.0.8 — 2026-11-24
- **Animation Performance & OSD Polish:**
  - Eliminated animation jitter and switched OSD transitions to smooth center-origin motion.

## v1.0.7 — 2026-11-22
- **Memory, Handle & Animation Clock Leak Fixes:**
  - Closed the temporary parsed XAML `Window` in `SettingsWindow.InitializeComponent()` so it is no longer pinned in `Application.Current.Windows`.
  - Tracked and stopped the `RepeatBehavior.Forever` hotkey-recording border pulse brush and all infinite update animations on cancel, hide, theme switch, and window close; released `HoldEnd` animation clocks and transient GPU `BitmapCache`s on completion/dismissal; cleared `RenderTargetBitmap` theme crossfade overlays on hide/close.
  - Reused `SoundPlayer` instances across `AudioFeedback.SetVolume()` (eliminating 2 leaked kernel `ManualResetEvent` handles per volume change) and released LOH chime buffers on `Dispose()`.
  - Disposed tray menu GDI `Font`, cleared `NotifyIcon.Icon`, unsubscribed `TaskbarCreated`, and disposed `SettingsManager` in `App.Teardown()`.
  - Detached `HwndSource` hooks and cleared `WM_SETICON`/`SetClassLongPtr` on `WM_DESTROY` while HWNDs are still valid (`MainWindow` & `OsdWindow`), and cancelled/disposed `LatestRefreshCoordinator` retry delays, `HotkeyManager` native resources (with atomic disposal and constructor rollback), and `AudioWorkQueue` handles cleanly.
- **Dead Code & Redundancy Cleanup:**
  - Removed unused P/Invokes (`DestroyIcon`, `ShowWindow` in `MainWindow`), dead `TrimWorkingSet()` no-op, unused `DiagnosticLogger.EnableConsole()`, unused `SettingsManager.SetPortableMode`, unused `AudioController.IsUsingFallback`, unused `UiMotion` constants, unused `ScrollBarThumbDragBrush`, dead hidden `imgAppIcon`, redundant per-button activation lambdas, duplicate `AudioFeedback.SetVolume` scheduling, and unreferenced XAML `x:Name`s.
  - Moved slider `PreviewMouseWheel` forwarding from `MainWindow` into `SettingsWindow` targeting `settingsScrollViewer`, eliminated per-keystroke modifier array allocations in `HotkeyState`, and fixed `schtasks` stdout/stderr pipe redirection in `StartupManager`.

## v1.0.6 — 2026-11-22
- **Unified Modern Motion System (`UiMotion`):** Standardized durations and frozen exponential deceleration / gentle spring curves across all windows, popups, and controls, and raised display refresh pacing cap to `240 FPS` for native `144Hz`/`165Hz`/`240Hz` VSync cadence.
- **Zero-Jitter Transient GPU Caching:** Enabled DPI-aware `BitmapCache` during whole-window and heavy-shadow transitions (`MainWindow`, `SettingsWindow`, `OsdWindow`, modals, dropdowns, app menu) and cleared it on rest so text stays crisp while motion runs smoothly.
- **Hero Mute Button, Toggle Switch & Slider Polish:** Added expanding ripple ring and animated mute slash on the Hero button, separated hover and press/click transforms so they never conflict, replaced linear keyframes with real cubic-bezier/spring curves, and replaced layout-thrashing slider thumb resizing with GPU `ScaleTransform`.
- **Upgraded Popups, Tray Open, OSD & Theme Sync:** Fixed clipped cross-screen tray-open teleport with a clean in-bounds lift, replaced fighting ComboBox `PopupAnimation="Fade"` with code-driven open animation on every open, added smooth close animation for the top-left app menu, added a smoothly gliding segmented pill indicator for OSD Position (`Center | Bottom | Top`), added directional glide entrance/exit for `SettingsWindow`, synchronized Light/Dark theme crossfades across both windows, and added tactile press micro-scale to buttons.

## v1.0.5 — 2026-11-22
- **Fixed Modal Vertical Shift on Check for Updates:** Dismissed transient bottom status toasts immediately before presenting any modal overlay (and removed the redundant bottom toast when showing the `"You're up to date!"` modal), keeping the modal dead-center and fixed with zero vertical jump.

## v1.0.4 — 2026-11-22
- **Simplified Main Window UI:** Streamlined the main window to show only the Hero Mute/Unmute button, Microphone Input selector, Shortcut Key section, and a dedicated Settings toggle row underneath.
- **Interactive Rotating Settings Toggle:** Added a Liquid Glass Settings toggle button with a vector gear icon that smoothly rotates on hover, open, and close interactions.
- **Dedicated Settings Popup Window:** Moved secondary settings (On-Screen Display, Sound Effects, Run on Windows Startup, Start Minimized, Run as Administrator, and Settings Location) into a draggable, owned Liquid Glass Settings popup window with smart adjacent placement, independent laptop adaptive scaling, instant Light/Dark theme sync, and in-window confirmation modals.
- **Native 1.0x Main Window on Laptops:** Compact main window height now fits at native 1.0 scale on common laptop displays without downscaling.

## v1.0.3 — 2026-11-22
- **Fixed Popup Double-Scaling on Laptops:** Removed duplicate `LayoutTransform` bindings from `dropDownBorder` (mic dropdown) and `borderAppMenu` (top-left menu) so WPF's `PopupRoot` scales them once, restoring exact 1:1 mic dropdown width alignment and proper top-left menu sizing.
- **Fixed Blurry Popup & Modal Text:** Isolated `DropShadowEffect` onto sibling background layers so text is never bitmap-rasterized, dynamically switched popup text formatting between `Ideal` (scaled) and `Display` (native 1.0), and enabled `PerMonitorV2` high-DPI mode at startup in `App.Main`.

## v1.0.2 — 2026-11-20
- Responsive laptop adaptive scaling, GitHub auto-updater, DaVinci-style top-left app menu (Startup State, Sync All Mics, Mute Lock), mouse hotkey support, and Hold/Toggle mode.
