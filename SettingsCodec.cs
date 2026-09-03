using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace MicMute;

/// <summary>JSON compatibility boundary for persisted application settings.</summary>
public static class SettingsCodec
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
    };

    private static readonly JsonDocumentOptions DocumentOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    private static readonly Regex LegacyNamedDuration = new(
        "(\\\"OsdDuration\\\"\\s*:\\s*)(NaN|Infinity|-Infinity)(?=\\s*[,}])",
        RegexOptions.CultureInvariant);

    public static string Serialize(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return JsonSerializer.Serialize(Normalize(settings), SerializerOptions);
    }

    public static AppSettings Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new AppSettings();

        try
        {
            using JsonDocument document = JsonDocument.Parse(LegacyNamedDuration.Replace(json, "$1\"$2\""), DocumentOptions);
            if (document.RootElement.ValueKind != JsonValueKind.Object) return new AppSettings();

            JsonElement root = document.RootElement;
            AppSettings defaults = new();
            return Normalize(new AppSettings
            {
                SelectedDeviceId = RepairEndpointId(GetString(root, "SelectedDeviceId", defaults.SelectedDeviceId)),
                Hotkey = GetEnum(root, "Hotkey", defaults.Hotkey),
                HotkeyModifiers = GetEnum(root, "HotkeyModifiers", defaults.HotkeyModifiers),
                RunOnStartup = GetBoolean(root, "RunOnStartup", defaults.RunOnStartup),
                StartMinimized = GetBoolean(root, "StartMinimized", defaults.StartMinimized),
                EnableOsd = GetBoolean(root, "EnableOsd", defaults.EnableOsd),
                OsdDuration = GetDouble(root, "OsdDuration", defaults.OsdDuration),
                LightMode = GetBoolean(root, "LightMode", defaults.LightMode),
                CustomDataPath = GetString(root, "CustomDataPath", defaults.CustomDataPath),
                UsePortableMode = GetBoolean(root, "UsePortableMode", defaults.UsePortableMode),
                RunAsAdmin = GetBoolean(root, "RunAsAdmin", defaults.RunAsAdmin),
                PlaySoundFeedback = GetBoolean(root, "PlaySoundFeedback", defaults.PlaySoundFeedback)
            });
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
    }

    public static AppSettings Normalize(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        double duration = settings.OsdDuration;
        duration = !double.IsFinite(duration) ? 1.5 : Math.Clamp(duration, 0.1, 30.0);
        return settings with
        {
            SelectedDeviceId = RepairEndpointId(settings.SelectedDeviceId ?? string.Empty),
            CustomDataPath = settings.CustomDataPath ?? string.Empty,
            Hotkey = Enum.IsDefined(settings.Hotkey) && settings.Hotkey != Key.None ? settings.Hotkey : Key.F1,
            HotkeyModifiers = ((int)settings.HotkeyModifiers & ~15) == 0 ? settings.HotkeyModifiers : ModifierKeys.None,
            OsdDuration = duration
        };
    }

    private static string RepairEndpointId(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value ?? string.Empty;

        const string prefix = "0.0.1.";
        bool isEndpoint = (value.StartsWith(prefix, StringComparison.Ordinal) || value.StartsWith("{" + prefix, StringComparison.Ordinal)) &&
                          value.Contains("}.{", StringComparison.Ordinal);
        if (!isEndpoint) return value;
        if (!value.StartsWith("{", StringComparison.Ordinal)) value = "{" + value;
        return value.EndsWith("}", StringComparison.Ordinal) ? value : value + "}";
    }

    private static bool GetBoolean(JsonElement root, string property, bool fallback)
    {
        if (!TryGetProperty(root, property, out JsonElement value)) return fallback;
        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(value.GetString(), out bool parsed) => parsed,
            _ => fallback
        };
    }

    private static string GetString(JsonElement root, string property, string fallback) =>
        TryGetProperty(root, property, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : fallback;

    private static double GetDouble(JsonElement root, string property, double fallback)
    {
        if (!TryGetProperty(root, property, out JsonElement value)) return fallback;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out double number)) return number;
        return value.ValueKind == JsonValueKind.String &&
               double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed)
            ? parsed
            : fallback;
    }

    private static TEnum GetEnum<TEnum>(JsonElement root, string property, TEnum fallback)
        where TEnum : struct, Enum
    {
        if (!TryGetProperty(root, property, out JsonElement value)) return fallback;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int number))
            return (TEnum)Enum.ToObject(typeof(TEnum), number);
        return value.ValueKind == JsonValueKind.String && Enum.TryParse(value.GetString(), true, out TEnum parsed)
            ? parsed
            : fallback;
    }

    private static bool TryGetProperty(JsonElement root, string name, out JsonElement value)
    {
        foreach (JsonProperty property in root.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
