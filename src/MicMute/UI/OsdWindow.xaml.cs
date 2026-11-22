using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace MicMute;

public partial class OsdWindow : Window
{
    private static OsdWindow? _instance;
    private static CancellationTokenSource? _cts;
    private static int _activeSequenceId;
    private static readonly System.Text.RegularExpressions.Regex XClassRegex =
        new(@"\s+x:Class=""[^""]+""", System.Text.RegularExpressions.RegexOptions.Compiled);

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_FRAMECHANGED = 0x0020;
    private const uint SWP_SHOWWINDOW = 0x0040;
    private const uint SWP_NOOWNERZORDER = 0x0200;

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOPMOST = 0x00000008;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WM_MOUSEACTIVATE = 0x0021;
    private const int MA_NOACTIVATE = 3;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    internal Border borderPanel = null!;
    internal ScaleTransform? panelScaleTransform;
    internal TranslateTransform? panelTranslateTransform;
    internal Grid pathActive = null!;
    internal Grid pathMuted = null!;
    internal TextBlock tbStatus = null!;
    internal DropShadowEffect? osdShadow;
    internal System.Windows.Shapes.Path activeGlyph = null!;
    internal System.Windows.Shapes.Path mutedGlyph = null!;
    internal System.Windows.Shapes.Line muteSlash = null!;
    internal System.Windows.Shapes.Line? muteSlashBg;
    private bool _contentLoaded;
    private bool _lightMode;
    private bool _isMuted;
    private HwndSource? _hwndSource;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
    private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    private static IntPtr GetWindowLong(IntPtr hWnd, int nIndex)
    {
        if (IntPtr.Size == 8)
        {
            return GetWindowLongPtr64(hWnd, nIndex);
        }
        return new IntPtr(GetWindowLong32(hWnd, nIndex));
    }

