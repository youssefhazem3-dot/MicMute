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

    [DllImport("shell32.dll", SetLastError = true)]
    private static extern int SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);

    internal class LiquidGlassContextMenu : ContextMenuStrip
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;

        private bool _isLightMode;
        public bool IsLightMode => _isLightMode;

        public void RefreshTheme()
        {
            try { _isLightMode = SettingsManager.Load().LightMode; } catch { }
        }

        public LiquidGlassContextMenu()
        {
            DoubleBuffered = true;
            ShowImageMargin = false;
            ShowCheckMargin = false;
            CanOverflow = false;
            Padding = new Padding(4, 4, 4, 4);
            DropShadowEnabled = true;
            RefreshTheme();
        }

        public override System.Drawing.Size GetPreferredSize(System.Drawing.Size proposedSize)
        {
            System.Drawing.Size size = base.GetPreferredSize(proposedSize);
            // Compact wide rectangle (20% smaller) matching modern Windows 11 system tray menus
            return new System.Drawing.Size(Math.Max(size.Width, 164), Math.Max(size.Height, 98));
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
                return cp;
            }
        }

        protected override void OnOpening(CancelEventArgs e)
        {
            RefreshTheme();
            base.OnOpening(e);
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
                RefreshTheme();
                ApplyWindowRounding();
            }
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
            }
        }
    }

    internal class LiquidGlassMenuRenderer : ToolStripProfessionalRenderer
    {
        private static bool GetLightMode(ToolStrip? toolStrip) =>
            (toolStrip as LiquidGlassContextMenu)?.IsLightMode ?? SettingsManager.Load().LightMode;

        public LiquidGlassMenuRenderer(LiquidGlassContextMenu? menu = null)
            : base(new LiquidGlassColorTable(() => menu?.IsLightMode ?? SettingsManager.Load().LightMode))
        {
            RoundedEdges = true;
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            if (e.ToolStrip == null) return;
            bool lightMode = GetLightMode(e.ToolStrip);
            // Black in Dark Mode, White in Light Mode
            Color bgColor = lightMode ? Color.FromArgb(255, 255, 255) : Color.FromArgb(18, 18, 20);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            e.Graphics.Clear(bgColor);

            using SolidBrush brush = new SolidBrush(bgColor);
            RectangleF rect = new RectangleF(0.5f, 0.5f, e.ToolStrip.Width - 1f, e.ToolStrip.Height - 1f);
            using GraphicsPath path = CreateRoundedRectanglePath(rect, 8f);
            e.Graphics.FillPath(brush, path);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            if (e.ToolStrip == null) return;
            bool lightMode = GetLightMode(e.ToolStrip);
            // Gray border in Light Mode, subtle white in Dark Mode
            Color borderColor = lightMode ? Color.FromArgb(218, 220, 224) : Color.FromArgb(45, 255, 255, 255);
            using Pen pen = new Pen(borderColor, 1f);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            RectangleF rect = new RectangleF(0.5f, 0.5f, e.ToolStrip.Width - 1f, e.ToolStrip.Height - 1f);
            using GraphicsPath path = CreateRoundedRectanglePath(rect, 8f);
            e.Graphics.DrawPath(pen, path);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item == null) return;

            if (e.Item.Selected && e.Item.Enabled)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                bool lightMode = GetLightMode(e.ToolStrip);
                Color hoverColor = lightMode ? Color.FromArgb(20, 0, 0, 0) : Color.FromArgb(32, 255, 255, 255);
                using SolidBrush brush = new SolidBrush(hoverColor);

                // Smooth rounded pill across the full width of the item section
                const float marginX = 3.5f;
                const float marginY = 1.2f;
                float width = e.Item.Width - (marginX * 2f);
                float height = e.Item.Height - (marginY * 2f);
                if (width > 0 && height > 0)
                {
                    RectangleF rect = new RectangleF(marginX, marginY, width, height);
                    using GraphicsPath path = CreateRoundedRectanglePath(rect, 6f);
                    e.Graphics.FillPath(brush, path);
                }
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (e.Item == null) return;
            bool lightMode = GetLightMode(e.ToolStrip);
            // White in Dark Mode, dark gray in Light Mode
            Color textColor = lightMode ? Color.FromArgb(55, 65, 81) : Color.FromArgb(255, 255, 255);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Font font = e.TextFont ?? e.Item.Font;
            int fontHeight = font?.Height ?? 15;

            // Render vector outline icon (bold, crisp, perfectly proportioned for 20% smaller layout)
            int iconX = 14;
            int iconY = (e.Item.Height - 15) / 2;
            RenderMenuIcon(e.Graphics, e.Item.Text, iconX, iconY, textColor);

            // Render text
            int leftOnMenu = 38;
            int x = leftOnMenu;
            int y = (e.Item.Height - fontHeight) / 2;

            Rectangle textRect = new Rectangle(x, y, Math.Max(0, e.Item.Width - leftOnMenu - 6), fontHeight);
            if (font != null)
            {
                TextRenderer.DrawText(e.Graphics, e.Text ?? string.Empty, font, textRect, textColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
        }

        private static void RenderMenuIcon(Graphics g, string? text, int iconX, int iconY, Color color)
        {
            if (string.IsNullOrEmpty(text)) return;

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            using Pen pen = new Pen(color, 1.5f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };

            if (text == "Toggle Mute")
            {
                // Microphone outline: condenser body, U-cradle, stem, foot (15x15 box)
                RectangleF capRect = new RectangleF(iconX + 4.5f, iconY + 1f, 5f, 8.5f);
                using GraphicsPath micCap = CreateRoundedRectanglePath(capRect, 2.5f);
                g.DrawPath(pen, micCap);

                RectangleF cradleRect = new RectangleF(iconX + 2f, iconY + 4.5f, 10f, 7f);
                g.DrawArc(pen, cradleRect, 0f, 180f);

                g.DrawLine(pen, iconX + 7f, iconY + 11.5f, iconX + 7f, iconY + 14f);
                g.DrawLine(pen, iconX + 4.5f, iconY + 14f, iconX + 9.5f, iconY + 14f);
            }
            else if (text == "Open App")
            {
                // App window outline with top titlebar and diagonal pop-out arrow ↗
                RectangleF winRect = new RectangleF(iconX + 1.5f, iconY + 1.5f, 12f, 11f);
                using GraphicsPath winPath = CreateRoundedRectanglePath(winRect, 2f);
                g.DrawPath(pen, winPath);

                g.DrawLine(pen, iconX + 1.5f, iconY + 5f, iconX + 13.5f, iconY + 5f);

                // Pop-out arrow ↗
                g.DrawLine(pen, iconX + 5f, iconY + 10f, iconX + 9.5f, iconY + 6.5f);
                g.DrawLine(pen, iconX + 7f, iconY + 6.5f, iconX + 9.5f, iconY + 6.5f);
                g.DrawLine(pen, iconX + 9.5f, iconY + 6.5f, iconX + 9.5f, iconY + 9f);
            }
            else if (text == "Quit")
            {
                // Exit doorway bracket on left with centered 'x' cross
                using GraphicsPath quitPath = new GraphicsPath();
                quitPath.AddLine(iconX + 8.5f, iconY + 2f, iconX + 4.5f, iconY + 2f);
                quitPath.AddArc(iconX + 2f, iconY + 2f, 4f, 4f, 270f, -90f);
                quitPath.AddLine(iconX + 2f, iconY + 4f, iconX + 2f, iconY + 11f);
                quitPath.AddArc(iconX + 2f, iconY + 9f, 4f, 4f, 180f, -90f);
                quitPath.AddLine(iconX + 4.5f, iconY + 13f, iconX + 8.5f, iconY + 13f);
                g.DrawPath(pen, quitPath);

                // 'x' cross
                g.DrawLine(pen, iconX + 7f, iconY + 5.5f, iconX + 12f, iconY + 10.5f);
                g.DrawLine(pen, iconX + 7f, iconY + 10.5f, iconX + 12f, iconY + 5.5f);
            }
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            // Suppress default image margin
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            // Suppress separator
        }
    }

    private class LiquidGlassColorTable : ProfessionalColorTable
    {
        private readonly Func<bool> _getIsLight;

        public LiquidGlassColorTable(Func<bool>? getIsLight = null)
        {
            _getIsLight = getIsLight ?? (() => SettingsManager.Load().LightMode);
        }

        private bool IsLight => _getIsLight();

        private Color BgWindow => IsLight ? Color.FromArgb(255, 255, 255) : Color.FromArgb(18, 18, 20);
        private Color Border => IsLight ? Color.FromArgb(218, 220, 224) : Color.FromArgb(45, 255, 255, 255);

        public override Color ToolStripDropDownBackground => BgWindow;
        public override Color MenuBorder => Border;
        public override Color MenuItemBorder => Color.Transparent;
        public override Color MenuItemSelected => Color.Transparent;
        public override Color MenuItemSelectedGradientBegin => Color.Transparent;
        public override Color MenuItemSelectedGradientEnd => Color.Transparent;
        public override Color MenuItemPressedGradientBegin => Color.Transparent;
        public override Color MenuItemPressedGradientEnd => Color.Transparent;
        public override Color ImageMarginGradientBegin => BgWindow;
        public override Color ImageMarginGradientEnd => BgWindow;
        public override Color ImageMarginGradientMiddle => BgWindow;
    }

    private static bool _frameRateMetadataOverridden;
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
                string p = SettingsManager.GetDataFolderPath();
                Directory.CreateDirectory(p);
                File.WriteAllText(Path.Combine(p, "winforms_error.txt"), ev.Exception?.ToString() ?? "Unknown WinForms exception");
            }
            catch { }
        };

        AppDomain.CurrentDomain.UnhandledException += (s, ev) =>
        {
            try
            {
                string p = SettingsManager.GetDataFolderPath();
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
                string p = SettingsManager.GetDataFolderPath();
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

    [StructLayout(LayoutKind.Sequential)]
    private struct NOTIFYICONIDENTIFIER
    {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uID;
        public Guid guidItem;
    }

    [DllImport("shell32.dll", SetLastError = true)]
    private static extern int Shell_NotifyIconGetRect(ref NOTIFYICONIDENTIFIER identifier, out RECT iconLocation);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string? lpszClass, string? lpszWindow);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    internal System.Drawing.Point? GetTrayIconScreenPosition()
    {
        try
        {
            if (_notifyIcon != null)
            {
                var idField = typeof(NotifyIcon).GetField("id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var windowField = typeof(NotifyIcon).GetField("window", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (idField != null && windowField != null)
                {
                    int id = (int)idField.GetValue(_notifyIcon)!;
                    NativeWindow win = (NativeWindow)windowField.GetValue(_notifyIcon)!;
                    if (win != null && win.Handle != IntPtr.Zero)
                    {
                        NOTIFYICONIDENTIFIER nid = new NOTIFYICONIDENTIFIER
                        {
                            cbSize = (uint)Marshal.SizeOf<NOTIFYICONIDENTIFIER>(),
                            hWnd = win.Handle,
                            uID = (uint)id,
                            guidItem = Guid.Empty
                        };
                        if (Shell_NotifyIconGetRect(ref nid, out RECT rect) == 0 && (rect.Right > rect.Left) && (rect.Bottom > rect.Top))
                        {
                            return new System.Drawing.Point(
                                rect.Left + (rect.Right - rect.Left) / 2,
                                rect.Top + (rect.Bottom - rect.Top) / 2
                            );
                        }
                    }
                }
            }
        }
        catch { }

        try
        {
            IntPtr hTaskbar = FindWindow("Shell_TrayWnd", null);
            if (hTaskbar != IntPtr.Zero)
            {
                IntPtr hTray = FindWindowEx(hTaskbar, IntPtr.Zero, "TrayNotifyWnd", null);
                IntPtr target = hTray != IntPtr.Zero ? hTray : hTaskbar;
                if (GetWindowRect(target, out RECT r) && (r.Right > r.Left) && (r.Bottom > r.Top))
                {
                    return new System.Drawing.Point(
                        r.Left + (r.Right - r.Left) / 2,
                        r.Top + (r.Bottom - r.Top) / 2
                    );
                }
            }
        }
        catch { }

        try
        {
            var area = System.Windows.Forms.Screen.PrimaryScreen?.WorkingArea ?? System.Drawing.Rectangle.Empty;
            if (area.Width > 0 && area.Height > 0)
            {
                return new System.Drawing.Point(area.Right - 40, area.Bottom - 20);
            }
        }
        catch { }

        return null;
    }

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
            if (!_frameRateMetadataOverridden)
            {
                Timeline.DesiredFrameRateProperty.OverrideMetadata(typeof(Timeline), new FrameworkPropertyMetadata(refreshRate));
                _frameRateMetadataOverridden = true;
                if (DiagnosticLogger.IsEnabled)
                {
                    DiagnosticLogger.LogInfo($"Display refresh rate set to: {refreshRate} FPS");
                }
            }
        }
        catch
        {
        }

        try
        {
            SetCurrentProcessExplicitAppUserModelID("MicMute.App");
        }
        catch { }

        base.OnStartup(e);
        base.ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Preload the OSD to reduce work on the first toggle.
        OsdWindow.WarmUp();

        AppSettings appSettings = SettingsManager.Load();
        StartupManager.SetStartup(appSettings.RunOnStartup, appSettings.RunAsAdmin);
        _audioController = new AudioController();
        _audioController.MuteStateChanged += AudioController_MuteStateChanged;
        _audioController.SetTargetDevice(appSettings.SelectedDeviceId);
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
        contextMenuStrip.Renderer = new LiquidGlassMenuRenderer(contextMenuStrip);
        try
        {
            contextMenuStrip.Font = new Font("Segoe UI Variable Text", 9f, System.Drawing.FontStyle.Bold, GraphicsUnit.Point);
        }
        catch
        {
            try
            {
                contextMenuStrip.Font = new Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold, GraphicsUnit.Point);
            }
            catch
            {
                contextMenuStrip.Font = new Font("Arial", 9f, System.Drawing.FontStyle.Bold, GraphicsUnit.Point);
            }
        }

        ToolStripMenuItem value = new ToolStripMenuItem("Toggle Mute", null, delegate
        {
            _audioController?.ToggleMute();
        })
        {
            AutoSize = false,
            Size = new System.Drawing.Size(164, 29),
            Margin = new Padding(0, 1, 0, 1),
            Padding = new Padding(0)
        };

        ToolStripMenuItem value2 = new ToolStripMenuItem("Open App", null, delegate
        {
            ShowWindow();
        })
        {
            AutoSize = false,
            Size = new System.Drawing.Size(164, 29),
            Margin = new Padding(0, 1, 0, 1),
            Padding = new Padding(0)
        };

        ToolStripMenuItem value3 = new ToolStripMenuItem("Quit", null, delegate
        {
            ExitApp();
        })
        {
            AutoSize = false,
            Size = new System.Drawing.Size(164, 29),
            Margin = new Padding(0, 1, 0, 1),
            Padding = new Padding(0)
        };

        contextMenuStrip.Items.Add(value);
        contextMenuStrip.Items.Add(value2);
        contextMenuStrip.Items.Add(value3);
        _notifyIcon.ContextMenuStrip = contextMenuStrip;
        UpdateTrayIcon(_audioController?.IsMuted ?? false);

        WarmUpContextMenu(contextMenuStrip);
    }

    private static void WarmUpContextMenu(LiquidGlassContextMenu menu)
    {
        try
        {
            // Force Win32 HWND creation, triggering OnHandleCreated and ApplyWindowRounding (DwmSetWindowAttribute)
            _ = menu.Handle;

            // Perform layout calculation
            menu.PerformLayout();

            // Pre-execute a dummy render pass to JIT compile all renderer methods and initialize GDI/GDI+ caches
            using Bitmap dummyBmp = new Bitmap(164, 98);
            using Graphics g = Graphics.FromImage(dummyBmp);
            Rectangle bounds = new Rectangle(0, 0, 164, 98);

            ToolStripRenderEventArgs bgArgs = new ToolStripRenderEventArgs(g, menu, bounds, Color.Empty);
            menu.Renderer.DrawToolStripBackground(bgArgs);
            menu.Renderer.DrawToolStripBorder(bgArgs);

            foreach (ToolStripItem item in menu.Items)
            {
                ToolStripItemRenderEventArgs itemArgs = new ToolStripItemRenderEventArgs(g, item);
                menu.Renderer.DrawMenuItemBackground(itemArgs);
                ToolStripItemTextRenderEventArgs textArgs = new ToolStripItemTextRenderEventArgs(
                    g, item, item.Text, item.Bounds, Color.White, item.Font, TextFormatFlags.Default);
                menu.Renderer.DrawItemText(textArgs);
            }
        }
        catch { }
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
            try
            {
                Icon icon = Icon.FromHandle(newHIcon);
                Icon? oldIcon = _notifyIcon.Icon;
                _notifyIcon.Icon = icon;
                oldIcon?.Dispose();

                if (_currentHIcon != IntPtr.Zero)
                {
                    DestroyIcon(_currentHIcon);
                }
                _currentHIcon = newHIcon;
            }
            catch
            {
                // Ensure native GDI icon handle is released if Icon.FromHandle or NotifyIcon fails
                DestroyIcon(newHIcon);
                throw;
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

    private void ShowWindow()
    {
        if (_mainWindow != null)
        {
            bool wasHiddenOrMinimized = !_mainWindow.IsVisible || _mainWindow.WindowState == WindowState.Minimized;
            var trayPos = GetTrayIconScreenPosition();

            if (wasHiddenOrMinimized)
            {
                _mainWindow.PlayOpenFromTrayAnimation(trayPos);
            }
            else
            {
                _mainWindow.Activate();
                _mainWindow.Focus();
                var handle = new WindowInteropHelper(_mainWindow).Handle;
                if (handle != IntPtr.Zero)
                {
                    SetForegroundWindow(handle);
                }
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

        try { OsdWindow.HideOsd(); } catch { }
        try { AudioFeedback.Dispose(); } catch { }

        try
        {
            if (_audioController != null)
            {
                _audioController.MuteStateChanged -= AudioController_MuteStateChanged;
                _audioController.Dispose();
                _audioController = null;
            }
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
        try { OsdWindow.HideOsd(); } catch { }
        try { AudioFeedback.Dispose(); } catch { }

        try
        {
            if (_audioController != null)
            {
                _audioController.MuteStateChanged -= AudioController_MuteStateChanged;
                _audioController.Dispose();
                _audioController = null;
            }
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
