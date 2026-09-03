using System;
using System.Runtime.InteropServices;

namespace MicMute;

internal static class RawKeyboardPacket
{
    public static int HeaderSize => 8 + 2 * IntPtr.Size;

    public static bool TryRead(IntPtr buffer, uint bytesRead, uint reportedSize, int capacity, out int vk, out bool isDown)
    {
        vk = 0;
        isDown = false;
        int headerSize = HeaderSize;
        // Validate native return values before dereferencing even the header. UINT_MAX is
        // GetRawInputData's failure sentinel, not a positive successful byte count.
        if (buffer == IntPtr.Zero || capacity < headerSize + 16 || bytesRead == uint.MaxValue ||
            bytesRead < headerSize + 16 || bytesRead > capacity || reportedSize > capacity ||
            reportedSize < bytesRead) return false;
        if (Marshal.ReadInt32(buffer, 0) != 1 || unchecked((uint)Marshal.ReadInt32(buffer, 4)) != bytesRead)
            return false;

        ushort flags = unchecked((ushort)Marshal.ReadInt16(buffer, headerSize + 2));
        int key = unchecked((ushort)Marshal.ReadInt16(buffer, headerSize + 6));
        int message = Marshal.ReadInt32(buffer, headerSize + 8);
        bool down = (flags & 1) == 0;
        if (key == 0 || key >= 255 || (down ? message != 0x100 && message != 0x104 : message != 0x101 && message != 0x105))
            return false;
        // Raw keyboard packets use generic modifier VKeys; normalize them to hook VKeys.
        if (key == 0x10) key = Marshal.ReadInt16(buffer, headerSize) == 0x36 ? 0xA1 : 0xA0;
        else if (key == 0x11) key = (flags & 2) != 0 ? 0xA3 : 0xA2;
        else if (key == 0x12) key = (flags & 2) != 0 ? 0xA5 : 0xA4;
        vk = key;
        isDown = down;
        return true;
    }
}
