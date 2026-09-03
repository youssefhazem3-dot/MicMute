using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Interop;

namespace MicMute.Tests;

static class HotkeyCases
{
    private const int V = 0x56;

    public static void Run(Action<string, Action> test)
    {
        test("hotkeys: configured shortcuts remain available to other Windows clients", SharesShortcutWithoutReservingIt);
        test("hotkeys: one activation across hold, repeats and engines", () =>
        {
            var state = Bound();
            Check.True(state.Observe(HotkeySource.Hook, V, true, ModifierKeys.None, 10), "first press");
            for (uint time = 10; time < 1000; time += 10)
            {
                Check.True(!state.Observe(HotkeySource.RawInput, V, true, ModifierKeys.None, time), "raw repeat");
                Check.True(!state.Observe(HotkeySource.Hook, V, true, ModifierKeys.None, time), "hook repeat");
                Check.True(!state.Poll(V, true, ModifierKeys.None, time), "poll duplicate");
            }
        });
        test("hotkeys: rapid physical presses are not debounced", () =>
        {
            var state = Bound();
            for (uint time = 10; time < 110; time += 10)
            {
                Check.True(state.Observe(HotkeySource.Hook, V, true, ModifierKeys.None, time), "distinct down");
                state.Observe(HotkeySource.Hook, V, false, ModifierKeys.None, time + 1);
            }
        });
        test("hotkeys: concurrent engines activate once", () =>
        {
            var state = Bound();
            int count = 0;
            Parallel.For(0, 300, i =>
            {
                bool activated = i % 3 == 0
                    ? state.Poll(V, true, ModifierKeys.None, 10)
                    : state.Observe(i % 3 == 1 ? HotkeySource.Hook : HotkeySource.RawInput, V, true, ModifierKeys.None, 10);
                if (activated) Interlocked.Increment(ref count);
            });
            Check.Equal(1, count);
        });
        test("hotkeys: exact modifier matching for every engine", () =>
        {
            foreach (ModifierKeys required in Enum.GetValues<ModifierKeys>())
            {
                for (int actual = 0; actual < 16; actual++)
                {
                    foreach (HotkeySource source in Enum.GetValues<HotkeySource>())
                    {
                        var state = Bound(required);
                        Check.Equal(actual == (int)required, state.Observe(source, V, true, (ModifierKeys)actual, 10));
                    }
                    var polled = Bound(required);
                    Check.Equal(actual == (int)required, polled.Poll(V, true, (ModifierKeys)actual, 10));
                }
            }
        });
        test("hotkeys: releasing Ctrl during held Ctrl+V cannot activate V", () =>
        {
            var state = Bound();
            Check.True(!state.Observe(HotkeySource.Hook, V, true, ModifierKeys.Control, 10), "Ctrl+V rejected");
            Check.True(!state.Observe(HotkeySource.RawInput, V, true, ModifierKeys.None, 11), "mismatch consumes hold");
            Check.True(!state.Poll(V, true, ModifierKeys.None, 12), "modifier release is not a new V press");
            state.Observe(HotkeySource.Hook, V, false, ModifierKeys.None, 20);
            Check.True(state.Observe(HotkeySource.Hook, V, true, ModifierKeys.None, 30), "next plain V accepted");
        });
        test("hotkeys: stale engine edges cannot reopen or release a later hold", () =>
        {
            var state = Bound();
            Check.True(state.Observe(HotkeySource.Hook, V, true, ModifierKeys.None, 10), "first down");
            state.Observe(HotkeySource.Hook, V, false, ModifierKeys.None, 20);
            Check.True(!state.Observe(HotkeySource.RawInput, V, true, ModifierKeys.None, 10), "delayed down after release");
            Check.True(state.Observe(HotkeySource.Hook, V, true, ModifierKeys.None, 30), "next press");
            state.Observe(HotkeySource.RawInput, V, false, ModifierKeys.None, 20);
            Check.True(!state.Observe(HotkeySource.RawInput, V, true, ModifierKeys.None, 30), "old up cannot release current hold");
        });
        test("hotkeys: poll cannot see stale up before hook key state updates", () =>
        {
            var state = Bound();
            state.Observe(HotkeySource.Hook, V, true, ModifierKeys.None, 10);
            state.Poll(V, false, ModifierKeys.None, 11);
            Check.True(!state.Observe(HotkeySource.RawInput, V, true, ModifierKeys.None, 10), "stale poll up cannot reopen hold");
            Check.True(!state.Poll(V, true, ModifierKeys.None, 12), "first physical sample confirms hold");
            state.Poll(V, false, ModifierKeys.None, 20);
            Check.True(!state.Observe(HotkeySource.RawInput, V, true, ModifierKeys.None, 15), "delayed repeat before observed release");
            Check.True(state.Poll(V, true, ModifierKeys.None, 30), "poll fallback next press");
        });
        test("hotkeys: unregister and rebind fence queued old input", () =>
        {
            var state = Bound();
            state.Unregister();
            Check.True(!state.Observe(HotkeySource.Hook, V, true, ModifierKeys.None, 10), "disabled hook");
            Check.True(!state.Poll(V, true, ModifierKeys.None, 10), "disabled poll");
            state.Register(V, ModifierKeys.Control, false, 50);
            Check.True(!state.Observe(HotkeySource.RawInput, V, true, ModifierKeys.Control, 40), "old queued event");
            Check.True(state.Observe(HotkeySource.Hook, V, true, ModifierKeys.Control, 60), "new binding");
            state.Register(0x57, ModifierKeys.None, true, 70);
            Check.True(!state.Poll(0x57, true, ModifierKeys.None, 80), "recording key remains held");
            state.Poll(0x57, false, ModifierKeys.None, 90);
            Check.True(state.Poll(0x57, true, ModifierKeys.None, 100), "new key after release");
            Check.True(!state.Poll(V, true, ModifierKeys.Control, 110), "old binding disabled");
        });
        test("hotkeys: poll cannot reopen after native up before async state updates", () =>
        {
            var state = Bound();
            state.Observe(HotkeySource.Hook, V, true, ModifierKeys.None, 10);
            state.Poll(V, true, ModifierKeys.None, 11);
            state.Observe(HotkeySource.Hook, V, false, ModifierKeys.None, 20);
            Check.True(!state.Poll(V, true, ModifierKeys.None, 21), "stale physical down after native up");
            state.Poll(V, false, ModifierKeys.None, 22);
            Check.True(state.Poll(V, true, ModifierKeys.None, 30), "new physical down");
        });
        test("hotkeys: same-tick native presses retain source ordering", () =>
        {
            var state = Bound();
            Check.True(state.Observe(HotkeySource.Hook, V, true, ModifierKeys.None, 10), "first down");
            state.Observe(HotkeySource.Hook, V, false, ModifierKeys.None, 10);
            Check.True(!state.Observe(HotkeySource.RawInput, V, true, ModifierKeys.None, 10), "same-tick delayed duplicate");
            Check.True(state.Observe(HotkeySource.Hook, V, true, ModifierKeys.None, 10), "same-source next down");
            state.Observe(HotkeySource.RawInput, V, false, ModifierKeys.None, 10);
            Check.True(!state.Observe(HotkeySource.Hook, V, true, ModifierKeys.None, 11), "old same-tick up did not release hold");
        });
        test("hotkeys: native timestamp wrap remains ordered", () =>
        {
            var state = new HotkeyState();
            state.Register(V, ModifierKeys.None, false, uint.MaxValue - 10);
            Check.True(state.Observe(HotkeySource.Hook, V, true, ModifierKeys.None, uint.MaxValue - 5), "before wrap");
            state.Observe(HotkeySource.Hook, V, false, ModifierKeys.None, 2);
            Check.True(!state.Observe(HotkeySource.RawInput, V, true, ModifierKeys.None, uint.MaxValue - 5), "old before-wrap duplicate");
            Check.True(state.Observe(HotkeySource.Hook, V, true, ModifierKeys.None, 5), "after wrap");
        });
        test("hotkeys: malformed raw input never reads stale keyboard bytes", RawPackets);
    }

