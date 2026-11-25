using System.Windows.Input;

namespace MicMute;

public sealed record AppSettings
{
    public string SelectedDeviceId { get; init; } = string.Empty;

    public Key Hotkey { get; init; } = Key.F1;

    public ModifierKeys HotkeyModifiers { get; init; } = ModifierKeys.None;

    public bool RunOnStartup { get; init; }

    public bool StartMinimized { get; init; }

    public bool EnableOsd { get; init; } = true;

    public double OsdDuration { get; init; } = 1.5;

    public string OsdPosition { get; init; } = "Center";

    public bool LightMode { get; init; }

    public string CustomDataPath { get; init; } = string.Empty;

    public bool UsePortableMode { get; init; }

    public bool RunAsAdmin { get; init; }

    public bool PlaySoundFeedback { get; init; }
    public int SoundVolume { get; init; } = 100;
    public bool SoundVolumeCustomized { get; init; }

    public bool HoldKeyMode { get; init; }
    public bool SyncAllMicrophones { get; init; }
    public bool LockMuteState { get; init; }
    public int StartupMuteMode { get; init; }
}