using System;
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

    private readonly IntPtr _hWnd;
    private HwndSource? _hwndSource;
    private Key _currentKey;
    private ModifierKeys _currentModifiers;

    public event Action? HotkeyPressed;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public HotkeyManager(IntPtr hWnd)
    {
        _hWnd = hWnd;
        _hwndSource = HwndSource.FromHwnd(_hWnd);
        _hwndSource?.AddHook(HwndHook);
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
        bool success = RegisterHotKey(_hWnd, HOTKEY_ID, fsModifiers, (uint)vk);
        if (success)
        {
            _currentKey = key;
            _currentModifiers = modifiers;
        }
        return success;
    }

    public void Unregister()
    {
        UnregisterHotKey(_hWnd, HOTKEY_ID);
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            HotkeyPressed?.Invoke();
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        Unregister();
        _hwndSource?.RemoveHook(HwndHook);
        _hwndSource = null;
    }
}