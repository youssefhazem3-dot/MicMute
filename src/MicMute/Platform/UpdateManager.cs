using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MicMute;

public enum UpdateCheckStatus
{
    UpToDate,
    UpdateAvailable,
    NoReleases,
    Failed
}

public enum UpdatePhase
{
    Idle,
    Checking,
    UpToDate,
    Available,
    Downloading,
    Verifying,
    Extracting,
    Installing,
    Failed
}

public sealed record UpdateInfo(
    Version CurrentVersion,
    Version LatestVersion,
    string TagName,
    string AssetName,
    string DownloadUrl,
    string? ChecksumsUrl,
    long AssetSizeBytes,
    string ReleasePageUrl);

public sealed record UpdateCheckResult(
    UpdateCheckStatus Status,
    Version CurrentVersion,
    UpdateInfo? Update,
    string Message);

public sealed record UpdateProgress(
    UpdatePhase Phase,
    double Percent,
    long BytesReceived,
    long TotalBytes,
    string StatusText);

public static class UpdateManager
{
    public const string RepositoryOwner = "youssefhazem3-dot";
    public const string RepositoryName = "MicMute";
    public const string LatestReleaseApiUrl = $"https://api.github.com/repos/{RepositoryOwner}/{RepositoryName}/releases/latest";
    public const string ReleasesPageUrl = $"https://github.com/{RepositoryOwner}/{RepositoryName}/releases/latest";
    private const long StandaloneSizeThresholdBytes = 15L * 1024L * 1024L;

    private static readonly HttpClient SharedHttpClient = CreateHttpClient();

