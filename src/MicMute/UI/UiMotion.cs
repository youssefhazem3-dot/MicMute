using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MicMute;

/// <summary>
/// Centralized motion language (durations, frozen easing curves, and DPI-aware transient GPU caching)
/// to ensure smooth, consistent, jitter-free animations across all MicMute windows and popups.
/// </summary>
internal static class UiMotion
{
    internal static readonly Duration Fast = new(TimeSpan.FromMilliseconds(160));
    internal static readonly Duration Standard = new(TimeSpan.FromMilliseconds(220));
    internal static readonly Duration Emphasized = new(TimeSpan.FromMilliseconds(260));
    internal static readonly Duration Exit = new(TimeSpan.FromMilliseconds(120));
    internal static readonly Duration OsdExit = new(TimeSpan.FromMilliseconds(180));

    /// <summary>Silky fast-response exponential deceleration (Fluent 2 / Apple HIG feel).</summary>
    internal static readonly IEasingFunction Decelerate = CreateFrozen(new ExponentialEase
    {
        Exponent = 4.5,
        EasingMode = EasingMode.EaseOut
    });

    /// <summary>Clean accelerating exit curve for dismissals and collapses.</summary>
    internal static readonly IEasingFunction Accelerate = CreateFrozen(new CubicEase
    {
        EasingMode = EasingMode.EaseIn
    });

    /// <summary>Subtle tactile spring overshoot for hero feedback and OSD pop-in.</summary>
    internal static readonly IEasingFunction SpringGentle = CreateFrozen(new BackEase
    {
        Amplitude = 0.25,
        EasingMode = EasingMode.EaseOut
    });

    private static T CreateFrozen<T>(T freezable) where T : Freezable
    {
        if (freezable.CanFreeze)
        {
            freezable.Freeze();
        }
        return freezable;
    }

    /// <summary>
    /// Temporarily caches an animated container into a DPI-matched GPU bitmap so AllowsTransparency
    /// windows and DropShadowEffect subtrees animate without per-frame shader re-rasterization.
    /// Always pair with <see cref="ClearTransientCache"/> when the animation finishes.
    /// </summary>
    internal static void EnableTransientCache(UIElement? element, Visual? dpiReference = null)
    {
        if (element == null) return;
        double scale = 1.0;
        try
        {
            Visual? target = dpiReference ?? element;
            DpiScale dpi = VisualTreeHelper.GetDpi(target);
            scale = Math.Clamp(Math.Max(dpi.DpiScaleX, dpi.DpiScaleY), 1.0, 2.5);
        }
        catch
        {
            scale = 1.0;
        }

        if (element.CacheMode is BitmapCache existing && Math.Abs(existing.RenderAtScale - scale) < 0.05)
        {
            return;
        }

        element.CacheMode = new BitmapCache(scale)
        {
            EnableClearType = true,
            SnapsToDevicePixels = false
        };
    }

    internal static void ClearTransientCache(UIElement? element)
    {
        if (element != null && element.CacheMode != null)
        {
            element.CacheMode = null;
        }
    }
}
