using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace MicMute;

/// <summary>Thread-safe settings cache and isolated persistence implementation.</summary>
public sealed class SettingsStore : IDisposable
{
    private const int SaveDelayMilliseconds = 200;
    private readonly object _sync = new();
    private readonly string _defaultFolder;
    private readonly string _appDirectory;
    private readonly string _locationPointerFile;
    private readonly Timer _saveTimer;
    private AppSettings? _cachedSettings;
    private PendingSave? _pendingSave;
    private Exception? _lastSaveFailure;
    private bool _disposed;
    private string? _activeFolder;

    public SettingsStore(string defaultAppDataFolder, string appDirectory)
    {
        if (string.IsNullOrWhiteSpace(defaultAppDataFolder)) throw new ArgumentException("A default settings directory is required.", nameof(defaultAppDataFolder));
        if (string.IsNullOrWhiteSpace(appDirectory)) throw new ArgumentException("An application directory is required.", nameof(appDirectory));
        _defaultFolder = Path.GetFullPath(defaultAppDataFolder);
        _appDirectory = Path.GetFullPath(appDirectory);
        _locationPointerFile = Path.Combine(_defaultFolder, "location.txt");
        _saveTimer = new Timer(OnSaveTimer, null, Timeout.Infinite, Timeout.Infinite);
    }

    public event EventHandler<string>? SaveFailed;

    public string GetDataFolderPath()
    {
        lock (_sync) return GetDataFolderPathNoLock();
    }

    public string GetSettingsFilePath()
    {
        lock (_sync) return GetSettingsFilePathNoLock();
    }

