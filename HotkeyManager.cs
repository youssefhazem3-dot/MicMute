using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Input;
using System.Windows.Interop;

namespace MicMute;

public class HotkeyManager : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const int WM_INPUT = 0x00FF;
    private const int HOTKEY_BASE_ID = 9000;

    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;
    private const uint MOD_NOREPEAT = 0x4000;

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;

    private const uint MSGFLT_ADD = 1;
    private const uint MSGFLT_ALLOW = 1;

    private const uint RID_INPUT = 0x10000003;
    private const uint RIDEV_INPUTSINK = 0x00000100;
    private const int RIM_TYPEKEYBOARD = 1;

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUTDEVICE
    {
        public ushort usUsagePage;
        public ushort usUsage;
        public uint dwFlags;
        public IntPtr hwndTarget;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUTHEADER
    {
        public uint dwType;
        public uint dwSize;
        public IntPtr hDevice;
        public IntPtr wParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWKEYBOARD
    {
        public ushort MakeCode;
        public ushort Flags;
        public ushort Reserved;
        public ushort VKey;
        public uint Message;
        public uint ExtraInformation;
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ChangeWindowMessageFilterEx(IntPtr hWnd, uint msg, uint action, IntPtr pChangeFilterStruct);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ChangeWindowMessageFilter(uint msg, uint action);

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

    private readonly IntPtr _hWnd;
    private HwndSource? _hwndSource;
    private Key _currentKey;
    private ModifierKeys _currentModifiers;
    private int _currentVk;
    private IntPtr _hookId = IntPtr.Zero;
    private readonly LowLevelKeyboardProc _proc;
    private DateTime _lastTriggerTime = DateTime.MinValue;

    private Thread? _pollingThread;
    private volatile bool _isPolling;
    private bool _wasKeyDown;

    public event Action? HotkeyPressed;

    public HotkeyManager(IntPtr hWnd)
    {
        _hWnd = hWnd;
        _hwndSource = HwndSource.FromHwnd(_hWnd);
        _hwndSource?.AddHook(HwndHook);

        // 1. Allow WM_HOTKEY and WM_INPUT through UIPI security filter from elevated games
        try
        {
            ChangeWindowMessageFilter(WM_HOTKEY, MSGFLT_ADD);
            ChangeWindowMessageFilterEx(_hWnd, WM_HOTKEY, MSGFLT_ALLOW, IntPtr.Zero);
            ChangeWindowMessageFilter(WM_INPUT, MSGFLT_ADD);
            ChangeWindowMessageFilterEx(_hWnd, WM_INPUT, MSGFLT_ALLOW, IntPtr.Zero);
        }
        catch
        {
        }

        // 2. Register Raw Input Sink for background hardware monitoring (bypasses window message hooks)
        try
        {
            RAWINPUTDEVICE[] rid = new RAWINPUTDEVICE[1];
            rid[0].usUsagePage = 0x01; // Generic desktop controls
            rid[0].usUsage = 0x06;     // Keyboard
            rid[0].dwFlags = RIDEV_INPUTSINK;
            rid[0].hwndTarget = _hWnd;
            RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf<RAWINPUTDEVICE>());
        }
        catch
        {
        }

        // 3. Install Low-Level Keyboard Hook
        _proc = HookCallback;
        InstallHook();

        // 4. Start dedicated high-priority GetAsyncKeyState polling thread (immune to game window hooks)
        StartPolling();
    }

    private void StartPolling()
    {
        if (_isPolling) return;
        _isPolling = true;
        _pollingThread = new Thread(PollLoop)
        {
            IsBackground = true,
            Priority = ThreadPriority.Highest,
            Name = "MicMute_HardwareInputPoller"
        };
        _pollingThread.Start();
    }

    private void StopPolling()
    {
        _isPolling = false;
        _pollingThread = null;
    }

    private void PollLoop()
    {
        while (_isPolling)
        {
            int vk = _currentVk;
            if (vk != 0)
            {
                short state = GetAsyncKeyState(vk);
                bool isDown = (state & 0x8000) != 0;

                if (isDown && !_wasKeyDown)
                {
                    _wasKeyDown = true;
                    if (CheckModifiersMatch(_currentModifiers))
                    {
                        TriggerHotKey();
                    }
                }
                else if (!isDown && _wasKeyDown)
                {
                    _wasKeyDown = false;
                }
            }

            Thread.Sleep(12); // ~80 Hz polling interval, negligible CPU usage
        }
    }

    private void InstallHook()
    {
        if (_hookId != IntPtr.Zero) return;
        try
        {
            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;
            IntPtr modHandle = curModule != null ? GetModuleHandle(curModule.ModuleName) : IntPtr.Zero;
            _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, modHandle, 0);
        }
        catch
        {
        }
    }

    private void UninstallHook()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }

    public bool Register(Key key, ModifierKeys modifiers)
    {
        Unregister();

        int vk = KeyInterop.VirtualKeyFromKey(key);
        _currentVk = vk;
        _currentKey = key;
        _currentModifiers = modifiers;

        bool anySuccess = false;

        // Base registration
        uint fsModifiers = MOD_NOREPEAT;
        if (modifiers.HasFlag(ModifierKeys.Alt)) fsModifiers |= MOD_ALT;
        if (modifiers.HasFlag(ModifierKeys.Control)) fsModifiers |= MOD_CONTROL;
        if (modifiers.HasFlag(ModifierKeys.Shift)) fsModifiers |= MOD_SHIFT;
        if (modifiers.HasFlag(ModifierKeys.Windows)) fsModifiers |= MOD_WIN;

        if (RegisterHotKey(_hWnd, HOTKEY_BASE_ID, fsModifiers, (uint)vk))
        {
            anySuccess = true;
        }

        // If no modifiers specified (e.g. single key like ~ or F1), also register with Shift and Ctrl
        // so walking (holding Shift) or crouching (holding Ctrl) in games like Valorant doesn't block the hotkey!
        if (modifiers == ModifierKeys.None)
        {
            RegisterHotKey(_hWnd, HOTKEY_BASE_ID + 1, MOD_NOREPEAT | MOD_SHIFT, (uint)vk);
            RegisterHotKey(_hWnd, HOTKEY_BASE_ID + 2, MOD_NOREPEAT | MOD_CONTROL, (uint)vk);
            RegisterHotKey(_hWnd, HOTKEY_BASE_ID + 3, MOD_NOREPEAT | MOD_SHIFT | MOD_CONTROL, (uint)vk);
        }

        InstallHook();
        return anySuccess;
    }

    public void Unregister()
    {
        UnregisterHotKey(_hWnd, HOTKEY_BASE_ID);
        UnregisterHotKey(_hWnd, HOTKEY_BASE_ID + 1);
        UnregisterHotKey(_hWnd, HOTKEY_BASE_ID + 2);
        UnregisterHotKey(_hWnd, HOTKEY_BASE_ID + 3);
        _currentVk = 0;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
        {
            int vkCode = Marshal.ReadInt32(lParam);
            if (_currentVk != 0 && vkCode == _currentVk)
            {
                if (CheckModifiersMatch(_currentModifiers))
                {
                    TriggerHotKey();
                }
            }
        }
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private static bool CheckModifiersMatch(ModifierKeys modifiers)
    {
        bool ctrlPressed = (GetAsyncKeyState(0x11) & 0x8000) != 0; // VK_CONTROL
        bool altPressed = (GetAsyncKeyState(0x12) & 0x8000) != 0;  // VK_MENU
        bool shiftPressed = (GetAsyncKeyState(0x10) & 0x8000) != 0;// VK_SHIFT
        bool winPressed = (GetAsyncKeyState(0x5B) & 0x8000) != 0 || (GetAsyncKeyState(0x5C) & 0x8000) != 0; // VK_LWIN / VK_RWIN

        // If no modifier is required (e.g. single key like ~ or F1), do NOT block the hotkey
        // if user is holding Shift (walking) or Ctrl (crouching) in Valorant!
        if (modifiers == ModifierKeys.None)
        {
            return !altPressed && !winPressed;
        }

        bool reqCtrl = modifiers.HasFlag(ModifierKeys.Control);
        bool reqAlt = modifiers.HasFlag(ModifierKeys.Alt);
        bool reqShift = modifiers.HasFlag(ModifierKeys.Shift);
        bool reqWin = modifiers.HasFlag(ModifierKeys.Windows);

        return ctrlPressed == reqCtrl && altPressed == reqAlt && shiftPressed == reqShift && winPressed == reqWin;
    }

    private void TriggerHotKey()
    {
        var now = DateTime.UtcNow;
        if ((now - _lastTriggerTime).TotalMilliseconds < 220)
        {
            return; // Debounce multi-engine triggers
        }
        _lastTriggerTime = now;
        HotkeyPressed?.Invoke();
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        // Engine 1: Win32 WM_HOTKEY
        if (msg == WM_HOTKEY && (wParam.ToInt32() >= HOTKEY_BASE_ID && wParam.ToInt32() <= HOTKEY_BASE_ID + 3))
        {
            TriggerHotKey();
            handled = true;
            return IntPtr.Zero;
        }

        // Engine 2: Raw Input WM_INPUT (captures exclusive fullscreen game inputs)
        if (msg == WM_INPUT)
        {
            try
            {
                uint dwSize = 0;
                int headerSize = Marshal.SizeOf<RAWINPUTHEADER>();
                GetRawInputData(lParam, RID_INPUT, IntPtr.Zero, ref dwSize, (uint)headerSize);
                if (dwSize > 0)
                {
                    IntPtr buffer = Marshal.AllocHGlobal((int)dwSize);
                    try
                    {
                        if (GetRawInputData(lParam, RID_INPUT, buffer, ref dwSize, (uint)headerSize) == dwSize)
                        {
                            RAWINPUTHEADER header = Marshal.PtrToStructure<RAWINPUTHEADER>(buffer);
                            if (header.dwType == RIM_TYPEKEYBOARD)
                            {
                                RAWKEYBOARD kbd = Marshal.PtrToStructure<RAWKEYBOARD>(new IntPtr(buffer.ToInt64() + headerSize));
                                // (Flags & 1) == 0 means KeyDown (RI_KEY_MAKE)
                                if ((kbd.Flags & 1) == 0 && _currentVk != 0 && kbd.VKey == _currentVk)
                                {
                                    if (CheckModifiersMatch(_currentModifiers))
                                    {
                                        TriggerHotKey();
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(buffer);
                    }
                }
            }
            catch
            {
            }
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        StopPolling();
        Unregister();
        UninstallHook();
        _hwndSource?.RemoveHook(HwndHook);
        _hwndSource = null;
    }
}
