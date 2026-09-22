using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Windows.Threading;

namespace MicMute;

public readonly record struct PixelSize(int Width, int Height);

public readonly record struct PixelRect(int Left, int Top, int Width, int Height);

public readonly record struct DipSize(double Width, double Height);

public readonly record struct DipRect(double Left, double Top, double Width, double Height);

public static class UiBehavior
{
    public const double MinimumOsdDuration = 0.1;
    public const double MaximumOsdDuration = 30.0;
    public const int NotifyIconTooltipMaximumLength = 127;
    public const double DefaultAdaptiveVerticalMargin = 24.0;
    public const double MinimumWindowHeight = 260.0;

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

    public static double CalculateAdaptiveMaxHeight(double workAreaHeight, double verticalMargin = DefaultAdaptiveVerticalMargin, double minimumHeight = MinimumWindowHeight)
    {
        if (!double.IsFinite(workAreaHeight) || workAreaHeight <= 0)
        {
            return 750.0;
        }
        return Math.Max(minimumHeight, workAreaHeight - verticalMargin);
    }

    public static DipRect CalculateCenteredWindowBounds(DipRect workArea, double windowWidth, double windowHeight, double verticalMargin = DefaultAdaptiveVerticalMargin)
    {
        double width = Math.Max(200.0, Math.Min(windowWidth, workArea.Width));
        double maxHeight = CalculateAdaptiveMaxHeight(workArea.Height, verticalMargin);
        double height = Math.Min(windowHeight, maxHeight);

        double left = workArea.Left + Math.Max(0.0, (workArea.Width - width) / 2.0);
        double top = workArea.Top + Math.Max(verticalMargin / 2.0, (workArea.Height - height) / 2.0);

        if (top + height > workArea.Top + workArea.Height - (verticalMargin / 2.0))
        {
            top = Math.Max(workArea.Top + (verticalMargin / 2.0), workArea.Top + workArea.Height - height - (verticalMargin / 2.0));
        }
        if (top < workArea.Top + (verticalMargin / 2.0))
        {
            top = workArea.Top + (verticalMargin / 2.0);
        }

        return new DipRect(left, top, width, height);
    }

    public static DipRect ClampWindowBoundsToWorkArea(DipRect workArea, DipRect currentBounds, double verticalMargin = DefaultAdaptiveVerticalMargin)
    {
        double width = Math.Max(200.0, Math.Min(currentBounds.Width, workArea.Width));
        double maxHeight = CalculateAdaptiveMaxHeight(workArea.Height, verticalMargin);
        double height = Math.Min(currentBounds.Height, maxHeight);

        if (!double.IsFinite(currentBounds.Left) || !double.IsFinite(currentBounds.Top))
        {
            return CalculateCenteredWindowBounds(workArea, width, height, verticalMargin);
        }

        double halfMargin = verticalMargin / 2.0;
        double minTop = workArea.Top + halfMargin;
        double maxTop = workArea.Top + workArea.Height - height - halfMargin;
        double top = maxTop >= minTop ? Math.Clamp(currentBounds.Top, minTop, maxTop) : minTop;

        double minLeft = workArea.Left;
        double maxLeft = workArea.Left + workArea.Width - width;
        double left = maxLeft >= minLeft ? Math.Clamp(currentBounds.Left, minLeft, maxLeft) : minLeft;

        return new DipRect(left, top, width, height);
    }

    public static bool ShouldStartMinimized(bool storedPreference, string[]? arguments)
    {
        if (HasArgument(arguments, "--show") || IsDiagnosticMode(arguments))
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

    public static bool IsDiagnosticMode(string[]? arguments)
    {
        return HasArgument(arguments, "--diagnostic")
            || HasArgument(arguments, "--diag")
            || HasArgument(arguments, "-d")
            || string.Equals(Environment.GetEnvironmentVariable("MICMUTE_DIAGNOSTIC"), "1", StringComparison.OrdinalIgnoreCase);
    }

    public static bool HasArgument(string[]? arguments, string expected)
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
    private readonly object _sync = new();
    private readonly Dispatcher _dispatcher;
    private readonly RefreshGeneration _generation = new();
    private readonly DispatcherTimer _timer;
    private Action? _action;
    private long _scheduledGeneration;
    private TimeSpan _delay;
    private bool _updateQueued;

    public DispatcherDebouncer(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        _timer = new DispatcherTimer(DispatcherPriority.Background, dispatcher);
        _timer.Tick += OnTick;
    }

    public void Schedule(TimeSpan delay, Action action)
    {
        lock (_sync)
        {
            long generation = _generation.Next();
            if (generation == 0 || _dispatcher.HasShutdownStarted) return;
            _scheduledGeneration = generation;
            _action = action;
            _delay = delay;
            if (_updateQueued) return;
            _updateQueued = true;
            try { _dispatcher.BeginInvoke(new Action(ApplyPendingSchedule)); }
            catch (InvalidOperationException)
            {
                _updateQueued = false;
                _action = null;
            }
        }
    }

    private void ApplyPendingSchedule()
    {
        lock (_sync)
        {
            _updateQueued = false;
            if (!_generation.IsCurrent(_scheduledGeneration) || _action == null) return;
            _timer.Stop();
            _timer.Interval = _delay;
            _timer.Start();
        }
    }

    private void OnTick(object? sender, EventArgs e)
    {
        Action? action;
        lock (_sync)
        {
            _timer.Stop();
            // A newer request must receive its full delay before it runs.
            if (_updateQueued || !_generation.IsCurrent(_scheduledGeneration)) return;
            action = _action;
            _action = null;
        }
        action?.Invoke();
    }

    public void Cancel()
    {
        lock (_sync)
        {
            _generation.Next();
            _timer.Stop();
            _action = null;
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            _generation.Dispose();
            // The owning window disposes this on its dispatcher.
            _timer.Stop();
            _timer.Tick -= OnTick;
            _action = null;
        }
    }
}
