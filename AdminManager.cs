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
                    key.SetValue(exePath, "~ RUNASADMIN");

                    string rootPath = @"e:\MicMute\MicMute.exe";
                    string pubPath = @"e:\MicMute\publish\MicMute.exe";
                    if (File.Exists(rootPath)) key.SetValue(rootPath, "~ RUNASADMIN");
                    if (File.Exists(pubPath)) key.SetValue(pubPath, "~ RUNASADMIN");
                }
                else
                {
                    key.DeleteValue(exePath, throwOnMissingValue: false);
                    string rootPath = @"e:\MicMute\MicMute.exe";
                    string pubPath = @"e:\MicMute\publish\MicMute.exe";
                    key.DeleteValue(rootPath, throwOnMissingValue: false);
                    key.DeleteValue(pubPath, throwOnMissingValue: false);
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
            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
                Verb = "runas"
            };

            Process.Start(startInfo);
            System.Windows.Application.Current.Shutdown();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
