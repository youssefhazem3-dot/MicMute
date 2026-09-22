using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace MicMute;

public static class DiagnosticLogger
{
    private static readonly object _sync = new();
    private static bool _isEnabled;
    private static bool _consoleInitialized;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    private const int ATTACH_PARENT_PROCESS = -1;
    private const int STD_OUTPUT_HANDLE = -11;
    private const int STD_ERROR_HANDLE = -12;

    public static bool IsEnabled => _isEnabled;

    public static void Initialize(string[]? args)
    {
        if (UiBehavior.IsDiagnosticMode(args))
        {
            _isEnabled = true;
            EnsureConsole();
            PrintBanner();
        }
    }

    public static void EnableConsole()
    {
        _isEnabled = true;
        EnsureConsole();
    }

    private static void EnsureConsole()
    {
        if (_consoleInitialized) return;
        lock (_sync)
        {
            if (_consoleInitialized) return;
            if (!AttachConsole(ATTACH_PARENT_PROCESS))
            {
                AllocConsole();
            }
            try
            {
                IntPtr stdOut = GetStdHandle(STD_OUTPUT_HANDLE);
                if (stdOut != IntPtr.Zero && stdOut != new IntPtr(-1))
                {
                    var safeHandle = new SafeFileHandle(stdOut, ownsHandle: false);
                    var fs = new FileStream(safeHandle, FileAccess.Write);
                    var writer = new StreamWriter(fs, Encoding.UTF8) { AutoFlush = true };
                    Console.SetOut(writer);
                    Console.SetError(writer);
                }
                Console.OutputEncoding = Encoding.UTF8;
                Console.Title = "MicMute Diagnostic Console";
            }
            catch { }
            _consoleInitialized = true;
        }
    }

    private static void PrintBanner()
    {
        lock (_sync)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine();
            Console.WriteLine("==============================================================");
            Console.WriteLine("  MicMute — Local Diagnostic Console");
            Console.WriteLine("==============================================================");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"  Timestamp:       {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"  OS Architecture: {(Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit")}");
            Console.WriteLine($"  Runtime:         .NET {Environment.Version}");
            Console.WriteLine($"  Process ID:      {Environment.ProcessId}");
            Console.WriteLine($"  Settings File:   {SettingsManager.GetSettingsFilePath()}");
            Console.WriteLine($"  Data Directory:  {SettingsManager.GetDataFolderPath()}");
            Console.WriteLine($"  Working Area:    {System.Windows.SystemParameters.WorkArea.Width:F0}x{System.Windows.SystemParameters.WorkArea.Height:F0}");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("==============================================================");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  Listening for real-time audio, hotkey, and UI events...");
            Console.WriteLine();
            Console.ForegroundColor = oldColor;
        }
    }

    public static void LogInfo(string message) => WriteLog("INFO", message, ConsoleColor.Cyan);
    public static void LogWarning(string message) => WriteLog("WARN", message, ConsoleColor.Yellow);
    public static void LogError(string message, Exception? ex = null)
    {
        string fullMessage = ex != null ? $"{message} | {ex.GetType().Name}: {ex.Message}" : message;
        WriteLog("FAIL", fullMessage, ConsoleColor.Red);
    }
    public static void LogAudio(string message) => WriteLog("AUDIO", message, ConsoleColor.Green);
    public static void LogHotkey(string message) => WriteLog("HOTKEY", message, ConsoleColor.Magenta);
    public static void LogUi(string message) => WriteLog("UI", message, ConsoleColor.White);

    private static void WriteLog(string category, string message, ConsoleColor color)
    {
        if (!_isEnabled) return;
        lock (_sync)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"[{DateTime.Now:HH:mm:ss.fff}] ");
            Console.ForegroundColor = color;
            Console.Write($"[{category}] ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(message);
            Console.ForegroundColor = oldColor;
        }
    }
}
