using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
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
    private bool _contentLoaded;
    private bool _isReloadingSettings;
    private bool _isDisposed;
    private readonly DispatcherDebouncer _deviceRefreshDebouncer;
    private readonly DispatcherDebouncer _statusDebouncer;

    internal System.Windows.Controls.Button btnStateToggle = null!;
    internal TextBlock tbStatusText = null!;
    internal DropShadowEffect statusGlow = null!;
    internal Border? borderStatusCapsule;
    internal System.Windows.Shapes.Ellipse? dotStatus;
    internal DropShadowEffect? dotGlow;
    internal System.Windows.Controls.Button? btnHeroHotkey;
    internal TextBlock? tbHeroHotkey;
    internal System.Windows.Controls.Button? btnTitleTheme;
    internal System.Windows.Shapes.Path? pathTitleTheme;
    internal System.Windows.Shapes.Path? titleBarIcon;
    internal System.Windows.Controls.Button? btnMin;
    internal System.Windows.Controls.Button? btnCls;
    internal System.Windows.Controls.ComboBox cbDevices = null!;
    internal Border borderHotkey = null!;
    internal DropShadowEffect glowHotkey = null!;
    internal TextBlock tbHotkey = null!;
    internal System.Windows.Controls.Button btnRecordHotkey = null!;
    internal System.Windows.Controls.CheckBox cbEnableOsd = null!;
    internal Slider sliderOsdDuration = null!;
    internal System.Windows.Controls.TextBox txtOsdDuration = null!;
    internal System.Windows.Controls.CheckBox cbStartup = null!;
    internal System.Windows.Controls.CheckBox cbStartMinimized = null!;
    internal System.Windows.Controls.CheckBox cbLightMode = null!;
    internal System.Windows.Controls.CheckBox cbRunAsAdmin = null!;
    internal System.Windows.Controls.CheckBox cbSoundFeedback = null!;
    internal TextBlock tbStoragePath = null!;
    internal Border borderWarning = null!;
    internal TextBlock tbWarningMessage = null!;
    internal System.Windows.Controls.Image imgAppIcon = null!;
    internal ScrollViewer contentScrollViewer = null!;

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
        _deviceRefreshDebouncer = new DispatcherDebouncer(Dispatcher);
        _statusDebouncer = new DispatcherDebouncer(Dispatcher);
        _audioController = audioController;
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

        Window? root = null;
        string? xaml = null;

        string localFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MainWindow.xaml");
        if (File.Exists(localFile))
        {
            try { xaml = File.ReadAllText(localFile); } catch { }
        }

        if (string.IsNullOrEmpty(xaml))
        {
            using (Stream? stream = typeof(MainWindow).Assembly.GetManifestResourceStream("MicMute.MainWindow.xaml"))
            {
                if (stream != null)
                {
                    using (StreamReader sr = new StreamReader(stream))
                    {
                        xaml = sr.ReadToEnd();
                    }
                }
            }
        }

        if (!string.IsNullOrEmpty(xaml))
        {
            xaml = System.Text.RegularExpressions.Regex.Replace(xaml, @"\s+x:Class=""[^""]+""", "");
            xaml = System.Text.RegularExpressions.Regex.Replace(xaml, @"\s+(Click|MouseLeftButtonDown|SelectionChanged|Checked|Unchecked|ValueChanged|LostFocus|KeyDown|TextChanged)=""[^""]+""", "");
            root = (Window)XamlReader.Parse(xaml);
            
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

            // Bind all controls from root before detaching content
            btnStateToggle = (System.Windows.Controls.Button)root.FindName("btnStateToggle");
            tbStatusText = (TextBlock)root.FindName("tbStatusText");
            dotStatus = root.FindName("dotStatus") as System.Windows.Shapes.Ellipse;
            dotGlow = root.FindName("dotGlow") as DropShadowEffect;
            statusGlow = (DropShadowEffect)root.FindName("statusGlow") ?? dotGlow!;
            borderStatusCapsule = root.FindName("borderStatusCapsule") as Border;
            btnHeroHotkey = root.FindName("btnHeroHotkey") as System.Windows.Controls.Button;
            tbHeroHotkey = root.FindName("tbHeroHotkey") as TextBlock;
            btnTitleTheme = root.FindName("btnTitleTheme") as System.Windows.Controls.Button;
            pathTitleTheme = root.FindName("pathTitleTheme") as System.Windows.Shapes.Path;
            titleBarIcon = root.FindName("titleBarIcon") as System.Windows.Shapes.Path;

            cbDevices = (System.Windows.Controls.ComboBox)root.FindName("cbDevices");
            borderHotkey = (Border)root.FindName("borderHotkey");
            glowHotkey = (DropShadowEffect)root.FindName("glowHotkey");
            tbHotkey = (TextBlock)root.FindName("tbHotkey");
            btnRecordHotkey = (System.Windows.Controls.Button)root.FindName("btnRecordHotkey");
            cbEnableOsd = (System.Windows.Controls.CheckBox)root.FindName("cbEnableOsd");
            sliderOsdDuration = (Slider)root.FindName("sliderOsdDuration");
            txtOsdDuration = (System.Windows.Controls.TextBox)root.FindName("txtOsdDuration");
            cbStartup = (System.Windows.Controls.CheckBox)root.FindName("cbStartup");
            cbStartMinimized = (System.Windows.Controls.CheckBox)root.FindName("cbStartMinimized");
            cbLightMode = (System.Windows.Controls.CheckBox)root.FindName("cbLightMode");
            cbRunAsAdmin = (System.Windows.Controls.CheckBox)root.FindName("cbRunAsAdmin");
            cbSoundFeedback = (System.Windows.Controls.CheckBox)root.FindName("cbSoundFeedback");
            tbStoragePath = (TextBlock)root.FindName("tbStoragePath");
            borderWarning = (Border)root.FindName("borderWarning");
            tbWarningMessage = (TextBlock)root.FindName("tbWarningMessage");
            contentScrollViewer = (ScrollViewer)root.FindName("contentScrollViewer");

            // Event hooks
            var titleBar = (Border)root.FindName("borderTitleBar");
            if (titleBar != null) titleBar.MouseLeftButtonDown += TitleBar_MouseLeftButtonDown;
            btnMin = (System.Windows.Controls.Button)root.FindName("btnMinimize");
            if (btnMin != null) btnMin.Click += MinimizeButton_Click;
            btnCls = (System.Windows.Controls.Button)root.FindName("btnClose");
            if (btnCls != null) btnCls.Click += CloseButton_Click;
            if (btnTitleTheme != null) btnTitleTheme.Click += BtnTitleTheme_Click;
            if (btnHeroHotkey != null) btnHeroHotkey.Click += BtnRecordHotkey_Click;
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
        if (txtOsdDuration != null)
        {
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
        this.SizeChanged += MainWindow_SizeChanged;
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

            double maxAllowedHeight = UiBehavior.CalculateAdaptiveMaxHeight(workHeight, UiBehavior.DefaultAdaptiveVerticalMargin);
            if (Math.Abs(this.MaxHeight - maxAllowedHeight) > 0.5)
            {
                this.MaxHeight = maxAllowedHeight;
            }

            double currentWidth = this.ActualWidth > 0 ? this.ActualWidth : (this.Width > 0 ? this.Width : 350.0);

            if (isInitialPlacement)
            {
                this.Measure(new System.Windows.Size(currentWidth, maxAllowedHeight));
                double desiredHeight = this.DesiredSize.Height > 0 ? this.DesiredSize.Height : maxAllowedHeight;
                double targetHeight = Math.Min(desiredHeight, maxAllowedHeight);

                var centered = UiBehavior.CalculateCenteredWindowBounds(
                    new DipRect(workLeft, workTop, workWidth, workHeight),
                    currentWidth,
                    targetHeight,
                    UiBehavior.DefaultAdaptiveVerticalMargin);

                this.Left = centered.Left;
                this.Top = centered.Top;
            }
            else
            {
                double currentHeight = this.ActualHeight > 0 ? this.ActualHeight : this.MaxHeight;
                var clamped = UiBehavior.ClampWindowBoundsToWorkArea(
                    new DipRect(workLeft, workTop, workWidth, workHeight),
                    new DipRect(this.Left, this.Top, currentWidth, currentHeight),
                    UiBehavior.DefaultAdaptiveVerticalMargin);

                this.Left = clamped.Left;
                this.Top = clamped.Top;
            }
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
            double scaleY = dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1.0;
            double workTop = screen.WorkingArea.Top / scaleY;
            double workHeight = screen.WorkingArea.Height / scaleY;

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
            sliderOsdDuration.Value = appSettings.OsdDuration;
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
            if (imgAppIcon != null) imgAppIcon.ToolTip = "Mic Mute (Administrator - Elevated Mode Active)";
            tbStatusText.ToolTip = "Administrator Mode Active: In-game hotkeys enabled over games and elevated windows";
        }
        else
        {
            if (imgAppIcon != null) imgAppIcon.ToolTip = "Mic Mute (Standard User)";
            tbStatusText.ToolTip = "Tip: Enable 'Run as Administrator' below to use hotkeys inside games and elevated windows";
        }

    }

    private void ApplySettingsToRuntime(AppSettings settings)
    {
        _audioController.SetTargetDevice(settings.SelectedDeviceId);
        StartupManager.SetStartup(settings.RunOnStartup);
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
        if (_isUpdatingDeviceList || _isDisposed)
        {
            return;
        }
        _isUpdatingDeviceList = true;
        Task.Run(() =>
        {
            try
            {
                List<AudioDevice> captureDevices = _audioController.GetCaptureDevices();
                string currentId = _audioController.CurrentDeviceId;
                if (_isDisposed || Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished) return;
                Dispatcher.BeginInvoke((Action)delegate
                {
                    if (_isDisposed) return;
                    try
                    {
                        cbDevices.ItemsSource = captureDevices;
                        AudioDevice? audioDevice = captureDevices.FirstOrDefault((AudioDevice d) => d.Id == currentId);
                        if (audioDevice != null)
                        {
                            cbDevices.SelectedItem = audioDevice;
                        }
                    }
                    finally
                    {
                        _isUpdatingDeviceList = false;
                    }
                });
            }
            catch
            {
                if (!_isDisposed && !Dispatcher.HasShutdownStarted)
                {
                    Dispatcher.BeginInvoke((Action)delegate { _isUpdatingDeviceList = false; });
                }
            }
        });
    }

    private void CbDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isUpdatingDeviceList && cbDevices.SelectedItem is AudioDevice audioDevice)
        {
            _audioController.SetTargetDevice(audioDevice.Id);
            SettingsManager.Save(SettingsManager.Load() with
            {
                SelectedDeviceId = audioDevice.Id
            });
        }
    }

    private void BtnStateToggle_Click(object sender, RoutedEventArgs e)
    {
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
                borderStatusCapsule.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 100, 116, 139));
            }
            ShowWarningMessage("No active audio capture devices found.", "");
            borderWarning.Visibility = Visibility.Visible;
            return;
        }

        btnStateToggle.Tag = isMuted ? "Muted" : "Active";
        tbStatusText.Text = isMuted ? "MICROPHONE MUTED" : "MICROPHONE LIVE";

        if (isMuted)
        {
            var redColor = System.Windows.Media.Color.FromRgb(239, 68, 68);
            var redTextBrush = new SolidColorBrush(isLight ? System.Windows.Media.Color.FromRgb(220, 38, 38) : System.Windows.Media.Color.FromRgb(248, 113, 113));
            tbStatusText.Foreground = redTextBrush;
            if (dotStatus != null) dotStatus.Fill = new SolidColorBrush(redColor);
            if (dotGlow != null)
            {
                dotGlow.Color = redColor;
                dotGlow.Opacity = isLight ? 0.3 : 0.8;
                dotGlow.BlurRadius = 8;
            }
            if (borderStatusCapsule != null)
            {
                borderStatusCapsule.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(isLight ? (byte)25 : (byte)32, 239, 68, 68));
                borderStatusCapsule.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(isLight ? (byte)80 : (byte)96, 239, 68, 68));
            }
        }
        else
        {
            var activeColor = isLight ? System.Windows.Media.Color.FromRgb(15, 23, 42) : System.Windows.Media.Color.FromRgb(255, 255, 255);
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
                borderStatusCapsule.Background = new SolidColorBrush(isLight ? System.Windows.Media.Color.FromArgb(16, 15, 23, 42) : System.Windows.Media.Color.FromArgb(32, 255, 255, 255));
                borderStatusCapsule.BorderBrush = new SolidColorBrush(isLight ? System.Windows.Media.Color.FromArgb(40, 15, 23, 42) : System.Windows.Media.Color.FromArgb(80, 255, 255, 255));
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
            tbWarningMessage.Inlines.Clear();
            System.Windows.Media.Brush foreground = (System.Windows.Media.Brush)FindResource("TextWhiteBrush");
            tbWarningMessage.Inlines.Add(new Run(message)
            {
                Foreground = foreground
            });
            borderWarning.Visibility = Visibility.Visible;
        });
    }

    private void HotkeyManager_HotkeyPressed()
    {
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

    private void StartRecordingHotkey()
    {
        _hotkeyManager?.Unregister();
        _isRecordingHotkey = true;
        btnRecordHotkey.Content = "Cancel";
        tbHotkey.Text = "Press keys...";
        tbHotkey.Foreground = (System.Windows.Media.Brush)FindResource("AccentBrush");
        if (tbHeroHotkey != null)
        {
            tbHeroHotkey.Text = "Press keys...";
            tbHeroHotkey.Foreground = (System.Windows.Media.Brush)FindResource("AccentBrush");
        }
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

    private void DisplayHotkey(Key key, ModifierKeys modifiers)
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
            tbHeroHotkey.SetResourceReference(TextBlock.ForegroundProperty, "KeycapTextBrush");
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
            StartupManager.SetStartup(runOnStartup: true);
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
        SettingsManager.Save(SettingsManager.Load() with { RunAsAdmin = true });

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
        SettingsManager.Save(SettingsManager.Load() with { RunAsAdmin = false });
    }

    private void CbSoundFeedback_Checked(object sender, RoutedEventArgs e)
    {
        if (!_isInitialized || _isReloadingSettings) return;
        SettingsManager.Save(SettingsManager.Load() with { PlaySoundFeedback = true });
    }

    private void CbSoundFeedback_Unchecked(object sender, RoutedEventArgs e)
    {
        if (!_isInitialized || _isReloadingSettings) return;
        SettingsManager.Save(SettingsManager.Load() with { PlaySoundFeedback = false });
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
        System.Windows.Media.Color accentColor = isLight ? System.Windows.Media.Color.FromRgb(15, 23, 42) : System.Windows.Media.Color.FromRgb(255, 255, 255);
        Resources["AccentColor"] = accentColor;
        Resources["AccentBrush"] = new SolidColorBrush(accentColor);
        Resources["AccentHoverBrush"] = new SolidColorBrush(isLight ? System.Windows.Media.Color.FromArgb(20, 15, 23, 42) : System.Windows.Media.Color.FromArgb(37, 255, 255, 255));

        var themeGlyphBrush = new SolidColorBrush(isLight ? System.Windows.Media.Color.FromRgb(15, 23, 42) : System.Windows.Media.Color.FromRgb(248, 250, 252));
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
                System.Windows.Media.Color.FromArgb(248, 248, 250, 252),
                System.Windows.Media.Color.FromArgb(248, 226, 232, 240),
                new Point(0.0, 0.0), new Point(1.0, 1.0));
            Resources["CaptionButtonHoverBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(20, 0, 0, 0));
            Resources["CardBgBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(240, 255, 255, 255));
            Resources["InputBgBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
            Resources["TitleBarBgBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0, 255, 255, 255));
            Resources["TitleTextBrush"] = themeGlyphBrush;
            Resources["TextWhiteBrush"] = themeGlyphBrush; // dark text in light mode
            Resources["TextGrayBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(71, 85, 105)); // dark slate
            Resources["TextDimBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(148, 163, 184));
            Resources["BorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(35, 0, 0, 0));
            Resources["DividerBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(20, 0, 0, 0));

            // Liquid Switches (Black Liquid in Light Mode)
            Resources["ToggleOnBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42));
            Resources["ToggleOnBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42));
            Resources["ToggleOffBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(226, 232, 240));
            Resources["ToggleOffBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(203, 213, 225));
            Resources["ToggleKnobBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42));
            Resources["ToggleKnobBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(203, 213, 225));
            Resources["ToggleKnobActiveBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
            Resources["ToggleKnobActiveBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(226, 232, 240));

            // Liquid OSD Duration Slider (Black Liquid in Light Mode)
            Resources["SliderTrackBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42));
            Resources["SliderTrackEmptyBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(226, 232, 240));
            Resources["SliderTrackBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(203, 213, 225));
            Resources["SliderThumbBgBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
            Resources["SliderThumbBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42));

            // Jewel Ring
            Resources["JewelRingBgBrush"] = new LinearGradientBrush(
                System.Windows.Media.Color.FromArgb(180, 255, 255, 255),
                System.Windows.Media.Color.FromArgb(120, 241, 245, 249),
                new Point(0.0, 0.0), new Point(1.0, 1.0));
            Resources["JewelRingBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(180, 255, 255, 255));

            // Hero Mute Button: Liquid Black Lens in Light Mode
            var heroLensLight = new RadialGradientBrush
            {
                Center = new Point(0.35, 0.30),
                GradientOrigin = new Point(0.35, 0.30),
                RadiusX = 0.65,
                RadiusY = 0.65
            };
            heroLensLight.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromRgb(51, 65, 85), 0.0));
            heroLensLight.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromRgb(30, 41, 59), 0.45));
            heroLensLight.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromRgb(15, 23, 42), 0.85));
            heroLensLight.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromRgb(2, 6, 23), 1.0));
            Resources["HeroLensActiveBrush"] = heroLensLight;

            var heroGlowLight = new RadialGradientBrush
            {
                Center = new Point(0.5, 0.5),
                GradientOrigin = new Point(0.5, 0.5),
                RadiusX = 0.5,
                RadiusY = 0.5
            };
            heroGlowLight.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(50, 15, 23, 42), 0.0));
            heroGlowLight.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(0, 15, 23, 42), 1.0));
            Resources["HeroGlowActiveBrush"] = heroGlowLight;
            Resources["HeroIconActiveBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));

            // Monochrome Squircle Tiles: Pure Black Icon in Light Mode
            Resources["SquircleBgBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(16, 0, 0, 0));
            Resources["SquircleBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(28, 0, 0, 0));
            Resources["SquircleIconBrush"] = themeGlyphBrush;

            // Keycaps
            Resources["KeycapBgBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 245, 249));
            Resources["KeycapBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(203, 213, 225));
            Resources["KeycapTextBrush"] = themeGlyphBrush;

            // Status Capsule
            Resources["StatusCapsuleBgBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(20, 15, 23, 42));
            Resources["StatusCapsuleBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 15, 23, 42));
            Resources["StatusCapsuleTextBrush"] = themeGlyphBrush;
            Resources["StatusDotBrush"] = themeGlyphBrush;

            // Warning
            Resources["WarningBgBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(30, 239, 68, 68));
            Resources["WarningBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68));
            Resources["WarningTextBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 38, 38));

            // Scrollbars
            Resources["ScrollBarThumbBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(45, 0, 0, 0));
            Resources["ScrollBarThumbHoverBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(80, 0, 0, 0));
            Resources["ScrollBarThumbDragBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(120, 0, 0, 0));
        }
        else
        {
            Resources["WindowBgBrush"] = new LinearGradientBrush(
                System.Windows.Media.Color.FromArgb(240, 15, 23, 42),
                System.Windows.Media.Color.FromArgb(240, 2, 6, 23),
                new Point(0.0, 0.0), new Point(1.0, 1.0));
            Resources["CaptionButtonHoverBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(24, 255, 255, 255));
            Resources["CardBgBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(18, 255, 255, 255));
            Resources["InputBgBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 41, 59));
            Resources["TitleBarBgBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0, 0, 0, 0));
            Resources["TitleTextBrush"] = themeGlyphBrush;
            Resources["TextWhiteBrush"] = themeGlyphBrush;
            Resources["TextGrayBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(148, 163, 184));
            Resources["TextDimBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139));
            Resources["BorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(37, 255, 255, 255));
            Resources["DividerBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(24, 255, 255, 255));

            // Liquid Switches (White Liquid in Dark Mode)
            Resources["ToggleOnBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
            Resources["ToggleOnBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
            Resources["ToggleOffBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(42, 55, 74));
            Resources["ToggleOffBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(71, 85, 105));
            Resources["ToggleKnobBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
            Resources["ToggleKnobBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(226, 232, 240));
            Resources["ToggleKnobActiveBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42));
            Resources["ToggleKnobActiveBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(51, 65, 85));

            // Liquid OSD Duration Slider (White Liquid in Dark Mode)
            Resources["SliderTrackBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
            Resources["SliderTrackEmptyBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 41, 59));
            Resources["SliderTrackBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(51, 65, 85));
            Resources["SliderThumbBgBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
            Resources["SliderThumbBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42));

            // Jewel Ring
            Resources["JewelRingBgBrush"] = new LinearGradientBrush(
                System.Windows.Media.Color.FromArgb(37, 255, 255, 255),
                System.Windows.Media.Color.FromArgb(6, 255, 255, 255),
                new Point(0.0, 0.0), new Point(1.0, 1.0));
            Resources["JewelRingBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(69, 255, 255, 255));

            // Hero Mute Button: Liquid White Lens in Dark Mode
            var heroLensDark = new RadialGradientBrush
            {
                Center = new Point(0.35, 0.30),
                GradientOrigin = new Point(0.35, 0.30),
                RadiusX = 0.65,
                RadiusY = 0.65
            };
            heroLensDark.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromRgb(255, 255, 255), 0.0));
            heroLensDark.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromRgb(241, 245, 249), 0.45));
            heroLensDark.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromRgb(226, 232, 240), 0.85));
            heroLensDark.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromRgb(203, 213, 225), 1.0));
            Resources["HeroLensActiveBrush"] = heroLensDark;

            var heroGlowDark = new RadialGradientBrush
            {
                Center = new Point(0.5, 0.5),
                GradientOrigin = new Point(0.5, 0.5),
                RadiusX = 0.5,
                RadiusY = 0.5
            };
            heroGlowDark.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(144, 255, 255, 255), 0.0));
            heroGlowDark.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(0, 255, 255, 255), 1.0));
            Resources["HeroGlowActiveBrush"] = heroGlowDark;
            Resources["HeroIconActiveBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42));

            // Monochrome Squircle Tiles: Pure White Icon in Dark Mode
            Resources["SquircleBgBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(22, 255, 255, 255));
            Resources["SquircleBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(38, 255, 255, 255));
            Resources["SquircleIconBrush"] = themeGlyphBrush;

            // Keycaps
            Resources["KeycapBgBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 41, 59));
            Resources["KeycapBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(71, 85, 105));
            Resources["KeycapTextBrush"] = themeGlyphBrush;

            // Status Capsule
            Resources["StatusCapsuleBgBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(32, 255, 255, 255));
            Resources["StatusCapsuleBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(80, 255, 255, 255));
            Resources["StatusCapsuleTextBrush"] = themeGlyphBrush;
            Resources["StatusDotBrush"] = themeGlyphBrush;

            // Warning
            Resources["WarningBgBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(26, 255, 69, 58));
            Resources["WarningBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 69, 58));
            Resources["WarningTextBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 69, 58));

            // Scrollbars
            Resources["ScrollBarThumbBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(48, 255, 255, 255));
            Resources["ScrollBarThumbHoverBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(85, 255, 255, 255));
            Resources["ScrollBarThumbDragBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(128, 255, 255, 255));
        }

        if (_audioController != null)
        {
            UpdateMuteStateUI(_audioController.IsMuted);
        }
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
            btnStateToggle.Focus();
        }
    }

    private void TxtOsdDuration_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isInitialized && !_isReloadingSettings && !_isUpdatingOsdTextFromSlider
            && UiBehavior.TryParseOsdDuration(txtOsdDuration.Text, CultureInfo.CurrentCulture, out var result))
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

    private void CommitOsdDurationText()
    {
        if (UiBehavior.TryParseOsdDuration(txtOsdDuration.Text, CultureInfo.CurrentCulture, out var result))
        {
            SettingsManager.Save(SettingsManager.Load() with
            {
                OsdDuration = result
            });
            _isUpdatingOsdTextFromSlider = true;
            sliderOsdDuration.Value = result;
            txtOsdDuration.Text = UiBehavior.FormatOsdDuration(result, CultureInfo.CurrentCulture);
            _isUpdatingOsdTextFromSlider = false;
        }
        else
        {
            AppSettings appSettings = SettingsManager.Load();
            txtOsdDuration.Text = UiBehavior.FormatOsdDuration(appSettings.OsdDuration, CultureInfo.CurrentCulture);
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
        SettingsManager.SaveFailed -= SettingsManager_SaveFailed;
        _audioController.MuteStateChanged -= AudioController_MuteStateChanged;
        _audioController.DevicesChanged -= AudioController_DevicesChanged;
        _audioController.WarningNotification -= AudioController_WarningNotification;
        base.OnClosed(e);
        _hotkeyManager?.Dispose();
    }
}
