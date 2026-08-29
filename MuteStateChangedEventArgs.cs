using System;

namespace MicMute;

public class MuteStateChangedEventArgs : EventArgs
{
	public bool IsMuted { get; }

	public bool ShowOsd { get; }

	public MuteStateChangedEventArgs(bool isMuted, bool showOsd)
	{
		IsMuted = isMuted;
		ShowOsd = showOsd;
	}
}
