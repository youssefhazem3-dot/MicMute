using System;

namespace MicMute;

/// <summary>Accepts only notifications that still match the endpoint's current state.</summary>
internal sealed class AudioMuteState
{
    public bool? Current { get; private set; }

    public bool RecordLocalChange(bool muted)
    {
        if (Current == muted) return false;
        Current = muted;
        return true;
    }

    public bool TryApplyNotification(bool notifiedMuted, Func<bool> readCurrentMute, out bool currentMuted)
    {
        currentMuted = readCurrentMute();
        if (currentMuted != notifiedMuted || Current == currentMuted) return false;
        Current = currentMuted;
        return true;
    }

    public void Reset() => Current = null;
}
