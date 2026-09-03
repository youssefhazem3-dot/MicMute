using System;
using System.IO;
using System.Threading;
using System.Windows.Input;

namespace MicMute.Tests;

public static class SettingsCases
{
    public static void Run(Action<string, Action> test)
    {
        test("loads compact JSON containing commas and escaped quotes", LoadsCompactEscapedJson);
        test("loads multiline JSON containing commas and escaped quotes", LoadsMultilineEscapedJson);
        test("repairs missing braces on a Windows capture endpoint ID", RepairsEndpointBraces);
        test("normalizes nonfinite and out-of-range OSD durations", NormalizesDurations);
        test("save updates memory immediately and flush persists the last coalesced value", SaveFlushesLatestValue);
        test("flush raises and reports persistence failures", FlushReportsPersistenceFailure);
        test("portable to custom migration carries current settings", MigratesPortableSettings);
        test("failed location migration leaves portable data active and intact", FailedMigrationPreservesSource);
        test("reset affects only the current settings file and reloads defaults", ResetLeavesUnrelatedFiles);
        test("portable reset preserves the active directory after restart", PortableResetKeepsDirectory);
        test("flush retries the last value after a transient filesystem failure", FlushRetriesFailure);
        test("invalid hotkey enum values normalize to a usable default", InvalidHotkeyUsesDefault);
        test("JSON string text is not rewritten by legacy NaN migration", NamedFloatInStringIsPreserved);
        test("background persistence eventually writes the latest cached value", BackgroundPersistence);
        test("pending saves cannot overwrite migrated or reset settings", PendingSavesRespectTransitions);
    }

    private static void LoadsCompactEscapedJson()
    {
        using var scope = new SettingsScope();
        scope.WriteSettings("{\"SelectedDeviceId\":\"device, \\\"quoted\\\"\\\\path\",\"Hotkey\":\"F9\",\"HotkeyModifiers\":\"Control, Shift\",\"OsdDuration\":2.25}");

        AppSettings settings = scope.Store.Load();

        Check.Equal("device, \"quoted\"\\path", settings.SelectedDeviceId);
        Check.Equal(Key.F9, settings.Hotkey);
        Check.Equal(ModifierKeys.Control | ModifierKeys.Shift, settings.HotkeyModifiers);
        Check.Equal(2.25, settings.OsdDuration);
    }

    private static void InvalidHotkeyUsesDefault()
    {
        AppSettings settings = SettingsCodec.Deserialize("{\"Hotkey\":-999,\"HotkeyModifiers\":999}");
        Check.Equal(Key.F1, settings.Hotkey);
        Check.Equal(ModifierKeys.None, settings.HotkeyModifiers);
    }

    private static void BackgroundPersistence()
    {
        using var scope = new SettingsScope();
        for (int i = 0; i < 50; i++) scope.Store.Save(new AppSettings { SelectedDeviceId = "value-" + i });
        Check.True(SpinWait.SpinUntil(() => File.Exists(scope.SettingsFile), 3000), "background save did not run");
        using var reopened = new SettingsStore(scope.DefaultDirectory, scope.AppDirectory);
        Check.Equal("value-49", reopened.Load().SelectedDeviceId);
    }

    private static void PendingSavesRespectTransitions()
    {
        using var scope = new SettingsScope();
        scope.Store.Save(new AppSettings { SelectedDeviceId = "latest-pending" });
        string custom = Path.Combine(scope.Root, "new-location");
        scope.Store.SetCustomDataFolder(custom);
        using (var moved = new SettingsStore(scope.DefaultDirectory, scope.AppDirectory))
            Check.Equal("latest-pending", moved.Load().SelectedDeviceId);
        scope.Store.Save(new AppSettings { SelectedDeviceId = "discard-on-reset" });
        scope.Store.ResetAllSettings();
        scope.Store.Flush();
        using var reset = new SettingsStore(scope.DefaultDirectory, scope.AppDirectory);
        Check.Equal(string.Empty, reset.Load().SelectedDeviceId);
    }

    private static void NamedFloatInStringIsPreserved()
    {
        const string value = "quoted \"OsdDuration\":NaN, still text";
        string json = System.Text.Json.JsonSerializer.Serialize(new AppSettings { SelectedDeviceId = value });
        Check.Equal(value, SettingsCodec.Deserialize(json).SelectedDeviceId);
    }

