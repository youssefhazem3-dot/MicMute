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

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool EnumDisplaySettings(string? deviceName, int modeNum, ref DEVMODE devMode);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool DestroyIcon(IntPtr handle);

    [DllImport("shell32.dll", SetLastError = true)]
    private static extern int SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);

    internal static void TrimWorkingSet()
    {
        // Intentionally left as a no-op: calling EmptyWorkingSet forces the process's
        // WH_KEYBOARD_LL hook callback and UI dispatcher pages out to the pagefile,
        // which can cause Windows LowLevelHooksTimeout silent hook drops under heavy game RAM pressure.
    }

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
            try { _isLightMode = SettingsManager.Load().LightMode; } catch (Exception ex) { DiagnosticLogger.LogWarning("Failed to load LightMode setting for theme: " + ex.Message); }
        }

        public LiquidGlassContextMenu()
        {
            DoubleBuffered = true;
            ShowImageMargin = false;
            ShowCheckMargin = false;
            CanOverflow = false;
            Padding = new Padding(0, 4, 0, 4);
            DropShadowEnabled = true;
            RefreshTheme();
        }

        public override System.Drawing.Size GetPreferredSize(System.Drawing.Size proposedSize)
        {
            System.Drawing.Size size = base.GetPreferredSize(proposedSize);
            // Clean 192px rectangle with breathing room for shortcut hints and divider gap above Quit
            return new System.Drawing.Size(Math.Max(size.Width, 192), Math.Max(size.Height, 104));
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

            // Clean divider line centered in the margin gap above Quit so hover pills never touch it
            for (int i = 1; i < e.ToolStrip.Items.Count; i++)
            {
                if (string.Equals(e.ToolStrip.Items[i].Text, "Quit", StringComparison.Ordinal))
                {
                    ToolStripItem prevItem = e.ToolStrip.Items[i - 1];
                    ToolStripItem quitItem = e.ToolStrip.Items[i];
                    if (prevItem.Bounds.Bottom > 0 && quitItem.Bounds.Top > prevItem.Bounds.Bottom)
                    {
                        float dividerY = (prevItem.Bounds.Bottom + quitItem.Bounds.Top) / 2f;
                        Color dividerColor = lightMode ? Color.FromArgb(26, 0, 0, 0) : Color.FromArgb(28, 255, 255, 255);
                        using Pen dividerPen = new Pen(dividerColor, 1f);
                        e.Graphics.DrawLine(dividerPen, 12f, dividerY, Math.Max(12f, e.ToolStrip.Width - 12f), dividerY);
                    }
                    break;
                }
            }
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
                bool isQuit = string.Equals(e.Item.Text, "Quit", StringComparison.Ordinal);
                Color hoverColor = isQuit
                    ? (lightMode ? Color.FromArgb(26, 220, 38, 38) : Color.FromArgb(41, 239, 68, 68))
                    : (lightMode ? Color.FromArgb(20, 0, 0, 0) : Color.FromArgb(32, 255, 255, 255));
                using SolidBrush brush = new SolidBrush(hoverColor);

                // Smooth rounded pill across the full width of the item section
                const float marginX = 4.5f;
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
            bool isQuit = string.Equals(e.Item.Text, "Quit", StringComparison.Ordinal);
            bool isQuitHover = isQuit && e.Item.Selected && e.Item.Enabled;

            // White in Dark Mode, dark gray in Light Mode; coral red on Quit hover
            Color textColor = isQuitHover
                ? (lightMode ? Color.FromArgb(220, 38, 38) : Color.FromArgb(248, 113, 113))
                : (lightMode ? Color.FromArgb(55, 65, 81) : Color.FromArgb(255, 255, 255));
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Font font = e.TextFont ?? e.Item.Font;
            int fontHeight = font?.Height ?? 15;

            // Render vector outline icon (coral-red slashed mic when muted)
            int iconX = 14;
            int iconY = (e.Item.Height - 15) / 2;
            Color iconColor = string.Equals(e.Item.Text, "Unmute Mic", StringComparison.Ordinal)
                ? (lightMode ? Color.FromArgb(220, 38, 38) : Color.FromArgb(248, 113, 113))
                : textColor;
            Color bgColor = lightMode ? Color.FromArgb(255, 255, 255) : Color.FromArgb(18, 18, 20);
            RenderMenuIcon(e.Graphics, e.Item.Text, iconX, iconY, iconColor, bgColor);

            // Render label and optional right-aligned shortcut hint
            int leftOnMenu = 38;
            int rightMargin = 14;
            int y = (e.Item.Height - fontHeight) / 2;
            string mainText = e.Text ?? string.Empty;
            int hintReserved = 0;

            if (font != null && e.Item.Tag is string hint && !string.IsNullOrEmpty(hint) && !string.Equals(hint, "None", StringComparison.Ordinal))
            {
                using Font hintFont = new Font(font.FontFamily, Math.Max(7.5f, font.Size - 1.2f), System.Drawing.FontStyle.Regular, GraphicsUnit.Point);
                System.Drawing.Size mainSize = TextRenderer.MeasureText(e.Graphics, mainText, font, new System.Drawing.Size(int.MaxValue, fontHeight), TextFormatFlags.NoPadding);
                System.Drawing.Size hintSize = TextRenderer.MeasureText(e.Graphics, hint, hintFont, new System.Drawing.Size(int.MaxValue, hintFont.Height), TextFormatFlags.NoPadding);
                if (leftOnMenu + mainSize.Width + 8 + hintSize.Width <= e.Item.Width - rightMargin)
                {
                    hintReserved = hintSize.Width + 8;
                    int hintY = ((e.Item.Height - hintFont.Height) / 2) + 1;
                    Rectangle hintRect = new Rectangle(leftOnMenu + mainSize.Width + 6, hintY, Math.Max(0, e.Item.Width - (leftOnMenu + mainSize.Width + 6) - rightMargin), hintFont.Height);
                    Color hintColor = lightMode ? Color.FromArgb(148, 163, 184) : Color.FromArgb(136, 136, 146);
                    TextRenderer.DrawText(e.Graphics, hint, hintFont, hintRect, hintColor,
                        TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                }
            }

            Rectangle textRect = new Rectangle(leftOnMenu, y, Math.Max(0, e.Item.Width - leftOnMenu - rightMargin - hintReserved), fontHeight);
            if (font != null)
            {
                TextRenderer.DrawText(e.Graphics, mainText, font, textRect, textColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
        }

        private static void RenderMenuIcon(Graphics g, string? text, int iconX, int iconY, Color color, Color bgColor)
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

            if (text is "Toggle Mute" or "Mute Mic" or "Unmute Mic")
            {
                // Microphone outline: condenser body, U-cradle, stem, foot (15x15 box)
                RectangleF capRect = new RectangleF(iconX + 4.5f, iconY + 1f, 5f, 8.5f);
                using GraphicsPath micCap = CreateRoundedRectanglePath(capRect, 2.5f);
                g.DrawPath(pen, micCap);

                RectangleF cradleRect = new RectangleF(iconX + 2f, iconY + 4.5f, 10f, 7f);
                g.DrawArc(pen, cradleRect, 0f, 180f);

                g.DrawLine(pen, iconX + 7f, iconY + 11.5f, iconX + 7f, iconY + 14f);
                g.DrawLine(pen, iconX + 4.5f, iconY + 14f, iconX + 9.5f, iconY + 14f);

                if (text == "Unmute Mic")
                {
                    using Pen maskPen = new Pen(bgColor, 3.2f)
                    {
                        StartCap = LineCap.Round,
                        EndCap = LineCap.Round
                    };
                    g.DrawLine(maskPen, iconX + 2f, iconY + 1.5f, iconX + 12f, iconY + 12f);

                    using Pen slashPen = new Pen(color, 1.6f)
                    {
                        StartCap = LineCap.Round,
                        EndCap = LineCap.Round
                    };
                    g.DrawLine(slashPen, iconX + 2f, iconY + 1.5f, iconX + 12f, iconY + 12f);
                }
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
    private ToolStripMenuItem? _trayMuteMenuItem;
    private AudioController? _audioController;
    private MainWindow? _mainWindow;

    [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    private int _cachedTrayIconSize;
    private Icon? _trayIconMuted;
    private IntPtr _hIconMuted = IntPtr.Zero;
    private Icon? _trayIconActiveDark;
    private IntPtr _hIconActiveDark = IntPtr.Zero;
    private Icon? _trayIconActiveLight;
    private IntPtr _hIconActiveLight = IntPtr.Zero;

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

    /// <summary>
    /// Explicit implementation required when EnableDefaultApplicationDefinition is disabled.
    /// Application resources are configured programmatically or loaded on demand.
    /// </summary>
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

    // WinForms NotifyIcon encapsulates the underlying HWND and icon uID in private fields.
    // We inspect them via reflection to query exact shell coordinates via Shell_NotifyIconGetRect.
    // If reflection fails or internal structures change across runtime updates, the method falls back to TrayNotifyWnd bounds below.
    private static readonly System.Reflection.FieldInfo? NotifyIconIdField =
        typeof(NotifyIcon).GetField("id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    private static readonly System.Reflection.FieldInfo? NotifyIconWindowField =
        typeof(NotifyIcon).GetField("window", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

    internal System.Drawing.Point? GetTrayIconScreenPosition()
    {
        try
        {
            if (_notifyIcon != null && NotifyIconIdField != null && NotifyIconWindowField != null)
            {
                int id = (int)NotifyIconIdField.GetValue(_notifyIcon)!;
                NativeWindow? win = NotifyIconWindowField.GetValue(_notifyIcon) as NativeWindow;
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
        catch (Exception ex)
        {
            DiagnosticLogger.LogError("GetTrayIconScreenPosition failed", ex);
        }

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

        // Automatic High Refresh Rate Detection (capped at 120 FPS to avoid heavy DWM layered composition)
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
        AdminManager.SetRunAsAdmin(appSettings.RunAsAdmin);
        if (appSettings.RunAsAdmin && !AdminManager.IsRunningAsAdmin() && !UiBehavior.TryGetParentProcessId(e.Args, out _))
        {
            _mutex.Dispose();
            _mutex = null;
            if (AdminManager.RestartAsAdmin(showWindow: !UiBehavior.ShouldStartMinimized(appSettings.StartMinimized, e.Args)))
            {
                Environment.Exit(0);
                return;
            }
            _mutex = AppInstanceMutex.TryAcquire(MutexName);
            if (_mutex == null)
            {
                Environment.Exit(0);
                return;
            }
        }
        StartupManager.SetStartup(appSettings.RunOnStartup, appSettings.RunAsAdmin);
        _audioController = new AudioController
        {
            SyncAllMicrophones = appSettings.SyncAllMicrophones,
            LockMuteState = appSettings.LockMuteState
        };
        _audioController.MuteStateChanged += AudioController_MuteStateChanged;
        _audioController.SetTargetDevice(appSettings.SelectedDeviceId);
        if (appSettings.StartupMuteMode == 1)
        {
            _audioController.SetMute(true, showOsd: false);
        }
        else if (appSettings.StartupMuteMode == 2)
        {
            _audioController.SetMute(false, showOsd: false);
        }
        InitializeTrayIcon();

        if (DiagnosticLogger.IsEnabled)
        {
            DiagnosticLogger.LogAudio($"Active capture endpoint: '{_audioController.CurrentDeviceName}' (Muted: {_audioController.IsMuted})");
            DiagnosticLogger.LogInfo($"Shortcut configured: {appSettings.Hotkey} (Modifiers: {appSettings.HotkeyModifiers})");
        }

        _mainWindow = new MainWindow(_audioController);
        _mainWindow.TaskbarCreated += (_, _) =>
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Visible = true;
                UpdateTrayIconState();
            }
        };
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
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(TrimWorkingSet));
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
        _notifyIcon.MouseClick += (_, e) =>
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                ShowWindow();
            }
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

        ToolStripMenuItem value = new ToolStripMenuItem("Mute Mic", null, delegate
        {
            _mainWindow?.ClearHoldToggleState();
            _audioController?.ToggleMute();
        })
        {
            AutoSize = false,
            Size = new System.Drawing.Size(192, 29),
            Margin = new Padding(0, 1, 0, 1),
            Padding = new Padding(0)
        };
        _trayMuteMenuItem = value;

        ToolStripMenuItem value2 = new ToolStripMenuItem("Open App", null, delegate
        {
            ShowWindow();
        })
        {
            AutoSize = false,
            Size = new System.Drawing.Size(192, 29),
            Margin = new Padding(0, 1, 0, 1),
            Padding = new Padding(0)
        };

        ToolStripMenuItem value3 = new ToolStripMenuItem("Quit", null, delegate
        {
            ExitApp();
        })
        {
            AutoSize = false,
            Size = new System.Drawing.Size(192, 29),
            Margin = new Padding(0, 7, 0, 1),
            Padding = new Padding(0)
        };

        contextMenuStrip.Items.Add(value);
        contextMenuStrip.Items.Add(value2);
        contextMenuStrip.Items.Add(value3);
        contextMenuStrip.Opening += delegate
        {
            SyncTrayMuteMenuItem(_audioController?.IsMuted ?? false);
        };
        _notifyIcon.ContextMenuStrip = contextMenuStrip;
        UpdateTrayIcon(_audioController?.IsMuted ?? false);
    }

    private void SyncTrayMuteMenuItem(bool isMuted)
    {
        if (_trayMuteMenuItem == null)
        {
            return;
        }
        _trayMuteMenuItem.Text = isMuted ? "Unmute Mic" : "Mute Mic";
        AppSettings settings = SettingsManager.Load();
        _trayMuteMenuItem.Tag = UiBehavior.FormatHotkeyText(settings.Hotkey, settings.HotkeyModifiers, compact: true);
    }

    private void UpdateTrayIcon(bool isMuted)
    {
        SyncTrayMuteMenuItem(isMuted);
        if (_notifyIcon == null)
        {
            return;
        }
        string text = _audioController?.CurrentDeviceName ?? "No microphone";
        string text2 = string.IsNullOrEmpty(_audioController?.CurrentDeviceId) ? "NO MICROPHONE" : isMuted ? "MUTED" : "ACTIVE";
        string tooltip = UiBehavior.LimitTooltip("Mic Mute (" + text2 + ")\nDevice: " + text);
        if (!string.Equals(_notifyIcon.Text, tooltip, StringComparison.Ordinal))
        {
            _notifyIcon.Text = tooltip;
        }
        try
        {
            int size = Math.Max(16, SystemInformation.SmallIconSize.Width);
            if (_cachedTrayIconSize != size)
            {
                DisposeCachedTrayIcons();
                _cachedTrayIconSize = size;
            }

            bool lightMode = SettingsManager.Load().LightMode;
            Icon? icon = GetOrCreateCachedTrayIcon(size, isMuted, lightMode);
            if (icon != null && !ReferenceEquals(_notifyIcon.Icon, icon))
            {
                _notifyIcon.Icon = icon;
            }
        }
        catch (Exception ex)
        {
            DiagnosticLogger.LogError("UpdateTrayIcon failed", ex);
        }
    }

    private Icon? GetOrCreateCachedTrayIcon(int size, bool isMuted, bool lightMode)
    {
        if (isMuted)
        {
            if (_trayIconMuted == null)
                (_trayIconMuted, _hIconMuted) = RenderTrayIcon(size, isMuted: true, lightMode: false);
            return _trayIconMuted;
        }
        if (lightMode)
        {
            if (_trayIconActiveLight == null)
                (_trayIconActiveLight, _hIconActiveLight) = RenderTrayIcon(size, isMuted: false, lightMode: true);
            return _trayIconActiveLight;
        }
        if (_trayIconActiveDark == null)
            (_trayIconActiveDark, _hIconActiveDark) = RenderTrayIcon(size, isMuted: false, lightMode: false);
        return _trayIconActiveDark;
    }

    private static (Icon Icon, IntPtr Handle) RenderTrayIcon(int size, bool isMuted, bool lightMode)
    {
        using Bitmap bitmap = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.Clear(Color.Transparent);

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

            // Center and scale microphone glyph from reference design (24x24 box)
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

        IntPtr hIcon = bitmap.GetHicon();
        try
        {
            return (Icon.FromHandle(hIcon), hIcon);
        }
        catch
        {
            DestroyIcon(hIcon);
            throw;
        }
    }

    private void DisposeCachedTrayIcons()
    {
        if (_notifyIcon != null) _notifyIcon.Icon = null;

        _trayIconMuted?.Dispose();
        _trayIconMuted = null;
        if (_hIconMuted != IntPtr.Zero) { DestroyIcon(_hIconMuted); _hIconMuted = IntPtr.Zero; }

        _trayIconActiveDark?.Dispose();
        _trayIconActiveDark = null;
        if (_hIconActiveDark != IntPtr.Zero) { DestroyIcon(_hIconActiveDark); _hIconActiveDark = IntPtr.Zero; }

        _trayIconActiveLight?.Dispose();
        _trayIconActiveLight = null;
        if (_hIconActiveLight != IntPtr.Zero) { DestroyIcon(_hIconActiveLight); _hIconActiveLight = IntPtr.Zero; }
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
        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(TrimWorkingSet));
    }

    private void Teardown()
    {
        try { SettingsManager.Flush(); }
        catch (Exception ex) { DiagnosticLogger.LogError("SettingsManager.Flush failed during teardown", ex); }

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
        catch (Exception ex) { DiagnosticLogger.LogError("NotifyIcon disposal failed during teardown", ex); }

        try
        {
            DisposeCachedTrayIcons();
        }
        catch (Exception ex) { DiagnosticLogger.LogError("Tray icon handle destruction failed during teardown", ex); }

        try
        {
            if (_mainWindow != null)
            {
                _mainWindow.Closing -= MainWindow_Closing;
                _mainWindow.Close();
                _mainWindow = null;
            }
        }
        catch (Exception ex) { DiagnosticLogger.LogError("MainWindow close failed during teardown", ex); }

        try { OsdWindow.CloseOsd(); }
        catch (Exception ex) { DiagnosticLogger.LogError("OsdWindow.CloseOsd failed during teardown", ex); }

        try { AudioFeedback.Dispose(); }
        catch (Exception ex) { DiagnosticLogger.LogError("AudioFeedback.Dispose failed during teardown", ex); }

        try
        {
            if (_audioController != null)
            {
                _audioController.MuteStateChanged -= AudioController_MuteStateChanged;
                _audioController.Dispose();
                _audioController = null;
            }
        }
        catch (Exception ex) { DiagnosticLogger.LogError("AudioController disposal failed during teardown", ex); }

        try
        {
            _mutex?.Dispose();
            _mutex = null;
        }
        catch (Exception ex) { DiagnosticLogger.LogError("Mutex disposal failed during teardown", ex); }
    }

    private void ExitApp()
    {
        Teardown();
        Environment.Exit(0);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Teardown();
        base.OnExit(e);
    }
}
