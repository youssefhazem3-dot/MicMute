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
                return false;
            }
            if (_held) return false;
            _held = true;
            _pollConfirmedDown = false;
            // A mismatching first down consumes this hold too. Releasing a modifier while
            // V remains down must never turn Ctrl+V into a plain-V activation.
            return modifiers == _modifiers;
        }
    }

    public bool Poll(int vk, bool isDown, ModifierKeys modifiers, uint timestamp)
    {
        lock (_sync)
        {
            if (_vk == 0 || vk != _vk || unchecked((int)(timestamp - _lastEventTime)) < 0) return false;
            if (!isDown)
            {
                _pollNeedsRelease = false;
                // WH_KEYBOARD_LL is called before the asynchronous state changes. Its
                // initial stale-up snapshot cannot release a native-owned press.
                if (_held && _pollConfirmedDown)
                {
                    _held = false;
                    _lastEventTime = timestamp;
                    _lastSource = null;
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
            if (_pollNeedsRelease) return false;
            _held = _pollConfirmedDown = true;
            _lastEventTime = timestamp;
            _lastSource = null;
            return modifiers == _modifiers;
        }
    }
}

/// <summary>Tracks modifiers in each ordered native stream, including both sides.</summary>
internal sealed class HotkeyModifiers
{
    private readonly bool[] _keys = new bool[256];

    public void Reset(Func<int, bool> isDown)
    {
        Array.Clear(_keys);
        foreach (int vk in new[] { 0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0x5B, 0x5C })
            _keys[vk] = isDown(vk);
    }

    public void Observe(int vk, bool down)
    {
        if (vk >= 0 && vk < _keys.Length) _keys[vk] = down;
    }

    public ModifierKeys Current =>
        ((_keys[0xA0] || _keys[0xA1]) ? ModifierKeys.Shift : ModifierKeys.None) |
        ((_keys[0xA2] || _keys[0xA3]) ? ModifierKeys.Control : ModifierKeys.None) |
        ((_keys[0xA4] || _keys[0xA5]) ? ModifierKeys.Alt : ModifierKeys.None) |
        ((_keys[0x5B] || _keys[0x5C]) ? ModifierKeys.Windows : ModifierKeys.None);
}
