using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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

    internal class LiquidGlassContextMenu : ContextMenuStrip
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int cx, int cy);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll")]
        private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;

        public LiquidGlassContextMenu()
        {
            DoubleBuffered = true;
            ShowImageMargin = false;
            ShowCheckMargin = false;
            CanOverflow = false;
            Padding = new Padding(3, 4, 3, 4);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyWindowRounding();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible)
            {
                ApplyWindowRounding();
            }
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            ApplyWindowRounding();
        }

        private void ApplyWindowRounding()
        {
            if (Handle != IntPtr.Zero)
            {
                try
                {
                    int preference = DWMWCP_ROUND;
                    DwmSetWindowAttribute(Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
                }
                catch { }

                if (Width > 0 && Height > 0)
                {
                    try
                    {
                        IntPtr hRgn = CreateRoundRectRgn(0, 0, Width + 1, Height + 1, 16, 16);
                        if (hRgn != IntPtr.Zero)
                        {
                            if (SetWindowRgn(Handle, hRgn, true) == 0)
                            {
                                DeleteObject(hRgn);
                            }
                        }
                    }
                    catch { }
                }
            }
        }
    }

    internal class LiquidGlassMenuRenderer : ToolStripProfessionalRenderer
    {
        public LiquidGlassMenuRenderer()
            : base(new LiquidGlassColorTable())
        {
            RoundedEdges = true;
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            bool lightMode = SettingsManager.Load().LightMode;
            Color bgColor = lightMode ? Color.FromArgb(250, 250, 252) : Color.FromArgb(28, 28, 32);
            using SolidBrush brush = new SolidBrush(bgColor);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            RectangleF rect = new RectangleF(0.5f, 0.5f, e.ToolStrip.Width - 1f, e.ToolStrip.Height - 1f);
            using GraphicsPath path = CreateRoundedRectanglePath(rect, 8f);
            e.Graphics.FillPath(brush, path);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            bool lightMode = SettingsManager.Load().LightMode;
            Color borderColor = lightMode ? Color.FromArgb(228, 228, 231) : Color.FromArgb(45, 255, 255, 255);
            using Pen pen = new Pen(borderColor, 1f);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            RectangleF rect = new RectangleF(0.5f, 0.5f, e.ToolStrip.Width - 1f, e.ToolStrip.Height - 1f);
            using GraphicsPath path = CreateRoundedRectanglePath(rect, 8f);
            e.Graphics.DrawPath(pen, path);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (e.Item == null || e.ToolStrip == null) return;
            bool lightMode = SettingsManager.Load().LightMode;
            Color textColor = lightMode ? Color.FromArgb(24, 24, 27) : Color.FromArgb(244, 244, 246);
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Font font = e.TextFont ?? e.Item.Font;
            int fontHeight = font?.Height ?? 17;

            // Render vector outline icon
            int iconX = 14 - e.Item.Bounds.X;
            int iconY = (e.Item.Height - 16) / 2;
            RenderMenuIcon(e.Graphics, e.Item.Text, iconX, iconY, textColor);

            int leftOnMenu = 40;
            int x = leftOnMenu - e.Item.Bounds.X;
            int y = (e.Item.Height - fontHeight) / 2;

            Rectangle textRect = new Rectangle(x, y, Math.Max(0, e.ToolStrip.ClientRectangle.Width - leftOnMenu - 4), fontHeight);
            if (font != null)
            {
                TextRenderer.DrawText(e.Graphics, e.Text ?? string.Empty, font, textRect, textColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
        }

        private static void RenderMenuIcon(Graphics g, string? text, int iconX, int iconY, Color color)
        {
            if (string.IsNullOrEmpty(text)) return;

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            using Pen pen = new Pen(color, 1.25f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };

            if (text == "Toggle Mute")
            {
                // Microphone outline: condenser capsule, cradle, stem, base
                RectangleF capRect = new RectangleF(iconX + 5.5f, iconY + 1.5f, 5f, 7.5f);
                using GraphicsPath micCap = CreateRoundedRectanglePath(capRect, 2.5f);
                g.DrawPath(pen, micCap);

                RectangleF cradleRect = new RectangleF(iconX + 3f, iconY + 4f, 10f, 6.5f);
                g.DrawArc(pen, cradleRect, 0f, 180f);

                g.DrawLine(pen, iconX + 8f, iconY + 10.5f, iconX + 8f, iconY + 13.5f);
                g.DrawLine(pen, iconX + 5.5f, iconY + 13.5f, iconX + 10.5f, iconY + 13.5f);
            }
            else if (text == "Open App")
            {
                // Window outline with diagonal pop-out arrow
                RectangleF winRect = new RectangleF(iconX + 1.5f, iconY + 2f, 13f, 11.5f);
                using GraphicsPath winPath = CreateRoundedRectanglePath(winRect, 1.75f);
                g.DrawPath(pen, winPath);

                g.DrawLine(pen, iconX + 1.5f, iconY + 5.5f, iconX + 14.5f, iconY + 5.5f);

                // Pop-out arrow ↗
                g.DrawLine(pen, iconX + 5.5f, iconY + 10.5f, iconX + 10f, iconY + 7.5f);
                g.DrawLine(pen, iconX + 7.5f, iconY + 7.5f, iconX + 10f, iconY + 7.5f);
                g.DrawLine(pen, iconX + 10f, iconY + 7.5f, iconX + 10f, iconY + 10f);
            }
            else if (text == "Quit")
            {
                // Exit / Door bracket with centered 'x'
                using GraphicsPath quitPath = new GraphicsPath();
                quitPath.AddLine(iconX + 9.5f, iconY + 2.5f, iconX + 5.5f, iconY + 2.5f);
                quitPath.AddArc(iconX + 3f, iconY + 2.5f, 5f, 5f, 270f, -90f);
                quitPath.AddLine(iconX + 3f, iconY + 5f, iconX + 3f, iconY + 11f);
                quitPath.AddArc(iconX + 3f, iconY + 8.5f, 5f, 5f, 180f, -90f);
                quitPath.AddLine(iconX + 5.5f, iconY + 13.5f, iconX + 9.5f, iconY + 13.5f);
                g.DrawPath(pen, quitPath);

                // 'x' mark centered at (iconX + 10.5f, iconY + 8f)
                g.DrawLine(pen, iconX + 8.5f, iconY + 6.5f, iconX + 12.5f, iconY + 9.5f);
                g.DrawLine(pen, iconX + 8.5f, iconY + 9.5f, iconX + 12.5f, iconY + 6.5f);
            }
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            bool lightMode = SettingsManager.Load().LightMode;
            e.ArrowColor = lightMode ? Color.FromArgb(113, 113, 122) : Color.FromArgb(161, 161, 170);
            base.OnRenderArrow(e);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item != null && e.ToolStrip != null && e.Item.Selected && e.Item.Enabled)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                bool lightMode = SettingsManager.Load().LightMode;
                Color hoverColor = lightMode ? Color.FromArgb(232, 232, 237) : Color.FromArgb(25, 255, 255, 255);
                using SolidBrush brush = new SolidBrush(hoverColor);

                const int marginX = 4;
                int width = e.Item.Width - (marginX * 2);
                if (width > 0 && e.Item.Height > 2)
                {
                    Rectangle rect = new Rectangle(marginX, 1, width, e.Item.Height - 2);
                    using GraphicsPath path = CreateRoundedRectanglePath(rect, 5f);
                    e.Graphics.FillPath(brush, path);
                }
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            if (e.Item == null || e.ToolStrip == null) return;
            bool lightMode = SettingsManager.Load().LightMode;
            Color sepColor = lightMode ? Color.FromArgb(228, 228, 231) : Color.FromArgb(39, 39, 45);
            int y = e.Item.Height / 2;
            using Pen pen = new Pen(sepColor, 1f);
            const int marginX = 5;
            int x1 = marginX;
            int x2 = e.Item.Width - marginX;
            if (x2 > x1)
            {
                e.Graphics.DrawLine(pen, x1, y, x2, y);
            }
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

    private static AppInstanceMutex? _mutex;
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

        System.Windows.Forms.Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        System.Windows.Forms.Application.ThreadException += (s, ev) =>
        {
            DiagnosticLogger.LogError("WinForms ThreadException", ev.Exception);
            try
            {
                string p = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MicMute");
                Directory.CreateDirectory(p);
                File.WriteAllText(Path.Combine(p, "winforms_error.txt"), ev.Exception?.ToString() ?? "Unknown WinForms exception");
            }
            catch { }
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

        _mutex = AppInstanceMutex.TryAcquire(MutexName);
        if (_mutex == null)
        {
            if (DiagnosticLogger.IsEnabled)
            {
                DiagnosticLogger.LogWarning("MicMute is already running. Quit it from the tray before starting a diagnostic session.");
            }
            IntPtr existingHwnd = FindWindow(null, "Mic Mute");
            if (existingHwnd != IntPtr.Zero)
            {
                PostMessage(existingHwnd, (uint)WM_SHOWME, IntPtr.Zero, IntPtr.Zero);
            }
            else
            {
                PostMessage((IntPtr)HWND_BROADCAST, (uint)WM_SHOWME, IntPtr.Zero, IntPtr.Zero);
            }
            if (DiagnosticLogger.IsEnabled)
                System.Windows.MessageBox.Show("MicMute is already running. Quit it from the tray, then start diagnostics again.", "MicMute diagnostics");
            Environment.Exit(0);
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
        StartupManager.SetStartup(appSettings.RunOnStartup, appSettings.RunAsAdmin);
        _audioController = new AudioController();
        _audioController.SetTargetDeviceAsync(appSettings.SelectedDeviceId).GetAwaiter().GetResult();
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
            var handle = new WindowInteropHelper(_mainWindow).Handle;
            if (handle != IntPtr.Zero)
            {
                ShowWindow(handle, 9); // SW_RESTORE
                SetForegroundWindow(handle);
            }
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
        LiquidGlassContextMenu contextMenuStrip = new LiquidGlassContextMenu();
        contextMenuStrip.Renderer = new LiquidGlassMenuRenderer();
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
        })
        { AutoSize = true, Margin = new Padding(0), Padding = new Padding(0, 10, 48, 10) };

        ToolStripMenuItem value2 = new ToolStripMenuItem("Open App", null, delegate
        {
            ShowWindow();
        })
        { AutoSize = true, Margin = new Padding(0), Padding = new Padding(0, 10, 48, 10) };

        ToolStripMenuItem value3 = new ToolStripMenuItem("Quit", null, delegate
        {
            ExitApp();
        })
        { AutoSize = true, Margin = new Padding(0), Padding = new Padding(0, 10, 48, 10) };

        contextMenuStrip.Items.Add(value);
        contextMenuStrip.Items.Add(value2);
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
                    ? Color.FromArgb(225, 45, 45) // Standard clean UI red (#E12D2D)
                    : (lightMode ? Color.FromArgb(45, 45, 48) : Color.FromArgb(24, 24, 27)); // Sleek neutral glass
                Color badgeBorder = isMuted
                    ? Color.FromArgb(248, 113, 113) // Crisp highlight rim (#F87171)
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
        try
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.ContextMenuStrip?.Dispose();
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
        }
        catch { }

        try
        {
            if (_currentHIcon != IntPtr.Zero)
            {
                DestroyIcon(_currentHIcon);
                _currentHIcon = IntPtr.Zero;
            }
        }
        catch { }

        try { SettingsManager.Flush(); } catch { }

        try
        {
            if (_mainWindow != null)
            {
                _mainWindow.Closing -= MainWindow_Closing;
                _mainWindow.Close();
                _mainWindow = null;
            }
        }
        catch { }

        try
        {
            _audioController?.Dispose();
            _audioController = null;
        }
        catch { }

        try
        {
            _mutex?.Dispose();
            _mutex = null;
        }
        catch { }

        Environment.Exit(0);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try { SettingsManager.Flush(); }
        catch { }
        try
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.ContextMenuStrip?.Dispose();
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
        }
        catch { }
        try
        {
            if (_currentHIcon != IntPtr.Zero)
            {
                DestroyIcon(_currentHIcon);
                _currentHIcon = IntPtr.Zero;
            }
        }
        catch { }
        try
        {
            if (_mainWindow != null)
            {
                _mainWindow.Closing -= MainWindow_Closing;
                _mainWindow.Close();
                _mainWindow = null;
            }
        }
        catch { }
        try
        {
            _audioController?.Dispose();
            _audioController = null;
        }
        catch { }
        try
        {
            _mutex?.Dispose();
            _mutex = null;
        }
        catch { }
        base.OnExit(e);
    }
}