    private static HotkeyState Bound(ModifierKeys modifiers = ModifierKeys.None)
    {
        var state = new HotkeyState();
        state.Register(V, modifiers, false, 0);
        return state;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr window, int id);

    private static void SharesShortcutWithoutReservingIt()
    {
        // No keys are injected and no clipboard/microphone/settings are touched.
        // A message-only test window temporarily reserves an unused Ctrl+Shift+F13..F24 chord.
        using var window = new HwndSource(new HwndSourceParameters("MicMute shortcut coexistence regression")
        {
            ParentWindow = new IntPtr(-3), WindowStyle = 0, Width = 1, Height = 1
        });
        const int otherClientId = 19543;
        const uint modifiers = 2 | 4;
        uint key = 0;
        for (uint candidate = 0x7c; candidate <= 0x87; candidate++)
        {
            if (RegisterHotKey(window.Handle, otherClientId, modifiers, candidate)) { key = candidate; break; }
        }
        Check.True(key != 0, "could not find an unused diagnostic shortcut");
        try
        {
            using var observer = new HotkeyManager(window.Handle);
            Check.True(observer.Register(KeyInterop.KeyFromVirtualKey((int)key), ModifierKeys.Control | ModifierKeys.Shift),
                "MicMute must observe a shortcut even when another client already uses it");
            Check.True(UnregisterHotKey(window.Handle, otherClientId), "release diagnostic reservation");
            Check.True(RegisterHotKey(window.Handle, otherClientId, modifiers, key),
                "MicMute must not reserve the configured shortcut away from other clients");
        }
        finally { UnregisterHotKey(window.Handle, otherClientId); }
    }

