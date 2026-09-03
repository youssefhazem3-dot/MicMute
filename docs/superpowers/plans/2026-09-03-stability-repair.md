# MicMute Stability Repair Implementation Plan

> Execute directly using focused-fix/TDD. The user permanently prohibited subagents during this task. The user authorized implementation with “okay fix all these issues”; no additional approval gate or commit is authorized.

**Goal:** Fix every remaining second-audit issue and deliver verified source plus a reproducible local package.

**Architecture:** Retain WPF/native integration and raw XAML loading. Add shared press/release hotkey state, System.Text.Json settings with atomic coalesced persistence, and UI-thread device-refresh coordination.

**Tech Stack:** C#/.NET 8 Windows, WPF, WinForms, NAudio 2.2.1, PowerShell, package-free console regressions.

## Constraints and stable interfaces

- Preserve user edits; no commit/push, remote release, or unrelated cleanup. Coordinator alone updates handoff.
- Exact modifiers everywhere, including single keys; one activation per hold across all engines. No implicit Ctrl/Shift game exception.
- Keep public SettingsManager Load(), Save(AppSettings), GetDataFolderPath(), SetCustomDataFolder(string), ResetAllSettings(), OpenDataFolderInExplorer(). Add Flush() and `event EventHandler<string>? SaveFailed`. Save updates cache immediately and coalesces atomic writes; Flush persists synchronously and throws on failure. Location changes throw on failure and are transactional. Reset preferences in current directory, preserving unrelated files.
- Keep HotkeyManager Register(Key, ModifierKeys), Unregister(), Dispose(), and HotkeyPressed. Unregister suspends every engine; reset press state safely on binding changes.
- Test files use `namespace MicMute.Tests; static class SettingsCases/HotkeyCases/UiCases` with `public static void Run(Action<string, Action> test)`. Assertions: Check.True(bool,string), Check.Equal<T>(T,T,string=""), Check.Throws<T>(Action). Coordinator owns Program.cs, test project and scripts. Workers own area case files.

## Task 1 — Settings, solo

Own SettingsManager.cs, new SettingsStore.cs/SettingsCodec.cs as needed, tests/MicMute.Tests/SettingsCases.cs. Do not edit UI/input/build.

- [x] Add failing cases for fresh/damaged IDs, compact/multiline JSON, comma/escaped paths, finite durations, atomic/coalesced persistence and Flush, failures, portable-to-custom migration, resets and reloads.
- [x] Replace handwritten JSON with System.Text.Json, accepting legacy enum/numeric representations and invalid named floats for migration. Validate/rebuild both missing braces for known endpoint ID format. Clamp finite duration and replace NaN/infinity safely.
- [x] Use an instance store with constructor-supplied app/default paths for real isolated filesystem tests. Serialize writes/path transitions, surface async failures, preserve current settings during migration, never repopulate stale caches during reset.
- [x] Run targeted regressions, report red/green evidence and interface details.

## Task 2 — Input, solo

Own HotkeyManager.cs, new HotkeyState.cs/input helpers as needed, tests/MicMute.Tests/HotkeyCases.cs.

- [x] Write failing hold/repeat, concurrent engine, rapid repress, modifier mismatch, unregister/rebind and invalid raw-packet cases.
- [x] Use a shared lock-protected key press/release gate; hook/raw/polling must not independently re-trigger a held key. Exact modifiers only. Do not solve repeat suppression solely with an elapsed-time debounce.
- [x] Reject UINT_MAX/short reads; reuse buffer safely. Stop/join polling, unregister raw input and dispose resources. Preserve prior binding on registration failure. Keep low-level hook work bounded.
- [x] Run targeted tests and report native-only verification limitations.

## Task 3 — UI/audio/lifecycle, solo

Own MainWindow.xaml.cs, App.xaml.cs, OsdWindow.xaml.cs, AudioController.cs, AdminManager.cs, StartupManager.cs, new UiBehavior.cs/helpers as needed, tests/MicMute.Tests/UiCases.cs. Do not edit settings/input/build files.

- [x] Write failing regressions for duration culture/NaN, tooltip length, start policy, screen coordinates, restart ordering and refresh coalescing where isolated execution is possible.
- [x] Suppress reload handlers and apply reset to active microphone, shortcut, startup/admin flags and controls. Surface location/save failures, Flush before shutdown/restart.
- [x] Bound tooltip length and match duration formatting/parsing culture, accepting invariant decimal fallback without thousands ambiguity.
- [x] Position OSD using native screen pixels and actual native window rectangle; preserve click-through/focus behavior.
- [x] Route device notifications through one dispatcher debouncer; remove immediate duplicate rebuilds, avoid reopening unchanged valid endpoints, reject callbacks during disposal, clean up timers/tokens.
- [x] Child restart waits for parent exit before mutex acquisition; parent shuts down after successful spawn. Stored StartMinimized is effective with explicit minimized/show overrides. Do not restart the user's process while implementing.
- [x] Run cases and report outcomes.

## Task 4 — Tooling/build/integration, coordinator

Own MicMute.csproj, tests/MicMute.Tests/{MicMute.Tests.csproj,Program.cs}, scripts, .gitignore, README.md, handoff.md.

- [x] Establish workspace-local SDK or use existing compiler. Add package-free regression runner with selectable area and nonzero failures.
- [x] Disable conflicting default XAML/assembly generation and implicit usings; embed raw resources with expected names; exclude tests from app compile. Verify ordinary dotnet build and native GUI executable.
- [x] Run regressions before/after fixes, Release build and framework-dependent publish. Document .NET 8 Desktop Runtime requirement and exact build/test/package commands.
- [x] Independently review high-risk input/settings and integrated diff; fix material findings and rerun relevant checks. Preserve active locked binaries; no indiscriminate process termination.
- [x] Update local package and handoff with actual verification and remaining hardware limits.

## Risks and containment, highest first

1. Accidental unmute: shared state, exact modifiers, deterministic hold/concurrency tests and available native smoke checks.
2. Settings loss: isolated filesystem regressions, atomic replacement, serialized persistence/transitions, flush on exit, failure reporting and unchanged source files on failed migration.
3. Lifecycle races: parent-exit handshake, dispatcher/disposal guards, no kill during implementation.
4. Build/XAML divergence: keep existing raw-resource route, verify resource names, build and smoke-test actual release. Standard generated WPF was considered but broadens this repair unnecessarily.
5. DPI/hardware coverage: coordinate/policy tests and available monitors; document untested devices/games. Timing-only input debounce and further handwritten JSON patching were rejected because they preserve root causes.

## Completion notes

Completed directly after the user's no-subagents instruction. See `docs/stability-verification.md` for exact automated verification and hardware limits. Independent-worker review was superseded by the user's instruction; coordinator performed the complete final review. Automated XAML/native-policy checks do not replace UAC/game/physical hotplug testing.