    private static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        if (IntPtr.Size == 8)
        {
            return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
        }
        return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
    }

    public OsdWindow()
    {
        InitializeComponent();
    }

    public void InitializeComponent()
    {
        if (_contentLoaded) return;
        _contentLoaded = true;

        Window? root = null;
        using (Stream? stream = typeof(OsdWindow).Assembly.GetManifestResourceStream("MicMute.OsdWindow.xaml"))
        {
            if (stream != null)
            {
                using (StreamReader sr = new StreamReader(stream))
                {
                    string xaml = sr.ReadToEnd();
                    xaml = XClassRegex.Replace(xaml, "");
                    root = (Window)XamlReader.Parse(xaml);

                    this.Resources = root.Resources;
                    this.Width = root.Width;
                    this.Height = root.Height;
                    this.WindowStyle = root.WindowStyle;
                    this.AllowsTransparency = root.AllowsTransparency;
                    this.Background = root.Background;
                    this.ResizeMode = root.ResizeMode;
                    this.ShowInTaskbar = root.ShowInTaskbar;
                    this.Topmost = root.Topmost;
                    this.ShowActivated = root.ShowActivated;
                    this.WindowStartupLocation = root.WindowStartupLocation;
                    this.SnapsToDevicePixels = root.SnapsToDevicePixels;
                    this.UseLayoutRounding = root.UseLayoutRounding;
                    this.FontWeight = root.FontWeight;
                    TextOptions.SetTextFormattingMode(this, TextOptions.GetTextFormattingMode(root));
                    TextOptions.SetTextRenderingMode(this, TextOptions.GetTextRenderingMode(root));
                    TextOptions.SetTextHintingMode(this, TextOptions.GetTextHintingMode(root));
                    RenderOptions.SetClearTypeHint(this, RenderOptions.GetClearTypeHint(root));
                    RenderOptions.SetEdgeMode(this, RenderOptions.GetEdgeMode(root));

                    borderPanel = (Border)root.FindName("borderPanel");
                    panelScaleTransform = root.FindName("panelScaleTransform") as ScaleTransform;
                    panelTranslateTransform = root.FindName("panelTranslateTransform") as TranslateTransform;
                    pathActive = (Grid)root.FindName("pathActive");
                    pathMuted = (Grid)root.FindName("pathMuted");
                    tbStatus = (TextBlock)root.FindName("tbStatus");
                    activeGlyph = (System.Windows.Shapes.Path)root.FindName("activeGlyph");
                    mutedGlyph = (System.Windows.Shapes.Path)root.FindName("mutedGlyph");
                    muteSlash = (System.Windows.Shapes.Line)root.FindName("muteSlash");
                    muteSlashBg = root.FindName("muteSlashBg") as System.Windows.Shapes.Line;
                    if (borderPanel != null)
                    {
                        osdShadow = borderPanel.Effect as DropShadowEffect;
                    }

                    var content = root.Content;
                    root.Content = null;
                    root.Resources = new ResourceDictionary();
                    this.Content = content;
                    try { root.Close(); } catch { }
                }
            }
        }
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        IntPtr handle = new WindowInteropHelper(this).Handle;
        long exStyle = GetWindowLong(handle, GWL_EXSTYLE).ToInt64();
        exStyle |= WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_TRANSPARENT;
        SetWindowLong(handle, GWL_EXSTYLE, new IntPtr(exStyle));
        SetWindowPos(handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOOWNERZORDER | SWP_FRAMECHANGED);

        _hwndSource = HwndSource.FromHwnd(handle);
        _hwndSource?.AddHook(WndProc);
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_hwndSource != null)
        {
            _hwndSource.RemoveHook(WndProc);
            _hwndSource = null;
        }
        BeginAnimation(OpacityProperty, null);
        ResetOverlayAnimations();
        if (ReferenceEquals(_instance, this))
        {
            _instance = null;
        }
        base.OnClosed(e);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_MOUSEACTIVATE)
        {
            handled = true;
            return new IntPtr(MA_NOACTIVATE);
        }
        return IntPtr.Zero;
    }

    public static void WarmUp()
    {
        try
        {
            var appDispatcher = System.Windows.Application.Current?.Dispatcher;
            if (appDispatcher != null && !appDispatcher.CheckAccess())
            {
                appDispatcher.Invoke(WarmUp);
                return;
            }
            if (_instance == null)
            {
                _instance = new OsdWindow();
                new WindowInteropHelper(_instance).EnsureHandle();
                _instance.Opacity = 0;
                _instance.Show();
                _instance.Hide();
            }
        }
        catch
        {
            // Expected: may fail during early startup before the Application object is ready.
        }
    }

    public static void ShowOsd(bool isMuted, double durationSeconds)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher ?? _instance?.Dispatcher;
        if (dispatcher != null && !dispatcher.CheckAccess())
        {
            dispatcher.BeginInvoke(() => ShowOsd(isMuted, durationSeconds));
            return;
        }

        int sequenceId = Interlocked.Increment(ref _activeSequenceId);

        CancellationTokenSource? previousCts = _cts;
        _cts = null;
        try
        {
            previousCts?.Cancel();
        }
        catch (Exception ex) { DiagnosticLogger.LogError("Error cancelling previous OSD token", ex); }

        if (_instance == null)
        {
            _instance = new OsdWindow();
        }

        bool wasVisible = _instance.IsVisible && _instance.Opacity > 0.05 && (_instance.borderPanel?.Opacity ?? 1.0) > 0.05;
        bool wasMuted = _instance._isMuted;

        // Cancel any pending opacity animation and restore full window visibility immediately
        _instance.BeginAnimation(OpacityProperty, null);
        _instance.Opacity = 0.96;

        AppSettings settings = SettingsManager.Load();
        _instance.ApplyThemeCore(settings.LightMode, refreshState: false);
        _instance.UpdateState(isMuted);

        if (!wasVisible)
        {
            if (_instance.borderPanel != null)
            {
                _instance.borderPanel.BeginAnimation(UIElement.OpacityProperty, null);
                _instance.borderPanel.Opacity = 0.0;
            }
            if (_instance.panelScaleTransform != null)
            {
                _instance.panelScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                _instance.panelScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                _instance.panelScaleTransform.ScaleX = 0.88;
                _instance.panelScaleTransform.ScaleY = 0.88;
            }
            if (_instance.panelTranslateTransform != null)
            {
                _instance.panelTranslateTransform.BeginAnimation(TranslateTransform.YProperty, null);
                _instance.panelTranslateTransform.Y = 10.0;
            }
        }
        else
        {
            _instance.ResetOverlayAnimations();
        }

        IntPtr handle = new WindowInteropHelper(_instance).EnsureHandle();

        _instance.PositionOnActiveScreenCore(handle, settings.OsdPosition);

        if (!_instance.IsVisible)
        {
            _instance.Show();
        }

        if (!wasVisible)
        {
            _instance.AnimatePanelEntrance(sequenceId);
        }
        else if (wasMuted != isMuted)
        {
            _instance.AnimatePanelPulse(sequenceId);
        }

        double safeDuration = double.IsFinite(durationSeconds)
            ? Math.Clamp(durationSeconds, UiBehavior.MinimumOsdDuration, UiBehavior.MaximumOsdDuration)
            : UiBehavior.MinimumOsdDuration;
        var currentCts = new CancellationTokenSource();
        _cts = currentCts;
        _ = _instance.BeginFadeSequenceAsync(safeDuration, sequenceId, currentCts);
    }

    /// <summary>
    /// Immediately cancels any active fade sequence and hides the OSD window.
    /// Called when the user disables OSD or the application exits.
    /// </summary>
    public static void HideOsd()
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher ?? _instance?.Dispatcher;
        if (dispatcher != null && !dispatcher.CheckAccess())
        {
            dispatcher.BeginInvoke(() => HideOsd());
            return;
        }

        try
        {
            Interlocked.Increment(ref _activeSequenceId);
            if (_cts != null)
            {
                try { _cts.Cancel(); } catch (Exception ex) { DiagnosticLogger.LogError("Error cancelling OSD token", ex); }
                _cts = null;
            }
            if (_instance != null)
            {
                _instance.BeginAnimation(OpacityProperty, null);
                _instance.ResetOverlayAnimations();
                _instance.Opacity = 0.0;
                _instance.Hide();
            }
        }
        catch (Exception ex)
        {
            DiagnosticLogger.LogError("HideOsd error", ex);
        }
    }

    /// <summary>
    /// Closes and releases the shared OSD window instance on application exit.
    /// </summary>
    public static void CloseOsd()
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher ?? _instance?.Dispatcher;
        if (dispatcher != null && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(CloseOsd);
            return;
        }

        HideOsd();
        try
        {
            if (_instance != null)
            {
                _instance.Close();
                _instance = null;
            }
        }
        catch (Exception ex)
        {
            DiagnosticLogger.LogError("CloseOsd error", ex);
        }
    }

    private void PositionOnActiveScreen(IntPtr handle)
        => PositionOnActiveScreenCore(handle, SettingsManager.Load().OsdPosition);

    private void PositionOnActiveScreenCore(IntPtr handle, string? osdPosition)
    {
        try
        {
            System.Windows.Forms.Screen? screen = null;
            IntPtr fg = GetForegroundWindow();
            if (fg != IntPtr.Zero && fg != handle)
            {
                try
                {
                    screen = System.Windows.Forms.Screen.FromHandle(fg);
                }
                catch
                {
                    screen = null;
                }
            }

            if (screen == null)
            {
                try
                {
                    screen = System.Windows.Forms.Screen.FromPoint(System.Windows.Forms.Cursor.Position);
                }
                catch
                {
                    screen = null;
                }
            }

            screen ??= System.Windows.Forms.Screen.PrimaryScreen;
            if (screen == null) return;
            string normalizedPosition = SettingsCodec.NormalizeOsdPosition(osdPosition);
            var bounds = normalizedPosition == "Center" ? screen.Bounds : screen.WorkingArea;
            DpiScale dpi = VisualTreeHelper.GetDpi(this);
            double scaleX = dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0;
            double scaleY = dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1.0;
            int width = (int)Math.Round((this.Width > 0 ? this.Width : 180.0) * scaleX);
            int height = (int)Math.Round((this.Height > 0 ? this.Height : 180.0) * scaleY);
            if (GetWindowRect(handle, out RECT nativeRect))
            {
                int curW = nativeRect.Right - nativeRect.Left;
                int curH = nativeRect.Bottom - nativeRect.Top;
                if (curW > 0 && curH > 0)
                {
                    width = curW;
                    height = curH;
                }
            }
            var size = new PixelSize(width, height);
            PixelRect target = UiBehavior.CalculateOsdPositionInPixels(new PixelRect(bounds.Left, bounds.Top, bounds.Width, bounds.Height), size, normalizedPosition);

            this.Left = target.Left / scaleX;
            this.Top = target.Top / scaleY;

            SetWindowPos(handle, HWND_TOPMOST, target.Left, target.Top, 0, 0, SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOOWNERZORDER | SWP_SHOWWINDOW);
        }
        catch
        {
            SetWindowPos(handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOOWNERZORDER | SWP_SHOWWINDOW);
        }
    }

    private static readonly Color ColorMutedText = Color.FromRgb(0xF8, 0x71, 0x71); // Clean standard coral red (#F87171)
    private static readonly Color ColorMutedBorder = Color.FromRgb(0xF8, 0x71, 0x71);
    private static readonly Color ColorMutedGlow = Color.FromRgb(0xEF, 0x44, 0x44);
    private static readonly SolidColorBrush BrushMutedText = CreateFrozenBrush(ColorMutedText);
    private static readonly SolidColorBrush BrushMutedBorder = CreateFrozenBrush(ColorMutedBorder);
    private static readonly SolidColorBrush BrushDarkMutedBackground = CreateFrozenBrush(Color.FromArgb(0xEE, 0x16, 0x11, 0x12));

    private static readonly SolidColorBrush BrushActiveText = CreateFrozenBrush(Colors.White);
    private static readonly SolidColorBrush BrushActiveBorder = CreateFrozenBrush(Colors.White);
    private static readonly SolidColorBrush BrushDarkActiveBackground = CreateFrozenBrush(Color.FromArgb(0xEE, 0x12, 0x14, 0x18));

    private static readonly SolidColorBrush BrushLightActiveBackground = CreateFrozenBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
    private static readonly SolidColorBrush BrushLightActiveBorder = CreateFrozenBrush(Color.FromRgb(0x12, 0x14, 0x18));
    private static readonly SolidColorBrush BrushLightActiveText = CreateFrozenBrush(Color.FromRgb(0x12, 0x14, 0x18));

    private static readonly SolidColorBrush BrushLightMutedBackground = CreateFrozenBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
    private static readonly SolidColorBrush BrushLightMutedBorder = CreateFrozenBrush(Color.FromRgb(0xDC, 0x26, 0x26));
    private static readonly SolidColorBrush BrushLightMutedText = CreateFrozenBrush(Color.FromRgb(0xDC, 0x26, 0x26));
    private static readonly SolidColorBrush BrushLightMuted = BrushLightMutedText;
    private static readonly SolidColorBrush BrushDarkSlashMask = CreateFrozenBrush(Color.FromArgb(0x90, 0x00, 0x00, 0x00));
    private static readonly SolidColorBrush BrushLightSlashMask = CreateFrozenBrush(Color.FromArgb(0xD8, 0xFF, 0xFF, 0xFF));

    private static SolidColorBrush CreateFrozenBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    public static void UpdateVisibleTheme(bool isLight)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher ?? _instance?.Dispatcher;
        if (dispatcher != null && !dispatcher.CheckAccess())
        {
            dispatcher.BeginInvoke(() => UpdateVisibleTheme(isLight));
            return;
        }
        if (_instance?.IsVisible == true) _instance.ApplyTheme(isLight);
    }

    internal void ApplyTheme(bool isLight) => ApplyThemeCore(isLight, refreshState: true);

    private void ApplyThemeCore(bool isLight, bool refreshState)
    {
        _lightMode = isLight;
        activeGlyph.Fill = isLight ? BrushLightActiveText : BrushActiveText;
        mutedGlyph.Fill = isLight ? BrushLightMutedText : BrushMutedText;
        muteSlash.Stroke = isLight ? BrushLightMutedText : BrushMutedText;
        if (muteSlashBg != null)
        {
            muteSlashBg.Stroke = isLight ? BrushLightSlashMask : BrushDarkSlashMask;
        }
        if (refreshState)
        {
            UpdateState(_isMuted);
        }
    }

    private void AnimatePanelEntrance(int sequenceId)
    {
        UiMotion.EnableTransientCache(borderPanel, this);
        if (borderPanel != null)
        {
            borderPanel.BeginAnimation(UIElement.OpacityProperty, null);
            DoubleAnimation fadeIn = new DoubleAnimation(0.0, 1.0, UiMotion.Fast)
            {
                EasingFunction = UiMotion.Decelerate
            };
            borderPanel.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        }
        if (panelTranslateTransform != null)
        {
            panelTranslateTransform.BeginAnimation(TranslateTransform.YProperty, null);
            DoubleAnimation slideIn = new DoubleAnimation(10.0, 0.0, UiMotion.Fast)
            {
                EasingFunction = UiMotion.Decelerate
            };
            panelTranslateTransform.BeginAnimation(TranslateTransform.YProperty, slideIn);
        }
        AnimatePanelScale(0.88, 1.0, 190, UiMotion.SpringGentle, () =>
        {
            if (sequenceId == _activeSequenceId)
            {
                UiMotion.ClearTransientCache(borderPanel);
            }
        });
    }

    private void AnimatePanelPulse(int sequenceId)
    {
        UiMotion.EnableTransientCache(borderPanel, this);
        AnimatePanelScale(0.94, 1.0, 170, UiMotion.SpringGentle, () =>
        {
            if (sequenceId == _activeSequenceId)
            {
                UiMotion.ClearTransientCache(borderPanel);
            }
        });
    }

    private void SnapPanelOpacity()
    {
        if (borderPanel != null)
        {
            borderPanel.BeginAnimation(UIElement.OpacityProperty, null);
            borderPanel.Opacity = 1.0;
        }
    }

    private void AnimatePanelScale(double fromScale, double toScale, double durationMs, IEasingFunction easing, Action? onCompleted = null)
    {
        if (panelScaleTransform == null)
        {
            onCompleted?.Invoke();
            return;
        }
        panelScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        panelScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);

        Duration duration = TimeSpan.FromMilliseconds(durationMs);
        DoubleAnimation animX = new DoubleAnimation(fromScale, toScale, duration) { EasingFunction = easing };
        DoubleAnimation animY = new DoubleAnimation(fromScale, toScale, duration) { EasingFunction = easing };
        if (onCompleted != null)
        {
            animX.Completed += (_, _) => onCompleted();
        }
        panelScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animX);
        panelScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animY);
    }

    private void ResetOverlayAnimations()
    {
        UiMotion.ClearTransientCache(borderPanel);
        SnapPanelOpacity();
        if (panelScaleTransform != null)
        {
            panelScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            panelScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            panelScaleTransform.ScaleX = 1.0;
            panelScaleTransform.ScaleY = 1.0;
        }
        if (panelTranslateTransform != null)
        {
            panelTranslateTransform.BeginAnimation(TranslateTransform.YProperty, null);
            panelTranslateTransform.Y = 0.0;
        }
    }

    private void UpdateState(bool isMuted)
    {
        _isMuted = isMuted;
        if (isMuted)
        {
            pathMuted.Visibility = Visibility.Visible;
            pathActive.Visibility = Visibility.Collapsed;
            tbStatus.Text = "MUTED";
            tbStatus.Foreground = _lightMode ? BrushLightMutedText : BrushMutedText;
            borderPanel.BorderBrush = _lightMode ? BrushLightMutedBorder : BrushMutedBorder;
            borderPanel.Background = _lightMode ? BrushLightMutedBackground : BrushDarkMutedBackground;
            if (osdShadow != null)
            {
                osdShadow.Color = _lightMode ? BrushLightMutedBorder.Color : ColorMutedGlow;
                osdShadow.Opacity = _lightMode ? 0.40 : 0.65;
            }
        }
        else
        {
            pathActive.Visibility = Visibility.Visible;
            pathMuted.Visibility = Visibility.Collapsed;
            tbStatus.Text = "ACTIVE";
            tbStatus.Foreground = _lightMode ? BrushLightActiveText : BrushActiveText;
            borderPanel.BorderBrush = _lightMode ? BrushLightActiveBorder : BrushActiveBorder;
            borderPanel.Background = _lightMode ? BrushLightActiveBackground : BrushDarkActiveBackground;
            if (osdShadow != null)
            {
                osdShadow.Color = Color.FromRgb(0x00, 0x00, 0x00);
                osdShadow.Opacity = _lightMode ? 0.25 : 0.45;
            }
        }
    }

    private async Task BeginFadeSequenceAsync(double durationSeconds, int sequenceId, CancellationTokenSource cts)
    {
        CancellationToken token = cts.Token;
        try
        {
            IntPtr handle = new WindowInteropHelper(this).Handle;
            SetWindowPos(handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOOWNERZORDER | SWP_SHOWWINDOW);

            DateTime endTime = DateTime.UtcNow.AddSeconds(durationSeconds);
            while (DateTime.UtcNow < endTime)
            {
                if (token.IsCancellationRequested || sequenceId != _activeSequenceId)
                {
                    // A newer sequence has taken over or cancellation was requested.
                    // Return without hiding or zeroing opacity so the newer sequence keeps the window displayed.
                    return;
                }

                SetWindowPos(handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOOWNERZORDER);
                TimeSpan remaining = endTime - DateTime.UtcNow;
                if (remaining <= TimeSpan.Zero) break;
                TimeSpan chunk = remaining < TimeSpan.FromMilliseconds(500) ? remaining : TimeSpan.FromMilliseconds(500);
                await Task.Delay(chunk, token);
            }

            if (token.IsCancellationRequested || sequenceId != _activeSequenceId)
            {
                return;
            }

            UiMotion.EnableTransientCache(borderPanel, this);
            double currentPanelOpacity = borderPanel?.Opacity ?? 1.0;
            DoubleAnimation fadeOut = new DoubleAnimation(currentPanelOpacity, 0.0, UiMotion.OsdExit)
            {
                EasingFunction = UiMotion.Accelerate
            };
            fadeOut.Completed += delegate
            {
                if (sequenceId == _activeSequenceId)
                {
                    BeginAnimation(OpacityProperty, null);
                    ResetOverlayAnimations();
                    this.Opacity = 0.0;
                    Hide();
                }
            };
            AnimatePanelScale(panelScaleTransform?.ScaleX ?? 1.0, 0.94, UiMotion.OsdExit.TimeSpan.TotalMilliseconds, UiMotion.Accelerate);
            if (panelTranslateTransform != null)
            {
                panelTranslateTransform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(panelTranslateTransform.Y, 6.0, UiMotion.OsdExit)
                {
                    EasingFunction = UiMotion.Accelerate
                });
            }
            if (borderPanel != null)
            {
                borderPanel.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            }
            else
            {
                BeginAnimation(OpacityProperty, fadeOut);
            }
        }
        catch (OperationCanceledException)
        {
            // Do not hide or zero opacity if cancelled because a newer sequence is active.
            // If HideOsd was explicitly called, HideOsd handles hiding directly.
        }
        catch (Exception ex)
        {
            DiagnosticLogger.LogError("OSD fade sequence error", ex);
        }
        finally
        {
            if (ReferenceEquals(_cts, cts))
            {
                _cts = null;
            }
            cts.Dispose();
        }
    }
}
