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
        string exePath = AdminManager.GetExecutablePath();

        // Never write test runners or development hosts to real system startup
        if (exePath.Contains("MicMute.Tests", StringComparison.OrdinalIgnoreCase) ||
            exePath.Contains("testhost", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

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
            if (IsScheduledTaskMatching(exePath))
            {
                RemoveRegistryRun();
                return;
            }

            bool taskCreated = CreateScheduledTask(exePath);
            if (taskCreated)
            {
                // Clean up redundant registry run value so Explorer doesn't attempt and discard it
                RemoveRegistryRun();
            }
            else
            {
                DiagnosticLogger.LogError("Scheduled task creation failed for elevated startup. Falling back to registry Run key (may require elevation at logon).", null);
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
        catch (Exception ex)
        {
            DiagnosticLogger.LogError("IsStartupEnabled check failed", ex);
            return false;
        }
    }

    private static void SetRegistryRun(string exePath)
    {
        try
        {
            string value = "\"" + exePath + "\"";
            using (RegistryKey? readKey = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: false))
            {
                if (readKey == null) return;
                string? existing = readKey.GetValue(AppName) as string;
                if (string.Equals(existing, value, StringComparison.OrdinalIgnoreCase)) return;
            }

            using RegistryKey? registryKey = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: true);
            registryKey?.SetValue(AppName, value);
        }
        catch (Exception ex)
        {
            DiagnosticLogger.LogError("SetRegistryRun failed", ex);
        }
    }

    private static void RemoveRegistryRun()
    {
        try
        {
            using (RegistryKey? readKey = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: false))
            {
                if (readKey == null || readKey.GetValue(AppName) == null) return;
            }

            using RegistryKey? registryKey = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: true);
            if (registryKey != null && registryKey.GetValue(AppName) != null)
            {
                registryKey.DeleteValue(AppName, throwOnMissingValue: false);
            }
        }
        catch (Exception ex)
        {
            DiagnosticLogger.LogError("RemoveRegistryRun failed", ex);
        }
    }

    private static void ReleaseCom(object? comObj)
    {
        if (comObj != null && Marshal.IsComObject(comObj))
        {
            try { Marshal.FinalReleaseComObject(comObj); } catch { }
        }
    }

    private static bool IsTestHost(string? exePath = null)
    {
        string checkPath = exePath ?? AdminManager.GetExecutablePath();
        return checkPath.Contains("MicMute.Tests", StringComparison.OrdinalIgnoreCase) ||
               checkPath.Contains("testhost", StringComparison.OrdinalIgnoreCase);
    }

    public static bool CreateScheduledTask(string exePath)
    {
        if (IsTestHost(exePath) || IsTestHost()) return true;
        object? scheduler = null;
        object? folder = null;
        object? taskDef = null;
        object? regInfo = null;
        object? principal = null;
        object? triggers = null;
        object? trigger = null;
        object? actions = null;
        object? action = null;
        object? registeredTask = null;
        try
        {
            Type? schedulerType = Type.GetTypeFromProgID("Schedule.Service");
            if (schedulerType != null)
            {
                scheduler = Activator.CreateInstance(schedulerType);
                if (scheduler != null)
                {
                    dynamic sched = scheduler;
                    sched.Connect();
                    folder = sched.GetFolder("\\");
                    dynamic dynFolder = folder!;
                    taskDef = sched.NewTask(0);
                    dynamic dynTaskDef = taskDef!;
                    regInfo = dynTaskDef.RegistrationInfo;
                    ((dynamic)regInfo!).Description = "MicMute Startup Task";
                    principal = dynTaskDef.Principal;
                    ((dynamic)principal!).RunLevel = 1; // TASK_RUNLEVEL_HIGHEST
                    ((dynamic)principal).LogonType = 3; // TASK_LOGON_INTERACTIVE_TOKEN
                    triggers = dynTaskDef.Triggers;
                    trigger = ((dynamic)triggers!).Create(9); // TASK_TRIGGER_LOGON
                    ((dynamic)trigger!).Enabled = true;
                    actions = dynTaskDef.Actions;
                    action = ((dynamic)actions!).Create(0); // TASK_ACTION_EXEC
                    ((dynamic)action!).Path = exePath;
                    registeredTask = dynFolder.RegisterTaskDefinition(TaskName, dynTaskDef, 6, null, null, 3, null); // TASK_CREATE_OR_UPDATE = 6
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            // Fall back to schtasks if COM registration fails
            DiagnosticLogger.LogError("CreateScheduledTask COM failed", ex);
        }
        finally
        {
            ReleaseCom(registeredTask);
            ReleaseCom(action);
            ReleaseCom(actions);
            ReleaseCom(trigger);
            ReleaseCom(triggers);
            ReleaseCom(principal);
            ReleaseCom(regInfo);
            ReleaseCom(taskDef);
            ReleaseCom(folder);
            ReleaseCom(scheduler);
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
        if (IsTestHost()) return true;
        object? scheduler = null;
        object? folder = null;
        try
        {
            Type? schedulerType = Type.GetTypeFromProgID("Schedule.Service");
            if (schedulerType != null)
            {
                scheduler = Activator.CreateInstance(schedulerType);
                if (scheduler != null)
                {
                    dynamic sched = scheduler;
                    sched.Connect();
                    folder = sched.GetFolder("\\");
                    try
                    {
                        ((dynamic)folder!).DeleteTask(TaskName, 0);
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
        catch (Exception ex)
        {
            // Fall back if COM is unavailable
            DiagnosticLogger.LogError("RemoveScheduledTask COM failed", ex);
        }
        finally
        {
            ReleaseCom(folder);
            ReleaseCom(scheduler);
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

    public static bool IsScheduledTaskConfigured() => IsScheduledTaskMatching(null);

    private static bool IsScheduledTaskMatching(string? expectedExePath)
    {
        object? scheduler = null;
        object? folder = null;
        object? task = null;
        object? definition = null;
        object? principal = null;
        object? actions = null;
        object? action = null;
        try
        {
            Type? schedulerType = Type.GetTypeFromProgID("Schedule.Service");
            if (schedulerType != null)
            {
                scheduler = Activator.CreateInstance(schedulerType);
                if (scheduler != null)
                {
                    dynamic sched = scheduler;
                    sched.Connect();
                    folder = sched.GetFolder("\\");
                    task = ((dynamic)folder!).GetTask(TaskName);
                    if (task == null) return false;
                    if (expectedExePath == null) return true;

                    dynamic dynTask = task;
                    if (!(bool)dynTask.Enabled) return false;
                    definition = dynTask.Definition;
                    principal = ((dynamic)definition!).Principal;
                    if ((int)((dynamic)principal!).RunLevel != 1) return false;
                    actions = ((dynamic)definition).Actions;
                    if ((int)((dynamic)actions!).Count < 1) return false;
                    action = ((dynamic)actions)[1]; // 1-based COM collection
                    string? configuredPath = ((dynamic)action!).Path as string;
                    if (string.IsNullOrWhiteSpace(configuredPath)) return false;
                    string cleanConfigured = configuredPath.Trim().Trim('"');
                    string cleanExpected = expectedExePath.Trim().Trim('"');
                    return string.Equals(cleanConfigured, cleanExpected, StringComparison.OrdinalIgnoreCase);
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
        catch (Exception ex)
        {
            // Non-COM environment fallback
            DiagnosticLogger.LogError("IsScheduledTaskConfigured COM failed", ex);
        }
        finally
        {
            ReleaseCom(action);
            ReleaseCom(actions);
            ReleaseCom(principal);
            ReleaseCom(definition);
            ReleaseCom(task);
            ReleaseCom(folder);
            ReleaseCom(scheduler);
        }

        try
        {
            if (expectedExePath != null) return false;
            string schtasksPath = GetSchtasksPath();
            var psi = new ProcessStartInfo
            {
                FileName = schtasksPath,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            psi.ArgumentList.Add("/query");
            psi.ArgumentList.Add("/tn");
            psi.ArgumentList.Add(TaskName);
            return RunSchtasks(psi);
        }
        catch (Exception ex)
        {
            DiagnosticLogger.LogError("IsScheduledTaskConfigured CLI fallback failed", ex);
            return false;
        }
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
        catch (Exception ex)
        {
            DiagnosticLogger.LogError("RunSchtasks failed", ex);
            return false;
        }
    }
}
