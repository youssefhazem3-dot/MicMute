using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;

namespace MicMute;

public static class SettingsManager
{
    private static readonly string DefaultAppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MicMute");
    private static readonly string AppDirectory = AppDomain.CurrentDomain.BaseDirectory;
    private static readonly string LocationPointerFile = Path.Combine(DefaultAppDataFolder, "location.txt");

    private static string Escape(string s) => s?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";
    private static string Unescape(string s) => s?.Replace("\\\"", "\"").Replace("\\\\", "\\") ?? "";

    private static string Serialize(AppSettings s)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"  \"SelectedDeviceId\": \"{Escape(s.SelectedDeviceId)}\",");
        sb.AppendLine($"  \"Hotkey\": {(int)s.Hotkey},");
        sb.AppendLine($"  \"HotkeyModifiers\": {(int)s.HotkeyModifiers},");
        sb.AppendLine($"  \"RunOnStartup\": {(s.RunOnStartup ? "true" : "false")},");
        sb.AppendLine($"  \"StartMinimized\": {(s.StartMinimized ? "true" : "false")},");
        sb.AppendLine($"  \"EnableOsd\": {(s.EnableOsd ? "true" : "false")},");
        sb.AppendLine($"  \"OsdDuration\": {s.OsdDuration.ToString(System.Globalization.CultureInfo.InvariantCulture)},");
        sb.AppendLine($"  \"LightMode\": {(s.LightMode ? "true" : "false")},");
        sb.AppendLine($"  \"CustomDataPath\": \"{Escape(s.CustomDataPath)}\",");
        sb.AppendLine($"  \"UsePortableMode\": {(s.UsePortableMode ? "true" : "false")},");
        sb.AppendLine($"  \"RunAsAdmin\": {(s.RunAsAdmin ? "true" : "false")}");
        sb.Append("}");
        return sb.ToString();
    }

    private static AppSettings Deserialize(string json)
    {
        string selectedDeviceId = "";
        Key hotkey = Key.F1;
        ModifierKeys hotkeyModifiers = ModifierKeys.None;
        bool runOnStartup = false;
        bool startMinimized = true;
        bool enableOsd = true;
        double osdDuration = 1.5;
        bool lightMode = false;
        string customDataPath = "";
        bool usePortableMode = false;
        bool runAsAdmin = false;

        foreach (var rawLine in json.Split(new[] { '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = rawLine.Split(new[] { ':' }, 2);
            if (parts.Length != 2) continue;
            string key = parts[0].Trim(' ', '\t', '"', '{', '}');
            string val = parts[1].Trim(' ', '\t', '"', '{', '}');

            switch (key)
            {
                case "SelectedDeviceId": selectedDeviceId = Unescape(val); break;
                case "Hotkey":
                    if (int.TryParse(val, out int hk)) hotkey = (Key)hk;
                    else if (Enum.TryParse<Key>(val, true, out var ek)) hotkey = ek;
                    break;
                case "HotkeyModifiers":
                    if (int.TryParse(val, out int hm)) hotkeyModifiers = (ModifierKeys)hm;
                    else if (Enum.TryParse<ModifierKeys>(val, true, out var em)) hotkeyModifiers = em;
                    break;
                case "RunOnStartup": bool.TryParse(val, out runOnStartup); break;
                case "StartMinimized": bool.TryParse(val, out startMinimized); break;
                case "EnableOsd": bool.TryParse(val, out enableOsd); break;
                case "OsdDuration": double.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out osdDuration); break;
                case "LightMode": bool.TryParse(val, out lightMode); break;
                case "CustomDataPath": customDataPath = Unescape(val); break;
                case "UsePortableMode": bool.TryParse(val, out usePortableMode); break;
                case "RunAsAdmin": bool.TryParse(val, out runAsAdmin); break;
            }
        }

        return new AppSettings
        {
            SelectedDeviceId = selectedDeviceId,
            Hotkey = hotkey,
            HotkeyModifiers = hotkeyModifiers,
            RunOnStartup = runOnStartup,
            StartMinimized = startMinimized,
            EnableOsd = enableOsd,
            OsdDuration = osdDuration,
            LightMode = lightMode,
            CustomDataPath = customDataPath,
            UsePortableMode = usePortableMode,
            RunAsAdmin = runAsAdmin
        };
    }

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
                AppSettings? appSettings = Deserialize(File.ReadAllText(filePath));
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
            string contents = Serialize(settings);
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