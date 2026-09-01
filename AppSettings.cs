using System.Windows.Input;

namespace MicMute;

public sealed record AppSettings
{
    public string SelectedDeviceId { get; init; } = string.Empty;

    public Key Hotkey { get; init; } = Key.F1;

    public ModifierKeys HotkeyModifiers { get; init; } = ModifierKeys.None;

    public bool RunOnStartup { get; init; }

    public bool StartMinimized { get; init; } = true;

    public bool EnableOsd { get; init; } = true;

    public double OsdDuration { get; init; } = 1.5;

    public bool LightMode { get; init; }

    public string CustomDataPath { get; init; } = string.Empty;

    public bool UsePortableMode { get; init; }

    public bool RunAsAdmin { get; init; }
}