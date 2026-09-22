using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
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

    private class LiquidGlassMenuRenderer : ToolStripProfessionalRenderer
    {
        public LiquidGlassMenuRenderer()
            : base(new LiquidGlassColorTable())
        {
            RoundedEdges = true;
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            bool lightMode = SettingsManager.Load().LightMode;
            Color bgColor = lightMode ? Color.FromArgb(250, 250, 252) : Color.FromArgb(24, 24, 27);
            using SolidBrush brush = new SolidBrush(bgColor);
            e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            bool lightMode = SettingsManager.Load().LightMode;
            Color borderColor = lightMode ? Color.FromArgb(228, 228, 231) : Color.FromArgb(46, 46, 52);
            using Pen pen = new Pen(borderColor, 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            bool lightMode = SettingsManager.Load().LightMode;
            e.TextColor = lightMode ? Color.FromArgb(24, 24, 27) : Color.FromArgb(244, 244, 246);
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            bool lightMode = SettingsManager.Load().LightMode;
            e.ArrowColor = lightMode ? Color.FromArgb(113, 113, 122) : Color.FromArgb(161, 161, 170);
            base.OnRenderArrow(e);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item.Selected && e.Item.Enabled)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                bool lightMode = SettingsManager.Load().LightMode;
                Color hoverColor = lightMode ? Color.FromArgb(232, 232, 237) : Color.FromArgb(44, 44, 50);
                using SolidBrush brush = new SolidBrush(hoverColor);
                Rectangle rect = new Rectangle(4, 2, e.Item.Width - 8, e.Item.Height - 4);
                using GraphicsPath path = CreateRoundedRectanglePath(rect, 5f);
                e.Graphics.FillPath(brush, path);
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            bool lightMode = SettingsManager.Load().LightMode;
            Color sepColor = lightMode ? Color.FromArgb(228, 228, 231) : Color.FromArgb(39, 39, 45);
            int y = e.Item.Height / 2;
            using Pen pen = new Pen(sepColor, 1f);
            e.Graphics.DrawLine(pen, 8, y, e.Item.Width - 8, y);
        }
    }

    private class LiquidGlassColorTable : ProfessionalColorTable
    {
        private bool IsLight => SettingsManager.Load().LightMode;

        private Color BgWindow => IsLight ? Color.FromArgb(250, 250, 252) : Color.FromArgb(24, 24, 27);
        private Color BgSelection => IsLight ? Color.FromArgb(232, 232, 237) : Color.FromArgb(44, 44, 50);
        private Color Border => IsLight ? Color.FromArgb(228, 228, 231) : Color.FromArgb(46, 46, 52);
        private Color Separator => IsLight ? Color.FromArgb(228, 228, 231) : Color.FromArgb(39, 39, 45);

        public override Color ToolStripDropDownBackground => BgWindow;
        public override Color MenuBorder => Border;
        public override Color MenuItemBorder => Color.Transparent;
        public override Color MenuItemSelected => BgSelection;
        public override Color MenuItemSelectedGradientBegin => BgSelection;
        public override Color MenuItemSelectedGradientEnd => BgSelection;
        public override Color MenuItemPressedGradientBegin => BgWindow;
        public override Color MenuItemPressedGradientEnd => BgWindow;
        public override Color ImageMarginGradientBegin => BgWindow;
        public override Color ImageMarginGradientEnd => BgWindow;
        public override Color ImageMarginGradientMiddle => BgWindow;
        public override Color SeparatorDark => Separator;
        public override Color SeparatorLight => Color.Transparent;
    }

    private static Mutex? _mutex;
    private const string MutexName = "Global\\MicMuteAppMutex_7FA5D9E0-9E11-40EA-B368-C8E649F56A49";
    private NotifyIcon? _notifyIcon;
    private AudioController? _audioController;
    private MainWindow? _mainWindow;

    [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    private static IntPtr _currentHIcon = IntPtr.Zero;

    [STAThread]
    public static void Main(string[] args)
    {
        if (UiBehavior.TryGetParentProcessId(args, out int parentId))
        {
            try
            {
                if (!UiBehavior.WaitForParentExit(parentId, TimeSpan.FromSeconds(30)))
                {
                    System.Windows.MessageBox.Show("The previous MicMute instance has not finished closing. Please try again.", "MicMute restart");
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Could not wait for the previous MicMute instance: " + ex.Message, "MicMute restart");
                return;
            }
        }
        AppDomain.CurrentDomain.AssemblyResolve += (sender, resolveArgs) =>
        {
            string? simpleName = new System.Reflection.AssemblyName(resolveArgs.Name).Name;
            if (string.IsNullOrEmpty(simpleName)) return null;
            string resourceName = simpleName + ".dll";
            using (Stream? stream = typeof(App).Assembly.GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    using MemoryStream ms = new MemoryStream();
                    stream.CopyTo(ms);
                    return System.Reflection.Assembly.Load(ms.ToArray());
                }
            }
            return null;
        };

        AppDomain.CurrentDomain.UnhandledException += (s, ev) =>
        {
            try
            {
                string p = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MicMute");
                Directory.CreateDirectory(p);
                File.WriteAllText(Path.Combine(p, "crash_log.txt"), ev.ExceptionObject?.ToString() ?? "Unknown exception");
            }
            catch { }
        };

        try
        {
            App app = new App();
            app.InitializeComponent();
            app.Run();
        }
        catch (Exception ex)
        {
            try
            {
                string p = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MicMute");
                Directory.CreateDirectory(p);
                File.WriteAllText(Path.Combine(p, "crash_log.txt"), ex.ToString());
            }
            catch { }
        }
    }

    public void InitializeComponent()
    {
    }

    private const int HWND_BROADCAST = 0xFFFF;

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int RegisterWindowMessage(string lpString);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private static readonly int WM_SHOWME = RegisterWindowMessage("MICMUTE_SHOW_WINDOW_MSG_7FA5D9E0");

    protected override void OnStartup(StartupEventArgs e)
    {
        DiagnosticLogger.Initialize(e.Args);

        _mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        if (!createdNew)
        {
            if (DiagnosticLogger.IsEnabled)
            {
                DiagnosticLogger.LogWarning("Another MicMute instance is running in the background.");
                DiagnosticLogger.LogInfo("Attempting to close previous instance for this diagnostic session...");
                try
                {
                    var currentId = Environment.ProcessId;
                    foreach (var proc in Process.GetProcessesByName("MicMute"))
                    {
                        if (proc.Id != currentId)
                        {
                            proc.Kill();
                            proc.WaitForExit(3000);
                        }
                    }
                    _mutex = new Mutex(initiallyOwned: true, MutexName, out createdNew);
                    if (createdNew)
                    {
                        DiagnosticLogger.LogInfo("Successfully acquired application mutex.");
                    }
                }
                catch (Exception ex)
                {
                    DiagnosticLogger.LogError("Could not close previous instance", ex);
                }
            }

            if (!createdNew)
            {
                IntPtr existingHwnd = FindWindow(null, "Mic Mute");
                if (existingHwnd != IntPtr.Zero)
                {
                    PostMessage(existingHwnd, (uint)WM_SHOWME, IntPtr.Zero, IntPtr.Zero);
                }
                else
                {
                    PostMessage((IntPtr)HWND_BROADCAST, (uint)WM_SHOWME, IntPtr.Zero, IntPtr.Zero);
                }
                Environment.Exit(0);
                return;
            }
        }

        // Automatic High Refresh Rate Detection (144Hz, 240Hz, 360Hz)
        try
        {
            int refreshRate = 60;
            DEVMODE devMode = default;
            devMode.dmSize = (short)Marshal.SizeOf(devMode);
            if (EnumDisplaySettings(null, -1, ref devMode) && devMode.dmDisplayFrequency > 30)
            {
                refreshRate = Math.Clamp(devMode.dmDisplayFrequency, 60, 120);
            }
            Timeline.DesiredFrameRateProperty.OverrideMetadata(typeof(Timeline), new FrameworkPropertyMetadata(refreshRate));
            if (DiagnosticLogger.IsEnabled)
            {
                DiagnosticLogger.LogInfo($"Display refresh rate set to: {refreshRate} FPS");
            }
        }
        catch
        {
        }

        base.OnStartup(e);
        base.ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Preload the OSD to reduce work on the first toggle.
        OsdWindow.WarmUp();

        AppSettings appSettings = SettingsManager.Load();
        StartupManager.SetStartup(appSettings.RunOnStartup);
        _audioController = new AudioController();
        _audioController.SetTargetDevice(appSettings.SelectedDeviceId);
        _audioController.MuteStateChanged += AudioController_MuteStateChanged;
        InitializeTrayIcon();

        if (DiagnosticLogger.IsEnabled)
        {
            DiagnosticLogger.LogAudio($"Active capture endpoint: '{_audioController.CurrentDeviceName}' (Muted: {_audioController.IsMuted})");
            DiagnosticLogger.LogInfo($"Shortcut configured: {appSettings.Hotkey} (Modifiers: {appSettings.HotkeyModifiers})");
        }

        _mainWindow = new MainWindow(_audioController);
        if (!UiBehavior.ShouldStartMinimized(appSettings.StartMinimized, e.Args))
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
        contextMenuStrip.Renderer = new LiquidGlassMenuRenderer();
        contextMenuStrip.Padding = new Padding(4, 5, 4, 5);
        try
        {
            contextMenuStrip.Font = new Font("Segoe UI Variable Text", 9.5f, System.Drawing.FontStyle.Regular, GraphicsUnit.Point);
        }
        catch
        {
            contextMenuStrip.Font = new Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Regular, GraphicsUnit.Point);
        }

        ToolStripMenuItem value = new ToolStripMenuItem("Toggle Mute", null, delegate
        {
            _audioController?.ToggleMute();
        }) { AutoSize = true, Margin = new Padding(0, 1, 0, 1), Padding = new Padding(12, 6, 12, 6) };

        ToolStripMenuItem value2 = new ToolStripMenuItem("Open Control Panel", null, delegate
        {
            ShowWindow();
        }) { AutoSize = true, Margin = new Padding(0, 1, 0, 1), Padding = new Padding(12, 6, 12, 6) };

        ToolStripSeparator separator = new ToolStripSeparator { Margin = new Padding(0, 3, 0, 3) };

        ToolStripMenuItem value3 = new ToolStripMenuItem("Quit", null, delegate
        {
            ExitApp();
        }) { AutoSize = true, Margin = new Padding(0, 1, 0, 1), Padding = new Padding(12, 6, 12, 6) };

        contextMenuStrip.Items.Add(value);
        contextMenuStrip.Items.Add(value2);
        contextMenuStrip.Items.Add(separator);
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
        string text2 = string.IsNullOrEmpty(_audioController?.CurrentDeviceId) ? "NO MICROPHONE" : isMuted ? "MUTED" : "ACTIVE";
        _notifyIcon.Text = UiBehavior.LimitTooltip("Mic Mute (" + text2 + ")\nDevice: " + text);
        try
        {
            int size = Math.Max(16, SystemInformation.SmallIconSize.Width);
            using Bitmap bitmap = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.Clear(Color.Transparent);

                bool lightMode = SettingsManager.Load().LightMode;

                // Squircle badge background & border
                float s = size;
                RectangleF badgeRect = new RectangleF(0.5f, 0.5f, s - 1f, s - 1f);
                float cornerRadius = s * 0.28f;

                Color badgeBg = isMuted
                    ? Color.FromArgb(185, 42, 42) // Chill low-light red (#B92A2A)
                    : (lightMode ? Color.FromArgb(45, 45, 48) : Color.FromArgb(24, 24, 27)); // Sleek neutral glass
                Color badgeBorder = isMuted
                    ? Color.FromArgb(220, 38, 38)
                    : (lightMode ? Color.FromArgb(70, 70, 75) : Color.FromArgb(46, 46, 52));

                using (GraphicsPath badgePath = CreateRoundedRectanglePath(badgeRect, cornerRadius))
                {
                    using (SolidBrush bgBrush = new SolidBrush(badgeBg))
                        graphics.FillPath(bgBrush, badgePath);
                    using (Pen borderPen = new Pen(badgeBorder, 1f))
                        graphics.DrawPath(borderPen, badgePath);
                }

                // Center and scale microphone glyph from Gemini reference design (24x24 box)
                float cx = s / 2f;
                float cy = s / 2f;
                float scale = (s * 0.65f) / 24f;

                graphics.TranslateTransform(cx, cy);
                graphics.ScaleTransform(scale, scale);
                graphics.TranslateTransform(-12f, -12f);

                // Microphone Capsule (Solid rounded pill)
                using (GraphicsPath capsule = new GraphicsPath())
                {
                    capsule.AddArc(8.25f, 2.5f, 7.5f, 7.5f, 180, 180);
                    capsule.AddLine(15.75f, 6.25f, 15.75f, 10.5f);
                    capsule.AddArc(8.25f, 6.75f, 7.5f, 7.5f, 0, 180);
                    capsule.AddLine(8.25f, 10.5f, 8.25f, 6.25f);
                    capsule.CloseFigure();

                    using (SolidBrush micBrush = new SolidBrush(Color.White))
                        graphics.FillPath(micBrush, capsule);
                }

                // Microphone Cradle, Stem, and Foot
                using (Pen cradlePen = new Pen(Color.White, 2.0f))
                {
                    cradlePen.StartCap = LineCap.Round;
                    cradlePen.EndCap = LineCap.Round;

                    using (GraphicsPath cradle = new GraphicsPath())
                    {
                        cradle.AddLine(6.0f, 7.5f, 6.0f, 10.5f);
                        cradle.AddArc(6.0f, 4.5f, 12.0f, 12.0f, 180, -180);
                        cradle.AddLine(18.0f, 10.5f, 18.0f, 7.5f);
                        graphics.DrawPath(cradlePen, cradle);
                    }

                    // Neck / stem
                    graphics.DrawLine(cradlePen, 12.0f, 16.5f, 12.0f, 20.0f);
                    // Foot base
                    graphics.DrawLine(cradlePen, 8.0f, 20.0f, 16.0f, 20.0f);
                }

                // Muted diagonal slash
                if (isMuted)
                {
                    using (Pen slashBacking = new Pen(Color.FromArgb(140, 0, 0, 0), 4.2f))
                    {
                        slashBacking.StartCap = LineCap.Round;
                        slashBacking.EndCap = LineCap.Round;
                        graphics.DrawLine(slashBacking, 3.5f, 3.5f, 20.5f, 20.5f);
                    }
                    using (Pen slashWhite = new Pen(Color.White, 2.2f))
                    {
                        slashWhite.StartCap = LineCap.Round;
                        slashWhite.EndCap = LineCap.Round;
                        graphics.DrawLine(slashWhite, 3.5f, 3.5f, 20.5f, 20.5f);
                    }
                }

                graphics.ResetTransform();
            }
            IntPtr newHIcon = bitmap.GetHicon();
            Icon icon = Icon.FromHandle(newHIcon);
            Icon? oldIcon = _notifyIcon.Icon;
            _notifyIcon.Icon = icon;
            if (oldIcon != null)
            {
                oldIcon.Dispose();
            }
            if (_currentHIcon != IntPtr.Zero)
            {
                DestroyIcon(_currentHIcon);
            }
            _currentHIcon = newHIcon;
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

    public void UpdateTrayIconState()
    {
        UpdateTrayIcon(_audioController?.IsMuted ?? false);
    }

    private static GraphicsPath CreateRoundedRectanglePath(RectangleF rect, float radius)
    {
        GraphicsPath path = new GraphicsPath();
        float d = radius * 2f;
        if (d > rect.Width) d = rect.Width;
        if (d > rect.Height) d = rect.Height;
        path.AddArc(rect.X, rect.Y, d, d, 180f, 90f);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270f, 90f);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0f, 90f);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90f, 90f);
        path.CloseFigure();
        return path;
    }

    private static void FillRoundedRectangle(Graphics g, Brush brush, float x, float y, float width, float height, float radius)
    {
        using GraphicsPath graphicsPath = CreateRoundedRectanglePath(new RectangleF(x, y, width, height), radius);
        g.FillPath(brush, graphicsPath);
    }

    private void AudioController_MuteStateChanged(object? sender, MuteStateChangedEventArgs e)
    {
        DiagnosticLogger.LogAudio($"Event: MuteStateChanged -> {(e.IsMuted ? "MUTED [Chill Red]" : "LIVE [Active]")}");
        if (Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished) return;
        Dispatcher.BeginInvoke((Action)delegate
        {
            if (!Dispatcher.HasShutdownStarted) UpdateTrayIcon(e.IsMuted);
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
            _mainWindow.Focus();
            var handle = new WindowInteropHelper(_mainWindow).Handle;
            if (handle != IntPtr.Zero)
            {
                ShowWindow(handle, 9); // SW_RESTORE
                SetForegroundWindow(handle);
            }
        }
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        e.Cancel = true;
        _mainWindow?.Hide();
    }

    private void ExitApp()
    {
        try { SettingsManager.Flush(); }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Settings could not be saved. MicMute will stay open so you can retry.\n\n" + ex.Message, "Save settings");
            return;
        }
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try { SettingsManager.Flush(); }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine("Could not save settings at shutdown: " + ex.Message);
        }
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
        if (_currentHIcon != IntPtr.Zero)
        {
            DestroyIcon(_currentHIcon);
            _currentHIcon = IntPtr.Zero;
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
