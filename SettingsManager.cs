using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace MicMute;

public static class SettingsManager
{
    private static readonly string DefaultAppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MicMute");
    private static readonly string AppDirectory = AppDomain.CurrentDomain.BaseDirectory;
    private static readonly string LocationPointerFile = Path.Combine(DefaultAppDataFolder, "location.txt");

    private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
    {
        WriteIndented = true
    };

    public static string GetDataFolderPath()
    {
        try
        {
            // 1. Portable Mode check (if portable.flag or settings.json exists in app directory)
            string localSettings = Path.Combine(AppDirectory, "settings.json");
            string portableFlag = Path.Combine(AppDirectory, "portable.flag");
            if (File.Exists(portableFlag) || File.Exists(localSettings))
            {
                return AppDirectory;
            }

            // 2. Custom location pointer check
            if (File.Exists(LocationPointerFile))
            {
                string customPath = File.ReadAllText(LocationPointerFile).Trim();
                if (!string.IsNullOrEmpty(customPath) && Directory.Exists(customPath))
                {
                    return customPath;
                }
            }
        }
        catch
        {
        }

        return DefaultAppDataFolder;
    }

    public static string GetSettingsFilePath()
    {
        return Path.Combine(GetDataFolderPath(), "settings.json");
    }

    public static AppSettings Load()
    {
        try
        {
            string filePath = GetSettingsFilePath();
            if (File.Exists(filePath))
            {
                AppSettings? appSettings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(filePath));
                if (appSettings != null)
                {
                    return appSettings;
                }
            }
        }
        catch
        {
        }

        AppSettings defaultSettings = new AppSettings();
        Save(defaultSettings);
        return defaultSettings;
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            string folderPath = GetDataFolderPath();
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            string filePath = Path.Combine(folderPath, "settings.json");
            string contents = JsonSerializer.Serialize(settings, SerializerOptions);
            File.WriteAllText(filePath, contents);
        }
        catch
        {
        }
    }

    public static void SetCustomDataFolder(string newPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(newPath) || newPath.Equals(DefaultAppDataFolder, StringComparison.OrdinalIgnoreCase))
            {
                if (File.Exists(LocationPointerFile))
                {
                    File.Delete(LocationPointerFile);
                }
                return;
            }

            if (!Directory.Exists(newPath))
            {
                Directory.CreateDirectory(newPath);
            }

            // Copy existing settings to new path if not present
            string currentSettingsPath = GetSettingsFilePath();
            string newSettingsPath = Path.Combine(newPath, "settings.json");
            if (File.Exists(currentSettingsPath) && !File.Exists(newSettingsPath))
            {
                File.Copy(currentSettingsPath, newSettingsPath, overwrite: true);
            }

            if (!Directory.Exists(DefaultAppDataFolder))
            {
                Directory.CreateDirectory(DefaultAppDataFolder);
            }
            File.WriteAllText(LocationPointerFile, newPath);
        }
        catch
        {
        }
    }

    public static void SetPortableMode(bool enable)
    {
        try
        {
            string portableFlag = Path.Combine(AppDirectory, "portable.flag");
            if (enable)
            {
                File.WriteAllText(portableFlag, "1");
                Save(Load() with { UsePortableMode = true });
            }
            else
            {
                if (File.Exists(portableFlag))
                {
                    File.Delete(portableFlag);
                }
                Save(Load() with { UsePortableMode = false });
            }
        }
        catch
        {
        }
    }

    public static void ResetAllSettings()
    {
        try
        {
            string filePath = GetSettingsFilePath();
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            if (File.Exists(LocationPointerFile))
            {
                File.Delete(LocationPointerFile);
            }
        }
        catch
        {
        }
    }

    public static void OpenDataFolderInExplorer()
    {
        try
        {
            string folder = GetDataFolderPath();
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            Process.Start(new ProcessStartInfo
            {
                FileName = folder,
                UseShellExecute = true
            });
        }
        catch
        {
        }
    }
}