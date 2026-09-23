using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace MicMute;

public partial class MainWindow : Window
{
    private readonly AudioController _audioController;
    private HotkeyManager? _hotkeyManager;
    private bool _isRecordingHotkey;
    private bool _isUpdatingDeviceList;
    private bool _isInitialized;
    private bool _isUpdatingOsdTextFromSlider;
    private bool _isUpdatingSoundVolumeTextFromSlider;
    private bool _contentLoaded;
    private bool _isReloadingSettings;
    private bool _isDisposed;
    private readonly DispatcherDebouncer _deviceRefreshDebouncer;
    private readonly DispatcherDebouncer _statusDebouncer;
    private readonly LatestRefreshCoordinator<List<AudioDevice>> _deviceRefreshCoordinator;
    private readonly double _preferredWidth;
    private bool _hasInitialCentered;
    private double? _osdDurationBeforeEdit;
    private int? _soundVolumeBeforeEdit;

    internal System.Windows.Controls.Button btnStateToggle = null!;
    internal TextBlock tbStatusText = null!;
    internal DropShadowEffect statusGlow = null!;
    internal Border? borderStatusCapsule;
    internal System.Windows.Shapes.Ellipse? dotStatus;
    internal DropShadowEffect? dotGlow;
    internal System.Windows.Controls.Button? btnTitleTheme;
    internal System.Windows.Shapes.Path? pathTitleTheme;
    internal System.Windows.Shapes.Path? titleBarIcon;
    internal System.Windows.Controls.Button? btnMin;
    internal System.Windows.Controls.Button? btnCls;
    internal System.Windows.Controls.ComboBox cbDevices = null!;
    internal Border borderHotkey = null!;
    internal TextBlock tbHotkey = null!;
    internal TextBlock? tbHeroHotkey;
    internal System.Windows.Controls.Button btnRecordHotkey = null!;
    internal System.Windows.Controls.CheckBox cbEnableOsd = null!;
    internal Slider sliderOsdDuration = null!;
    internal Border? borderOsdDurationKeycap;
    internal System.Windows.Controls.TextBox txtOsdDuration = null!;
    internal Grid? panelSoundVolume;
    internal Slider sliderSoundVolume = null!;
    internal Border? borderSoundVolumeKeycap;
    internal System.Windows.Controls.TextBox txtSoundVolume = null!;
    internal System.Windows.Controls.CheckBox cbStartup = null!;
    internal System.Windows.Controls.CheckBox cbStartMinimized = null!;
    internal System.Windows.Controls.CheckBox cbLightMode = null!;
    internal System.Windows.Controls.CheckBox cbRunAsAdmin = null!;
    internal System.Windows.Controls.CheckBox cbSoundFeedback = null!;
    internal TextBlock tbStoragePath = null!;
    internal Border borderWarning = null!;
    internal TextBlock tbWarningMessage = null!;
    internal Border borderWarningIcon = null!;
    internal System.Windows.Shapes.Path pathWarningIcon = null!;
    internal System.Windows.Controls.Image imgAppIcon = null!;
    internal ScrollViewer contentScrollViewer = null!;

    private static readonly Geometry IconCheck = Geometry.Parse("M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z");
    private static readonly Geometry IconWarning = Geometry.Parse("M1 21h22L12 2 1 21zm12-3h-2v-2h2v2zm0-4h-2v-4h2v4z");

    private const int WM_SETTINGCHANGE = 0x001A;
    private const int WM_DISPLAYCHANGE = 0x007E;
    private const int WM_DPICHANGED = 0x02E0;
    private const int WM_EXITSIZEMOVE = 0x0232;
    private const int WM_DEVICECHANGE = 0x0219;
    private const int WM_NCLBUTTONDOWN = 0x00A1;
    private const int HTCAPTION = 0x0002;

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int RegisterWindowMessage(string lpString);

    public static readonly int WM_SHOWME = RegisterWindowMessage("MICMUTE_SHOW_WINDOW_MSG_7FA5D9E0");

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ChangeWindowMessageFilterEx(IntPtr hWnd, uint msg, uint action, IntPtr pChangeFilterStruct);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ChangeWindowMessageFilter(uint msg, uint action);

    public MainWindow(AudioController audioController)
    {
        InitializeComponent();
        _preferredWidth = Width > 0 ? Width : 412.0;
        _deviceRefreshDebouncer = new DispatcherDebouncer(Dispatcher);
        _statusDebouncer = new DispatcherDebouncer(Dispatcher);
        _audioController = audioController;
        _deviceRefreshCoordinator = new LatestRefreshCoordinator<List<AudioDevice>>(
            _audioController.GetCaptureDevicesAsync,
            ApplyDeviceList,
            retryDelay: TimeSpan.FromMilliseconds(500),
            onFailure: ex => AudioController_WarningNotification(this, "Failed to refresh microphones: " + ex.Message),
            dispatcher: Dispatcher);
        _audioController.MuteStateChanged += AudioController_MuteStateChanged;
        _audioController.DevicesChanged += AudioController_DevicesChanged;
        _audioController.WarningNotification += AudioController_WarningNotification;
        SettingsManager.SaveFailed += SettingsManager_SaveFailed;
        new WindowInteropHelper(this).EnsureHandle();
        RefreshDeviceList();
        LoadSettingsIntoUI();
        UpdateMuteStateUI(_audioController.IsMuted);
        _isInitialized = true;
    }

    public void InitializeComponent()
    {
        if (_contentLoaded) return;
        _contentLoaded = true;

        string localFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MainWindow.xaml");
        Window root = LoadWindowRoot(localFile);
        if (root != null)
        {
            this.Resources = root.Resources;
            this.Title = root.Title;
            this.Width = root.Width;
            if (!double.IsNaN(root.Height)) this.Height = root.Height;
            this.SizeToContent = root.SizeToContent;
            this.WindowStyle = root.WindowStyle;
            this.AllowsTransparency = root.AllowsTransparency;
            this.Background = root.Background;
            this.ResizeMode = root.ResizeMode;
            this.WindowStartupLocation = root.WindowStartupLocation;
            this.SnapsToDevicePixels = root.SnapsToDevicePixels;
            this.UseLayoutRounding = root.UseLayoutRounding;

            try
            {
                string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    this.Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri(iconPath));
                }
            }
            catch { }

            // Bind all controls from root before detaching content
            btnStateToggle = (System.Windows.Controls.Button)root.FindName("btnStateToggle");
            tbStatusText = (TextBlock)root.FindName("tbStatusText");
            dotStatus = root.FindName("dotStatus") as System.Windows.Shapes.Ellipse;
            dotGlow = root.FindName("dotGlow") as DropShadowEffect;
            statusGlow = (DropShadowEffect)root.FindName("statusGlow") ?? dotGlow!;
            borderStatusCapsule = root.FindName("borderStatusCapsule") as Border;
            btnTitleTheme = root.FindName("btnTitleTheme") as System.Windows.Controls.Button;
            pathTitleTheme = root.FindName("pathTitleTheme") as System.Windows.Shapes.Path;
            titleBarIcon = root.FindName("titleBarIcon") as System.Windows.Shapes.Path;

            cbDevices = (System.Windows.Controls.ComboBox)root.FindName("cbDevices");
            borderHotkey = (Border)root.FindName("borderHotkey");
            tbHotkey = (TextBlock)root.FindName("tbHotkey");
            tbHeroHotkey = root.FindName("tbHeroHotkey") as TextBlock;
            btnRecordHotkey = (System.Windows.Controls.Button)root.FindName("btnRecordHotkey");
            cbEnableOsd = (System.Windows.Controls.CheckBox)root.FindName("cbEnableOsd");
            sliderOsdDuration = (Slider)root.FindName("sliderOsdDuration");
            if (sliderOsdDuration != null)
            {
                sliderOsdDuration.Minimum = UiBehavior.MinimumOsdDuration;
                sliderOsdDuration.Maximum = UiBehavior.MaximumOsdDuration;
            }
            borderOsdDurationKeycap = root.FindName("borderOsdDurationKeycap") as Border;
            txtOsdDuration = (System.Windows.Controls.TextBox)root.FindName("txtOsdDuration");
            cbStartup = (System.Windows.Controls.CheckBox)root.FindName("cbStartup");
            cbStartMinimized = (System.Windows.Controls.CheckBox)root.FindName("cbStartMinimized");
            cbLightMode = (System.Windows.Controls.CheckBox)root.FindName("cbLightMode");
            cbRunAsAdmin = (System.Windows.Controls.CheckBox)root.FindName("cbRunAsAdmin");
            cbSoundFeedback = (System.Windows.Controls.CheckBox)root.FindName("cbSoundFeedback");
            panelSoundVolume = root.FindName("panelSoundVolume") as Grid;
            sliderSoundVolume = (Slider)root.FindName("sliderSoundVolume");
            if (sliderSoundVolume != null)
            {
                sliderSoundVolume.Minimum = UiBehavior.MinimumSoundVolume;
                sliderSoundVolume.Maximum = UiBehavior.MaximumSoundVolume;
            }
            borderSoundVolumeKeycap = root.FindName("borderSoundVolumeKeycap") as Border;
            txtSoundVolume = (System.Windows.Controls.TextBox)root.FindName("txtSoundVolume");
            tbStoragePath = (TextBlock)root.FindName("tbStoragePath");
            borderWarning = (Border)root.FindName("borderWarning");
            tbWarningMessage = (TextBlock)root.FindName("tbWarningMessage");
            borderWarningIcon = (Border)root.FindName("borderWarningIcon");
            pathWarningIcon = (System.Windows.Shapes.Path)root.FindName("pathWarningIcon");
            contentScrollViewer = (ScrollViewer)root.FindName("contentScrollViewer");