    private static HttpClient CreateHttpClient()
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };
        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMinutes(3)
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"MicMute/{GetCurrentVersionDisplay()}");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }

    public static Version GetCurrentVersion()
    {
        Version? raw = typeof(UpdateManager).Assembly.GetName().Version;
        if (raw == null) return new Version(1, 0, 0);
        return new Version(Math.Max(0, raw.Major), Math.Max(0, raw.Minor), Math.Max(0, raw.Build));
    }

    public static string GetCurrentVersionDisplay()
    {
        Version v = GetCurrentVersion();
        return $"{v.Major}.{v.Minor}.{v.Build}";
    }

    public static bool TryParseReleaseVersion(string? tag, out Version version)
    {
        version = new Version(0, 0, 0);
        if (string.IsNullOrWhiteSpace(tag)) return false;

        string cleaned = tag.Trim().TrimStart('v', 'V');
        int dashIdx = cleaned.IndexOf('-');
        if (dashIdx >= 0) cleaned = cleaned.Substring(0, dashIdx);
        int plusIdx = cleaned.IndexOf('+');
        if (plusIdx >= 0) cleaned = cleaned.Substring(0, plusIdx);

        if (!Version.TryParse(cleaned, out Version? parsed) || parsed == null)
        {
            return false;
        }

        version = new Version(
            Math.Max(0, parsed.Major),
            Math.Max(0, parsed.Minor),
            Math.Max(0, parsed.Build));
        return true;
    }

    public static bool IsStandaloneBuild(string? exePath = null)
    {
        try
        {
            string path = exePath ?? AdminManager.GetExecutablePath();
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                return new FileInfo(path).Length >= StandaloneSizeThresholdBytes;
            }
        }
        catch
        {
        }
        return false;
    }

    public static bool CanSelfUpdate(string? exePath = null)
    {
        try
        {
            string path = exePath ?? AdminManager.GetExecutablePath();
            if (string.IsNullOrWhiteSpace(path)) return false;
            if (!path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) return false;
            string fileName = Path.GetFileName(path);
            if (fileName.Equals("dotnet.exe", StringComparison.OrdinalIgnoreCase)) return false;
            if (fileName.Contains("testhost", StringComparison.OrdinalIgnoreCase)) return false;
            if (fileName.Contains("MicMute.Tests", StringComparison.OrdinalIgnoreCase)) return false;
            return File.Exists(path);
        }
        catch
        {
            return false;
        }
    }

    public static UpdateCheckResult ParseReleaseJson(string json, Version currentVersion, bool preferStandalone)
    {
        string localDisplay = $"{currentVersion.Major}.{currentVersion.Minor}.{currentVersion.Build}";
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;

        string tag = root.TryGetProperty("tag_name", out JsonElement tagEl) && tagEl.ValueKind == JsonValueKind.String
            ? (tagEl.GetString() ?? string.Empty).Trim()
            : string.Empty;

        string releasePageUrl = root.TryGetProperty("html_url", out JsonElement htmlEl) && htmlEl.ValueKind == JsonValueKind.String
            ? (htmlEl.GetString() ?? ReleasesPageUrl)
            : ReleasesPageUrl;

        if (!TryParseReleaseVersion(tag, out Version remoteVersion))
        {
            return new UpdateCheckResult(UpdateCheckStatus.Failed, currentVersion, null, "Latest release tag is invalid.");
        }

        if (remoteVersion <= currentVersion)
        {
            return new UpdateCheckResult(UpdateCheckStatus.UpToDate, currentVersion, null, $"MicMute v{localDisplay} is up to date.");
        }

        string preferredZip = preferStandalone ? "MicMute-win-x64-standalone.zip" : "MicMute-win-x64.zip";
        string fallbackZip = preferStandalone ? "MicMute-win-x64.zip" : "MicMute-win-x64-standalone.zip";

        string? preferredUrl = null;
        long preferredSize = 0;
        string? fallbackUrl = null;
        long fallbackSize = 0;
        string? directExeUrl = null;
        long directExeSize = 0;
        string? directExeName = null;
        string? checksumsUrl = null;

        if (root.TryGetProperty("assets", out JsonElement assetsEl) && assetsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement asset in assetsEl.EnumerateArray())
            {
                string name = asset.TryGetProperty("name", out JsonElement nameEl) && nameEl.ValueKind == JsonValueKind.String
                    ? (nameEl.GetString() ?? string.Empty)
                    : string.Empty;
                string url = asset.TryGetProperty("browser_download_url", out JsonElement urlEl) && urlEl.ValueKind == JsonValueKind.String
                    ? (urlEl.GetString() ?? string.Empty)
                    : string.Empty;
                long size = asset.TryGetProperty("size", out JsonElement sizeEl) && sizeEl.ValueKind == JsonValueKind.Number
                    ? sizeEl.GetInt64()
                    : 0;

                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(url)) continue;

                if (name.Equals(preferredZip, StringComparison.OrdinalIgnoreCase))
                {
                    preferredUrl = url;
                    preferredSize = size;
                }
                else if (name.Equals(fallbackZip, StringComparison.OrdinalIgnoreCase))
                {
                    fallbackUrl = url;
                    fallbackSize = size;
                }
                else if (name.Equals("MicMute.exe", StringComparison.OrdinalIgnoreCase))
                {
                    directExeUrl = url;
                    directExeSize = size;
                    directExeName = name;
                }
                else if (name.Equals("SHA256SUMS.txt", StringComparison.OrdinalIgnoreCase))
                {
                    checksumsUrl = url;
                }
            }
        }

        string? chosenName = null;
        string? chosenUrl = null;
        long chosenSize = 0;

        if (!string.IsNullOrWhiteSpace(preferredUrl))
        {
            chosenName = preferredZip;
            chosenUrl = preferredUrl;
            chosenSize = preferredSize;
        }
        else if (!string.IsNullOrWhiteSpace(fallbackUrl))
        {
            chosenName = fallbackZip;
            chosenUrl = fallbackUrl;
            chosenSize = fallbackSize;
        }
        else if (!string.IsNullOrWhiteSpace(directExeUrl))
        {
            chosenName = directExeName ?? "MicMute.exe";
            chosenUrl = directExeUrl;
            chosenSize = directExeSize;
        }

        string remoteDisplay = $"{remoteVersion.Major}.{remoteVersion.Minor}.{remoteVersion.Build}";
        if (string.IsNullOrWhiteSpace(chosenUrl) || string.IsNullOrWhiteSpace(chosenName))
        {
            return new UpdateCheckResult(
                UpdateCheckStatus.Failed,
                currentVersion,
                null,
                $"Update v{remoteDisplay} was found, but no Windows x64 package was attached.");
        }

        var info = new UpdateInfo(
            CurrentVersion: currentVersion,
            LatestVersion: remoteVersion,
            TagName: tag,
            AssetName: chosenName,
            DownloadUrl: chosenUrl,
            ChecksumsUrl: checksumsUrl,
            AssetSizeBytes: chosenSize,
            ReleasePageUrl: releasePageUrl);

        return new UpdateCheckResult(
            UpdateCheckStatus.UpdateAvailable,
            currentVersion,
            info,
            $"Update available: v{remoteDisplay}");
    }

    public static string? ParseSha256Sum(string? checksumsText, string assetName)
    {
        if (string.IsNullOrWhiteSpace(checksumsText) || string.IsNullOrWhiteSpace(assetName))
        {
            return null;
        }

        string[] lines = checksumsText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();
            if (line.Length < 66) continue;
            string[] parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) continue;

            string hash = parts[0].Trim();
            string file = parts[^1].Trim().TrimStart('*');
            if (hash.Length == 64 && IsHexString(hash) &&
                (file.Equals(assetName, StringComparison.OrdinalIgnoreCase) ||
                 Path.GetFileName(file).Equals(assetName, StringComparison.OrdinalIgnoreCase)))
            {
                return hash.ToLowerInvariant();
            }
        }
        return null;
    }

    private static bool IsHexString(string value)
    {
        foreach (char c in value)
        {
            bool isHex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
            if (!isHex) return false;
        }
        return true;
    }

    public static string ComputeFileSha256(string filePath)
    {
        using FileStream fs = File.OpenRead(filePath);
        byte[] hash = SHA256.HashData(fs);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static async Task<UpdateCheckResult> CheckForUpdateAsync(
        HttpClient? client = null,
        CancellationToken cancellationToken = default)
    {
        Version currentVersion = GetCurrentVersion();
        string localDisplay = GetCurrentVersionDisplay();
        HttpClient http = client ?? SharedHttpClient;

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(10));

            using HttpResponseMessage response = await http.GetAsync(LatestReleaseApiUrl, timeoutCts.Token).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new UpdateCheckResult(UpdateCheckStatus.NoReleases, currentVersion, null, $"MicMute v{localDisplay} is up to date.");
            }
            if (!response.IsSuccessStatusCode)
            {
                return new UpdateCheckResult(
                    UpdateCheckStatus.Failed,
                    currentVersion,
                    null,
                    $"Could not check updates (GitHub returned {(int)response.StatusCode}).");
            }

            string json = await response.Content.ReadAsStringAsync(timeoutCts.Token).ConfigureAwait(false);
            return ParseReleaseJson(json, currentVersion, IsStandaloneBuild());
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new UpdateCheckResult(UpdateCheckStatus.Failed, currentVersion, null, "Update check timed out.");
        }
        catch (Exception ex)
        {
            DiagnosticLogger.LogWarning("Update check failed: " + ex.Message);
            return new UpdateCheckResult(UpdateCheckStatus.Failed, currentVersion, null, "Could not check updates (offline or unreachable).");
        }
    }

    public static async Task<string> DownloadAndStageUpdateAsync(
        UpdateInfo update,
        IProgress<UpdateProgress>? progress = null,
        HttpClient? client = null,
        CancellationToken cancellationToken = default)
    {
        HttpClient http = client ?? SharedHttpClient;
        string remoteDisplay = $"{update.LatestVersion.Major}.{update.LatestVersion.Minor}.{update.LatestVersion.Build}";
        string stageRoot = Path.Combine(
            Path.GetTempPath(),
            $"MicMute-Update-{remoteDisplay}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(stageRoot);

        try
        {
            string downloadedAssetPath = Path.Combine(stageRoot, Path.GetFileName(update.AssetName));
            progress?.Report(new UpdateProgress(UpdatePhase.Downloading, 0, 0, update.AssetSizeBytes, $"Downloading v{remoteDisplay}..."));

            using (HttpResponseMessage response = await http.GetAsync(
                update.DownloadUrl,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                long totalBytes = response.Content.Headers.ContentLength ?? update.AssetSizeBytes;
                long received = 0;

                await using Stream netStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                await using FileStream fileStream = new FileStream(
                    downloadedAssetPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 32768,
                    useAsync: true);

                byte[] buffer = new byte[32768];
                long lastReportTicks = 0;
                int bytesRead;
                while ((bytesRead = await netStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                    received += bytesRead;

                    long nowTicks = Environment.TickCount64;
                    if (nowTicks - lastReportTicks >= 50 || (totalBytes > 0 && received >= totalBytes))
                    {
                        lastReportTicks = nowTicks;
                        double pct = totalBytes > 0 ? Math.Clamp((received * 100.0) / totalBytes, 0.0, 100.0) : -1.0;
                        string detail = totalBytes > 0
                            ? $"Downloading v{remoteDisplay} ({Math.Round(pct)}%)"
                            : $"Downloading v{remoteDisplay} ({FormatSize(received)})";
                        progress?.Report(new UpdateProgress(UpdatePhase.Downloading, pct, received, totalBytes, detail));
                    }
                }
            }

            // Verify SHA-256 checksum if SHA256SUMS.txt is published with the release
            if (!string.IsNullOrWhiteSpace(update.ChecksumsUrl))
            {
                progress?.Report(new UpdateProgress(UpdatePhase.Verifying, -1, 0, 0, $"Verifying v{remoteDisplay}..."));
                using HttpResponseMessage sumResponse = await http.GetAsync(update.ChecksumsUrl, cancellationToken).ConfigureAwait(false);
                if (sumResponse.IsSuccessStatusCode)
                {
                    string sumsText = await sumResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    string? expectedHash = ParseSha256Sum(sumsText, update.AssetName);
                    if (!string.IsNullOrWhiteSpace(expectedHash))
                    {
                        string actualHash = ComputeFileSha256(downloadedAssetPath);
                        if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidDataException("Downloaded update failed SHA-256 integrity verification.");
                        }
                    }
                }
            }

            // Extract MicMute.exe from ZIP or validate direct executable
            progress?.Report(new UpdateProgress(UpdatePhase.Extracting, -1, 0, 0, $"Preparing v{remoteDisplay}..."));
            string stagedExePath = downloadedAssetPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
                ? StageFromZipFile(downloadedAssetPath, stageRoot)
                : downloadedAssetPath;

            ValidateWindowsExecutable(stagedExePath);
            return stagedExePath;
        }
        catch
        {
            TryDeleteDirectoryQuietly(stageRoot);
            throw;
        }
    }

    public static string StageFromZipFile(string zipPath, string extractDir)
    {
        Directory.CreateDirectory(extractDir);
        using ZipArchive archive = ZipFile.OpenRead(zipPath);
        ZipArchiveEntry? exeEntry = null;
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            if (entry.Name.Equals("MicMute.exe", StringComparison.OrdinalIgnoreCase))
            {
                exeEntry = entry;
                break;
            }
        }

        if (exeEntry == null)
        {
            throw new InvalidDataException("Update package does not contain MicMute.exe.");
        }

        string destPath = Path.Combine(extractDir, "MicMute.exe");
        exeEntry.ExtractToFile(destPath, overwrite: true);
        return destPath;
    }

    private static void ValidateWindowsExecutable(string exePath)
    {
        var info = new FileInfo(exePath);
        if (!info.Exists || info.Length < 64 * 1024)
        {
            throw new InvalidDataException("Staged update executable is missing or unexpectedly small.");
        }

        byte[] header = new byte[2];
        using (FileStream fs = File.OpenRead(exePath))
        {
            if (fs.Read(header, 0, 2) != 2 || header[0] != (byte)'M' || header[1] != (byte)'Z')
            {
                throw new InvalidDataException("Staged update is not a valid Windows executable.");
            }
        }
    }

    public static bool CanWriteToDirectory(string directoryPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(directoryPath)) return false;
            string probe = Path.Combine(directoryPath, ".micmute-update-probe-" + Guid.NewGuid().ToString("N"));
            using (File.Create(probe)) { }
            File.Delete(probe);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool LaunchStagedUpdater(
        string stagedExePath,
        string targetExePath,
        bool showWindow,
        string? previousVersion = null)
    {
        if (string.IsNullOrWhiteSpace(stagedExePath) || !File.Exists(stagedExePath)) return false;
        if (string.IsNullOrWhiteSpace(targetExePath)) return false;

        string targetDir = Path.GetDirectoryName(targetExePath) ?? string.Empty;
        bool needsElevation = !AdminManager.IsRunningAsAdmin() && !CanWriteToDirectory(targetDir);

        string args = BuildApplyUpdateArguments(
            targetExePath,
            Environment.ProcessId,
            showWindow,
            previousVersion);

        var startInfo = new ProcessStartInfo
        {
            FileName = stagedExePath,
            Arguments = args,
            UseShellExecute = needsElevation,
            WorkingDirectory = Path.GetDirectoryName(stagedExePath) ?? Path.GetTempPath()
        };

        if (needsElevation)
        {
            startInfo.Verb = "runas";
        }
        else
        {
            startInfo.CreateNoWindow = true;
        }

        using Process? helper = Process.Start(startInfo);
        return helper != null;
    }

    public static string BuildApplyUpdateArguments(
        string targetExePath,
        int parentProcessId,
        bool showWindow,
        string? previousVersion = null)
    {
        string escapedTarget = targetExePath.Replace("\"", "\\\"");
        string showFlag = showWindow ? "--show" : "--minimized";
        string baseArgs = $"--apply-update \"{escapedTarget}\" --wait-for-parent {parentProcessId.ToString(CultureInfo.InvariantCulture)} {showFlag}";
        if (!string.IsNullOrWhiteSpace(previousVersion))
        {
            baseArgs += $" --updated-from \"{previousVersion.Trim()}\"";
        }
        return baseArgs;
    }

    /// <summary>
    /// Handles self-update helper mode when launched with --apply-update &lt;targetExe&gt;.
    /// Returns true when the current process acted as the updater helper and should exit immediately.
    /// </summary>
    public static bool TryHandleUpdaterArguments(string[]? args)
    {
        if (!TryGetArgumentValue(args, "--apply-update", out string targetExePath) || string.IsNullOrWhiteSpace(targetExePath))
        {
            return false;
        }

        if (UiBehavior.TryGetParentProcessId(args, out int parentId))
        {
            UiBehavior.WaitForParentExit(parentId, TimeSpan.FromSeconds(30));
        }

        string currentStagedExe = AdminManager.GetExecutablePath();
        bool showWindow = !UiBehavior.HasArgument(args, "--minimized");
        TryGetArgumentValue(args, "--updated-from", out string previousVersion);

        bool replaced = ReplaceExecutableWithRetry(currentStagedExe, targetExePath);
        string stagedDir = Path.GetDirectoryName(currentStagedExe) ?? string.Empty;

        try
        {
            string launchArgs = showWindow ? "--show" : "--minimized";
            if (!string.IsNullOrWhiteSpace(stagedDir))
            {
                launchArgs += $" --cleanup-update \"{stagedDir.Replace("\"", "\\\"")}\"";
            }
            if (replaced)
            {
                launchArgs += $" --updated-from \"{(!string.IsNullOrWhiteSpace(previousVersion) ? previousVersion : "previous")}\"";
            }

            using Process? relaunched = Process.Start(new ProcessStartInfo
            {
                FileName = targetExePath,
                Arguments = launchArgs,
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(targetExePath) ?? string.Empty
            });
        }
        catch (Exception ex)
        {
            DiagnosticLogger.LogError("Failed to relaunch updated MicMute executable", ex);
        }

        return true;
    }

    public static bool ReplaceExecutableWithRetry(string sourceExePath, string targetExePath, int maxAttempts = 25, int retryDelayMs = 200)
    {
        if (!File.Exists(sourceExePath)) return false;

        string targetDir = Path.GetDirectoryName(targetExePath) ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        string backupPath = targetExePath + ".update.bak";
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                if (File.Exists(backupPath))
                {
                    try { File.Delete(backupPath); } catch { }
                }

                bool hadExisting = File.Exists(targetExePath);
                if (hadExisting)
                {
                    File.Move(targetExePath, backupPath, overwrite: true);
                }

                try
                {
                    File.Copy(sourceExePath, targetExePath, overwrite: true);
                }
                catch
                {
                    // Restore original executable if copy failed mid-way
                    if (hadExisting && File.Exists(backupPath) && !File.Exists(targetExePath))
                    {
                        try { File.Move(backupPath, targetExePath, overwrite: true); } catch { }
                    }
                    throw;
                }

                if (File.Exists(backupPath))
                {
                    try { File.Delete(backupPath); } catch { }
                }
                return true;
            }
            catch (IOException)
            {
                Thread.Sleep(retryDelayMs);
            }
            catch (UnauthorizedAccessException)
            {
                Thread.Sleep(retryDelayMs);
            }
        }

        return false;
    }

    public static bool TryGetCleanupDirectory(string[]? args, out string directory)
        => TryGetArgumentValue(args, "--cleanup-update", out directory);

    public static bool TryGetUpdatedFromVersion(string[]? args, out string version)
        => TryGetArgumentValue(args, "--updated-from", out version);

    public static void ScheduleCleanupDirectory(string? directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath)) return;

        // Safety guard: only ever clean directories inside Temp starting with MicMute-Update-
        string tempRoot = Path.GetFullPath(Path.GetTempPath());
        string fullDir;
        try { fullDir = Path.GetFullPath(directoryPath); }
        catch { return; }

        string dirName = Path.GetFileName(fullDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (!fullDir.StartsWith(tempRoot, StringComparison.OrdinalIgnoreCase) ||
            !dirName.StartsWith("MicMute-Update-", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            for (int attempt = 0; attempt < 15; attempt++)
            {
                try
                {
                    if (!Directory.Exists(fullDir)) return;
                    Directory.Delete(fullDir, recursive: true);
                    return;
                }
                catch
                {
                    await Task.Delay(300).ConfigureAwait(false);
                }
            }
        });
    }

    private static bool TryGetArgumentValue(string[]? args, string flag, out string value)
    {
        value = string.Empty;
        if (args == null) return false;
        for (int i = 0; i + 1 < args.Length; i++)
        {
            if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase))
            {
                value = args[i + 1];
                return true;
            }
        }
        return false;
    }

    private static void TryDeleteDirectoryQuietly(string dir)
    {
        try
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
        catch
        {
        }
    }

    private static string FormatSize(long bytes)
    {
        if (bytes >= 1024L * 1024L)
        {
            return $"{bytes / (1024.0 * 1024.0):F1} MB";
        }
        if (bytes >= 1024L)
        {
            return $"{bytes / 1024.0:F0} KB";
        }
        return $"{bytes} B";
    }
}
