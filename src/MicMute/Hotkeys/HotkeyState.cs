using System;
using System.Windows.Input;

namespace MicMute;

internal enum HotkeySource { Hook, RawInput }

/// <summary>One press/release state shared by native events and physical polling.</summary>
internal sealed class HotkeyState
{
    private readonly object _sync = new();
    private int _vk;
    private ModifierKeys _modifiers;
    private bool _held;
    private bool _pollConfirmedDown;
    private bool _pollNeedsRelease;
    private bool _activeHold;
    private bool _releasedPending;
    private uint _lastEventTime;
    private HotkeySource? _lastSource;

    public void Register(int vk, ModifierKeys modifiers, bool isCurrentlyDown, uint timestamp)
    {
        lock (_sync)
        {
            _vk = vk;
            _modifiers = modifiers;
            _held = isCurrentlyDown;
            _pollConfirmedDown = isCurrentlyDown;
            _pollNeedsRelease = isCurrentlyDown;
            _activeHold = false;
            _releasedPending = false;
            _lastEventTime = timestamp;
            _lastSource = null;
        }
    }

    public void Unregister()
    {
        lock (_sync)
        {
            _vk = 0;
            _held = _pollConfirmedDown = _pollNeedsRelease = false;
            _activeHold = false;
            _releasedPending = false;
        }
    }

    public bool ConsumeRelease()
    {
        lock (_sync)
        {
            if (!_releasedPending) return false;
            _releasedPending = false;
            return true;
        }
    }

    public bool Observe(HotkeySource source, int vk, bool isDown, ModifierKeys modifiers, uint timestamp)
    {
        lock (_sync)
        {
            if (_vk == 0 || vk != _vk) return false;
            int elapsed = unchecked((int)(timestamp - _lastEventTime));
            // Native timestamps share the Windows tick clock. Ignore old packets, including
            // duplicates that arrive after another engine has already observed the release.
            // At the clock's millisecond resolution only the same source proves edge order.
            if (elapsed < 0 || (elapsed == 0 && _lastSource != source)) return false;
            _lastEventTime = timestamp;
            _lastSource = source;
            if (!isDown)
            {
                _held = false;
                _pollConfirmedDown = false;
                _pollNeedsRelease = true;
                if (_activeHold)
                {
                    _activeHold = false;
                    _releasedPending = true;
                }
                return false;
            }
            if (_held) return false;
            _held = true;
            _pollConfirmedDown = false;
            // A mismatching first down consumes this hold too. Releasing a modifier while
            // V remains down must never turn Ctrl+V into a plain-V activation.
            if (ModifiersMatch(modifiers))
            {
                _activeHold = true;
                _releasedPending = false;
                return true;
            }
            return false;
        }
    }

    public bool Poll(int vk, bool isDown, ModifierKeys modifiers, uint timestamp)
    {
        lock (_sync)
        {
            if (_vk == 0 || vk != _vk || unchecked((int)(timestamp - _lastEventTime)) < 0) return false;
            int timeSinceEvent = unchecked((int)(timestamp - _lastEventTime));
            if (!isDown)
            {
                _pollNeedsRelease = false;
                // WH_KEYBOARD_LL is called before the asynchronous state changes. Its
                // initial stale-up snapshot cannot release a native-owned press.
                // But once the key is confirmed down OR sufficient time has elapsed
                // (past any initial stale snapshot), an up sample must clear the hold.
                if (_held && (_pollConfirmedDown || timeSinceEvent >= 40))
                {
                    _held = false;
                    _lastEventTime = timestamp;
                    _lastSource = null;
                    if (_activeHold)
                    {
                        _activeHold = false;
                        _releasedPending = true;
                    }
                }
                _pollConfirmedDown = false;
                return false;
            }
            if (_held)
            {
                _pollConfirmedDown = true;
                return false;
            }
            // The opposite race exists after a native keyup: wait for an actual up sample
            // before accepting another poll-only press. Native presses still work immediately.
            if (_pollNeedsRelease && timeSinceEvent < 40) return false;
            _pollNeedsRelease = false;
            _held = _pollConfirmedDown = true;
            _lastEventTime = timestamp;
            _lastSource = null;
            if (ModifiersMatch(modifiers))
            {
                _activeHold = true;
                _releasedPending = false;
                return true;
            }
            return false;
        }
    }

    private bool ModifiersMatch(ModifierKeys actual)
    {
        if (actual == _modifiers) return true;
        // When a dedicated non-typing key (e.g. Insert, F1..F24, Mouse 4/5, Pause, Home/End/PgUp/PgDn)
        // is bound without modifiers, allow it to trigger even while Shift/Ctrl/Alt are held in-game
        // (such as sprinting with Shift or crouching with Ctrl in Fortnite).
        return _modifiers == ModifierKeys.None && IsDedicatedNonTypingKey(_vk);
    }

    internal static bool IsDedicatedNonTypingKey(int vk) => vk switch
    {
        >= 0x04 and <= 0x06 => true, // MouseMiddle, MouseXButton1, MouseXButton2
        0x13 or 0x14 => true,        // Pause, CapsLock
        >= 0x21 and <= 0x2E => true, // PageUp, PageDown, End, Home, Arrow keys, Select, Print, Execute, PrintScreen, Insert, Delete
        0x5D => true,                // Apps / ContextMenu key
        >= 0x60 and <= 0x87 => true, // NumPad 0-9, Multiply/Add/Separator/Subtract/Decimal/Divide, F1-F24
        0x90 or 0x91 => true,        // NumLock, ScrollLock
        >= 0xA6 and <= 0xB7 => true, // Browser / Media / Launch keys
        _ => false
    };
}

/// <summary>Tracks modifiers in each ordered native stream, including both sides.</summary>
internal sealed class HotkeyModifiers
{
    private static readonly int[] ModifierVirtualKeys = { 0x10, 0x11, 0x12, 0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0x5B, 0x5C };
    private readonly bool[] _keys = new bool[256];

    public void Reset(Func<int, bool> isDown)
    {
        Array.Clear(_keys);
        foreach (int vk in ModifierVirtualKeys)
            _keys[vk] = isDown(vk);
    }

    public void Observe(int vk, bool down)
    {
        if (vk >= 0 && vk < _keys.Length) _keys[vk] = down;
    }

    public void Synchronize(Func<int, bool> isDown)
    {
        foreach (int vk in ModifierVirtualKeys)
        {
            if (_keys[vk] && !isDown(vk))
            {
                _keys[vk] = false;
            }
        }
    }

    public ModifierKeys Current =>
        ((_keys[0x10] || _keys[0xA0] || _keys[0xA1]) ? ModifierKeys.Shift : ModifierKeys.None) |
        ((_keys[0x11] || _keys[0xA2] || _keys[0xA3]) ? ModifierKeys.Control : ModifierKeys.None) |
        ((_keys[0x12] || _keys[0xA4] || _keys[0xA5]) ? ModifierKeys.Alt : ModifierKeys.None) |
        ((_keys[0x5B] || _keys[0x5C]) ? ModifierKeys.Windows : ModifierKeys.None);
}