    private static void LoadsMultilineEscapedJson()
    {
        using var scope = new SettingsScope();
        scope.WriteSettings("""
            {
              "SelectedDeviceId": "C:\\Users\\me, \"Mic\"",
              "CustomDataPath": "D:\\Settings, Backup",
              "Hotkey": 94,
              "HotkeyModifiers": 3
            }
            """);

        AppSettings settings = scope.Store.Load();

        Check.Equal("C:\\Users\\me, \"Mic\"", settings.SelectedDeviceId);
        Check.Equal("D:\\Settings, Backup", settings.CustomDataPath);
        Check.Equal(Key.F5, settings.Hotkey);
        Check.Equal(ModifierKeys.Alt | ModifierKeys.Control, settings.HotkeyModifiers);
    }

    private static void RepairsEndpointBraces()
    {
        using var scope = new SettingsScope();
        scope.WriteSettings("{\"SelectedDeviceId\":\"0.0.1.00000000}.{7d749311-9f2f-4c5f-aec7-938f4af8ee4c\"}");

        AppSettings settings = scope.Store.Load();

        Check.Equal("{0.0.1.00000000}.{7d749311-9f2f-4c5f-aec7-938f4af8ee4c}", settings.SelectedDeviceId);
    }

    private static void NormalizesDurations()
    {
        using (var nanScope = new SettingsScope())
        {
            nanScope.WriteSettings("{\"OsdDuration\":\"NaN\"}");
            Check.Equal(1.5, nanScope.Store.Load().OsdDuration);
        }

        using (var legacyNanScope = new SettingsScope())
        {
            legacyNanScope.WriteSettings("{\"OsdDuration\":NaN}");
            Check.Equal(1.5, legacyNanScope.Store.Load().OsdDuration);
        }

        using (var infinityScope = new SettingsScope())
        {
            infinityScope.WriteSettings("{\"OsdDuration\":\"Infinity\"}");
            Check.Equal(1.5, infinityScope.Store.Load().OsdDuration);
        }

        using (var lowScope = new SettingsScope())
        {
            lowScope.WriteSettings("{\"OsdDuration\":0.01}");
            Check.Equal(0.1, lowScope.Store.Load().OsdDuration);
        }

        using var highScope = new SettingsScope();
        highScope.WriteSettings("{\"OsdDuration\":99}");
        Check.Equal(30.0, highScope.Store.Load().OsdDuration);
    }

    private static void SaveFlushesLatestValue()
    {
        using var scope = new SettingsScope();
        scope.Store.Save(new AppSettings { SelectedDeviceId = "first" });
        scope.Store.Save(new AppSettings { SelectedDeviceId = "second" });

        Check.Equal("second", scope.Store.Load().SelectedDeviceId, "Save must update the in-memory value immediately.");
        scope.Store.Flush();

        string persisted = File.ReadAllText(scope.SettingsFile);
        Check.True(persisted.Contains("\"second\"", StringComparison.Ordinal), "Flush must atomically persist the final coalesced value.");
    }

    private static void FlushReportsPersistenceFailure()
    {
        using var scope = new SettingsScope(createDefaultDirectory: false);
        File.WriteAllText(scope.DefaultDirectory, "blocks directory creation");
        string failedMessage = string.Empty;
        scope.Store.SaveFailed += (_, message) => failedMessage = message;
        scope.Store.Save(new AppSettings { SelectedDeviceId = "will fail" });

        Check.Throws<IOException>(() => scope.Store.Flush());
        Check.True(!string.IsNullOrWhiteSpace(failedMessage), "SaveFailed should report the failed persistence operation.");
    }

    private static void MigratesPortableSettings()
    {
        using var scope = new SettingsScope();
        File.WriteAllText(Path.Combine(scope.AppDirectory, "portable.flag"), "1");
        scope.WriteSettings("{\"SelectedDeviceId\":\"portable-device\",\"OsdDuration\":2}", scope.AppDirectory);
        string custom = Path.Combine(scope.Root, "custom");

        scope.Store.SetCustomDataFolder(custom);

        Check.Equal(Path.GetFullPath(custom), scope.Store.GetDataFolderPath());
        Check.True(!File.Exists(Path.Combine(scope.AppDirectory, "portable.flag")), "Migration must leave portable mode.");
        AppSettings reloaded = new SettingsStore(scope.DefaultDirectory, scope.AppDirectory).Load();
        Check.Equal("portable-device", reloaded.SelectedDeviceId);
        Check.Equal(2.0, reloaded.OsdDuration);
    }

