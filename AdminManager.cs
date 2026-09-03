using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using Microsoft.Win32;

namespace MicMute;

public static class AdminManager
{
    private const string AppCompatKey = @"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers";

    public static bool IsRunningAsAdmin()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    public static string GetExecutablePath()
    {
        try
        {
            return Process.GetCurrentProcess().MainModule?.FileName ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MicMute.exe");
        }
        catch
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MicMute.exe");
        }
    }

    public static bool IsRunAsAdminConfigured()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(AppCompatKey, writable: false);
            if (key != null)
            {
                string exePath = GetExecutablePath();
                string? val = key.GetValue(exePath) as string;
                if (!string.IsNullOrEmpty(val) && val.Contains("RUNASADMIN"))
                {
                    return true;
                }
            }
        }
        catch
        {
        }
        return false;
    }

    public static void SetRunAsAdmin(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(AppCompatKey, writable: true);
            if (key != null)
            {
                string exePath = GetExecutablePath();
                if (enable)
                {
                    string existing = key.GetValue(exePath) as string ?? string.Empty;
                    if (!existing.Contains("RUNASADMIN", StringComparison.OrdinalIgnoreCase))
                        key.SetValue(exePath, (string.IsNullOrWhiteSpace(existing) ? "~" : existing) + " RUNASADMIN");
                }
                else
                {
                    string existing = key.GetValue(exePath) as string ?? string.Empty;
                    string remaining = System.Text.RegularExpressions.Regex.Replace(existing, @"\bRUNASADMIN\b", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
                    if (remaining.Length == 0 || remaining == "~") key.DeleteValue(exePath, throwOnMissingValue: false);
                    else key.SetValue(exePath, remaining);
                }
            }
        }
        catch
        {
        }
    }

    public static bool RestartAsAdmin()
    {
        try
        {
            string exePath = GetExecutablePath();
            SettingsManager.Flush();
            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
                Verb = "runas",
                Arguments = UiBehavior.BuildRestartArguments(Environment.ProcessId, showWindow: true)
            };

            using Process? replacement = Process.Start(startInfo);
            if (replacement == null) return false;
            System.Windows.Application.Current.Shutdown();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