            // Event hooks
            var titleBar = (Border)root.FindName("borderTitleBar");
            if (titleBar != null) titleBar.MouseLeftButtonDown += TitleBar_MouseLeftButtonDown;
            btnMin = (System.Windows.Controls.Button)root.FindName("btnMinimize");
            if (btnMin != null) btnMin.Click += MinimizeButton_Click;
            btnCls = (System.Windows.Controls.Button)root.FindName("btnClose");
            if (btnCls != null) btnCls.Click += CloseButton_Click;
            if (btnTitleTheme != null) btnTitleTheme.Click += BtnTitleTheme_Click;
            var btnOpenFolder = (System.Windows.Controls.Button)root.FindName("btnOpenFolder");
            if (btnOpenFolder != null) btnOpenFolder.Click += BtnOpenDataFolder_Click;
            var btnChangeFolder = (System.Windows.Controls.Button)root.FindName("btnChangeFolder");
            if (btnChangeFolder != null) btnChangeFolder.Click += BtnChangeDataFolder_Click;
            var btnResetData = (System.Windows.Controls.Button)root.FindName("btnResetData");
            if (btnResetData != null) btnResetData.Click += BtnResetSettings_Click;

            var imgIcon = (System.Windows.Controls.Image)root.FindName("imgAppIcon");
            imgAppIcon = imgIcon;
            if (imgIcon != null)
            {
                try
                {
                    using (Stream? iconStream = typeof(MainWindow).Assembly.GetManifestResourceStream("MicMute.app.ico"))
                    {
                        if (iconStream != null)
                        {
                            var decoder = System.Windows.Media.Imaging.BitmapDecoder.Create(iconStream, System.Windows.Media.Imaging.BitmapCreateOptions.None, System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);
                            imgIcon.Source = decoder.Frames[0];
                            this.Icon = decoder.Frames[0];
                        }
                    }
                }
                catch { }
            }

