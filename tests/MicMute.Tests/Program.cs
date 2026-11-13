using System;
using System.Linq;
using System.Reflection;

namespace MicMute.Tests;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "--wait-fixture") { System.Threading.Thread.Sleep(300); return 0; }
        string area = args.FirstOrDefault() ?? "all";
        int passed = 0, failed = 0;
        void Run(string name, Action test)
        {
            try { test(); passed++; Console.WriteLine("PASS " + name); }
            catch (Exception ex) { failed++; Console.WriteLine("FAIL " + name + ": " + ex); }
        }
        var cases = typeof(Program).Assembly.GetTypes()
            .Where(t => t.Namespace == "MicMute.Tests" && t.Name.EndsWith("Cases", StringComparison.Ordinal))
            .Where(t => area == "all" || t.Name.Equals(area + "Cases", StringComparison.OrdinalIgnoreCase))
            .OrderBy(t => t.Name).ToArray();
        if (cases.Length == 0) { Console.Error.WriteLine("No test cases found for " + area); return 2; }

        string testSandboxRoot = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "MicMute.TestRun", Guid.NewGuid().ToString("N"));
        string testDefaultDir = System.IO.Path.Combine(testSandboxRoot, "appdata");
        string testAppDir = System.IO.Path.Combine(testSandboxRoot, "portable");
        System.IO.Directory.CreateDirectory(testDefaultDir);
        System.IO.Directory.CreateDirectory(testAppDir);
        using var sandboxStore = new SettingsStore(testDefaultDir, testAppDir);
        SettingsStore originalStore = SettingsManager.ReplaceStoreForTesting(sandboxStore);
        try
        {
            foreach (Type type in cases)
            {
                try { type.GetMethod("Run", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, new object[] { (Action<string, Action>)Run }); }
                catch (Exception ex) { failed++; Console.WriteLine("FAIL suite " + type.Name + ": " + ex.GetBaseException()); }
            }
        }
        finally
        {
            SettingsManager.ReplaceStoreForTesting(originalStore);
            try { sandboxStore.Dispose(); } catch { }
            try { if (System.IO.Directory.Exists(testSandboxRoot)) System.IO.Directory.Delete(testSandboxRoot, recursive: true); } catch { }
        }
        Console.WriteLine($"RESULT: {passed} passed, {failed} failed");
        return failed == 0 && passed > 0 ? 0 : 1;
    }
}

internal static class Check
{
    public static void True(bool value, string message = "Expected true")
    {
        if (!value) throw new Exception(message);
    }
    public static void Equal<T>(T expected, T actual, string message = "")
    {
        if (!System.Collections.Generic.EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception($"{message} Expected: {expected}; actual: {actual}");
    }
    public static T Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T exception) { return exception; }
        throw new Exception("Expected exception " + typeof(T).Name);
    }
}
