using System;
using System.IO;

namespace MicMute;

/// <summary>Application-wide settings store; tests use independent SettingsStore instances.</summary>
public static class SettingsManager
{
    private static SettingsStore _store = new(
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MicMute"),
        AppContext.BaseDirectory);

    internal static SettingsStore ReplaceStoreForTesting(SettingsStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        SettingsStore previous = _store;
        _store = store;
        return previous;
    }

    public static event EventHandler<string>? SaveFailed
    {
        add => _store.SaveFailed += value;
        remove => _store.SaveFailed -= value;
    }

    public static AppSettings Load() => _store.Load();
    public static void Save(AppSettings settings) => _store.Save(settings);
    public static void Flush() => _store.Flush();
    public static void Dispose() => _store.Dispose();
    public static string GetDataFolderPath() => _store.GetDataFolderPath();
    public static string GetSettingsFilePath() => _store.GetSettingsFilePath();
    public static void SetCustomDataFolder(string path) => _store.SetCustomDataFolder(path);
    public static void ResetAllSettings() => _store.ResetAllSettings();
    public static void OpenDataFolderInExplorer() => _store.OpenDataFolderInExplorer();
}
