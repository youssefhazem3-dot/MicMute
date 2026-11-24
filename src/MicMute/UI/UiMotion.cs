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
    internal static readonly Duration OsdEnter = new(TimeSpan.FromMilliseconds(175));
    internal static readonly Duration Standard = new(TimeSpan.FromMilliseconds(220));
    internal static readonly Duration Emphasized = new(TimeSpan.FromMilliseconds(250));
    internal static readonly Duration Exit = new(TimeSpan.FromMilliseconds(125));
    internal static readonly Duration OsdExit = new(TimeSpan.FromMilliseconds(155));

    /// <summary>Balanced exponential deceleration with smooth initial ramp and clean settle (no subpixel tail crawl).</summary>
    internal static readonly IEasingFunction Decelerate = CreateFrozen(new ExponentialEase
    {
        Exponent = 3.2,
        EasingMode = EasingMode.EaseOut
    });

    /// <summary>Clean accelerating exit curve for dismissals and collapses.</summary>
    internal static readonly IEasingFunction Accelerate = CreateFrozen(new CubicEase
    {
        EasingMode = EasingMode.EaseIn
    });

    /// <summary>Subtle tactile spring overshoot for micro-interactions.</summary>
    internal static readonly IEasingFunction SpringGentle = CreateFrozen(new BackEase
    {
        Amplitude = 0.18,
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

    private static double GetDpiScale(UIElement element, Visual? dpiReference)
    {
        try
        {
            Visual target = dpiReference ?? element;
            DpiScale dpi = VisualTreeHelper.GetDpi(target);
            return Math.Clamp(Math.Max(dpi.DpiScaleX, dpi.DpiScaleY), 1.0, 2.5);
        }
        catch
        {
            return 1.0;
        }
    }

    /// <summary>
    /// Attaches a persistent DPI-matched GPU BitmapCache (with EnableClearType=false so layered
    /// AllowsTransparency surfaces never fall back to software alpha-ClearType or snap glyph weights).
    /// Ideal for static shadow layers and the circular OSD panel.
    /// </summary>
    internal static void EnablePersistentCache(UIElement? element, Visual? dpiReference = null)
    {
        if (element == null) return;
        double scale = GetDpiScale(element, dpiReference);

        if (element.CacheMode is BitmapCache existing
            && !existing.EnableClearType
            && Math.Abs(existing.RenderAtScale - scale) < 0.05)
        {
            return;
        }

        element.CacheMode = new BitmapCache(scale)
        {
            EnableClearType = false,
            SnapsToDevicePixels = false
        };
    }

    /// <summary>
    /// Temporarily caches an animated whole-window container into an alpha-safe, DPI-matched GPU bitmap
    /// during whole-window transitions. Pair with <see cref="ClearTransientCache"/> when finished.
    /// </summary>
    internal static void EnableTransientCache(UIElement? element, Visual? dpiReference = null)
    {
        EnablePersistentCache(element, dpiReference);
    }

    internal static void ClearTransientCache(UIElement? element)
    {
        if (element != null && element.CacheMode != null)
        {
            element.CacheMode = null;
        }
    }
}
