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

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;

    internal Border borderPanel = null!;
    internal Grid pathActive = null!;
    internal Grid pathMuted = null!;
    internal TextBlock tbStatus = null!;
    internal DropShadowEffect osdShadow = null!;
    private bool _contentLoaded;

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
                    var content = root.Content;
                    root.Content = null;
                    this.Content = content;
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
                }
            }
        }

        FrameworkElement? scope = this.Content as FrameworkElement;
        if (scope == null) return;

        borderPanel = (Border)scope.FindName("borderPanel");
        pathActive = (Grid)scope.FindName("pathActive");
        pathMuted = (Grid)scope.FindName("pathMuted");
        tbStatus = (TextBlock)scope.FindName("tbStatus");
        if (borderPanel != null)
        {
            osdShadow = (DropShadowEffect)borderPanel.Effect;
        }
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        IntPtr handle = new WindowInteropHelper(this).Handle;
        IntPtr dwNewLong = new IntPtr(GetWindowLong(handle, GWL_EXSTYLE).ToInt64() | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW);
        SetWindowLong(handle, GWL_EXSTYLE, dwNewLong);
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
        if (!_instance.IsVisible)
        {
            _instance.Show();
        }
        _instance.BeginFadeSequence(durationSeconds, _cts.Token);
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
            Left = (SystemParameters.PrimaryScreenWidth - Width) / 2.0;
            Top = (SystemParameters.PrimaryScreenHeight - Height) / 2.0;
            DoubleAnimation fadeIn = new DoubleAnimation(0.0, 0.92, TimeSpan.FromSeconds(0.1));
            BeginAnimation(OpacityProperty, fadeIn);

            await Task.Delay(TimeSpan.FromSeconds(durationSeconds), token);

            DoubleAnimation fadeOut = new DoubleAnimation(0.92, 0.0, TimeSpan.FromSeconds(0.22));
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