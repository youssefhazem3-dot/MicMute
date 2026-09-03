# MicMute

A Windows 10/11 microphone mute utility with configurable global shortcuts, a tray menu, optional sound feedback, an on-screen status popup, and light/dark themes.

[![Download MicMute.zip](https://img.shields.io/badge/Download-MicMute.zip-0078D4?style=for-the-badge&logo=windows&logoColor=white)](https://github.com/youssefhazem3-dot/MicMute/raw/main/MicMute.zip)

## Run

1. Install the [.NET 8 Desktop Runtime for Windows x64](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).
2. Extract `MicMute.zip` into a writable folder.
3. Run `MicMute.exe`. If **Start minimized** is enabled, open the control panel by double-clicking its tray icon.

This is a framework-dependent package. Keep the executable, DLLs, `.deps.json`, and `.runtimeconfig.json` together.

## Shortcuts

The default shortcut is **F1**. Click **Record** to change it. Each physical press toggles once; holding the key does not repeatedly toggle. All modifier keys must match the recorded combination exactly: a binding for `V` does not also trigger on `Ctrl+V` or `Shift+V`.

Shortcuts work alongside their normal action: for example, binding **Ctrl+C** both copies in the foreground application and toggles the microphone. MicMute observes input through a low-level keyboard hook, raw input and physical polling, sharing one press/release state. It does not reserve the shortcut with Windows or block the original keystroke. Shortcut recording suspends the old shortcut until recording finishes or is cancelled.

The popup is click-through and centered on the monitor containing the pointer, using native screen coordinates. Some games and secure desktops restrict external input or overlays; exclusive-fullscreen and anti-cheat behavior depends on the game.

## Settings and storage

- Default location: `%APPDATA%\\MicMute\\settings.json`.
- Portable mode: put `portable.flag` or `settings.json` beside the executable.
- **Change…** moves the active settings to the chosen folder and leaves portable mode when appropriate. Failed moves report an error and retain the previous active location.
- **Reset Data** restores preferences in the current settings folder and applies the default device, shortcut, startup/admin preferences, and appearance. Unrelated files are preserved.
- Settings accept ordinary JSON, including compact formatting and escaped strings. Old microphone IDs missing outer braces are repaired.
- Duration accepts the current locale's decimal separator, with `.` as a fallback, from 0.1 to 30 seconds.
- Changes update memory immediately and are saved atomically after a short quiet period. Pending changes are flushed on a normal exit or administrator restart. Save failures are reported.
- The stored **Start minimized** preference applies to normal and login launches. `--show` and `--minimized` explicitly override it.

## Build, test and package

Install the [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0), then run PowerShell from the repository root:

```powershell
.\\scripts\\build.ps1
.\\scripts\\test.ps1
.\\scripts\\publish.ps1 -UpdateRoot
```

The scripts use an optional workspace-local SDK at `.tools/dotnet/dotnet.exe`, otherwise the SDK on PATH. Tool caches and intermediate artifacts stay in ignored folders. `global.json` selects the .NET 8 SDK family.

- Build: `bin/Release/net8.0-windows/win-x64/`.
- Tests: a package-free console regression runner; nonzero exit status indicates failure. Use `-Area settings`, `-Area hotkey`, or `-Area ui` for a focused run.
- Package: `MicMute.zip` and `publish/`. `-UpdateRoot` also updates the repository-root executable and dependencies. Previous files are backed up under `.artifacts/`.
- A running/locked destination is left untouched; the new ZIP remains available to extract after closing that instance.
- Once packages have been restored, the scripts support `-NoRestore`.

Standard SDK commands also work:

```powershell
dotnet build MicMute.csproj -c Release
dotnet run --project tests/MicMute.Tests/MicMute.Tests.csproj -c Release
```

The project deliberately embeds raw XAML for its existing custom window initialization. Default WPF XAML and assembly-info generation are disabled to avoid duplicate members. The SDK produces the GUI host and application icon; no private scripts or PE patching are required.

## Main components

| File | Responsibility |
|---|---|
| `HotkeyManager.cs`, `HotkeyState.cs`, `RawKeyboardPacket.cs` | Native input, exact modifier matching, repeat suppression, packet validation |
| `AudioController.cs` | Audio-device selection, mute state, dispatcher-safe notifications |
| `SettingsManager.cs`, `SettingsStore.cs`, `SettingsCodec.cs` | Settings facade, atomic/coalesced persistence, JSON compatibility |
| `App.xaml.cs`, `MainWindow.xaml.cs`, `OsdWindow.xaml.cs` | Lifecycle, tray, controls, popup |
| `UiBehavior.cs` | Duration/tooltip/startup/positioning policies and refresh debouncing |
| `AdminManager.cs`, `StartupManager.cs` | Elevation and login startup preferences |

## Verification scope

Automated checks exercise input ordering and concurrency, malformed raw packets, JSON migration, filesystem failures and transitions, UI policies, XAML loading, dispatcher debounce and process-exit waiting. They do not change the user's microphone, settings or startup registry entries.

Live UAC prompts, every audio driver, mixed-DPI monitor transitions, and protected games still require testing on the target machine.
