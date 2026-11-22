using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace MicMute;

public class HotkeyManager : IDisposable
{
    private const int WM_INPUT = 0x00FF;

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;

    private const uint RID_INPUT = 0x10000003;
    private const uint RIDEV_INPUTSINK = 0x00000100;

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUTDEVICE
    {
        public ushort usUsagePage;
        public ushort usUsage;
        public uint dwFlags;
        public IntPtr hwndTarget;
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

    private const uint MSGFLT_ALLOW = 1;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ChangeWindowMessageFilterEx(IntPtr hWnd, uint msg, uint action, IntPtr pChangeFilterStruct);

    [DllImport("user32.dll")]
    private static extern int GetMessageTime();

    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYUP = 0x0105;
    private const uint RIDEV_REMOVE = 0x00000001;

    private volatile HwndSource? _hwndSource;
    private readonly HotkeyState _state = new();
    private readonly HotkeyModifiers _hookModifiers = new();
    private readonly HotkeyModifiers _rawModifiers = new();
    private readonly AutoResetEvent _pollWake;
    private readonly LowLevelKeyboardProc _proc;
    private readonly Thread _pollingThread;
    private IntPtr _hookId;
    private long _lastHookEventTick = Environment.TickCount64;
    private long _lastHookInstallAttemptTick;
    private IntPtr _rawInputBuffer;
    private int _currentVk;
    private Key _currentKey;
    private ModifierKeys _currentModifiers;
    private int _bindingVersion;
    private int _disposed;

    private bool IsDisposed => Volatile.Read(ref _disposed) != 0;
    private static bool IsMouseVk(int vk) => vk is >= 0x04 and <= 0x06;

    public const Key MouseMiddleKey = Key.NoName;
    public const Key MouseXButton1Key = Key.Pa1;
    public const Key MouseXButton2Key = Key.OemClear;

    public static bool IsMouseKey(Key key) => key is MouseMiddleKey or MouseXButton1Key or MouseXButton2Key;

    public static int ToVirtualKey(Key key) => key switch
    {
        MouseMiddleKey => 0x04,
        MouseXButton1Key => 0x05,
        MouseXButton2Key => 0x06,
        _ => KeyInterop.VirtualKeyFromKey(key)
    };

    public event Action? HotkeyPressed;
    public event Action? HotkeyReleased;

    public HotkeyManager(IntPtr hWnd)
    {
        HwndSource source = HwndSource.FromHwnd(hWnd) ?? throw new ArgumentException("A live window handle is required.", nameof(hWnd));
        _hwndSource = source;
        _pollWake = new AutoResetEvent(false);
        _proc = HookCallback;
        _rawInputBuffer = Marshal.AllocHGlobal(128);
        bool hwndHookAdded = false;
        bool rawInputRegistered = false;
        try
        {
            _hookModifiers.Reset(IsKeyDown);
            _rawModifiers.Reset(IsKeyDown);
            source.AddHook(HwndHook);
            hwndHookAdded = true;
            try
            {
                ChangeWindowMessageFilterEx(hWnd, WM_INPUT, MSGFLT_ALLOW, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                DiagnosticLogger.LogError("ChangeWindowMessageFilterEx failed", ex);
            }
            rawInputRegistered = RegisterRawInputDevices(new[] { new RAWINPUTDEVICE { usUsagePage = 1, usUsage = 6, dwFlags = RIDEV_INPUTSINK, hwndTarget = hWnd } },
                1, (uint)Marshal.SizeOf<RAWINPUTDEVICE>());
            InstallHook();
            _pollingThread = new Thread(PollLoop) { IsBackground = true, Priority = ThreadPriority.AboveNormal, Name = "MicMute_InputPoller" };
            _pollingThread.Start();
        }
        catch
        {
            IntPtr hook = Interlocked.Exchange(ref _hookId, IntPtr.Zero);
            if (hook != IntPtr.Zero) try { UnhookWindowsHookEx(hook); } catch { }
            if (rawInputRegistered)
            {
                try
                {
                    RegisterRawInputDevices(new[] { new RAWINPUTDEVICE { usUsagePage = 1, usUsage = 6, dwFlags = RIDEV_REMOVE, hwndTarget = IntPtr.Zero } },
                        1, (uint)Marshal.SizeOf<RAWINPUTDEVICE>());
                }
                catch { }
            }
            if (hwndHookAdded) try { source.RemoveHook(HwndHook); } catch { }
            _hwndSource = null;
            _pollWake.Dispose();
            IntPtr buf = Interlocked.Exchange(ref _rawInputBuffer, IntPtr.Zero);
            if (buf != IntPtr.Zero) Marshal.FreeHGlobal(buf);
            throw;
        }
    }

    public bool Register(Key key, ModifierKeys modifiers)
    {
        if (IsDisposed) return false;
        int vk = ToVirtualKey(key);
        if (vk <= 0 || vk >= 255 || ((int)modifiers & ~15) != 0) return false;
        if (Volatile.Read(ref _currentVk) != 0 && _currentKey == key && _currentModifiers == modifiers) return true;

        // Observe the chord without reserving it with Windows. The foreground application
        // must still receive the original key and perform its normal action (e.g. Ctrl+C).
        Interlocked.Increment(ref _bindingVersion);
        _currentKey = key;
        _currentModifiers = modifiers;
        _hookModifiers.Reset(IsKeyDown);
        _rawModifiers.Reset(IsKeyDown);
        _state.Register(vk, modifiers, IsKeyDown(vk), TickNow);
        Volatile.Write(ref _currentVk, vk);
        RefreshHook();
        _pollWake.Set();
        return true;
    }

    public void Unregister()
    {
        Interlocked.Increment(ref _bindingVersion);
        _state.Unregister();
        Volatile.Write(ref _currentVk, 0);
        _currentKey = Key.None;
        _currentModifiers = ModifierKeys.None;
    }

    public void RefreshHook()
    {
        if (IsDisposed) return;
        IntPtr prev = Interlocked.Exchange(ref _hookId, IntPtr.Zero);
        if (prev != IntPtr.Zero)
        {
            try { UnhookWindowsHookEx(prev); } catch (Exception ex) { DiagnosticLogger.LogWarning("Failed to unhook hotkey hook: " + ex.Message); }
        }
        InstallHook();
    }

    private static uint TickNow => unchecked((uint)Environment.TickCount);
    private static bool IsKeyDown(int vk) => (GetAsyncKeyState(vk) & 0x8000) != 0;

    private static ModifierKeys PhysicalModifiers() =>
        (IsKeyDown(0x11) || IsKeyDown(0xA2) || IsKeyDown(0xA3) ? ModifierKeys.Control : ModifierKeys.None) |
        (IsKeyDown(0x12) || IsKeyDown(0xA4) || IsKeyDown(0xA5) ? ModifierKeys.Alt : ModifierKeys.None) |
        (IsKeyDown(0x10) || IsKeyDown(0xA0) || IsKeyDown(0xA1) ? ModifierKeys.Shift : ModifierKeys.None) |
        (IsKeyDown(0x5B) || IsKeyDown(0x5C) ? ModifierKeys.Windows : ModifierKeys.None);

    private void PollLoop()
    {
        long lastHookRecoveryCheck = Environment.TickCount64;
        while (!IsDisposed)
        {
            try
            {
                int vk = Volatile.Read(ref _currentVk);
                bool isMouse = IsMouseVk(vk);
                int version = Volatile.Read(ref _bindingVersion);
                bool isTargetDown = vk != 0 && IsKeyDown(vk);
                ModifierKeys modifiers = isTargetDown ? PhysicalModifiers() : ModifierKeys.None;

                if (vk != 0)
                {
                    if (_state.Poll(vk, isTargetDown, modifiers, TickNow))
                    {
                        QueueActivation(version);
                        // Physical poller caught an activation that low-level hook missed.
                        // The hook may have timed out or been dropped by Windows; self-heal.
                        if (!isMouse) ScheduleHookRefresh();
                    }
                    if (_state.ConsumeRelease())
                    {
                        QueueRelease(version);
                    }
                }

                long now = Environment.TickCount64;
                if (!isMouse)
                {
                    bool hookMissing = Volatile.Read(ref _hookId) == IntPtr.Zero;
                    bool hookSilent = now - Volatile.Read(ref _lastHookEventTick) > 10000;
                    if ((hookMissing && now - lastHookRecoveryCheck > 2000) ||
                        (hookSilent && now - lastHookRecoveryCheck > 10000))
                    {
                        lastHookRecoveryCheck = now;
                        ScheduleHookRefresh();
                    }
                }

                int waitMs = vk == 0 ? Timeout.Infinite : (isMouse ? 4 : 16);
                _pollWake.WaitOne(waitMs);
            }
            catch (ObjectDisposedException)
            {
                break;
            }
        }
    }

    private void ScheduleHookRefresh()
    {
        HwndSource? source = _hwndSource;
        if (IsDisposed || source == null || source.Dispatcher.HasShutdownStarted) return;
        try
        {
            source.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(RefreshHook));
        }
        catch (InvalidOperationException) { }
    }

    private void QueueActivation(int version)
    {
        HwndSource? source = _hwndSource;
        if (IsDisposed || source == null || source.Dispatcher.HasShutdownStarted) return;
        try
        {
            source.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                if (!IsDisposed && version == Volatile.Read(ref _bindingVersion)) HotkeyPressed?.Invoke();
            }));
        }
        catch (InvalidOperationException) { }
    }

    private void QueueRelease(int version)
    {
        HwndSource? source = _hwndSource;
        if (IsDisposed || source == null || source.Dispatcher.HasShutdownStarted) return;
        try
        {
            source.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                if (!IsDisposed && version == Volatile.Read(ref _bindingVersion)) HotkeyReleased?.Invoke();
            }));
        }
        catch (InvalidOperationException) { }
    }

    private void InstallHook()
    {
        if (IsDisposed || Volatile.Read(ref _hookId) != IntPtr.Zero) return;
        Volatile.Write(ref _lastHookInstallAttemptTick, Environment.TickCount64);
        IntPtr installed = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(null), 0);
        if (installed != IntPtr.Zero)
        {
            if (IsDisposed || Interlocked.CompareExchange(ref _hookId, installed, IntPtr.Zero) != IntPtr.Zero)
            {
                try { UnhookWindowsHookEx(installed); } catch { }
                return;
            }
            Volatile.Write(ref _lastHookEventTick, Environment.TickCount64);
        }
        else
        {
            int err = Marshal.GetLastWin32Error();
            DiagnosticLogger.LogWarning($"SetWindowsHookEx failed with Win32 error {err}");
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (!IsDisposed && nCode >= 0 && lParam != IntPtr.Zero)
        {
            Volatile.Write(ref _lastHookEventTick, Environment.TickCount64);
            int message = wParam.ToInt32();
            bool down = message == WM_KEYDOWN || message == WM_SYSKEYDOWN;
            if (down || message == WM_KEYUP || message == WM_SYSKEYUP)
            {
                int vk = Marshal.ReadInt32(lParam);
                uint timestamp = unchecked((uint)Marshal.ReadInt32(lParam, 12));
                _hookModifiers.Synchronize(IsKeyDown);
                _hookModifiers.Observe(vk, down);
                int version = Volatile.Read(ref _bindingVersion);
                if (_state.Observe(HotkeySource.Hook, vk, down, _hookModifiers.Current, timestamp))
                    QueueActivation(version);
                if (!down && _state.ConsumeRelease())
                    QueueRelease(version);
            }
        }
        return CallNextHookEx(Volatile.Read(ref _hookId), nCode, wParam, lParam);
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (IsDisposed) return IntPtr.Zero;
        int version = Volatile.Read(ref _bindingVersion);
        IntPtr buffer = Volatile.Read(ref _rawInputBuffer);
        if (msg == WM_INPUT && buffer != IntPtr.Zero)
        {
            uint size = 128;
            uint bytes = GetRawInputData(lParam, RID_INPUT, buffer, ref size, (uint)RawKeyboardPacket.HeaderSize);
            if (RawKeyboardPacket.TryRead(buffer, bytes, size, 128, out int vk, out bool down))
            {
                _rawModifiers.Synchronize(IsKeyDown);
                _rawModifiers.Observe(vk, down);
                if (_state.Observe(HotkeySource.RawInput, vk, down, _rawModifiers.Current, unchecked((uint)GetMessageTime())))
                    QueueActivation(version);
                if (!down && _state.ConsumeRelease())
                    QueueRelease(version);
                long now = Environment.TickCount64;
                if (!IsMouseVk(Volatile.Read(ref _currentVk))
                    && now - Volatile.Read(ref _lastHookEventTick) > 250
                    && now - Volatile.Read(ref _lastHookInstallAttemptTick) > 2000)
                {
                    RefreshHook();
                }
            }
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        Unregister();
        HotkeyPressed = null;
        HotkeyReleased = null;
        try { _pollWake.Set(); } catch (ObjectDisposedException) { }
        _pollingThread.Join(1000);
        _pollWake.Dispose();
        IntPtr hook = Interlocked.Exchange(ref _hookId, IntPtr.Zero);
        if (hook != IntPtr.Zero)
        {
            try { UnhookWindowsHookEx(hook); } catch { }
        }
        try
        {
            RegisterRawInputDevices(new[] { new RAWINPUTDEVICE { usUsagePage = 1, usUsage = 6, dwFlags = RIDEV_REMOVE, hwndTarget = IntPtr.Zero } },
                1, (uint)Marshal.SizeOf<RAWINPUTDEVICE>());
        }
        catch { }
        HwndSource? source = _hwndSource;
        _hwndSource = null;
        if (source != null)
        {
            try { source.RemoveHook(HwndHook); } catch { }
        }
        IntPtr buffer = Interlocked.Exchange(ref _rawInputBuffer, IntPtr.Zero);
        if (buffer != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(buffer);
        }
        GC.SuppressFinalize(this);
    }
}
