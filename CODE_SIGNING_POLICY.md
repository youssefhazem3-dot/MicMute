# Code Signing Policy

Free code signing provided by [SignPath.io](https://signpath.io/), certificate by [SignPath Foundation](https://signpath.org/).

## Scope of Signing

Official Windows release binaries built exclusively from tagged releases in the public [`youssefhazem3-dot/MicMute`](https://github.com/youssefhazem3-dot/MicMute) GitHub repository via GitHub-hosted Actions runners (`.github/workflows/release.yml`) are eligible for code signing:

- `MicMute.exe` (Lightweight build packaged in `MicMute-win-x64.zip`)
- `MicMute.exe` (Standalone self-contained build packaged in `MicMute-win-x64-standalone.zip`)

Local developer builds and third-party dependencies outside the official GitHub Actions release pipeline are not signed with the project certificate.

## Team Roles & Responsibilities

All project members with commit, review, or signing approval privileges enforce Multi-Factor Authentication (MFA) on both GitHub and SignPath:

- **Author / Maintainer:** `@youssefhazem3-dot`
- **Reviewer:** `@youssefhazem3-dot`
- **Signing Approver:** `@youssefhazem3-dot`

Production release signing requests require manual review and approval by an Approver in SignPath after verifying build provenance against the corresponding GitHub release tag and CI run.

## Privacy Policy

This program will not transfer any information to other networked systems unless specifically requested by the user or the person installing or operating it.

- **Audio Privacy:** MicMute only toggles the Windows WASAPI capture endpoint mute state. It never records, captures, inspects, or transmits microphone audio.
- **Zero Telemetry:** MicMute collects no analytics, crash telemetry, or usage data.
- **Manual Update Check:** Network requests are only performed when the user explicitly clicks **Check for Updates...**, querying the public GitHub Releases API (`https://api.github.com/repos/youssefhazem3-dot/MicMute/releases/latest`) and downloading the user-confirmed release package verified against `SHA256SUMS.txt`.