    private static void RawPackets()
    {
        int header = 8 + 2 * IntPtr.Size;
        uint packetSize = (uint)(header + 16);
        IntPtr memory = Marshal.AllocHGlobal(128);
        try
        {
            for (int i = 0; i < 128; i++) Marshal.WriteByte(memory, i, 0);
            Marshal.WriteInt32(memory, 0, 1);
            Marshal.WriteInt32(memory, 4, (int)packetSize);
            Marshal.WriteInt16(memory, header + 6, V);
            Marshal.WriteInt32(memory, header + 8, 0x100);
            Check.True(RawKeyboardPacket.TryRead(memory, packetSize, 128, 128, out int vk, out bool down), "valid down");
            Check.Equal(V, vk);
            Check.True(down, "make event");
            foreach (uint read in new[] { 0u, uint.MaxValue, (uint)header - 1, packetSize - 1, 129u })
                Check.True(!RawKeyboardPacket.TryRead(memory, read, 128, 128, out _, out _), "invalid read length");
            Check.True(!RawKeyboardPacket.TryRead(memory, packetSize, packetSize - 1, 128, out _, out _), "size mismatch");
            Marshal.WriteInt32(memory, 4, 128);
            Check.True(!RawKeyboardPacket.TryRead(memory, packetSize, 128, 128, out _, out _), "truncated declared packet");
            Marshal.WriteInt32(memory, 4, (int)packetSize);
            Marshal.WriteInt16(memory, header + 2, 1);
            Marshal.WriteInt32(memory, header + 8, 0x101);
            Check.True(RawKeyboardPacket.TryRead(memory, packetSize, 128, 128, out _, out down) && !down, "valid release");
            Marshal.WriteInt32(memory, 0, 0);
            Check.True(!RawKeyboardPacket.TryRead(memory, packetSize, 128, 128, out _, out _), "mouse rejected");
        }
        finally { Marshal.FreeHGlobal(memory); }
    }
}
