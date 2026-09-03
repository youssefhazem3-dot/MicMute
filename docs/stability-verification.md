# MicMute stability verification — 2026-09-03

Latest follow-up, 17:33: shortcut observation is now nonexclusive. A real Win32 regression first reproduced failure to coexist with another shortcut client, then passed after removing RegisterHotKey. All 40 tests pass; Release build has 0 warnings and 0 errors. Updated ZIP SHA256: `8F99C29EF0B6B1761BE249E8434787DD3B4E4E7F56379ED467D5106BF9F9B37D`. Project root, publish and loose Downloads files match. Verified new app is running from E:/MicMute/MicMute.exe, PID 13172. The native test used an unused diagnostic chord and did not inject keys or alter the clipboard/microphone.

Implementation and final review were completed directly. The user permanently prohibited subagents. No commit, push or remote release was performed.

## Automated evidence

- Existing-binary baseline: four actual failing cases (compact JSON, repair of both missing ID braces, legacy NaN, repeated held-key activations). Isolated fixture: `.artifacts/baseline/`.
- `./scripts/build.ps1 -NoRestore`: Release build succeeded, 0 warnings, 0 errors.
- `./scripts/test.ps1 -NoRestore`: 39 passed, 0 failed.
- `./scripts/publish.ps1 -NoRestore -UpdateRoot`: test gate passed; SDK publish succeeded; GUI subsystem verified; root and publish artifacts updated; prior files backed up in `.artifacts/previous-release/`.
- Refreshed package (17:10): `MicMute.zip`, SHA256 `B4B6B389378BF2492C9323CB1869B590B56FDE72440FB1F6CCFF95E78615C170`. Forced rebuild and all 39 tests passed again. Root, publish, and both unpacked Downloads files and Downloads ZIP are synchronized; the unpacked Downloads executable had previously remained on September 1.

## Findings resolved

| Finding | Resolution / coverage |
|---|---|
| Held shortcut / cross-engine duplicates | Shared timestamped press/release state; repeated, concurrent, rapid, delayed, same-tick and rollover cases |
| Ctrl/Shift shortcuts toggling single-key binding | Exact modifiers in every listener; mismatch consumes the current hold |
| Malformed raw-input result | UINT_MAX, lengths, header/type/key/message validated before use; stale-buffer tests |
| Corrupt legacy IDs and compact JSON | System.Text.Json codec; braces repaired; comma/escaped/multiline/compact fixtures |
| NaN and invalid persisted settings | Finite duration normalization and enum validation; legacy bare NaN accepted only for migration |
| Duration locale/input fighting | Culture-aware finite parsing with invariant fallback; suppression of recursive slider saves |
| Long tray names | Tooltip capped at 127 characters |
| Settings disk work | Immediate cache update; 200ms coalesced atomic background writes; explicit Flush and reported failures |
| Failed writes / pending migration/reset | Pending value retained for retry; transactions serialized; failure and transition tests |
| Portable/custom/reset storage | Portable migration applies new path; failed move preserves old data; portable reset persists defaults in same folder |
| Reset not applied | Runtime device/hotkey/startup/admin and controls updated; old binding disabled if default cannot register, with visible error |
| Start minimized ignored | Stored preference honored; explicit --show/--minimized overrides; login command no longer forces minimized |
| Administrator restart mutex race | Child waits for specific parent to exit before acquiring instance mutex; real isolated process wait tested |
| DPI coordinate mismatch | Native screen coordinates and actual native window size; coordinate tests and OSD resource constructor smoke test |
| Duplicate device refreshes | One dispatcher debouncer; immediate duplicate updates removed; unchanged valid endpoint retained; 100-request coalescing/disposal test |
| Audio callbacks/disposal | Callbacks queue to UI and return without blocking COM calls/locks; stale endpoint events rejected; volume-only changes filtered |
| Build conflicts/private scripts | Explicit embedded raw XAML; duplicate SDK generation disabled; checked-in build/test/publish scripts |

## Review notes

The final review checked native buffer lifetime, polling stop/join, registration rollback, modifier-state ordering, settings-path transitions and atomic writes, exception paths, callback threading, shutdown Flush, resource embedding and packaging contents. Additional review fixes included retrying transient save failures and preserving portable reset location. Existing complete-stream loading, explicit HICON cleanup, frozen brushes and playback locking were retained.

## Limits

Tests used isolated temporary settings folders and synthetic input. They did not change the user's microphone, startup preferences, registry or live process. Real device hotplug/driver failures, mixed-DPI transitions, UAC consent, exclusive-fullscreen overlays and protected games still require target-machine testing. On the subsequent user-requested build refresh, the verified project-root executable was launched with --show (PID 8140, 17:14:38), and confirmed responding.
