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
    private const uint SWP_SHOWWINDOW = 0x0040;

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOPMOST = 0x00000008;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    internal Border borderPanel = null!;
    internal Grid pathActive = null!;
    internal Grid pathMuted = null!;
    internal TextBlock tbStatus = null!;
    internal DropShadowEffect osdShadow = null!;
    private bool _contentLoaded;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

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
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        if (_instance == null)
        {
            _instance = new OsdWindow();
        }
        _instance.UpdateState(isMuted);

        IntPtr handle = new WindowInteropHelper(_instance).EnsureHandle();
        _instance.PositionOnActiveScreen();

        if (!_instance.IsVisible)
        {
            _instance.Show();
        }

        // Forcefully re-assert HWND_TOPMOST above full-screen games
        SetWindowPos(handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);

        _instance.BeginFadeSequence(durationSeconds, _cts.Token);
    }

    private void PositionOnActiveScreen()
    {
        try
        {
            var screen = System.Windows.Forms.Screen.FromPoint(System.Windows.Forms.Cursor.Position);
            var bounds = screen.Bounds;
            Left = bounds.Left + (bounds.Width - Width) / 2.0;
            Top = bounds.Top + (bounds.Height - Height) / 2.0;
        }
        catch
        {
            Left = (SystemParameters.PrimaryScreenWidth - Width) / 2.0;
            Top = (SystemParameters.PrimaryScreenHeight - Height) / 2.0;
        }
    }

    private void UpdateState(bool isMuted)
    {
        if (isMuted)
        {
            pathMuted.Visibility = Visibility.Visible;
            pathActive.Visibility = Visibility.Collapsed;
            tbStatus.Text = "MUTED";
            tbStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
            borderPanel.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
            osdShadow.Color = (Color)ColorConverter.ConvertFromString("#EF4444");
        }
        else
        {
            pathActive.Visibility = Visibility.Visible;
            pathMuted.Visibility = Visibility.Collapsed;
            tbStatus.Text = "ACTIVE";
            tbStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0"));
            borderPanel.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
            osdShadow.Color = (Color)ColorConverter.ConvertFromString("#94A3B8");
        }
    }

    private async void BeginFadeSequence(double durationSeconds, CancellationToken token)
    {
        try
        {
            IntPtr handle = new WindowInteropHelper(this).Handle;
            SetWindowPos(handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);

            DoubleAnimation fadeIn = new DoubleAnimation(0.0, 0.95, TimeSpan.FromSeconds(0.08));
            BeginAnimation(OpacityProperty, fadeIn);

            await Task.Delay(TimeSpan.FromSeconds(durationSeconds), token);

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