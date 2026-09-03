using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace MicMute;

public static class StartupManager
{
	private const string RegistryKeyPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";

	private const string AppName = "MicMute";

	public static void SetStartup(bool runOnStartup)
	{
		try
		{
			using RegistryKey? registryKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", writable: true);
			if (registryKey != null)
			{
				if (runOnStartup)
				{
					string text = Process.GetCurrentProcess().MainModule?.FileName ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MicMute.exe");
                    string value = "\"" + text + "\"";
					registryKey.SetValue("MicMute", value);
				}
				else if (registryKey.GetValue("MicMute") != null)
				{
					registryKey.DeleteValue("MicMute");
				}
			}
		}
		catch (Exception)
		{
		}
	}

	public static bool IsStartupEnabled()
	{
		try
		{
			using RegistryKey? registryKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", writable: false);
			if (registryKey == null)
			{
				return false;
			}
			return !string.IsNullOrEmpty(registryKey.GetValue("MicMute") as string);
		}
		catch
		{
			return false;
		}
	}
}