    private static void FailedMigrationPreservesSource()
    {
        using var scope = new SettingsScope();
        File.WriteAllText(Path.Combine(scope.AppDirectory, "portable.flag"), "1");
        scope.WriteSettings("{\"SelectedDeviceId\":\"portable-device\"}", scope.AppDirectory);
        string blockedPath = Path.Combine(scope.Root, "blocked-target");
        File.WriteAllText(blockedPath, "not a directory");

        Check.Throws<IOException>(() => scope.Store.SetCustomDataFolder(blockedPath));

        Check.Equal(Path.GetFullPath(scope.AppDirectory), scope.Store.GetDataFolderPath());
        Check.True(File.Exists(Path.Combine(scope.AppDirectory, "portable.flag")));
        Check.True(File.ReadAllText(Path.Combine(scope.AppDirectory, "settings.json")).Contains("portable-device", StringComparison.Ordinal));
    }

    private static void ResetLeavesUnrelatedFiles()
    {
        using var scope = new SettingsScope();
        scope.Store.Save(new AppSettings { SelectedDeviceId = "device" });
        scope.Store.Flush();
        string unrelated = Path.Combine(scope.DefaultDirectory, "keep.txt");
        File.WriteAllText(unrelated, "keep");

        scope.Store.ResetAllSettings();

        Check.True(File.Exists(unrelated), "Reset must not delete files that are not settings.json.");
        Check.True(File.Exists(scope.SettingsFile), "Reset must persist defaults atomically in the current folder.");
        Check.Equal(string.Empty, scope.Store.Load().SelectedDeviceId);
    }

    private static void PortableResetKeepsDirectory()
    {
        using var scope = new SettingsScope();
        scope.WriteSettings("{\"SelectedDeviceId\":\"portable-device\"}", scope.AppDirectory);
        Check.Equal(scope.AppDirectory, scope.Store.GetDataFolderPath());
        scope.Store.ResetAllSettings();
        using var reopened = new SettingsStore(scope.DefaultDirectory, scope.AppDirectory);
        Check.Equal(scope.AppDirectory, reopened.GetDataFolderPath());
        Check.Equal(string.Empty, reopened.Load().SelectedDeviceId);
    }

    private static void FlushRetriesFailure()
    {
        using var scope = new SettingsScope(createDefaultDirectory: false);
        File.WriteAllText(scope.DefaultDirectory, "block");
        scope.Store.Save(new AppSettings { SelectedDeviceId = "retain-this-value" });
        Check.Throws<IOException>(() => scope.Store.Flush());
        File.Delete(scope.DefaultDirectory);
        scope.Store.Flush();
        using var reopened = new SettingsStore(scope.DefaultDirectory, scope.AppDirectory);
        Check.Equal("retain-this-value", reopened.Load().SelectedDeviceId);
    }

    private sealed class SettingsScope : IDisposable
    {
        public SettingsScope(bool createDefaultDirectory = true)
        {
            Root = Path.Combine(Path.GetTempPath(), "MicMute.SettingsCases", Guid.NewGuid().ToString("N"));
            DefaultDirectory = Path.Combine(Root, "default");
            AppDirectory = Path.Combine(Root, "portable");
            Directory.CreateDirectory(AppDirectory);
            if (createDefaultDirectory)
            {
                Directory.CreateDirectory(DefaultDirectory);
            }

            Store = new SettingsStore(DefaultDirectory, AppDirectory);
        }

        public string Root { get; }
        public string DefaultDirectory { get; }
        public string AppDirectory { get; }
        public SettingsStore Store { get; }
        public string SettingsFile => Path.Combine(DefaultDirectory, "settings.json");

        public void WriteSettings(string json, string? folder = null)
        {
            string targetFolder = folder ?? DefaultDirectory;
            Directory.CreateDirectory(targetFolder);
            File.WriteAllText(Path.Combine(targetFolder, "settings.json"), json);
        }

        public void Dispose()
        {
            try
            {
                Store.Dispose();
                if (Directory.Exists(Root))
                {
                    Directory.Delete(Root, recursive: true);
                }
            }
            catch
            {
            }
        }
    }
}
