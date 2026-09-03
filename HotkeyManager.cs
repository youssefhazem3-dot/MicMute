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

    [DllImport("user32.dll")]
    private static extern int GetMessageTime();

    private readonly IntPtr _hWnd;
    private HwndSource? _hwndSource;
    private readonly HotkeyState _state = new();
    private readonly HotkeyModifiers _hookModifiers = new();
    private readonly HotkeyModifiers _rawModifiers = new();
    private readonly AutoResetEvent _pollWake = new(false);
    private readonly LowLevelKeyboardProc _proc;
    private readonly Thread _pollingThread;
    private IntPtr _hookId;
    private IntPtr _rawInputBuffer;
    private int _currentVk;
    private Key _currentKey;
    private ModifierKeys _currentModifiers;
    private int _bindingVersion;
    private volatile bool _disposed;

    public event Action? HotkeyPressed;

    public HotkeyManager(IntPtr hWnd)
    {
        _hWnd = hWnd;
        _hwndSource = HwndSource.FromHwnd(hWnd) ?? throw new ArgumentException("A live window handle is required.", nameof(hWnd));
        _proc = HookCallback;
        _rawInputBuffer = Marshal.AllocHGlobal(128);
        _hookModifiers.Reset(IsKeyDown);
        _rawModifiers.Reset(IsKeyDown);
        _hwndSource.AddHook(HwndHook);
        RegisterRawInputDevices(new[] { new RAWINPUTDEVICE { usUsagePage = 1, usUsage = 6, dwFlags = RIDEV_INPUTSINK, hwndTarget = hWnd } },
            1, (uint)Marshal.SizeOf<RAWINPUTDEVICE>());
        InstallHook();
        _pollingThread = new Thread(PollLoop) { IsBackground = true, Priority = ThreadPriority.Normal, Name = "MicMute_InputPoller" };
        _pollingThread.Start();
    }

    public bool Register(Key key, ModifierKeys modifiers)
    {
        if (_disposed) return false;
        int vk = KeyInterop.VirtualKeyFromKey(key);
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
        InstallHook();
        _pollWake.Set();
        return true;
    }

    public void Unregister()
    {
        Interlocked.Increment(ref _bindingVersion);
        _state.Unregister();
        Volatile.Write(ref _currentVk, 0);
    }

    private static uint TickNow => unchecked((uint)Environment.TickCount);
    private static bool IsKeyDown(int vk) => (GetAsyncKeyState(vk) & 0x8000) != 0;

    private static ModifierKeys PhysicalModifiers() =>
        (IsKeyDown(0x11) ? ModifierKeys.Control : ModifierKeys.None) |
        (IsKeyDown(0x12) ? ModifierKeys.Alt : ModifierKeys.None) |
        (IsKeyDown(0x10) ? ModifierKeys.Shift : ModifierKeys.None) |
        (IsKeyDown(0x5B) || IsKeyDown(0x5C) ? ModifierKeys.Windows : ModifierKeys.None);

    private void PollLoop()
    {
        while (!_disposed)
        {
            int vk = Volatile.Read(ref _currentVk);
            int version = Volatile.Read(ref _bindingVersion);
            if (vk != 0 && _state.Poll(vk, IsKeyDown(vk), PhysicalModifiers(), TickNow))
                QueueActivation(version);
            _pollWake.WaitOne(vk == 0 ? Timeout.Infinite : 16);
        }
    }

    private void QueueActivation(int version)
    {
        HwndSource? source = _hwndSource;
        if (_disposed || source == null || source.Dispatcher.HasShutdownStarted) return;
        try
        {
            source.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
            {
                if (!_disposed && version == Volatile.Read(ref _bindingVersion)) HotkeyPressed?.Invoke();
            }));
        }
        catch (InvalidOperationException) { }
    }

    private void InstallHook()
    {
        if (_disposed || _hookId != IntPtr.Zero) return;
        _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(null), 0);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (!_disposed && nCode >= 0 && lParam != IntPtr.Zero)
        {
            int message = wParam.ToInt32();
            bool down = message == WM_KEYDOWN || message == WM_SYSKEYDOWN;
            if (down || message == 0x101 || message == 0x105)
            {
                int vk = Marshal.ReadInt32(lParam);
                uint timestamp = unchecked((uint)Marshal.ReadInt32(lParam, 12));
                _hookModifiers.Observe(vk, down);
                int version = Volatile.Read(ref _bindingVersion);
                if (_state.Observe(HotkeySource.Hook, vk, down, _hookModifiers.Current, timestamp))
                    QueueActivation(version);
            }
        }
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (_disposed) return IntPtr.Zero;
        int version = Volatile.Read(ref _bindingVersion);
        if (msg == WM_INPUT && _rawInputBuffer != IntPtr.Zero)
        {
            uint size = 128;
            uint bytes = GetRawInputData(lParam, RID_INPUT, _rawInputBuffer, ref size, (uint)RawKeyboardPacket.HeaderSize);
            if (RawKeyboardPacket.TryRead(_rawInputBuffer, bytes, size, 128, out int vk, out bool down))
            {
                _rawModifiers.Observe(vk, down);
                if (_state.Observe(HotkeySource.RawInput, vk, down, _rawModifiers.Current, unchecked((uint)GetMessageTime())))
                    QueueActivation(version);
            }
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Unregister();
        _pollWake.Set();
        _pollingThread.Join();
        _pollWake.Dispose();
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
        RegisterRawInputDevices(new[] { new RAWINPUTDEVICE { usUsagePage = 1, usUsage = 6, dwFlags = 1, hwndTarget = IntPtr.Zero } },
            1, (uint)Marshal.SizeOf<RAWINPUTDEVICE>());
        _hwndSource?.RemoveHook(HwndHook);
        _hwndSource = null;
        if (_rawInputBuffer != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_rawInputBuffer);
            _rawInputBuffer = IntPtr.Zero;
        }
    }
}