    public AppSettings Load()
    {
        lock (_sync)
        {
            ThrowIfDisposed();
            return LoadNoLock();
        }
    }

    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        lock (_sync)
        {
            ThrowIfDisposed();
            AppSettings normalized = SettingsCodec.Normalize(settings);
            _cachedSettings = normalized;
            _pendingSave = new PendingSave(GetSettingsFilePathNoLock(), normalized);
            _lastSaveFailure = null;
            _saveTimer.Change(SaveDelayMilliseconds, Timeout.Infinite);
        }
    }

    public void Flush()
    {
        Exception? failure;
        string? message;
        lock (_sync)
        {
            ThrowIfDisposed();
            _saveTimer.Change(Timeout.Infinite, Timeout.Infinite);
            failure = PersistPendingNoLock(out message);
        }
        RaiseSaveFailure(message);
        if (failure is not null) throw failure;
    }

    public void SetCustomDataFolder(string newPath)
    {
        string destination = string.IsNullOrWhiteSpace(newPath) ? _defaultFolder : Path.GetFullPath(newPath);
        lock (_sync)
        {
            ThrowIfDisposed();
            PersistBeforeLocationChangeNoLock();
            bool isDefault = PathsEqual(destination, _defaultFolder);
            AppSettings moved = LoadNoLock() with { CustomDataPath = isDefault ? string.Empty : destination, UsePortableMode = false };
            MoveSettingsNoLock(destination, moved, isDefault ? false : true, removePortableArtifacts: true, createPortableFlag: false);
        }
    }

    public void SetPortableMode(bool enable)
    {
        lock (_sync)
        {
            ThrowIfDisposed();
            PersistBeforeLocationChangeNoLock();
            AppSettings current = LoadNoLock();
            if (enable)
            {
                MoveSettingsNoLock(_appDirectory, current with { UsePortableMode = true }, null, removePortableArtifacts: false, createPortableFlag: true);
                return;
            }

            string destination = GetConfiguredCustomFolderNoLock() ?? _defaultFolder;
            MoveSettingsNoLock(destination, current with { UsePortableMode = false }, null, removePortableArtifacts: true, createPortableFlag: false);
        }
    }

    public void ResetAllSettings()
    {
        lock (_sync)
        {
            ThrowIfDisposed();
            string settingsFile = GetSettingsFilePathNoLock();
            AppSettings defaults = new();
            AtomicWrite(settingsFile, SettingsCodec.Serialize(defaults));
            _saveTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _pendingSave = null;
            _lastSaveFailure = null;
            _cachedSettings = defaults;
        }
    }

    public void OpenDataFolderInExplorer()
    {
        string folder = GetDataFolderPath();
        Directory.CreateDirectory(folder);
        Process.Start(new ProcessStartInfo { FileName = folder, UseShellExecute = true });
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed) return;
            _disposed = true;
            _saveTimer.Dispose();
        }
    }

    private AppSettings LoadNoLock()
    {
        if (_cachedSettings is not null) return _cachedSettings;
        string path = GetSettingsFilePathNoLock();
        if (File.Exists(path))
        {
            try
            {
                _cachedSettings = SettingsCodec.Deserialize(File.ReadAllText(path));
                return _cachedSettings;
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        _cachedSettings = new AppSettings();
        return _cachedSettings;
    }

    private void PersistBeforeLocationChangeNoLock()
    {
        _saveTimer.Change(Timeout.Infinite, Timeout.Infinite);
        Exception? failure = PersistPendingNoLock(out string? message);
        RaiseSaveFailure(message);
        if (failure is not null) throw failure;
    }

    private void MoveSettingsNoLock(string destinationFolder, AppSettings settings, bool? writeLocationPointer, bool removePortableArtifacts, bool createPortableFlag)
    {
        string destinationSettings = Path.Combine(destinationFolder, "settings.json");
        string sourceSettings = Path.Combine(_appDirectory, "settings.json");
        string portableFlag = Path.Combine(_appDirectory, "portable.flag");
        FileSnapshot destinationBefore = FileSnapshot.Capture(destinationSettings);
        FileSnapshot sourceBefore = FileSnapshot.Capture(sourceSettings);
        FileSnapshot pointerBefore = FileSnapshot.Capture(_locationPointerFile);
        bool flagBefore = File.Exists(portableFlag);

        try
        {
            AtomicWrite(destinationSettings, SettingsCodec.Serialize(settings));
            if (writeLocationPointer == true) AtomicWrite(_locationPointerFile, destinationFolder);
            else if (writeLocationPointer == false && File.Exists(_locationPointerFile)) File.Delete(_locationPointerFile);

            if (removePortableArtifacts)
            {
                if (flagBefore) File.Delete(portableFlag);
                if (!PathsEqual(destinationSettings, sourceSettings) && File.Exists(sourceSettings)) File.Delete(sourceSettings);
            }
            else if (createPortableFlag && !flagBefore)
            {
                Directory.CreateDirectory(_appDirectory);
                File.WriteAllText(portableFlag, "1", Utf8NoBom);
            }

            _cachedSettings = settings;
            _activeFolder = destinationFolder;
            _pendingSave = null;
            _lastSaveFailure = null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            RestoreSnapshot(destinationSettings, destinationBefore);
            RestoreSnapshot(sourceSettings, sourceBefore);
            RestoreSnapshot(_locationPointerFile, pointerBefore);
            RestorePortableFlag(portableFlag, flagBefore);
            throw ToIOException("Could not change the settings storage location.", exception);
        }
    }

    private string GetDataFolderPathNoLock()
    {
        if (_activeFolder is not null) return _activeFolder;
        string portableSettings = Path.Combine(_appDirectory, "settings.json");
        _activeFolder = File.Exists(Path.Combine(_appDirectory, "portable.flag")) || File.Exists(portableSettings)
            ? _appDirectory : GetConfiguredCustomFolderNoLock() ?? _defaultFolder;
        return _activeFolder;
    }

    private string? GetConfiguredCustomFolderNoLock()
    {
        try
        {
            if (!File.Exists(_locationPointerFile)) return null;
            string configured = File.ReadAllText(_locationPointerFile).Trim();
            return !string.IsNullOrWhiteSpace(configured) && Directory.Exists(configured) ? Path.GetFullPath(configured) : null;
        }
        catch (IOException) { return null; }
        catch (UnauthorizedAccessException) { return null; }
    }

    private string GetSettingsFilePathNoLock() => Path.Combine(GetDataFolderPathNoLock(), "settings.json");

    private Exception? PersistPendingNoLock(out string? message)
    {
        message = null;
        if (_pendingSave is null) return _lastSaveFailure;
        PendingSave pending = _pendingSave;
        try
        {
            AtomicWrite(pending.FilePath, SettingsCodec.Serialize(pending.Settings));
            _pendingSave = null;
            _lastSaveFailure = null;
            return null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            Exception failure = ToIOException("Could not save settings.", exception);
            _lastSaveFailure = failure;
            message = failure.Message;
            return failure;
        }
    }

    private void OnSaveTimer(object? _)
    {
        string? message;
        lock (_sync)
        {
            if (_disposed) return;
            PersistPendingNoLock(out message);
        }
        RaiseSaveFailure(message);
    }

    private void RaiseSaveFailure(string? message)
    {
        if (message is not null) SaveFailed?.Invoke(this, message);
    }

    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private static void AtomicWrite(string path, string contents)
    {
        string? directory = Path.GetDirectoryName(path);
        if (string.IsNullOrWhiteSpace(directory)) throw new IOException("The settings file has no parent directory.");
        Directory.CreateDirectory(directory);
        string temporary = Path.Combine(directory, "." + Path.GetFileName(path) + "." + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            File.WriteAllText(temporary, contents, Utf8NoBom);
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private static void RestoreSnapshot(string path, FileSnapshot snapshot)
    {
        try
        {
            if (snapshot.Exists) AtomicWrite(path, snapshot.Contents!);
            else if (File.Exists(path)) File.Delete(path);
        }
        catch { }
    }

    private static void RestorePortableFlag(string path, bool shouldExist)
    {
        try
        {
            if (shouldExist && !File.Exists(path))
            {
                string? directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
                File.WriteAllText(path, "1", Utf8NoBom);
            }
            else if (!shouldExist && File.Exists(path)) File.Delete(path);
        }
        catch { }
    }

    private static IOException ToIOException(string message, Exception exception) =>
        exception as IOException ?? new IOException(message, exception);

    private static bool PathsEqual(string first, string second) => string.Equals(
        Path.GetFullPath(first).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
        Path.GetFullPath(second).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
        StringComparison.OrdinalIgnoreCase);

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(SettingsStore));
    }

    private sealed record PendingSave(string FilePath, AppSettings Settings);
    private sealed record FileSnapshot(bool Exists, string? Contents)
    {
        public static FileSnapshot Capture(string path) => File.Exists(path)
            ? new FileSnapshot(true, File.ReadAllText(path))
            : new FileSnapshot(false, null);
    }
}
