using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace MicMute;

public static class StartupManager
{
    private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "MicMute";
    private const string TaskName = "MicMute";

    public static void SetStartup(bool runOnStartup, bool? runAsAdmin = null)
    {
        bool elevated = runAsAdmin ?? (AdminManager.IsRunningAsAdmin() || AdminManager.IsRunAsAdminConfigured());
        string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MicMute.exe");

        if (!runOnStartup)
        {
            RemoveRegistryRun();
            if (IsScheduledTaskConfigured())
            {
                RemoveScheduledTask();
            }
            return;
        }

        if (elevated)
        {
            // Windows Explorer automatically suppresses elevated HKCU\Run registry entries at logon.
            // When running elevated or configured for Admin, use Windows Task Scheduler with /rl highest.
            bool taskCreated = CreateScheduledTask(exePath);
            if (taskCreated)
            {
                // Clean up redundant registry run value so Explorer doesn't attempt and discard it
                RemoveRegistryRun();
            }
            else
            {
                // Fallback to registry if task creation failed
                SetRegistryRun(exePath);
            }
        }
        else
        {
            // Standard non-admin startup uses the standard HKCU Run key
            if (IsScheduledTaskConfigured())
            {
                RemoveScheduledTask();
            }
            SetRegistryRun(exePath);
        }
    }

    public static bool IsStartupEnabled()
    {
        try
        {
            if (IsScheduledTaskConfigured()) return true;

            using RegistryKey? registryKey = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: false);
            if (registryKey == null) return false;
            return !string.IsNullOrEmpty(registryKey.GetValue(AppName) as string);
        }
        catch
        {
            return false;
        }
    }

    private static void SetRegistryRun(string exePath)
    {
        try
        {
            using RegistryKey? registryKey = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: true);
            if (registryKey != null)
            {
                string value = "\"" + exePath + "\"";
                string? existing = registryKey.GetValue(AppName) as string;
                if (!string.Equals(existing, value, StringComparison.OrdinalIgnoreCase))
                {
                    registryKey.SetValue(AppName, value);
                }
            }
        }
        catch { }
    }

    private static void RemoveRegistryRun()
    {
        try
        {
            using RegistryKey? registryKey = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: true);
            if (registryKey != null && registryKey.GetValue(AppName) != null)
            {
                registryKey.DeleteValue(AppName, throwOnMissingValue: false);
            }
        }
        catch { }
    }

    public static bool CreateScheduledTask(string exePath)
    {
        try
        {
            Type? schedulerType = Type.GetTypeFromProgID("Schedule.Service");
            if (schedulerType != null)
            {
                dynamic? scheduler = Activator.CreateInstance(schedulerType);
                if (scheduler != null)
                {
                    scheduler.Connect();
                    dynamic folder = scheduler.GetFolder("\\");
                    dynamic taskDef = scheduler.NewTask(0);
                    taskDef.RegistrationInfo.Description = "MicMute Startup Task";
                    taskDef.Principal.RunLevel = 1; // TASK_RUNLEVEL_HIGHEST
                    taskDef.Principal.LogonType = 3; // TASK_LOGON_INTERACTIVE_TOKEN
                    dynamic trigger = taskDef.Triggers.Create(9); // TASK_TRIGGER_LOGON
                    trigger.Enabled = true;
                    dynamic action = taskDef.Actions.Create(0); // TASK_ACTION_EXEC
                    action.Path = exePath;
                    folder.RegisterTaskDefinition(TaskName, taskDef, 6, null, null, 3, null); // TASK_CREATE_OR_UPDATE = 6
                    return true;
                }
            }
        }
        catch
        {
            // Fall back to schtasks if COM registration fails
        }

        string schtasksPath = GetSchtasksPath();
        var psi = new ProcessStartInfo
        {
            FileName = schtasksPath,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        psi.ArgumentList.Add("/create");
        psi.ArgumentList.Add("/tn");
        psi.ArgumentList.Add(TaskName);
        psi.ArgumentList.Add("/tr");
        psi.ArgumentList.Add(exePath);
        psi.ArgumentList.Add("/sc");
        psi.ArgumentList.Add("onlogon");
        psi.ArgumentList.Add("/rl");
        psi.ArgumentList.Add("highest");
        psi.ArgumentList.Add("/f");

        return RunSchtasks(psi);
    }

    public static bool RemoveScheduledTask()
    {
        try
        {
            Type? schedulerType = Type.GetTypeFromProgID("Schedule.Service");
            if (schedulerType != null)
            {
                dynamic? scheduler = Activator.CreateInstance(schedulerType);
                if (scheduler != null)
                {
                    scheduler.Connect();
                    dynamic folder = scheduler.GetFolder("\\");
                    try
                    {
                        folder.DeleteTask(TaskName, 0);
                        return true;
                    }
                    catch (FileNotFoundException)
                    {
                        // Task does not exist, consider removal successful
                        return true;
                    }
                    catch (COMException ex) when ((uint)ex.HResult == 0x80070002) // ERROR_FILE_NOT_FOUND
                    {
                        return true;
                    }
                }
            }
        }
        catch
        {
            // Fall back if COM is unavailable
        }

        string schtasksPath = GetSchtasksPath();
        var psi = new ProcessStartInfo
        {
            FileName = schtasksPath,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        psi.ArgumentList.Add("/delete");
        psi.ArgumentList.Add("/tn");
        psi.ArgumentList.Add(TaskName);
        psi.ArgumentList.Add("/f");

        return RunSchtasks(psi);
    }

    public static bool IsScheduledTaskConfigured()
    {
        try
        {
            Type? schedulerType = Type.GetTypeFromProgID("Schedule.Service");
            if (schedulerType != null)
            {
                dynamic? scheduler = Activator.CreateInstance(schedulerType);
                if (scheduler != null)
                {
                    scheduler.Connect();
                    dynamic folder = scheduler.GetFolder("\\");
                    dynamic task = folder.GetTask(TaskName);
                    return task != null;
                }
            }
        }
        catch (FileNotFoundException)
        {
            return false;
        }
        catch (COMException ex) when ((uint)ex.HResult == 0x80070002) // ERROR_FILE_NOT_FOUND
        {
            return false;
        }
        catch
        {
            // Non-COM environment fallback
        }

        return false;
    }

    private static string GetSchtasksPath()
    {
        string systemSchtasks = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "schtasks.exe");
        return File.Exists(systemSchtasks) ? systemSchtasks : "schtasks.exe";
    }

    private static bool RunSchtasks(ProcessStartInfo psi)
    {
        try
        {
            using var process = Process.Start(psi);
            if (process == null) return false;
            if (!process.WaitForExit(5000))
            {
                try { process.Kill(); } catch { }
                return false;
            }
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
