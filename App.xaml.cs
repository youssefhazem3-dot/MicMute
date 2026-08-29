using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace MicMute;

public partial class App : System.Windows.Application
{
    [StructLayout(LayoutKind.Sequential)]
    private struct DEVMODE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }

    [DllImport("user32.dll")]
    private static extern bool EnumDisplaySettings(string? deviceName, int modeNum, ref DEVMODE devMode);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool DestroyIcon(IntPtr handle);

    private class ModernBlueRenderer : ToolStripProfessionalRenderer
    {
        public ModernBlueRenderer()
            : base(new ModernBlueColorTable())
        {
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            bool lightMode = SettingsManager.Load().LightMode;
            e.TextColor = (lightMode ? Color.FromArgb(29, 29, 31) : Color.FromArgb(245, 245, 247));
            base.OnRenderItemText(e);
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            bool lightMode = SettingsManager.Load().LightMode;
            e.ArrowColor = (lightMode ? Color.FromArgb(21, 90, 132) : Color.FromArgb(41, 127, 184));
            base.OnRenderArrow(e);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item.Selected)
            {
                using (SolidBrush brush = new SolidBrush(SettingsManager.Load().LightMode ? Color.FromArgb(229, 231, 235) : Color.FromArgb(31, 41, 55)))
                {
                    e.Graphics.FillRectangle(brush, 1, 1, e.Item.Width - 2, e.Item.Height - 2);
                }
            }
        }
    }

    private class ModernBlueColorTable : ProfessionalColorTable
    {
        private bool IsLight => SettingsManager.Load().LightMode;

        private Color BgWindow => !IsLight ? Color.FromArgb(11, 15, 25) : Color.FromArgb(255, 255, 255);

        private Color BgSelection => !IsLight ? Color.FromArgb(31, 41, 55) : Color.FromArgb(229, 231, 235);

        private Color AccentColor => !IsLight ? Color.FromArgb(41, 127, 184) : Color.FromArgb(21, 90, 132);

        private Color Border => AccentColor;

        private Color Separator => !IsLight ? Color.FromArgb(31, 41, 55) : Color.FromArgb(229, 231, 235);

        public override Color ToolStripDropDownBackground => BgWindow;
        public override Color MenuBorder => Border;
        public override Color MenuItemBorder => BgSelection;
        public override Color MenuItemSelected => BgSelection;
        public override Color MenuItemSelectedGradientBegin => BgSelection;
        public override Color MenuItemSelectedGradientEnd => BgSelection;
        public override Color MenuItemPressedGradientBegin => BgWindow;
        public override Color MenuItemPressedGradientEnd => BgWindow;
        public override Color ImageMarginGradientBegin => BgWindow;
        public override Color ImageMarginGradientEnd => BgWindow;
        public override Color SeparatorDark => Separator;
        public override Color SeparatorLight => Color.Transparent;
    }

    private static Mutex? _mutex;
    private const string MutexName = "Global\\MicMuteAppMutex_7FA5D9E0-9E11-40EA-B368-C8E649F56A49";
    private NotifyIcon? _notifyIcon;
    private AudioController? _audioController;
    private MainWindow? _mainWindow;

    [STAThread]
    public static void Main(string[] args)
    {
        App app = new App();
        app.InitializeComponent();
        app.Run();
    }

    public void InitializeComponent()
    {
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        if (!createdNew)
        {
            System.Windows.MessageBox.Show("Mic Mute is already running.", "Mic Mute", MessageBoxButton.OK, MessageBoxImage.Asterisk);
            Shutdown();
            return;
        }

        // Automatic High Refresh Rate Detection (144Hz, 240Hz, 360Hz)
        try
        {
            int refreshRate = 60;
            DEVMODE devMode = default;
            devMode.dmSize = (short)Marshal.SizeOf(devMode);
            if (EnumDisplaySettings(null, -1, ref devMode) && devMode.dmDisplayFrequency > 30)
            {
                refreshRate = Math.Max(60, devMode.dmDisplayFrequency);
            }
            Timeline.DesiredFrameRateProperty.OverrideMetadata(typeof(Timeline), new FrameworkPropertyMetadata(refreshRate));
        }
        catch
        {
        }

        base.OnStartup(e);
        base.ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // OSD Pre-Warmup to guarantee 0ms latency on first toggle
        OsdWindow.WarmUp();

        AppSettings appSettings = SettingsManager.Load();
        _audioController = new AudioController();
        _audioController.SetTargetDevice(appSettings.SelectedDeviceId);
        _audioController.MuteStateChanged += AudioController_MuteStateChanged;
        InitializeTrayIcon();
        _mainWindow = new MainWindow(_audioController);
        if (!e.Args.Contains("--minimized"))
        {
            _mainWindow.Show();
            _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Activate();
            _mainWindow.Focus();
        }
        else
        {
            _mainWindow.Visibility = Visibility.Hidden;
        }
        _mainWindow.Closing += MainWindow_Closing;
    }

    private void InitializeTrayIcon()
    {
        _notifyIcon = new NotifyIcon
        {
            Text = "Mic Mute",
            Visible = true
        };
        _notifyIcon.DoubleClick += delegate
        {
            ShowWindow();
        };
        ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
        contextMenuStrip.ShowImageMargin = false;
        contextMenuStrip.Renderer = new ModernBlueRenderer();
        ToolStripMenuItem value = new ToolStripMenuItem("Toggle Mute", null, delegate
        {
            _audioController?.ToggleMute();
        });
        ToolStripMenuItem value2 = new ToolStripMenuItem("Open Control Panel", null, delegate
        {
            ShowWindow();
        });
        ToolStripMenuItem value3 = new ToolStripMenuItem("Quit", null, delegate
        {
            ExitApp();
        });
        contextMenuStrip.Items.Add(value);
        contextMenuStrip.Items.Add(value2);
        contextMenuStrip.Items.Add(new ToolStripSeparator());
        contextMenuStrip.Items.Add(value3);
        _notifyIcon.ContextMenuStrip = contextMenuStrip;
        UpdateTrayIcon(_audioController?.IsMuted ?? false);
    }

    private void UpdateTrayIcon(bool isMuted)
    {
        if (_notifyIcon == null)
        {
            return;
        }
        string text = _audioController?.CurrentDeviceName ?? "No microphone";
        string text2 = isMuted ? "MUTED" : "ACTIVE";
        _notifyIcon.Text = "Mic Mute (" + text2 + ")\nDevice: " + text;
        try
        {
            using Bitmap bitmap = new Bitmap(16, 16);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.Clear(Color.Transparent);
                bool lightMode = SettingsManager.Load().LightMode;
                Color color = isMuted ? Color.FromArgb(255, 59, 48) : (lightMode ? Color.FromArgb(21, 90, 132) : Color.FromArgb(41, 127, 184));
                using (SolidBrush brush = new SolidBrush(color))
                {
                    graphics.FillEllipse(brush, 0, 0, 16, 16);
                }
                using Pen pen = new Pen(Color.White, 1.2f);
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                using (SolidBrush brush2 = new SolidBrush(Color.White))
                {
                    FillRoundedRectangle(graphics, brush2, 6f, 4f, 4f, 6f, 1.5f);
                    graphics.DrawArc(pen, 4.5f, 5.5f, 7f, 5f, 0f, 180f);
                    graphics.DrawLine(pen, 8f, 10.5f, 8f, 12.5f);
                    graphics.DrawLine(pen, 6f, 12.5f, 10f, 12.5f);
                }
                if (isMuted)
                {
                    using (Pen pen2 = new Pen(color, 2.2f))
                    {
                        pen2.StartCap = LineCap.Round;
                        pen2.EndCap = LineCap.Round;
                        graphics.DrawLine(pen2, 3, 3, 13, 13);
                    }
                    graphics.DrawLine(pen, 3, 3, 13, 13);
                }
            }
            Icon icon = Icon.FromHandle(bitmap.GetHicon());
            Icon? icon2 = _notifyIcon.Icon;
            _notifyIcon.Icon = icon;
            if (icon2 != null)
            {
                DestroyIcon(icon2.Handle);
                icon2.Dispose();
            }
        }
        catch (Exception ex)
        {
            try
            {
                string text3 = SettingsManager.GetDataFolderPath();
                Directory.CreateDirectory(text3);
                File.WriteAllText(Path.Combine(text3, "gdi_error.txt"), ex.ToString());
            }
            catch
            {
            }
        }
    }

    private static void FillRoundedRectangle(Graphics g, Brush brush, float x, float y, float width, float height, float radius)
    {
        using GraphicsPath graphicsPath = new GraphicsPath();
        float num = radius * 2f;
        graphicsPath.AddArc(x, y, num, num, 180f, 90f);
        graphicsPath.AddArc(x + width - num, y, num, num, 270f, 90f);
        graphicsPath.AddArc(x + width - num, y + height - num, num, num, 0f, 90f);
        graphicsPath.AddArc(x, y + height - num, num, num, 90f, 90f);
        graphicsPath.CloseAllFigures();
        g.FillPath(brush, graphicsPath);
    }

    private void AudioController_MuteStateChanged(object? sender, MuteStateChangedEventArgs e)
    {
        Dispatcher.BeginInvoke((Action)delegate
        {
            UpdateTrayIcon(e.IsMuted);
        });
    }

    public void ShowToastNotification(string message)
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.ShowBalloonTip(1500, "Mic Mute", message, ToolTipIcon.Info);
        }
    }

    private void ShowWindow()
    {
        if (_mainWindow != null)
        {
            _mainWindow.Show();
            _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Activate();
        }
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        e.Cancel = true;
        _mainWindow?.Hide();
    }

    private void ExitApp()
    {
        if (_mainWindow != null)
        {
            _mainWindow.Closing -= MainWindow_Closing;
            _mainWindow.Close();
        }
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
        _audioController?.Dispose();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
        _audioController?.Dispose();
        try
        {
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
        }
        catch
        {
        }
        base.OnExit(e);
    }
}