using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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

    internal System.Windows.Controls.Button btnStateToggle = null!;
    internal TextBlock tbStatusText = null!;
    internal DropShadowEffect statusGlow = null!;
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
    internal TextBlock tbStoragePath = null!;
    internal Border borderWarning = null!;
    internal TextBlock tbWarningMessage = null!;

    private const int WM_DEVICECHANGE = 0x0219;
    private const int WM_NCLBUTTONDOWN = 0x00A1;
    private const int HTCAPTION = 0x0002;

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    public MainWindow(AudioController audioController)
    {
        InitializeComponent();
        _audioController = audioController;
        _audioController.MuteStateChanged += AudioController_MuteStateChanged;
        _audioController.DevicesChanged += AudioController_DevicesChanged;
        _audioController.WarningNotification += AudioController_WarningNotification;
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
        using (Stream? stream = typeof(MainWindow).Assembly.GetManifestResourceStream("MicMute.MainWindow.xaml"))
        {
            if (stream != null)
            {
                using (StreamReader sr = new StreamReader(stream))
                {
                    string xaml = sr.ReadToEnd();
                    xaml = System.Text.RegularExpressions.Regex.Replace(xaml, @"\s+x:Class=""[^""]+""", "");
                    xaml = System.Text.RegularExpressions.Regex.Replace(xaml, @"\s+(Click|MouseLeftButtonDown|SelectionChanged|Checked|Unchecked|ValueChanged|LostFocus|KeyDown|TextChanged)=""[^""]+""", "");
                    root = (Window)XamlReader.Parse(xaml);
                    this.Content = root.Content;
                    this.Resources = root.Resources;
                    this.Width = root.Width;
                    this.Height = root.Height;
                    this.SizeToContent = root.SizeToContent;
                    this.WindowStyle = root.WindowStyle;
                    this.AllowsTransparency = root.AllowsTransparency;
                    this.Background = root.Background;
                    this.ResizeMode = root.ResizeMode;
                    this.WindowStartupLocation = root.WindowStartupLocation;
                    this.SnapsToDevicePixels = root.SnapsToDevicePixels;
                    this.UseLayoutRounding = root.UseLayoutRounding;
                }
            }
        }

        if (root == null) return;

        btnStateToggle = (System.Windows.Controls.Button)root.FindName("btnStateToggle");
        tbStatusText = (TextBlock)root.FindName("tbStatusText");
        statusGlow = (DropShadowEffect)root.FindName("statusGlow");
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
        tbStoragePath = (TextBlock)root.FindName("tbStoragePath");
        borderWarning = (Border)root.FindName("borderWarning");
        tbWarningMessage = (TextBlock)root.FindName("tbWarningMessage");

        // Event hooks
        var titleBar = (Border)root.FindName("borderTitleBar");
        if (titleBar != null) titleBar.MouseLeftButtonDown += TitleBar_MouseLeftButtonDown;
        var btnMin = (System.Windows.Controls.Button)root.FindName("btnMinimize");
        if (btnMin != null) btnMin.Click += MinimizeButton_Click;
        var btnCls = (System.Windows.Controls.Button)root.FindName("btnClose");
        if (btnCls != null) btnCls.Click += CloseButton_Click;
        var btnOpenFolder = (System.Windows.Controls.Button)root.FindName("btnOpenFolder");
        if (btnOpenFolder != null) btnOpenFolder.Click += BtnOpenDataFolder_Click;
        var btnChangeFolder = (System.Windows.Controls.Button)root.FindName("btnChangeFolder");
        if (btnChangeFolder != null) btnChangeFolder.Click += BtnChangeDataFolder_Click;
        var btnResetData = (System.Windows.Controls.Button)root.FindName("btnResetData");
        if (btnResetData != null) btnResetData.Click += BtnResetSettings_Click;

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
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        WindowInteropHelper windowInteropHelper = new WindowInteropHelper(this);
        HwndSource.FromHwnd(windowInteropHelper.Handle)?.AddHook(HwndMessageHook);
        _hotkeyManager = new HotkeyManager(windowInteropHelper.Handle);
        _hotkeyManager.HotkeyPressed += HotkeyManager_HotkeyPressed;
        AppSettings appSettings = SettingsManager.Load();
        RegisterGlobalHotkey(appSettings.Hotkey, appSettings.HotkeyModifiers);
    }

    private IntPtr HwndMessageHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_DEVICECHANGE)
        {
            TriggerDevicesChanged();
        }
        return IntPtr.Zero;
    }

    private async void TriggerDevicesChanged()
    {
        try
        {
            await Task.Delay(1200);
            _audioController?.ForceUpdateActiveDevice();
            await Dispatcher.InvokeAsync((Action)delegate
            {
                RefreshDeviceList();
            });
        }
        catch (Exception)
        {
        }
    }

    private void LoadSettingsIntoUI()
    {
        AppSettings appSettings = SettingsManager.Load();
        cbStartup.IsChecked = appSettings.RunOnStartup;
        cbStartMinimized.IsChecked = appSettings.StartMinimized;
        cbEnableOsd.IsChecked = appSettings.EnableOsd;
        sliderOsdDuration.Value = appSettings.OsdDuration;
        txtOsdDuration.Text = $"{appSettings.OsdDuration:F1}";
        cbLightMode.IsChecked = appSettings.LightMode;
        SetLightMode(appSettings.LightMode);
        DisplayHotkey(appSettings.Hotkey, appSettings.HotkeyModifiers);
        tbStoragePath.Text = SettingsManager.GetDataFolderPath();

        if (StartupManager.IsStartupEnabled() != appSettings.RunOnStartup)
        {
            StartupManager.SetStartup(appSettings.RunOnStartup);
        }
    }

    private void RefreshDeviceList()
    {
        if (_isUpdatingDeviceList)
        {
            return;
        }
        _isUpdatingDeviceList = true;
        try
        {
            List<AudioDevice> captureDevices = _audioController.GetCaptureDevices();
            cbDevices.ItemsSource = captureDevices;
            string currentId = _audioController.CurrentDeviceId;
            AudioDevice? audioDevice = captureDevices.FirstOrDefault((AudioDevice d) => d.Id == currentId);
            if (audioDevice != null)
            {
                cbDevices.SelectedItem = audioDevice;
            }
            else if (captureDevices.Count > 0)
            {
                cbDevices.SelectedIndex = 0;
            }
        }
        finally
        {
            _isUpdatingDeviceList = false;
        }
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
        bool isLight = SettingsManager.Load().LightMode;
        btnStateToggle.Tag = isMuted ? "Muted" : "Active";
        tbStatusText.Text = isMuted ? "M U T E D" : "A C T I V E";
        if (isMuted)
        {
            tbStatusText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68));
            if (tbStatusText.Effect is DropShadowEffect dropShadowEffect)
            {
                dropShadowEffect.Color = System.Windows.Media.Color.FromRgb(239, 68, 68);
                dropShadowEffect.Opacity = isLight ? 0.0 : 0.4;
                dropShadowEffect.BlurRadius = 8;
            }
        }
        else
        {
            tbStatusText.Foreground = isLight ? (SolidColorBrush)Resources["AccentBrush"] : new SolidColorBrush(System.Windows.Media.Color.FromRgb(41, 127, 184));
            if (tbStatusText.Effect is DropShadowEffect dropShadowEffect)
            {
                dropShadowEffect.Color = System.Windows.Media.Color.FromRgb(41, 127, 184);
                dropShadowEffect.Opacity = isLight ? 0.0 : 0.4;
                dropShadowEffect.BlurRadius = 8;
            }
        }
    }

    private void AudioController_MuteStateChanged(object? sender, MuteStateChangedEventArgs e)
    {
        Dispatcher.BeginInvoke((Action)delegate
        {
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

    private async void AudioController_DevicesChanged(object? sender, EventArgs e)
    {
        try
        {
            await Task.Delay(800);
            _audioController?.ForceUpdateActiveDevice();
            await Dispatcher.InvokeAsync((Action)delegate
            {
                RefreshDeviceList();
            });
        }
        catch (Exception)
        {
        }
    }

    private void AudioController_WarningNotification(object? sender, string message)
    {
        Dispatcher.BeginInvoke((Action)delegate
        {
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
        _isRecordingHotkey = true;
        btnRecordHotkey.Content = "Cancel";
        tbHotkey.Text = "Press keys...";
        tbHotkey.Foreground = (System.Windows.Media.Brush)FindResource("AccentBrush");
        PreviewKeyDown += MainWindow_PreviewKeyDown;
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
                ShowTemporaryStatus("Shortcut already in use by another app!");
                DisplayHotkey(appSettings.Hotkey, appSettings.HotkeyModifiers);
            }
        }
        else
        {
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
        ModifierKeys modifiers = System.Windows.Input.Keyboard.Modifiers;
        if (key == Key.LeftCtrl || key == Key.RightCtrl ||
            key == Key.LeftAlt || key == Key.RightAlt ||
            key == Key.LeftShift || key == Key.RightShift ||
            key == Key.LWin || key == Key.RWin)
        {
            tbHotkey.Text = FormatHotkeyText(Key.None, modifiers);
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
        tbHotkey.Text = FormatHotkeyText(key, modifiers);
        tbHotkey.SetResourceReference(TextBlock.ForegroundProperty, "TextWhiteBrush");
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

    private async void ShowTemporaryStatus(string message)
    {
        try
        {
            tbWarningMessage.Inlines.Clear();
            tbWarningMessage.Inlines.Add(new Run(message));
            borderWarning.Visibility = Visibility.Visible;
            await Task.Delay(3000);
            if (string.IsNullOrEmpty(_audioController.CurrentDeviceId))
            {
                ShowWarningMessage("No active audio capture devices found.", "");
                borderWarning.Visibility = Visibility.Visible;
            }
            else
            {
                borderWarning.Visibility = Visibility.Collapsed;
            }
        }
        catch (Exception)
        {
        }
    }

    private void CbStartup_Checked(object sender, RoutedEventArgs e)
    {
        if (_isInitialized)
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
        if (_isInitialized)
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
        if (_isInitialized)
        {
            SettingsManager.Save(SettingsManager.Load() with
            {
                StartMinimized = true
            });
        }
    }

    private void CbStartMinimized_Unchecked(object sender, RoutedEventArgs e)
    {
        if (_isInitialized)
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
        if (_isInitialized)
        {
            SetLightMode(isLight: true);
            SettingsManager.Save(SettingsManager.Load() with
            {
                LightMode = true
            });
        }
    }

    private void CbLightMode_Unchecked(object sender, RoutedEventArgs e)
    {
        if (_isInitialized)
        {
            SetLightMode(isLight: false);
            SettingsManager.Save(SettingsManager.Load() with
            {
                LightMode = false
            });
        }
    }

    private void SetLightMode(bool isLight)
    {
        System.Windows.Media.Color color = isLight ? System.Windows.Media.Color.FromRgb(21, 90, 132) : System.Windows.Media.Color.FromRgb(41, 127, 184);
        Resources["AccentColor"] = color;
        Resources["AccentBrush"] = new SolidColorBrush(color);
        Resources["AccentHoverBrush"] = new SolidColorBrush(isLight ? System.Windows.Media.Color.FromArgb(26, 21, 90, 132) : System.Windows.Media.Color.FromArgb(26, 41, 127, 184));
        if (isLight)
        {
            Resources["WindowBgBrush"] = new LinearGradientBrush(System.Windows.Media.Color.FromArgb(250, 241, 245, 249), System.Windows.Media.Color.FromArgb(250, 226, 234, 242), new Point(0.0, 0.0), new Point(1.0, 1.0));
            Resources["CaptionButtonHoverBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(24, 0, 0, 0));
            Resources["CardBgBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(245, 255, 255, 255));
            Resources["InputBgBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
            Resources["TitleBarBgBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0, 255, 255, 255));
            Resources["TitleTextBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42));
            Resources["TextWhiteBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42));
            Resources["TextGrayBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139));
            Resources["TextDimBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(148, 163, 184));
            Resources["BorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(203, 213, 225));
            Resources["ToggleOffBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(203, 213, 225));
            Resources["ToggleOffBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(148, 163, 184));
            LinearGradientBrush value = new LinearGradientBrush(System.Windows.Media.Color.FromArgb(255, 224, 242, 254), System.Windows.Media.Color.FromArgb(255, 186, 230, 253), new Point(0.0, 0.0), new Point(1.0, 1.0));
            LinearGradientBrush value2 = new LinearGradientBrush(System.Windows.Media.Color.FromArgb(255, 56, 189, 248), System.Windows.Media.Color.FromArgb(255, 14, 165, 233), new Point(0.0, 0.0), new Point(0.0, 1.0));
            Resources["RecordGlassBrush"] = value;
            Resources["RecordGlassBorderBrush"] = value2;
            Resources["WarningBgBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(30, 239, 68, 68));
            Resources["WarningBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68));
            Resources["WarningTextBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 38, 38));
        }
        else
        {
            Resources["WindowBgBrush"] = new LinearGradientBrush(System.Windows.Media.Color.FromArgb(235, 20, 30, 45), System.Windows.Media.Color.FromArgb(235, 10, 15, 25), new Point(0.0, 0.0), new Point(1.0, 1.0));
            Resources["CaptionButtonHoverBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(24, 255, 255, 255));
            Resources["CardBgBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(28, 255, 255, 255));
            Resources["InputBgBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 41, 59));
            Resources["TitleBarBgBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0, 0, 0, 0));
            Resources["TitleTextBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 245, 247));
            Resources["TextWhiteBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 245, 247));
            Resources["TextGrayBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(134, 134, 139));
            Resources["TextDimBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(134, 134, 139));
            Resources["BorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 255, 255, 255));
            Resources["ToggleOffBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(99, 99, 102));
            Resources["ToggleOffBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(142, 142, 147));
            LinearGradientBrush value3 = new LinearGradientBrush(System.Windows.Media.Color.FromArgb(160, 179, 224, 247), System.Windows.Media.Color.FromArgb(160, 129, 180, 214), new Point(0.0, 0.0), new Point(1.0, 1.0));
            LinearGradientBrush value4 = new LinearGradientBrush(System.Windows.Media.Color.FromArgb(192, 74, 178, 235), System.Windows.Media.Color.FromArgb(192, 29, 120, 168), new Point(0.0, 0.0), new Point(0.0, 1.0));
            Resources["RecordGlassBrush"] = value3;
            Resources["RecordGlassBorderBrush"] = value4;
            Resources["WarningBgBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(26, 255, 69, 58));
            Resources["WarningBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 69, 58));
            Resources["WarningTextBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 69, 58));
        }
        if (_audioController != null)
        {
            UpdateMuteStateUI(_audioController.IsMuted);
        }
    }

    private void CbEnableOsd_Checked(object sender, RoutedEventArgs e)
    {
        if (_isInitialized)
        {
            SettingsManager.Save(SettingsManager.Load() with
            {
                EnableOsd = true
            });
        }
    }

    private void CbEnableOsd_Unchecked(object sender, RoutedEventArgs e)
    {
        if (_isInitialized)
        {
            SettingsManager.Save(SettingsManager.Load() with
            {
                EnableOsd = false
            });
        }
    }

    private void SliderOsdDuration_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitialized)
        {
            if (txtOsdDuration != null)
            {
                _isUpdatingOsdTextFromSlider = true;
                txtOsdDuration.Text = $"{e.NewValue:F1}";
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
        if (_isInitialized && !_isUpdatingOsdTextFromSlider && double.TryParse(txtOsdDuration.Text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var result) && result >= 0.1 && result <= 30.0)
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
        if (double.TryParse(txtOsdDuration.Text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
        {
            if (result < 0.1)
            {
                result = 0.1;
            }
            if (result > 30.0)
            {
                result = 30.0;
            }
            SettingsManager.Save(SettingsManager.Load() with
            {
                OsdDuration = result
            });
            _isUpdatingOsdTextFromSlider = true;
            sliderOsdDuration.Value = result;
            txtOsdDuration.Text = $"{result:F1}";
            _isUpdatingOsdTextFromSlider = false;
        }
        else
        {
            AppSettings appSettings = SettingsManager.Load();
            txtOsdDuration.Text = $"{appSettings.OsdDuration:F1}";
        }
    }

    public void BtnOpenDataFolder_Click(object sender, RoutedEventArgs e)
    {
        SettingsManager.OpenDataFolderInExplorer();
    }

    public void BtnChangeDataFolder_Click(object sender, RoutedEventArgs e)
    {
        using (FolderBrowserDialog fbd = new FolderBrowserDialog())
        {
            fbd.Description = "Select preferred folder to store MicMute settings and cache:";
            fbd.UseDescriptionForTitle = true;
            fbd.SelectedPath = SettingsManager.GetDataFolderPath();
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                SettingsManager.SetCustomDataFolder(fbd.SelectedPath);
                tbStoragePath.Text = SettingsManager.GetDataFolderPath();
                ShowTemporaryStatus("Data folder updated to: " + fbd.SelectedPath);
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
            SettingsManager.ResetAllSettings();
            LoadSettingsIntoUI();
            ShowTemporaryStatus("Settings have been reset to factory defaults.");
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _hotkeyManager?.Dispose();
    }
}