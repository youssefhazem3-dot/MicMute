using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace MicMute;

public class HotkeyManager : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_ID = 9000;

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

    private readonly IntPtr _hWnd;
    private HwndSource? _hwndSource;
    private Key _currentKey;
    private ModifierKeys _currentModifiers;
    private int _currentVk;
    private IntPtr _hookId = IntPtr.Zero;
    private readonly LowLevelKeyboardProc _proc;
    private DateTime _lastTriggerTime = DateTime.MinValue;

    public event Action? HotkeyPressed;

    public HotkeyManager(IntPtr hWnd)
    {
        _hWnd = hWnd;
        _hwndSource = HwndSource.FromHwnd(_hWnd);
        _hwndSource?.AddHook(HwndHook);

        // Allow WM_HOTKEY to bypass UIPI message filtering from elevated full-screen games
        try
        {
            ChangeWindowMessageFilter(WM_HOTKEY, MSGFLT_ADD);
            ChangeWindowMessageFilterEx(_hWnd, WM_HOTKEY, MSGFLT_ALLOW, IntPtr.Zero);
        }
        catch
        {
        }

        // Install secondary Low-Level Keyboard Hook for direct in-game capture
        _proc = HookCallback;
        InstallHook();
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
        uint fsModifiers = MOD_NOREPEAT;
        if (modifiers.HasFlag(ModifierKeys.Alt))
        {
            fsModifiers |= MOD_ALT;
        }
        if (modifiers.HasFlag(ModifierKeys.Control))
        {
            fsModifiers |= MOD_CONTROL;
        }
        if (modifiers.HasFlag(ModifierKeys.Shift))
        {
            fsModifiers |= MOD_SHIFT;
        }
        if (modifiers.HasFlag(ModifierKeys.Windows))
        {
            fsModifiers |= MOD_WIN;
        }

        int vk = KeyInterop.VirtualKeyFromKey(key);
        _currentVk = vk;
        _currentKey = key;
        _currentModifiers = modifiers;

        bool success = RegisterHotKey(_hWnd, HOTKEY_ID, fsModifiers, (uint)vk);

        // Ensure LL hook is active
        InstallHook();

        return success;
    }

    public void Unregister()
    {
        UnregisterHotKey(_hWnd, HOTKEY_ID);
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

        bool reqCtrl = modifiers.HasFlag(ModifierKeys.Control);
        bool reqAlt = modifiers.HasFlag(ModifierKeys.Alt);
        bool reqShift = modifiers.HasFlag(ModifierKeys.Shift);
        bool reqWin = modifiers.HasFlag(ModifierKeys.Windows);

        return ctrlPressed == reqCtrl && altPressed == reqAlt && shiftPressed == reqShift && winPressed == reqWin;
    }

    private void TriggerHotKey()
    {
        var now = DateTime.UtcNow;
        if ((now - _lastTriggerTime).TotalMilliseconds < 200)
        {
            return; // Debounce dual-engine triggers
        }
        _lastTriggerTime = now;
        HotkeyPressed?.Invoke();
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            TriggerHotKey();
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        Unregister();
        UninstallHook();
        _hwndSource?.RemoveHook(HwndHook);
        _hwndSource = null;
    }
}
