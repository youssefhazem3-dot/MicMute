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

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    internal Border borderPanel = null!;
    internal Grid pathActive = null!;
    internal Grid pathMuted = null!;
    internal TextBlock tbStatus = null!;
    internal DropShadowEffect osdShadow = null!;
    internal System.Windows.Shapes.Path activeGlyph = null!;
    internal System.Windows.Shapes.Path mutedGlyph = null!;
    internal System.Windows.Shapes.Line muteSlash = null!;
    private bool _contentLoaded;
    private bool _lightMode;
    private bool _isMuted;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

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
                    xaml = System.Text.RegularExpressions.Regex.Replace(xaml, @"\s+x:Class=""[^""]+""", "");
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

                    borderPanel = (Border)root.FindName("borderPanel");
                    pathActive = (Grid)root.FindName("pathActive");
                    pathMuted = (Grid)root.FindName("pathMuted");
                    tbStatus = (TextBlock)root.FindName("tbStatus");
                    activeGlyph = (System.Windows.Shapes.Path)root.FindName("activeGlyph");
                    mutedGlyph = (System.Windows.Shapes.Path)root.FindName("mutedGlyph");
                    muteSlash = (System.Windows.Shapes.Line)root.FindName("muteSlash");
                    if (borderPanel != null)
                    {
                        osdShadow = (DropShadowEffect)borderPanel.Effect;
                    }

                    var content = root.Content;
                    root.Content = null;
                    this.Content = content;
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
    }

    public static void WarmUp()
    {
        try
        {
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
        }
    }

    public static void ShowOsd(bool isMuted, double durationSeconds)
    {
        CancellationTokenSource? previousCts = _cts;
        _cts = new CancellationTokenSource();
        previousCts?.Cancel();
        previousCts?.Dispose();
        if (_instance == null)
        {
            _instance = new OsdWindow();
        }
        _instance.ApplyTheme(SettingsManager.Load().LightMode);
        _instance.UpdateState(isMuted);

        IntPtr handle = new WindowInteropHelper(_instance).EnsureHandle();

        _instance.PositionOnActiveScreen(handle);

        if (!_instance.IsVisible)
        {
            _instance.Show();
        }

        double safeDuration = double.IsFinite(durationSeconds)
            ? Math.Clamp(durationSeconds, UiBehavior.MinimumOsdDuration, UiBehavior.MaximumOsdDuration)
            : UiBehavior.MinimumOsdDuration;
        _instance.BeginFadeSequence(safeDuration, _cts.Token);
    }

    private void PositionOnActiveScreen(IntPtr handle)
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
            var bounds = screen.Bounds;
            DpiScale dpi = VisualTreeHelper.GetDpi(this);
            double scaleX = dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0;
            double scaleY = dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1.0;
            int width = (int)Math.Round((this.Width > 0 ? this.Width : 140.0) * scaleX);
            int height = (int)Math.Round((this.Height > 0 ? this.Height : 140.0) * scaleY);
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
            PixelRect target = UiBehavior.CenterInPixels(new PixelRect(bounds.Left, bounds.Top, bounds.Width, bounds.Height), size);

            this.Left = target.Left / scaleX;
            this.Top = target.Top / scaleY;

            SetWindowPos(handle, HWND_TOPMOST, target.Left, target.Top, 0, 0, SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOOWNERZORDER);
        }
        catch
        {
            SetWindowPos(handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOOWNERZORDER);
        }
    }

    private static readonly Color ColorMutedText = Color.FromRgb(0xF8, 0x71, 0x71); // Clean standard balanced red (#F87171)
    private static readonly Color ColorMutedBorder = Color.FromRgb(0xDC, 0x26, 0x26); // Tailwind Red 600 (#DC2626)
    private static readonly SolidColorBrush BrushMutedText = CreateFrozenBrush(ColorMutedText);
    private static readonly SolidColorBrush BrushMutedBorder = CreateFrozenBrush(ColorMutedBorder);

    private static readonly Color ColorActive = Color.FromRgb(0x94, 0xA3, 0xB8);
    private static readonly SolidColorBrush BrushActiveText = CreateFrozenBrush(Color.FromRgb(0xE2, 0xE8, 0xF0));
    private static readonly SolidColorBrush BrushActiveBorder = CreateFrozenBrush(ColorActive);
    private static readonly SolidColorBrush BrushDarkBackground = CreateFrozenBrush(Color.FromArgb(0xE5, 0x0F, 0x14, 0x1C));
    private static readonly SolidColorBrush BrushLightBackground = CreateFrozenBrush(Color.FromArgb(0xF2, 0xF8, 0xF9, 0xFA));
    private static readonly SolidColorBrush BrushLightActive = CreateFrozenBrush(Color.FromRgb(55, 65, 81));
    private static readonly SolidColorBrush BrushLightMuted = CreateFrozenBrush(Color.FromRgb(0xDC, 0x26, 0x26)); // Clean standard balanced UI red (#DC2626)

    private static SolidColorBrush CreateFrozenBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    public static void UpdateVisibleTheme(bool isLight)
    {
        if (_instance?.IsVisible == true) _instance.ApplyTheme(isLight);
    }

    internal void ApplyTheme(bool isLight)
    {
        _lightMode = isLight;
        borderPanel.Background = isLight ? BrushLightBackground : BrushDarkBackground;
        activeGlyph.Fill = isLight ? BrushLightActive : BrushActiveText;
        mutedGlyph.Fill = isLight ? BrushLightMuted : BrushMutedText;
        muteSlash.Stroke = isLight ? BrushLightMuted : BrushMutedText;
        osdShadow.Opacity = isLight ? 0.25 : 0.45;
        UpdateState(_isMuted);
    }

    private void UpdateState(bool isMuted)
    {
        _isMuted = isMuted;
        if (isMuted)
        {
            pathMuted.Visibility = Visibility.Visible;
            pathActive.Visibility = Visibility.Collapsed;
            tbStatus.Text = "MUTED";
            tbStatus.Foreground = _lightMode ? BrushLightMuted : BrushMutedText;
            borderPanel.BorderBrush = _lightMode ? BrushLightMuted : BrushMutedBorder;
            osdShadow.Color = _lightMode ? BrushLightMuted.Color : ColorMutedBorder;
        }
        else
        {
            pathActive.Visibility = Visibility.Visible;
            pathMuted.Visibility = Visibility.Collapsed;
            tbStatus.Text = "ACTIVE";
            tbStatus.Foreground = _lightMode ? BrushLightActive : BrushActiveText;
            borderPanel.BorderBrush = _lightMode ? BrushLightActive : BrushActiveBorder;
            osdShadow.Color = _lightMode ? BrushLightActive.Color : ColorActive;
        }
    }

    private async void BeginFadeSequence(double durationSeconds, CancellationToken token)
    {
        try
        {
            IntPtr handle = new WindowInteropHelper(this).Handle;
            SetWindowPos(handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOOWNERZORDER | SWP_SHOWWINDOW);

            double startOpacity = Math.Clamp(this.Opacity, 0.0, 0.95);
            DoubleAnimation fadeIn = new DoubleAnimation(startOpacity, 0.95, TimeSpan.FromSeconds(0.08));
            BeginAnimation(OpacityProperty, fadeIn);

            DateTime endTime = DateTime.UtcNow.AddSeconds(durationSeconds);
            while (DateTime.UtcNow < endTime)
            {
                SetWindowPos(handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOOWNERZORDER);
                TimeSpan remaining = endTime - DateTime.UtcNow;
                if (remaining <= TimeSpan.Zero) break;
                TimeSpan chunk = remaining < TimeSpan.FromMilliseconds(80) ? remaining : TimeSpan.FromMilliseconds(80);
                await Task.Delay(chunk, token);
            }

            DoubleAnimation fadeOut = new DoubleAnimation(0.95, 0.0, TimeSpan.FromSeconds(0.20));
            fadeOut.Completed += delegate
            {
                if (!token.IsCancellationRequested)
                {
                    Hide();
                }
            };
            BeginAnimation(OpacityProperty, fadeOut);
        }
        catch (TaskCanceledException)
        {
        }
        catch
        {
        }
    }
}
