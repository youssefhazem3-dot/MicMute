using System;
using System.IO;

namespace MicMute;

/// <summary>Application-wide settings store; tests use independent SettingsStore instances.</summary>
public static class SettingsManager
{
    private static readonly SettingsStore Store = new(
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MicMute"),
        AppContext.BaseDirectory);

    public static event EventHandler<string>? SaveFailed
    {
        add => Store.SaveFailed += value;
        remove => Store.SaveFailed -= value;
    }

    public static AppSettings Load() => Store.Load();
    public static void Save(AppSettings settings) => Store.Save(settings);
    public static void Flush() => Store.Flush();
    public static string GetDataFolderPath() => Store.GetDataFolderPath();
    public static string GetSettingsFilePath() => Store.GetSettingsFilePath();
    public static void SetCustomDataFolder(string path) => Store.SetCustomDataFolder(path);
    public static void SetPortableMode(bool enabled) => Store.SetPortableMode(enabled);
    public static void ResetAllSettings() => Store.ResetAllSettings();
    public static void OpenDataFolderInExplorer() => Store.OpenDataFolderInExplorer();
}