            // Now safely detach content and attach to this Window
            var content = root.Content;
            root.Content = null;
            this.Content = content;
        }

        if (btnStateToggle != null) btnStateToggle.Click += BtnStateToggle_Click;
        if (cbDevices != null) cbDevices.SelectionChanged += CbDevices_SelectionChanged;
        if (btnRecordHotkey != null) btnRecordHotkey.Click += BtnRecordHotkey_Click;
        if (cbEnableOsd != null)
        {
            cbEnableOsd.Checked += CbEnableOsd_Checked;
            cbEnableOsd.Unchecked += CbEnableOsd_Unchecked;
        }
        if (sliderOsdDuration != null) sliderOsdDuration.ValueChanged += SliderOsdDuration_ValueChanged;
        if (borderOsdDurationKeycap != null)
        {
            borderOsdDurationKeycap.MouseLeftButtonDown += (s, e) =>
            {
                txtOsdDuration?.Focus();
                txtOsdDuration?.SelectAll();
                e.Handled = true;
            };
        }
        if (txtOsdDuration != null)
        {
            txtOsdDuration.PreviewMouseLeftButtonDown += (s, e) =>
            {
                if (!txtOsdDuration.IsKeyboardFocused)
                {
                    txtOsdDuration.Focus();
                    txtOsdDuration.SelectAll();
                    e.Handled = true;
                }
            };
            txtOsdDuration.GotFocus += (s, e) =>
            {
                _osdDurationBeforeEdit = SettingsManager.Load().OsdDuration;
                txtOsdDuration.SelectAll();
            };
            txtOsdDuration.LostFocus += TxtOsdDuration_LostFocus;
            txtOsdDuration.KeyDown += TxtOsdDuration_KeyDown;
            txtOsdDuration.TextChanged += TxtOsdDuration_TextChanged;
        }
        if (cbStartup != null)
        {
            cbStartup.Checked += CbStartup_Checked;
            cbStartup.Unchecked += CbStartup_Unchecked;
        }
        if (cbStartMinimized != null)
        {
            cbStartMinimized.Checked += CbStartMinimized_Checked;
            cbStartMinimized.Unchecked += CbStartMinimized_Unchecked;
        }
        if (cbLightMode != null)
        {
            cbLightMode.Checked += CbLightMode_Checked;
            cbLightMode.Unchecked += CbLightMode_Unchecked;
        }
        if (cbRunAsAdmin != null)
        {
            cbRunAsAdmin.Checked += CbRunAsAdmin_Checked;
            cbRunAsAdmin.Unchecked += CbRunAsAdmin_Unchecked;
        }
        if (cbSoundFeedback != null)
        {
            cbSoundFeedback.Checked += CbSoundFeedback_Checked;
            cbSoundFeedback.Unchecked += CbSoundFeedback_Unchecked;
        }
        if (sliderSoundVolume != null) sliderSoundVolume.ValueChanged += SliderSoundVolume_ValueChanged;
        if (borderSoundVolumeKeycap != null)
        {
            borderSoundVolumeKeycap.MouseLeftButtonDown += (s, e) =>
            {
                txtSoundVolume?.Focus();
                txtSoundVolume?.SelectAll();
                e.Handled = true;
            };
        }
        if (txtSoundVolume != null)
        {
            txtSoundVolume.PreviewMouseLeftButtonDown += (s, e) =>
            {
                if (!txtSoundVolume.IsKeyboardFocused)
                {
                    txtSoundVolume.Focus();
                    txtSoundVolume.SelectAll();
                    e.Handled = true;
                }
            };
            txtSoundVolume.GotFocus += (s, e) =>
            {
                _soundVolumeBeforeEdit = SettingsManager.Load().SoundVolume;
                txtSoundVolume.SelectAll();
            };
            txtSoundVolume.LostFocus += TxtSoundVolume_LostFocus;
            txtSoundVolume.KeyDown += TxtSoundVolume_KeyDown;
            txtSoundVolume.TextChanged += TxtSoundVolume_TextChanged;
        }
        if (cbDevices != null && contentScrollViewer != null)
        {
            cbDevices.PreviewMouseWheel += (s, e) =>
            {
                if (!cbDevices.IsDropDownOpen)
                {
                    e.Handled = true;
                    var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                    {
                        RoutedEvent = UIElement.MouseWheelEvent,
                        Source = s
                    };
                    contentScrollViewer.RaiseEvent(eventArg);
                }
            };
        }
        if (sliderOsdDuration != null && contentScrollViewer != null)
        {
            sliderOsdDuration.PreviewMouseWheel += (s, e) =>
            {
                if (!sliderOsdDuration.IsFocused)
                {
                    e.Handled = true;
                    var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                    {
                        RoutedEvent = UIElement.MouseWheelEvent,
                        Source = s
                    };
                    contentScrollViewer.RaiseEvent(eventArg);
                }
            };
        }
        if (sliderSoundVolume != null && contentScrollViewer != null)
        {
            sliderSoundVolume.PreviewMouseWheel += (s, e) =>
            {
                if (!sliderSoundVolume.IsFocused)
                {
                    e.Handled = true;
                    var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                    {
                        RoutedEvent = UIElement.MouseWheelEvent,
                        Source = s
                    };
                    contentScrollViewer.RaiseEvent(eventArg);
                }
            };
        }
        this.SizeChanged += MainWindow_SizeChanged;
    }

    internal static Window LoadWindowRoot(string localFile)
    {
        if (File.Exists(localFile))
        {
            try { return ParseWindowRoot(File.ReadAllText(localFile)); }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("Ignoring invalid loose MainWindow.xaml: " + ex);
            }
        }

        using Stream stream = typeof(MainWindow).Assembly.GetManifestResourceStream("MicMute.MainWindow.xaml")
            ?? throw new FileNotFoundException("Embedded MainWindow.xaml was not found.");
        using StreamReader reader = new(stream);
        return ParseWindowRoot(reader.ReadToEnd());
    }

    private static Window ParseWindowRoot(string xaml)
    {
        xaml = System.Text.RegularExpressions.Regex.Replace(xaml, @"\s+x:Class=""[^""]+""", "");
        xaml = System.Text.RegularExpressions.Regex.Replace(xaml,
            @"\s+(Click|MouseLeftButtonDown|SelectionChanged|Checked|Unchecked|ValueChanged|LostFocus|KeyDown|TextChanged)=""[^""]+""", "");
        Window root = (Window)XamlReader.Parse(xaml);
        bool valid = root.FindName("btnStateToggle") is System.Windows.Controls.Button
            && root.FindName("tbStatusText") is TextBlock
            && root.FindName("cbDevices") is System.Windows.Controls.ComboBox
            && root.FindName("tbHotkey") is TextBlock
            && root.FindName("btnRecordHotkey") is System.Windows.Controls.Button
            && root.FindName("cbEnableOsd") is System.Windows.Controls.CheckBox
            && root.FindName("sliderOsdDuration") is Slider
            && root.FindName("txtOsdDuration") is System.Windows.Controls.TextBox
            && root.FindName("cbStartup") is System.Windows.Controls.CheckBox
            && root.FindName("cbStartMinimized") is System.Windows.Controls.CheckBox
            && root.FindName("cbLightMode") is System.Windows.Controls.CheckBox
            && root.FindName("cbRunAsAdmin") is System.Windows.Controls.CheckBox
            && root.FindName("cbSoundFeedback") is System.Windows.Controls.CheckBox
            && root.FindName("sliderSoundVolume") is Slider
            && root.FindName("txtSoundVolume") is System.Windows.Controls.TextBox
            && root.FindName("tbStoragePath") is TextBlock
            && root.FindName("borderWarning") is Border
            && root.FindName("tbWarningMessage") is TextBlock
            && root.FindName("contentScrollViewer") is ScrollViewer;
        if (valid) return root;
        root.Close();
        throw new InvalidDataException("MainWindow.xaml is missing required controls.");
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        WindowInteropHelper windowInteropHelper = new WindowInteropHelper(this);
        try
        {
            ChangeWindowMessageFilter((uint)WM_SHOWME, 1);
            ChangeWindowMessageFilterEx(windowInteropHelper.Handle, (uint)WM_SHOWME, 1, IntPtr.Zero);
        }
        catch { }
        HwndSource.FromHwnd(windowInteropHelper.Handle)?.AddHook(HwndMessageHook);
        _hotkeyManager = new HotkeyManager(windowInteropHelper.Handle);
        _hotkeyManager.HotkeyPressed += HotkeyManager_HotkeyPressed;
        AppSettings appSettings = SettingsManager.Load();
        RegisterGlobalHotkey(appSettings.Hotkey, appSettings.HotkeyModifiers);
        ApplyAdaptiveScreenConstraints(isInitialPlacement: true);
    }

    public void ApplyAdaptiveScreenConstraints(bool isInitialPlacement)
    {
        try
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            Screen? screen = null;
            if (hwnd != IntPtr.Zero && !isInitialPlacement)
            {
                screen = Screen.FromHandle(hwnd);
            }
            if (screen == null)
            {
                screen = Screen.FromPoint(System.Windows.Forms.Cursor.Position) ?? Screen.PrimaryScreen;
            }
            if (screen == null) return;

            DpiScale dpi = VisualTreeHelper.GetDpi(this);
            double scaleX = dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0;
            double scaleY = dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1.0;

            double workLeft = screen.WorkingArea.Left / scaleX;
            double workTop = screen.WorkingArea.Top / scaleY;
            double workWidth = screen.WorkingArea.Width / scaleX;
            double workHeight = screen.WorkingArea.Height / scaleY;

            ApplyAdaptiveBounds(new DipRect(workLeft, workTop, workWidth, workHeight), isInitialPlacement);
        }
        catch
        {
            double workHeight = SystemParameters.WorkArea.Height;
            this.MaxHeight = UiBehavior.CalculateAdaptiveMaxHeight(workHeight);
        }
    }

    private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        try
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero) return;
            Screen? screen = Screen.FromHandle(hwnd) ?? Screen.PrimaryScreen;
            if (screen == null) return;

            DpiScale dpi = VisualTreeHelper.GetDpi(this);
            double scaleX = dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0;
            double scaleY = dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1.0;
            double workLeft = screen.WorkingArea.Left / scaleX;
            double workTop = screen.WorkingArea.Top / scaleY;
            double workWidth = screen.WorkingArea.Width / scaleX;
            double workHeight = screen.WorkingArea.Height / scaleY;

            if (!_hasInitialCentered && this.ActualHeight > 0)
            {
                _hasInitialCentered = true;
                var centered = UiBehavior.CalculateCenteredWindowBounds(
                    new DipRect(workLeft, workTop, workWidth, workHeight),
                    this.ActualWidth > 0 ? this.ActualWidth : _preferredWidth,
                    this.ActualHeight,
                    UiBehavior.DefaultAdaptiveVerticalMargin);
                this.Left = centered.Left;
                this.Top = centered.Top;
                return;
            }

            double halfMargin = UiBehavior.DefaultAdaptiveVerticalMargin / 2.0;
            double maxTop = workTop + workHeight - this.ActualHeight - halfMargin;
            if (this.Top > maxTop)
            {
                this.Top = Math.Max(workTop + halfMargin, maxTop);
            }
        }
        catch { }
    }

    private IntPtr HwndMessageHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_DEVICECHANGE)
        {
            TriggerDevicesChanged();
        }
        else if (msg == WM_SHOWME)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            ApplyAdaptiveScreenConstraints(isInitialPlacement: false);
            this.Activate();
            this.Focus();
            ShowWindow(hwnd, 9); // SW_RESTORE
            SetForegroundWindow(hwnd);
            handled = true;
        }
        else if (msg == WM_SETTINGCHANGE || msg == WM_DISPLAYCHANGE || msg == WM_DPICHANGED || msg == WM_EXITSIZEMOVE)
        {
            ApplyAdaptiveScreenConstraints(isInitialPlacement: false);
        }
        return IntPtr.Zero;
    }

    private void ScheduleDeviceRefresh(int delayMs)
    {
        if (!_isDisposed)
        {
            _deviceRefreshDebouncer.Schedule(TimeSpan.FromMilliseconds(delayMs), () =>
            {
                _audioController.ForceUpdateActiveDevice();
                RefreshDeviceList();
            });
        }
    }

    private void TriggerDevicesChanged()
    {
        ScheduleDeviceRefresh(1000);
    }

    private void LoadSettingsIntoUI()
    {
        AppSettings appSettings = SettingsManager.Load();
        _isReloadingSettings = true;
        try
        {
            cbStartup.IsChecked = appSettings.RunOnStartup;
            cbStartMinimized.IsChecked = appSettings.StartMinimized;
            cbEnableOsd.IsChecked = appSettings.EnableOsd;
            if (sliderOsdDuration != null)
            {
                sliderOsdDuration.Minimum = UiBehavior.MinimumOsdDuration;
                sliderOsdDuration.Maximum = UiBehavior.MaximumOsdDuration;
                sliderOsdDuration.Value = appSettings.OsdDuration;
            }
            txtOsdDuration.Text = UiBehavior.FormatOsdDuration(appSettings.OsdDuration, CultureInfo.CurrentCulture);
            cbLightMode.IsChecked = appSettings.LightMode;
            if (cbRunAsAdmin != null)
            {
                cbRunAsAdmin.IsChecked = appSettings.RunAsAdmin;
            }
            if (cbSoundFeedback != null)
            {
                cbSoundFeedback.IsChecked = appSettings.PlaySoundFeedback;
            }
            if (sliderSoundVolume != null)
            {
                sliderSoundVolume.Minimum = UiBehavior.MinimumSoundVolume;
                sliderSoundVolume.Maximum = UiBehavior.MaximumSoundVolume;
                sliderSoundVolume.Value = appSettings.SoundVolume;
            }
            if (txtSoundVolume != null)
            {
                txtSoundVolume.Text = UiBehavior.FormatSoundVolume(appSettings.SoundVolume);
            }
            UpdateSoundVolumeUiState(appSettings.PlaySoundFeedback);
            AudioFeedback.SetVolume(appSettings.SoundVolume);
            SetLightMode(appSettings.LightMode);
            DisplayHotkey(appSettings.Hotkey, appSettings.HotkeyModifiers);
            tbStoragePath.Text = SettingsManager.GetDataFolderPath();
        }
        finally
        {
            _isReloadingSettings = false;
        }

        if (AdminManager.IsRunningAsAdmin())
        {
            if (imgAppIcon != null) imgAppIcon.ToolTip = "Mic Mute (Administrator Mode)";
            tbStatusText.ToolTip = "Administrator Mode Active: Hotkeys enabled over anti-cheat and elevated windows";
        }
        else
        {
            if (imgAppIcon != null) imgAppIcon.ToolTip = "Mic Mute";
            tbStatusText.ToolTip = "Mic Mute Active: In-game hotkeys and OSD ready";
        }

    }

    private void ApplySettingsToRuntime(AppSettings settings)
    {
        _audioController.SetTargetDevice(settings.SelectedDeviceId);
        StartupManager.SetStartup(settings.RunOnStartup, settings.RunAsAdmin);
        AdminManager.SetRunAsAdmin(settings.RunAsAdmin);
        _hotkeyManager?.Unregister();
        if (!RegisterGlobalHotkey(settings.Hotkey, settings.HotkeyModifiers))
            throw new InvalidOperationException("The shortcut could not be configured. Choose a different key.");
        SetLightMode(settings.LightMode);
    }

    private void SettingsManager_SaveFailed(object? sender, string message)
    {
        if (_isDisposed || Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished)
        {
            return;
        }
        try
        {
            Dispatcher.BeginInvoke((Action)delegate
            {
                if (!_isDisposed)
                {
                    ShowTemporaryStatus("Could not save settings: " + message);
                }
            });
        }
        catch (InvalidOperationException)
        {
        }
    }

    private void RefreshDeviceList()
    {
        if (!_isDisposed) _deviceRefreshCoordinator.Request();
    }

    internal void ApplyAdaptiveBounds(DipRect workArea, bool isInitialPlacement)
    {
        double maxAllowedHeight = UiBehavior.CalculateAdaptiveMaxHeight(workArea.Height, UiBehavior.DefaultAdaptiveVerticalMargin);
        if (Math.Abs(MaxHeight - maxAllowedHeight) > 0.5) MaxHeight = maxAllowedHeight;

        double currentWidth = _preferredWidth;
        if (isInitialPlacement)
        {
            var contentElement = Content as UIElement;
            contentElement?.Measure(new System.Windows.Size(Math.Min(currentWidth, workArea.Width), maxAllowedHeight));
            double desiredHeight = contentElement != null && contentElement.DesiredSize.Height > 0
                ? contentElement.DesiredSize.Height
                : (ActualHeight > 0 ? ActualHeight : maxAllowedHeight);
            var centered = UiBehavior.CalculateCenteredWindowBounds(
                workArea, currentWidth, Math.Min(desiredHeight, maxAllowedHeight), UiBehavior.DefaultAdaptiveVerticalMargin);
            Width = centered.Width;
            Left = centered.Left;
            Top = centered.Top;
            _hasInitialCentered = ActualHeight > 0;
        }
        else
        {
            double currentHeight = ActualHeight > 0 ? ActualHeight : MaxHeight;
            var clamped = UiBehavior.ClampWindowBoundsToWorkArea(
                workArea, new DipRect(Left, Top, currentWidth, currentHeight), UiBehavior.DefaultAdaptiveVerticalMargin);
            Width = clamped.Width;
            Left = clamped.Left;
            Top = clamped.Top;
        }
    }

    private void ApplyDeviceList(List<AudioDevice> captureDevices)
    {
        if (_isDisposed) return;
        _isUpdatingDeviceList = true;
        try
        {
            cbDevices.ItemsSource = captureDevices;
            cbDevices.SelectedItem = captureDevices.FirstOrDefault(d => d.Id == _audioController.CurrentDeviceId);
        }
        finally { _isUpdatingDeviceList = false; }
    }

    private void CbDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isUpdatingDeviceList && cbDevices.SelectedItem is AudioDevice audioDevice)
        {
            DiagnosticLogger.LogUi($"Selected audio capture device: '{audioDevice.Name}' (ID: {audioDevice.Id})");
            _audioController.SetTargetDevice(audioDevice.Id);
            SettingsManager.Save(SettingsManager.Load() with
            {
                SelectedDeviceId = audioDevice.Id
            });
            RefreshDeviceList();
        }
    }

    private void BtnStateToggle_Click(object sender, RoutedEventArgs e)
    {
        DiagnosticLogger.LogUi("Mute button clicked in UI");
        _audioController.ToggleMute();
    }

    private void UpdateMuteStateUI(bool isMuted)
    {
        bool hasDevice = !string.IsNullOrEmpty(_audioController.CurrentDeviceId);
        btnStateToggle.IsEnabled = hasDevice;
        bool isLight = SettingsManager.Load().LightMode;

        if (!hasDevice)
        {
            btnStateToggle.Tag = "Unavailable";
            tbStatusText.Text = "NO MICROPHONE";
            tbStatusText.SetResourceReference(TextBlock.ForegroundProperty, "TextDimBrush");
            if (dotStatus != null) dotStatus.Fill = (System.Windows.Media.Brush)FindResource("TextDimBrush");
            if (dotGlow != null) dotGlow.Opacity = 0;
            if (borderStatusCapsule != null)
            {
                borderStatusCapsule.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(20, 100, 116, 139));
                borderStatusCapsule.BorderThickness = new Thickness(0);
                borderStatusCapsule.BorderBrush = null;
            }
            ShowWarningMessage("No active audio capture devices found.", "");
            borderWarning.Visibility = Visibility.Visible;
            return;
        }

        btnStateToggle.Tag = isMuted ? "Muted" : "Active";
        tbStatusText.Text = isMuted ? "MICROPHONE MUTED" : "MICROPHONE LIVE";

        if (isMuted)
        {
            // Standard balanced UI red: clean coral/ruby text, pure red dot, subtle translucent red wash
            var redTextColor = isLight ? System.Windows.Media.Color.FromRgb(220, 38, 38) : System.Windows.Media.Color.FromRgb(248, 113, 113);
            var redDotColor = isLight ? System.Windows.Media.Color.FromRgb(220, 38, 38) : System.Windows.Media.Color.FromRgb(239, 68, 68);

            tbStatusText.Foreground = new SolidColorBrush(redTextColor);
            if (dotStatus != null) dotStatus.Fill = new SolidColorBrush(redDotColor);
            if (dotGlow != null)
            {
                dotGlow.Color = redDotColor;
                dotGlow.Opacity = isLight ? 0.35 : 0.55;
                dotGlow.BlurRadius = 6;
            }
            if (borderStatusCapsule != null)
            {
                borderStatusCapsule.Background = new SolidColorBrush(
                    isLight ? System.Windows.Media.Color.FromArgb(20, 220, 38, 38)
                            : System.Windows.Media.Color.FromArgb(24, 239, 68, 68));
                borderStatusCapsule.BorderThickness = new Thickness(0);
                borderStatusCapsule.BorderBrush = null;
            }
        }
        else
        {
            var activeColor = isLight ? System.Windows.Media.Color.FromRgb(71, 85, 105) : System.Windows.Media.Color.FromRgb(244, 244, 245);
            var activeBrush = new SolidColorBrush(activeColor);
            tbStatusText.Foreground = activeBrush;
            if (dotStatus != null) dotStatus.Fill = activeBrush;
            if (dotGlow != null)
            {
                dotGlow.Color = activeColor;
                dotGlow.Opacity = isLight ? 0.25 : 0.85;
                dotGlow.BlurRadius = 8;
            }
            if (borderStatusCapsule != null)
            {
                borderStatusCapsule.Background = new SolidColorBrush(isLight ? System.Windows.Media.Color.FromArgb(18, 71, 85, 105) : System.Windows.Media.Color.FromArgb(28, 255, 255, 255));
                borderStatusCapsule.BorderThickness = new Thickness(0);
                borderStatusCapsule.BorderBrush = null;
            }
        }
    }

    private void AudioController_MuteStateChanged(object? sender, MuteStateChangedEventArgs e)
    {
        if (_isDisposed || Dispatcher.HasShutdownStarted) return;
        if (e.ShowOsd && SettingsManager.Load().PlaySoundFeedback)
        {
            AudioFeedback.Play(e.IsMuted);
        }
        Dispatcher.BeginInvoke((Action)delegate
        {
            if (_isDisposed) return;
            _statusDebouncer.Cancel();
            UpdateMuteStateUI(e.IsMuted);
            if (string.IsNullOrEmpty(_audioController.CurrentDeviceId))
            {
                ShowWarningMessage("No active audio capture devices found.", "");
                borderWarning.Visibility = Visibility.Visible;
            }
            else
            {
                borderWarning.Visibility = Visibility.Collapsed;
            }
            if (_isInitialized)
            {
                AppSettings appSettings = SettingsManager.Load();
                if (appSettings.EnableOsd && e.ShowOsd)
                {
                    OsdWindow.ShowOsd(e.IsMuted, appSettings.OsdDuration);
                }
                else if (e.ShowOsd && System.Windows.Application.Current is App app)
                {
                    string text = e.IsMuted ? "Muted" : "Active";
                    app.ShowToastNotification("Microphone is now " + text + ".");
                }
            }
        });
    }

    private void AudioController_DevicesChanged(object? sender, EventArgs e)
    {
        ScheduleDeviceRefresh(800);
    }

    private void AudioController_WarningNotification(object? sender, string message)
    {
        if (_isDisposed || Dispatcher.HasShutdownStarted) return;
        Dispatcher.BeginInvoke((Action)delegate
        {
            if (_isDisposed) return;
            _statusDebouncer.Cancel();
            if (pathWarningIcon != null) pathWarningIcon.Data = IconWarning;
            tbWarningMessage.Inlines.Clear();
            System.Windows.Media.Brush foreground = (System.Windows.Media.Brush)FindResource("WarningTextBrush");
            tbWarningMessage.Inlines.Add(new Run(message)
            {
                Foreground = foreground
            });
            borderWarning.Visibility = Visibility.Visible;
        });
    }

    private void HotkeyManager_HotkeyPressed()
    {
        DiagnosticLogger.LogHotkey("Global shortcut key triggered -> Toggling mute");
        _audioController.ToggleMute();
    }

    public void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            ReleaseCapture();
            SendMessage(new WindowInteropHelper(this).Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
        }
    }

    public void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    public void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BtnRecordHotkey_Click(object sender, RoutedEventArgs e)
    {
        if (_isRecordingHotkey)
        {
            StopRecordingHotkey(success: false, Key.None, ModifierKeys.None);
        }
        else
        {
            StartRecordingHotkey();
        }
    }

    internal void StartRecordingHotkey()
    {
        _hotkeyManager?.Unregister();
        _isRecordingHotkey = true;
        btnRecordHotkey.Content = "Cancel";
        tbHotkey.Text = "Press keys...";
        tbHotkey.Foreground = (System.Windows.Media.Brush)FindResource("AccentBrush");
        if (tbHeroHotkey != null) tbHeroHotkey.Text = "...";
        PreviewKeyDown += MainWindow_PreviewKeyDown;
    }

    protected override void OnDeactivated(EventArgs e)
    {
        if (_isRecordingHotkey) StopRecordingHotkey(success: false);
        base.OnDeactivated(e);
    }

    private void StopRecordingHotkey(bool success, Key key = Key.None, ModifierKeys modifiers = ModifierKeys.None)
    {
        _isRecordingHotkey = false;
        btnRecordHotkey.Content = "Record";
        PreviewKeyDown -= MainWindow_PreviewKeyDown;
        AppSettings appSettings = SettingsManager.Load();
        if (success && key != Key.None)
        {
            if (RegisterGlobalHotkey(key, modifiers))
            {
                SettingsManager.Save(appSettings with
                {
                    Hotkey = key,
                    HotkeyModifiers = modifiers
                });
                DisplayHotkey(key, modifiers);
                ShowTemporaryStatus("Shortcut changed successfully.");
            }
            else
            {
                ShowTemporaryStatus("That shortcut could not be configured. Try a different key.");
                // Restore previous working hotkey so the app is not left without any hotkey
                RegisterGlobalHotkey(appSettings.Hotkey, appSettings.HotkeyModifiers);
                DisplayHotkey(appSettings.Hotkey, appSettings.HotkeyModifiers);
            }
        }
        else
        {
            // Restore previous working hotkey on cancel
            RegisterGlobalHotkey(appSettings.Hotkey, appSettings.HotkeyModifiers);
            DisplayHotkey(appSettings.Hotkey, appSettings.HotkeyModifiers);
        }
    }

    private void MainWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        e.Handled = true;
        Key key = e.Key;
        if (key == Key.System)
        {
            key = e.SystemKey;
        }
        if (key == Key.Escape)
        {
            StopRecordingHotkey(success: false);
            return;
        }
        ModifierKeys modifiers = System.Windows.Input.Keyboard.Modifiers;
        if (key == Key.LeftCtrl || key == Key.RightCtrl ||
            key == Key.LeftAlt || key == Key.RightAlt ||
            key == Key.LeftShift || key == Key.RightShift ||
            key == Key.LWin || key == Key.RWin)
        {
            string modifierText = FormatHotkeyText(Key.None, modifiers);
            tbHotkey.Text = modifierText;
            if (tbHeroHotkey != null) tbHeroHotkey.Text = modifierText;
        }
        else if (key == Key.Tab || key == Key.Enter || key == Key.Space || key == Key.Back || key == Key.Capital)
        {
            if (modifiers == ModifierKeys.None)
            {
                ShowTemporaryStatus("Single key '" + key + "' cannot be used alone. Combine with Ctrl, Alt, or Shift.");
                return;
            }
            StopRecordingHotkey(success: true, key, modifiers);
        }
        else
        {
            StopRecordingHotkey(success: true, key, modifiers);
        }
    }

    private bool RegisterGlobalHotkey(Key key, ModifierKeys modifiers)
    {
        if (_hotkeyManager == null)
        {
            return false;
        }
        return _hotkeyManager.Register(key, modifiers);
    }

    internal void DisplayHotkey(Key key, ModifierKeys modifiers)
    {
        string hotkeyText = FormatHotkeyText(key, modifiers);
        if (tbHotkey != null)
        {
            tbHotkey.Text = hotkeyText;
            tbHotkey.SetResourceReference(TextBlock.ForegroundProperty, "KeycapTextBrush");
        }
        if (tbHeroHotkey != null)
        {
            tbHeroHotkey.Text = hotkeyText;
        }
    }

    private string FormatHotkeyText(Key key, ModifierKeys modifiers)
    {
        List<string> list = new List<string>();
        if (modifiers.HasFlag(ModifierKeys.Control))
        {
            list.Add("Ctrl");
        }
        if (modifiers.HasFlag(ModifierKeys.Alt))
        {
            list.Add("Alt");
        }
        if (modifiers.HasFlag(ModifierKeys.Shift))
        {
            list.Add("Shift");
        }
        if (modifiers.HasFlag(ModifierKeys.Windows))
        {
            list.Add("Win");
        }
        if (key != Key.None)
        {
            list.Add(key.ToString());
        }
        else if (list.Count > 0)
        {
            list.Add("...");
        }
        else
        {
            list.Add("None");
        }
        return string.Join(" + ", list);
    }

    private void ShowTemporaryStatus(string message)
    {
        if (_isDisposed) return;
        if (pathWarningIcon != null)
        {
            bool isWarning = message.StartsWith("Could not", StringComparison.OrdinalIgnoreCase) ||
                             message.Contains("cannot", StringComparison.OrdinalIgnoreCase) ||
                             message.Contains("cancelled", StringComparison.OrdinalIgnoreCase) ||
                             message.Contains("different key", StringComparison.OrdinalIgnoreCase) ||
                             message.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                             message.Contains("failed", StringComparison.OrdinalIgnoreCase);
            pathWarningIcon.Data = isWarning ? IconWarning : IconCheck;
        }
        tbWarningMessage.Inlines.Clear();
        tbWarningMessage.Inlines.Add(new Run(message));
        borderWarning.Visibility = Visibility.Visible;
        _statusDebouncer.Schedule(TimeSpan.FromSeconds(3), () =>
        {
            if (string.IsNullOrEmpty(_audioController.CurrentDeviceId))
            {
                ShowWarningMessage("No active audio capture devices found.", "");
                borderWarning.Visibility = Visibility.Visible;
            }
            else
            {
                borderWarning.Visibility = Visibility.Collapsed;
            }
        });
    }

    private void CbStartup_Checked(object sender, RoutedEventArgs e)
    {
        if (_isInitialized && !_isReloadingSettings)
        {
            bool runAsAdmin = cbRunAsAdmin?.IsChecked == true || SettingsManager.Load().RunAsAdmin;
            StartupManager.SetStartup(runOnStartup: true, runAsAdmin: runAsAdmin);
            SettingsManager.Save(SettingsManager.Load() with
            {
                RunOnStartup = true
            });
        }
    }

    private void CbStartup_Unchecked(object sender, RoutedEventArgs e)
    {
        if (_isInitialized && !_isReloadingSettings)
        {
            StartupManager.SetStartup(runOnStartup: false);
            SettingsManager.Save(SettingsManager.Load() with
            {
                RunOnStartup = false
            });
        }
    }

    private void CbStartMinimized_Checked(object sender, RoutedEventArgs e)
    {
        if (_isInitialized && !_isReloadingSettings)
        {
            SettingsManager.Save(SettingsManager.Load() with
            {
                StartMinimized = true
            });
        }
    }

    private void CbStartMinimized_Unchecked(object sender, RoutedEventArgs e)
    {
        if (_isInitialized && !_isReloadingSettings)
        {
            SettingsManager.Save(SettingsManager.Load() with
            {
                StartMinimized = false
            });
        }
    }

    private void ShowWarningMessage(string labelText, string micName)
    {
        if (pathWarningIcon != null)
        {
            pathWarningIcon.Data = IconWarning;
        }
        tbWarningMessage.Inlines.Clear();
        tbWarningMessage.Inlines.Add(new Run(labelText));
        if (!string.IsNullOrEmpty(micName))
        {
            tbWarningMessage.Inlines.Add(new Run(" " + micName)
            {
                FontWeight = FontWeights.Bold
            });
        }
    }

    private void CbLightMode_Checked(object sender, RoutedEventArgs e)
    {
        if (_isInitialized && !_isReloadingSettings)
        {
            SettingsManager.Save(SettingsManager.Load() with
            {
                LightMode = true
            });
            SetLightMode(isLight: true);
            (System.Windows.Application.Current as App)?.UpdateTrayIconState();
        }
    }

    private void CbLightMode_Unchecked(object sender, RoutedEventArgs e)
    {
        if (_isInitialized && !_isReloadingSettings)
        {
            SettingsManager.Save(SettingsManager.Load() with
            {
                LightMode = false
            });
            SetLightMode(isLight: false);
            (System.Windows.Application.Current as App)?.UpdateTrayIconState();
        }
    }

    private void CbRunAsAdmin_Checked(object sender, RoutedEventArgs e)
    {
        if (!_isInitialized || _isReloadingSettings) return;
        AdminManager.SetRunAsAdmin(true);
        var currentSettings = SettingsManager.Load();
        SettingsManager.Save(currentSettings with { RunAsAdmin = true });
        StartupManager.SetStartup(currentSettings.RunOnStartup, runAsAdmin: true);

        if (!AdminManager.IsRunningAsAdmin())
        {
            var res = System.Windows.MessageBox.Show(
                "Run as Administrator has been enabled for elevated windows and game hotkey support.\n\nWould you like to restart MicMute as Administrator now to apply it immediately?",
                "Restart as Administrator",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (res == MessageBoxResult.Yes)
            {
                if (!AdminManager.RestartAsAdmin()) ShowTemporaryStatus("Restart was cancelled or could not be completed.");
            }
        }
    }

    private void CbRunAsAdmin_Unchecked(object sender, RoutedEventArgs e)
    {
        if (!_isInitialized || _isReloadingSettings) return;
        AdminManager.SetRunAsAdmin(false);
        var currentSettings = SettingsManager.Load();
        SettingsManager.Save(currentSettings with { RunAsAdmin = false });
        StartupManager.SetStartup(currentSettings.RunOnStartup, runAsAdmin: false);
    }

    private void CbSoundFeedback_Checked(object sender, RoutedEventArgs e)
    {
        if (!_isInitialized || _isReloadingSettings) return;
        SettingsManager.Save(SettingsManager.Load() with { PlaySoundFeedback = true });
        UpdateSoundVolumeUiState(true);
    }

    private void CbSoundFeedback_Unchecked(object sender, RoutedEventArgs e)
    {
        if (!_isInitialized || _isReloadingSettings) return;
        SettingsManager.Save(SettingsManager.Load() with { PlaySoundFeedback = false });
        UpdateSoundVolumeUiState(false);
    }

    private void UpdateSoundVolumeUiState(bool soundFeedbackEnabled)
    {
        if (panelSoundVolume != null)
        {
            panelSoundVolume.IsEnabled = soundFeedbackEnabled;
            panelSoundVolume.Opacity = soundFeedbackEnabled ? 1.0 : 0.45;
        }
    }

    public void BtnTitleTheme_Click(object sender, RoutedEventArgs e)
    {
        if (cbLightMode != null)
        {
            cbLightMode.IsChecked = !cbLightMode.IsChecked;
        }
    }

    private void SetLightMode(bool isLight)
    {
        System.Windows.Media.Color accentColor = isLight ? System.Windows.Media.Color.FromRgb(71, 85, 105) : System.Windows.Media.Color.FromRgb(244, 244, 245);
        Resources["AccentColor"] = accentColor;
        Resources["AccentBrush"] = new SolidColorBrush(accentColor);
        Resources["AccentHoverBrush"] = new SolidColorBrush(isLight ? System.Windows.Media.Color.FromArgb(18, 71, 85, 105) : System.Windows.Media.Color.FromArgb(34, 255, 255, 255));

        var themeGlyphBrush = new SolidColorBrush(isLight ? System.Windows.Media.Color.FromRgb(55, 65, 81) : System.Windows.Media.Color.FromRgb(244, 244, 245));
        if (pathTitleTheme != null)
        {
            pathTitleTheme.Fill = themeGlyphBrush;
            pathTitleTheme.Data = Geometry.Parse(isLight
                ? "M12.3,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22C16.82,22 20.84,18.6 21.8,14C22.07,12.7 20.93,11.59 19.63,11.83C15.82,12.54 12,9.66 12,5.77C12,4.42 12.92,3.26 14.26,3.03C14.77,2.94 15.08,2.44 14.8,2C14.07,2 13.18,2 12.3,2Z"
                : "M12,7 A5,5 0 1 0 12,17 A5,5 0 1 0 12,7 Z M11,1 H13 V4 H11 Z M11,20 H13 V23 H11 Z M1,11 H4 V13 H1 Z M20,11 H23 V13 H20 Z M4.22,3.51 L5.64,4.93 L4.22,6.34 L2.81,4.93 Z M18.36,17.66 L19.78,19.07 L18.36,20.49 L16.95,19.07 Z M4.22,20.49 L5.64,19.07 L4.22,17.66 L2.81,19.07 Z M18.36,6.34 L19.78,4.93 L18.36,3.51 L16.95,4.93 Z");
        }
        if (titleBarIcon != null)
        {
            titleBarIcon.Fill = themeGlyphBrush;
        }
        if (btnMin != null)
        {
            btnMin.Foreground = themeGlyphBrush;
        }
        if (btnCls != null)
        {
            btnCls.Foreground = themeGlyphBrush;
        }

        if (isLight)
        {
            Resources["WindowBgBrush"] = new LinearGradientBrush(
                System.Windows.Media.Color.FromArgb(174, 248, 249, 250),
                System.Windows.Media.Color.FromArgb(174, 233, 236, 239),
                new Point(0.0, 0.0), new Point(1.0, 1.0));
            Resources["CaptionButtonHoverBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(18, 0, 0, 0));
            Resources["CardBgBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(180, 255, 255, 255));
            Resources["InputBgBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 244, 245));
            Resources["TitleBarBgBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0, 255, 255, 255));
            Resources["TitleTextBrush"] = themeGlyphBrush;
            Resources["TextWhiteBrush"] = themeGlyphBrush;
            Resources["TextGrayBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128));
            Resources["TextDimBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(156, 163, 175));
            Resources["BorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(28, 0, 0, 0));
            Resources["DividerBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(18, 0, 0, 0));

            // Liquid Switches (Soft Graphite in Light Mode)
            Resources["ToggleOnBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(71, 85, 105));
            Resources["ToggleOnBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(71, 85, 105));
            Resources["ToggleOffBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(228, 228, 231));
            Resources["ToggleOffBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(212, 212, 216));
            Resources["ToggleKnobBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(71, 85, 105));
            Resources["ToggleKnobBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(212, 212, 216));
            Resources["ToggleKnobActiveBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
            Resources["ToggleKnobActiveBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(228, 228, 231));

            // Liquid OSD Duration Slider (Soft Graphite in Light Mode)
            Resources["SliderTrackBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(71, 85, 105));
            Resources["SliderTrackEmptyBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(228, 228, 231));
            Resources["SliderTrackBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(212, 212, 216));
            Resources["SliderThumbBgBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
            Resources["SliderThumbBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(71, 85, 105));

            // Jewel Ring
            Resources["JewelRingBgBrush"] = new LinearGradientBrush(
                System.Windows.Media.Color.FromArgb(180, 255, 255, 255),
                System.Windows.Media.Color.FromArgb(120, 244, 244, 245),
                new Point(0.0, 0.0), new Point(1.0, 1.0));
            Resources["JewelRingBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(180, 255, 255, 255));

            // Hero Mute Button: Liquid Slate-Graphite Lens matching Toggle Switches in Light Mode
            var heroLensLight = new RadialGradientBrush
            {
                Center = new Point(0.35, 0.25),
                GradientOrigin = new Point(0.35, 0.25),
                RadiusX = 0.65,
                RadiusY = 0.65
            };
            heroLensLight.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromRgb(108, 122, 142), 0.0));
            heroLensLight.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromRgb(85, 99, 120), 0.35));
            heroLensLight.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromRgb(71, 85, 105), 0.70));
            heroLensLight.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromRgb(55, 67, 83), 1.0));
            Resources["HeroLensActiveBrush"] = heroLensLight;

            var heroGlowLight = new RadialGradientBrush
            {
                Center = new Point(0.5, 0.40),
                GradientOrigin = new Point(0.5, 0.30),
                RadiusX = 0.5,
                RadiusY = 0.5
            };
            heroGlowLight.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(35, 71, 85, 105), 0.0));
            heroGlowLight.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(12, 71, 85, 105), 0.5));
            heroGlowLight.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(0, 71, 85, 105), 1.0));
            Resources["HeroGlowActiveBrush"] = heroGlowLight;
            Resources["HeroIconActiveBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));

            // Monochrome Squircle Tiles
            Resources["SquircleBgBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(14, 0, 0, 0));
            Resources["SquircleBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(24, 0, 0, 0));
            Resources["SquircleIconBrush"] = themeGlyphBrush;

            // Keycaps
            Resources["KeycapBgBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 244, 245));
            Resources["KeycapBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(228, 228, 231));
            Resources["KeycapTextBrush"] = themeGlyphBrush;

            // Status Capsule (Borderless)
            Resources["StatusCapsuleBgBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(18, 71, 85, 105));
            Resources["StatusCapsuleBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0, 0, 0, 0));
            Resources["StatusCapsuleTextBrush"] = themeGlyphBrush;
            Resources["StatusDotBrush"] = themeGlyphBrush;

            // Warning / Status Message Box (Monochrome Black & White Theme)
            Resources["WarningBgBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(12, 0, 0, 0));
            Resources["WarningBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(22, 0, 0, 0));
            Resources["WarningTextBrush"] = themeGlyphBrush;

            // Scrollbars
            Resources["ScrollBarThumbBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 0, 0, 0));
            Resources["ScrollBarThumbHoverBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(70, 0, 0, 0));
            Resources["ScrollBarThumbDragBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(100, 0, 0, 0));
        }
        else
        {
            // True Neutral Gray-Black Dark Mode (Zero Blue)
            Resources["WindowBgBrush"] = new LinearGradientBrush(
                System.Windows.Media.Color.FromArgb(242, 24, 24, 27),
                System.Windows.Media.Color.FromArgb(242, 9, 9, 11),
                new Point(0.0, 0.0), new Point(1.0, 1.0));
            Resources["CaptionButtonHoverBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(24, 255, 255, 255));
            Resources["CardBgBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(20, 255, 255, 255));
            Resources["InputBgBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(24, 24, 27));
            Resources["TitleBarBgBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0, 0, 0, 0));
            Resources["TitleTextBrush"] = themeGlyphBrush;
            Resources["TextWhiteBrush"] = themeGlyphBrush;
            Resources["TextGrayBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(161, 161, 170));
            Resources["TextDimBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(113, 113, 122));
            Resources["BorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(32, 255, 255, 255));
            Resources["DividerBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(20, 255, 255, 255));

            // Liquid Switches (25% Softened White Liquid in Dark Mode)
            Resources["ToggleOnBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(184, 192, 204));
            Resources["ToggleOnBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(184, 192, 204));
            Resources["ToggleOffBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(28, 28, 31));
            Resources["ToggleOffBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(63, 63, 70));
            Resources["ToggleKnobBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(228, 228, 231));
            Resources["ToggleKnobBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(161, 161, 170));
            Resources["ToggleKnobActiveBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(24, 24, 27));
            Resources["ToggleKnobActiveBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(39, 39, 42));

            // Liquid OSD Duration Slider
            Resources["SliderTrackBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(184, 192, 204));
            Resources["SliderTrackEmptyBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(39, 39, 42));
            Resources["SliderTrackBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(63, 63, 70));
            Resources["SliderThumbBgBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
            Resources["SliderThumbBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(24, 24, 27));

            // Jewel Ring
            Resources["JewelRingBgBrush"] = new LinearGradientBrush(
                System.Windows.Media.Color.FromArgb(34, 255, 255, 255),
                System.Windows.Media.Color.FromArgb(5, 255, 255, 255),
                new Point(0.0, 0.0), new Point(1.0, 1.0));
            Resources["JewelRingBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(60, 255, 255, 255));

            // Hero Mute Button: Liquid White-Zinc Lens in Dark Mode
            var heroLensDark = new RadialGradientBrush
            {
                Center = new Point(0.35, 0.25),
                GradientOrigin = new Point(0.35, 0.25),
                RadiusX = 0.65,
                RadiusY = 0.65
            };
            heroLensDark.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromRgb(255, 255, 255), 0.0));
            heroLensDark.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromRgb(228, 228, 231), 0.45));
            heroLensDark.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromRgb(212, 212, 216), 0.85));
            heroLensDark.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromRgb(161, 161, 170), 1.0));
            Resources["HeroLensActiveBrush"] = heroLensDark;

            var heroGlowDark = new RadialGradientBrush
            {
                Center = new Point(0.5, 0.40),
                GradientOrigin = new Point(0.5, 0.30),
                RadiusX = 0.5,
                RadiusY = 0.5
            };
            heroGlowDark.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(65, 255, 255, 255), 0.0));
            heroGlowDark.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(20, 255, 255, 255), 0.5));
            heroGlowDark.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(0, 255, 255, 255), 1.0));
            Resources["HeroGlowActiveBrush"] = heroGlowDark;
            Resources["HeroIconActiveBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(24, 24, 27));

            // Monochrome Squircle Tiles: Pure White Icon in Dark Mode
            Resources["SquircleBgBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(20, 255, 255, 255));
            Resources["SquircleBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(32, 255, 255, 255));
            Resources["SquircleIconBrush"] = themeGlyphBrush;

            // Keycaps
            Resources["KeycapBgBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(39, 39, 42));
            Resources["KeycapBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(63, 63, 70));
            Resources["KeycapTextBrush"] = themeGlyphBrush;

            // Status Capsule (Borderless)
            Resources["StatusCapsuleBgBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(26, 255, 255, 255));
            Resources["StatusCapsuleBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0, 0, 0, 0));
            Resources["StatusCapsuleTextBrush"] = themeGlyphBrush;
            Resources["StatusDotBrush"] = themeGlyphBrush;

            // Warning / Status Message Box (Monochrome Black & White Theme)
            Resources["WarningBgBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(20, 255, 255, 255));
            Resources["WarningBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(32, 255, 255, 255));
            Resources["WarningTextBrush"] = themeGlyphBrush;

            // Scrollbars
            Resources["ScrollBarThumbBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(36, 255, 255, 255));
            Resources["ScrollBarThumbHoverBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(65, 255, 255, 255));
            Resources["ScrollBarThumbDragBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(100, 255, 255, 255));
        }

        if (_audioController != null)
        {
            UpdateMuteStateUI(_audioController.IsMuted);
        }
        OsdWindow.UpdateVisibleTheme(isLight);
    }

    private void CbEnableOsd_Checked(object sender, RoutedEventArgs e)
    {
        if (_isInitialized && !_isReloadingSettings)
        {
            SettingsManager.Save(SettingsManager.Load() with
            {
                EnableOsd = true
            });
        }
    }

    private void CbEnableOsd_Unchecked(object sender, RoutedEventArgs e)
    {
        if (_isInitialized && !_isReloadingSettings)
        {
            SettingsManager.Save(SettingsManager.Load() with
            {
                EnableOsd = false
            });
        }
    }

    private void SliderOsdDuration_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitialized && !_isReloadingSettings && !_isUpdatingOsdTextFromSlider)
        {
            if (txtOsdDuration != null && !txtOsdDuration.IsFocused)
            {
                _isUpdatingOsdTextFromSlider = true;
                txtOsdDuration.Text = UiBehavior.FormatOsdDuration(e.NewValue, CultureInfo.CurrentCulture);
                _isUpdatingOsdTextFromSlider = false;
            }
            SettingsManager.Save(SettingsManager.Load() with
            {
                OsdDuration = e.NewValue
            });
        }
    }

    private void TxtOsdDuration_LostFocus(object sender, RoutedEventArgs e)
    {
        CommitOsdDurationText();
    }

    private void TxtOsdDuration_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            CommitOsdDurationText();
            this.Focus();
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            double restoreVal = _osdDurationBeforeEdit ?? SettingsManager.Load().OsdDuration;
            SettingsManager.Save(SettingsManager.Load() with { OsdDuration = restoreVal });
            _isUpdatingOsdTextFromSlider = true;
            sliderOsdDuration.Value = restoreVal;
            txtOsdDuration.Text = UiBehavior.FormatOsdDuration(restoreVal, CultureInfo.CurrentCulture);
            _isUpdatingOsdTextFromSlider = false;
            _osdDurationBeforeEdit = null;
            this.Focus();
        }
    }

    private void TxtOsdDuration_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isInitialized && !_isReloadingSettings && !_isUpdatingOsdTextFromSlider)
        {
            if (_osdDurationBeforeEdit == null)
            {
                _osdDurationBeforeEdit = SettingsManager.Load().OsdDuration;
            }
            if (UiBehavior.TryParseOsdDuration(txtOsdDuration.Text, CultureInfo.CurrentCulture, out var result))
            {
                SettingsManager.Save(SettingsManager.Load() with
                {
                    OsdDuration = result
                });
                _isUpdatingOsdTextFromSlider = true;
                sliderOsdDuration.Value = result;
                _isUpdatingOsdTextFromSlider = false;
            }
        }
    }

    private void CommitOsdDurationText()
    {
        _osdDurationBeforeEdit = null;
        if (UiBehavior.TryParseAnyNumber(txtOsdDuration.Text, out double anyVal))
        {
            double clamped = Math.Clamp(anyVal, UiBehavior.MinimumOsdDuration, UiBehavior.MaximumOsdDuration);
            SettingsManager.Save(SettingsManager.Load() with
            {
                OsdDuration = clamped
            });
            _isUpdatingOsdTextFromSlider = true;
            sliderOsdDuration.Value = clamped;
            txtOsdDuration.Text = UiBehavior.FormatOsdDuration(clamped, CultureInfo.CurrentCulture);
            _isUpdatingOsdTextFromSlider = false;
        }
        else
        {
            AppSettings appSettings = SettingsManager.Load();
            txtOsdDuration.Text = UiBehavior.FormatOsdDuration(appSettings.OsdDuration, CultureInfo.CurrentCulture);
        }
    }

    private void SliderSoundVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitialized && !_isReloadingSettings && !_isUpdatingSoundVolumeTextFromSlider)
        {
            int volume = (int)Math.Round(e.NewValue);
            if (txtSoundVolume != null && !txtSoundVolume.IsFocused)
            {
                _isUpdatingSoundVolumeTextFromSlider = true;
                txtSoundVolume.Text = UiBehavior.FormatSoundVolume(volume);
                _isUpdatingSoundVolumeTextFromSlider = false;
            }
            SettingsManager.Save(SettingsManager.Load() with
            {
                SoundVolume = volume
            });
            AudioFeedback.SetVolume(volume);
        }
    }

    private void TxtSoundVolume_LostFocus(object sender, RoutedEventArgs e)
    {
        CommitSoundVolumeText();
    }

    private void TxtSoundVolume_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            CommitSoundVolumeText();
            this.Focus();
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            int restoreVal = _soundVolumeBeforeEdit ?? SettingsManager.Load().SoundVolume;
            SettingsManager.Save(SettingsManager.Load() with { SoundVolume = restoreVal });
            AudioFeedback.SetVolume(restoreVal);
            _isUpdatingSoundVolumeTextFromSlider = true;
            sliderSoundVolume.Value = restoreVal;
            txtSoundVolume.Text = UiBehavior.FormatSoundVolume(restoreVal);
            _isUpdatingSoundVolumeTextFromSlider = false;
            _soundVolumeBeforeEdit = null;
            this.Focus();
        }
    }

    private void TxtSoundVolume_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isInitialized && !_isReloadingSettings && !_isUpdatingSoundVolumeTextFromSlider)
        {
            if (_soundVolumeBeforeEdit == null)
            {
                _soundVolumeBeforeEdit = SettingsManager.Load().SoundVolume;
            }
            if (UiBehavior.TryParseSoundVolume(txtSoundVolume.Text, out var result))
            {
                SettingsManager.Save(SettingsManager.Load() with
                {
                    SoundVolume = result
                });
                AudioFeedback.SetVolume(result);
                _isUpdatingSoundVolumeTextFromSlider = true;
                sliderSoundVolume.Value = result;
                _isUpdatingSoundVolumeTextFromSlider = false;
            }
        }
    }

    private void CommitSoundVolumeText()
    {
        _soundVolumeBeforeEdit = null;
        string candidate = txtSoundVolume.Text.Trim().TrimEnd('%').Trim();
        if (int.TryParse(candidate, NumberStyles.Integer, CultureInfo.InvariantCulture, out int anyInt) ||
            int.TryParse(candidate, NumberStyles.Integer, CultureInfo.CurrentCulture, out anyInt))
        {
            int clamped = Math.Clamp(anyInt, UiBehavior.MinimumSoundVolume, UiBehavior.MaximumSoundVolume);
            SettingsManager.Save(SettingsManager.Load() with
            {
                SoundVolume = clamped
            });
            AudioFeedback.SetVolume(clamped);
            _isUpdatingSoundVolumeTextFromSlider = true;
            sliderSoundVolume.Value = clamped;
            txtSoundVolume.Text = UiBehavior.FormatSoundVolume(clamped);
            _isUpdatingSoundVolumeTextFromSlider = false;
        }
        else
        {
            AppSettings appSettings = SettingsManager.Load();
            txtSoundVolume.Text = UiBehavior.FormatSoundVolume(appSettings.SoundVolume);
        }
    }

    public void BtnOpenDataFolder_Click(object sender, RoutedEventArgs e)
    {
        try { SettingsManager.OpenDataFolderInExplorer(); }
        catch (Exception ex) { ShowTemporaryStatus("Could not open data folder: " + ex.Message); }
    }

    public void BtnChangeDataFolder_Click(object sender, RoutedEventArgs e)
    {
        using (FolderBrowserDialog fbd = new FolderBrowserDialog())
        {
            fbd.Description = "Select preferred folder to store MicMute settings and cache:";
            fbd.SelectedPath = SettingsManager.GetDataFolderPath();
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                try
                {
                    SettingsManager.SetCustomDataFolder(fbd.SelectedPath);
                    tbStoragePath.Text = SettingsManager.GetDataFolderPath();
                    ShowTemporaryStatus("Data folder updated to: " + fbd.SelectedPath);
                }
                catch (Exception ex)
                {
                    ShowTemporaryStatus("Could not change data folder: " + ex.Message);
                }
            }
        }
    }

    public void BtnResetSettings_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.MessageBoxResult res = System.Windows.MessageBox.Show(
            "Are you sure you want to reset all settings to defaults?",
            "Reset Settings",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (res == MessageBoxResult.Yes)
        {
            try
            {
                if (_isRecordingHotkey) StopRecordingHotkey(false);
                SettingsManager.ResetAllSettings();
                AppSettings defaultSettings = SettingsManager.Load();
                LoadSettingsIntoUI();
                ApplySettingsToRuntime(defaultSettings);
                (System.Windows.Application.Current as App)?.UpdateTrayIconState();
                ShowTemporaryStatus("Settings have been reset to factory defaults.");
            }
            catch (Exception ex) { ShowTemporaryStatus("Settings reset could not be completed: " + ex.Message); }
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _isDisposed = true;
        _deviceRefreshDebouncer.Dispose();
        _statusDebouncer.Dispose();
        _deviceRefreshCoordinator.Dispose();
        SettingsManager.SaveFailed -= SettingsManager_SaveFailed;
        _audioController.MuteStateChanged -= AudioController_MuteStateChanged;
        _audioController.DevicesChanged -= AudioController_DevicesChanged;
        _audioController.WarningNotification -= AudioController_WarningNotification;
        base.OnClosed(e);
        _hotkeyManager?.Dispose();
    }
}
