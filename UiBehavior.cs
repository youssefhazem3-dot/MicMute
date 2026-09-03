using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Windows.Threading;

namespace MicMute;

public readonly record struct PixelSize(int Width, int Height);

public readonly record struct PixelRect(int Left, int Top, int Width, int Height);

public static class UiBehavior
{
    public const double MinimumOsdDuration = 0.1;
    public const double MaximumOsdDuration = 30.0;
    public const int NotifyIconTooltipMaximumLength = 127;

    public static bool TryParseOsdDuration(string? text, CultureInfo culture, out double duration)
    {
        duration = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        const NumberStyles styles = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite;
        string candidate = text.Trim();
        if (!double.TryParse(candidate, styles, culture, out duration)
            && !double.TryParse(candidate, styles, CultureInfo.InvariantCulture, out duration))
        {
            return false;
        }

        return double.IsFinite(duration) && duration >= MinimumOsdDuration && duration <= MaximumOsdDuration;
    }

    public static string FormatOsdDuration(double duration, CultureInfo culture)
    {
        double safeDuration = double.IsFinite(duration)
            ? Math.Clamp(duration, MinimumOsdDuration, MaximumOsdDuration)
            : MinimumOsdDuration;
        return safeDuration.ToString("F1", culture);
    }

    public static string LimitTooltip(string? tooltip)
    {
        if (string.IsNullOrEmpty(tooltip))
        {
            return string.Empty;
        }
        return tooltip.Length <= NotifyIconTooltipMaximumLength
            ? tooltip
            : tooltip.Substring(0, NotifyIconTooltipMaximumLength);
    }

    public static PixelRect CenterInPixels(PixelRect screenBounds, PixelSize windowSize)
    {
        int width = Math.Max(0, windowSize.Width);
        int height = Math.Max(0, windowSize.Height);
        return new PixelRect(
            screenBounds.Left + (screenBounds.Width - width) / 2,
            screenBounds.Top + (screenBounds.Height - height) / 2,
            width,
            height);
    }

    public static bool ShouldStartMinimized(bool storedPreference, string[]? arguments)
    {
        if (HasArgument(arguments, "--show"))
        {
            return false;
        }
        return HasArgument(arguments, "--minimized") || storedPreference;
    }

    public static string BuildRestartArguments(int parentProcessId, bool showWindow)
    {
        if (parentProcessId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(parentProcessId));
        }
        return "--wait-for-parent " + parentProcessId.ToString(CultureInfo.InvariantCulture) + (showWindow ? " --show" : " --minimized");
    }

    public static bool TryGetParentProcessId(string[]? arguments, out int processId)
    {
        processId = 0;
        if (arguments == null)
        {
            return false;
        }
        for (int index = 0; index + 1 < arguments.Length; index++)
        {
            if (string.Equals(arguments[index], "--wait-for-parent", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(arguments[index + 1], NumberStyles.None, CultureInfo.InvariantCulture, out int parsed)
                && parsed > 0)
            {
                processId = parsed;
                return true;
            }
        }
        return false;
    }

    public static bool WaitForParentExit(int parentProcessId, TimeSpan timeout)
    {
        if (parentProcessId <= 0)
        {
            return true;
        }
        try
        {
            using Process parent = Process.GetProcessById(parentProcessId);
            return parent.HasExited || parent.WaitForExit((int)Math.Max(0, timeout.TotalMilliseconds));
        }
        catch (ArgumentException)
        {
            return true;
        }
        catch (InvalidOperationException)
        {
            return true;
        }
    }

    private static bool HasArgument(string[]? arguments, string expected)
    {
        if (arguments == null)
        {
            return false;
        }
        foreach (string argument in arguments)
        {
            if (string.Equals(argument, expected, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}

public sealed class RefreshGeneration : IDisposable
{
    private long _generation;
    private int _disposed;

    public long Next()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            return 0;
        }
        return Interlocked.Increment(ref _generation);
    }

    public bool IsCurrent(long generation)
    {
        return generation != 0
            && Volatile.Read(ref _disposed) == 0
            && Interlocked.Read(ref _generation) == generation;
    }

    public void Dispose()
    {
        Volatile.Write(ref _disposed, 1);
        Interlocked.Increment(ref _generation);
    }
}

internal sealed class DispatcherDebouncer : IDisposable
{
    private readonly Dispatcher _dispatcher;
    private readonly RefreshGeneration _generation = new();
    private readonly DispatcherTimer _timer;
    private Action? _action;
    private long _scheduledGeneration;

    public DispatcherDebouncer(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        _timer = new DispatcherTimer(DispatcherPriority.Background, dispatcher);
        _timer.Tick += OnTick;
    }

    public void Schedule(TimeSpan delay, Action action)
    {
        long generation = _generation.Next();
        if (generation == 0 || _dispatcher.HasShutdownStarted) return;
        try
        {
            _dispatcher.BeginInvoke(new Action(() =>
            {
                if (!_generation.IsCurrent(generation)) return;
                _timer.Stop();
                _scheduledGeneration = generation;
                _action = action;
                _timer.Interval = delay;
                _timer.Start();
            }));
        }
        catch (InvalidOperationException) { }
    }

    private void OnTick(object? sender, EventArgs e)
    {
        _timer.Stop();
        Action? action = _action;
        _action = null;
        if (_generation.IsCurrent(_scheduledGeneration)) action?.Invoke();
    }

    public void Dispose()
    {
        _generation.Dispose();
        // The owning window disposes this on its dispatcher.
        _timer.Stop();
        _timer.Tick -= OnTick;
        _action = null;
    }
}
