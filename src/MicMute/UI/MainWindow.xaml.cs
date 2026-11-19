using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
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
    private readonly DispatcherDebouncer _volumeAudioDebouncer;
    private readonly LatestRefreshCoordinator<List<AudioDevice>> _deviceRefreshCoordinator;
    private readonly double _preferredWidth;
    private bool _hasInitialCentered;
    private double? _osdDurationBeforeEdit;
    private int? _soundVolumeBeforeEdit;
    private int _themeTransitionVersion;
    private bool _holdToggledActive;
    private static readonly Regex XClassRegex = new(@"\s+x:Class=""[^""]+""", RegexOptions.Compiled);
    private static readonly Regex EventAttrRegex = new(
        @"\s+(Click|MouseLeftButtonDown|SelectionChanged|Checked|Unchecked|ValueChanged|LostFocus|KeyDown|TextChanged)=""[^""]+""",
        RegexOptions.Compiled);
    private static readonly HttpClient UpdateHttpClient = CreateUpdateHttpClient();
    private static Dictionary<string, object>? _cachedLightTheme;
    private static Dictionary<string, object>? _cachedDarkTheme;

    private static HttpClient CreateUpdateHttpClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("MicMute/1.0");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }

    public event EventHandler? TaskbarCreated;

    internal System.Windows.Controls.Button btnStateToggle = null!;
    internal TextBlock tbStatusText = null!;
    internal DropShadowEffect statusGlow = null!;
    internal Border? borderStatusCapsule;
    internal System.Windows.Shapes.Ellipse? dotStatus;
    internal DropShadowEffect? dotGlow;
    internal System.Windows.Controls.Button? btnTitleTheme;
    internal System.Windows.Shapes.Path? pathTitleTheme;
    internal System.Windows.Shapes.Path? titleBarIcon;
    internal System.Windows.Controls.Button? btnAppMenu;
    internal RotateTransform? appMenuChevronRot;
    internal System.Windows.Controls.Primitives.Popup? popupAppMenu;
    internal Border? borderAppMenu;
    internal ScaleTransform? appMenuScale;
    internal TranslateTransform? appMenuTranslate;
    internal System.Windows.Controls.Button? btnMenuStartupState;
    internal TextBlock? tbMenuStartupState;
    internal Border? borderMenuStartupBadge;
    internal TextBlock? tbMenuStartupBadge;
    internal System.Windows.Controls.Button? btnMenuSyncAllMics;
    internal TextBlock? tbMenuSyncAllMics;
    internal Border? borderMenuSyncBadge;
    internal TextBlock? tbMenuSyncBadge;
    internal System.Windows.Controls.Button? btnMenuLockMute;
    internal TextBlock? tbMenuLockMute;
    internal Border? borderMenuLockBadge;
    internal TextBlock? tbMenuLockBadge;
    internal System.Windows.Controls.Button? btnMenuCheckUpdates;
    internal System.Windows.Controls.Button? btnMenuAbout;
    internal TextBlock? tbMenuVersion;
    private long _lastMenuClosedTicks;
    private bool _menuWasOpened;
    private long _lastMenuDismissedByOutsideClickTicks;
    private bool _isOpenFromTrayActive;
    internal System.Windows.Controls.Button? btnMin;
    internal System.Windows.Controls.Button? btnCls;
    internal System.Windows.Controls.ComboBox cbDevices = null!;
    private List<AudioDevice>? _pendingDeviceList;
    private EventHandler? _dropDownOpenHandler;
    internal Border borderHotkey = null!;
    internal TextBlock tbHotkey = null!;
    internal TextBlock? tbHotkeySubtitle;
    internal TextBlock? tbHeroHotkeyPrefix;
    internal TextBlock? tbHeroHotkey;
    internal TextBlock? tbHeroHotkeySuffix;
    internal System.Windows.Controls.Button? btnHotkeyMode;
    internal System.Windows.Controls.Button btnRecordHotkey = null!;
    internal System.Windows.Controls.CheckBox cbEnableOsd = null!;
    internal Grid? panelOsdDuration;
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
    internal Border? rootBorder;
    internal ScaleTransform? rootScaleTransform;
    internal TranslateTransform? rootTranslateTransform;
    internal System.Windows.Shapes.Rectangle? themeTransitionOverlay;
    internal RotateTransform? themeIconRot;
    internal StackPanel? panelHeroModeChips;
    internal Border? chipMuteLock;
    internal Border? chipSyncAllMics;
    internal System.Windows.Controls.Button? btnOsdPosCenter;
    internal System.Windows.Controls.Button? btnOsdPosBottom;
    internal System.Windows.Controls.Button? btnOsdPosTop;
    internal TranslateTransform? warningTranslate;
    internal Border? borderModalOverlay;
    internal Border? borderModalCard;
    internal ScaleTransform? modalCardScale;
    internal TextBlock? tbModalTitle;
    internal TextBlock? tbModalMessage;
    internal System.Windows.Controls.Button? btnModalCancel;
    internal System.Windows.Controls.Button? btnModalConfirm;

    private bool? _lastDisplayedMuteState;
    private bool _soundVolumeChangedDuringDrag;
    private bool _isPointerInteractingWithVolumeSlider;
    private int _warningSequenceId;
    private int _modalSequenceId;
    private Action? _pendingModalConfirmAction;

    private System.Drawing.Icon? _icoSmall;
    private System.Drawing.Icon? _icoBig;
    private IntPtr _hIconSmall = IntPtr.Zero;
    private IntPtr _hIconBig = IntPtr.Zero;
    private bool _isMinimizing;
    private bool _isRestoring;

    private static readonly Geometry IconCheck = Freeze(Geometry.Parse("M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z"));
    private static readonly Geometry IconWarning = Freeze(Geometry.Parse("M1 21h22L12 2 1 21zm12-3h-2v-2h2v2zm0-4h-2v-4h2v4z"));
    private static readonly Geometry MoonIconGeometry = Freeze(Geometry.Parse("M21,12.79A9,9 0 1 1 11.21,3 7,7 0 0 0 21,12.79z"));
    private static readonly Geometry SunIconGeometry = Freeze(Geometry.Parse("M12,7 A5,5 0 1 0 12,17 A5,5 0 1 0 12,7 Z M11,1 H13 V4 H11 Z M11,20 H13 V23 H11 Z M1,11 H4 V13 H1 Z M20,11 H23 V13 H20 Z M4.22,3.51 L5.64,4.93 L4.22,6.34 L2.81,4.93 Z M18.36,17.66 L19.78,19.07 L18.36,20.49 L16.95,19.07 Z M4.22,20.49 L5.64,19.07 L4.22,17.66 L2.81,19.07 Z M18.36,6.34 L19.78,4.93 L18.36,3.51 L16.95,4.93 Z"));

    private const int WM_SETTINGCHANGE = 0x001A;
    private const int WM_DISPLAYCHANGE = 0x007E;
    private const int WM_DPICHANGED = 0x02E0;
    private const int WM_EXITSIZEMOVE = 0x0232;
    private const int WM_DEVICECHANGE = 0x0219;
    private const int WM_SYSCOMMAND = 0x0112;
    private const int SC_MINIMIZE = 0xF020;
    private const int SC_RESTORE = 0xF120;
    private const int WM_NCLBUTTONDOWN = 0x00A1;
    private const int HTCAPTION = 0x0002;
    private const int WM_SETICON = 0x0080;
    private const int WM_GETICON = 0x007F;
    private const int ICON_SMALL = 0;
    private const int ICON_BIG = 1;
    private const int ICON_SMALL2 = 2;
    private const int GCLP_HICON = -14;
    private const int GCLP_HICONSM = -34;

    [DllImport("user32.dll", EntryPoint = "DestroyIcon", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("user32.dll", EntryPoint = "SetClassLongPtr", CharSet = CharSet.Auto)]
    private static extern IntPtr SetClassLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetClassLong", CharSet = CharSet.Auto)]
    private static extern int SetClassLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    private static IntPtr SetClassLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        if (IntPtr.Size == 8)
            return SetClassLongPtr64(hWnd, nIndex, dwNewLong);
        else
            return new IntPtr(SetClassLong32(hWnd, nIndex, dwNewLong.ToInt32()));
    }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int RegisterWindowMessage(string lpString);

    public static readonly int WM_SHOWME = RegisterWindowMessage("MICMUTE_SHOW_WINDOW_MSG_7FA5D9E0");
    public static readonly int WM_TASKBARCREATED = RegisterWindowMessage("TaskbarCreated");

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

    private static T Freeze<T>(T freezable) where T : Freezable
    {
        if (freezable.CanFreeze)
        {
            freezable.Freeze();
        }
        return freezable;
    }

    public MainWindow(AudioController audioController)
    {
        InitializeComponent();
        this.StateChanged += MainWindow_StateChanged;
        this.Activated += (s, e) => { _hotkeyManager?.RefreshHook(); };
        this.PreviewMouseDown += MainWindow_PreviewMouseDown;
        this.PreviewKeyDown += MainWindow_ModalPreviewKeyDown;
        this.Deactivated += MainWindow_Deactivated;
        this.LocationChanged += MainWindow_LocationChanged;
        _preferredWidth = Width > 0 ? Width : 412.0;
        _deviceRefreshDebouncer = new DispatcherDebouncer(Dispatcher);
        _statusDebouncer = new DispatcherDebouncer(Dispatcher);
        _volumeAudioDebouncer = new DispatcherDebouncer(Dispatcher);
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
            TextOptions.SetTextFormattingMode(this, TextOptions.GetTextFormattingMode(root));
            TextOptions.SetTextRenderingMode(this, TextOptions.GetTextRenderingMode(root));
            TextOptions.SetTextHintingMode(this, TextOptions.GetTextHintingMode(root));
            RenderOptions.SetClearTypeHint(this, RenderOptions.GetClearTypeHint(root));
            RenderOptions.SetEdgeMode(this, RenderOptions.GetEdgeMode(root));
            RenderOptions.SetBitmapScalingMode(this, RenderOptions.GetBitmapScalingMode(root));

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
            btnAppMenu = root.FindName("btnAppMenu") as System.Windows.Controls.Button;
            appMenuChevronRot = root.FindName("appMenuChevronRot") as RotateTransform;
            popupAppMenu = root.FindName("popupAppMenu") as System.Windows.Controls.Primitives.Popup;
            borderAppMenu = root.FindName("borderAppMenu") as Border;
            appMenuScale = root.FindName("appMenuScale") as ScaleTransform;
            appMenuTranslate = root.FindName("appMenuTranslate") as TranslateTransform;
            btnMenuStartupState = root.FindName("btnMenuStartupState") as System.Windows.Controls.Button;
            tbMenuStartupState = root.FindName("tbMenuStartupState") as TextBlock;
            borderMenuStartupBadge = root.FindName("borderMenuStartupBadge") as Border;
            tbMenuStartupBadge = root.FindName("tbMenuStartupBadge") as TextBlock;
            btnMenuSyncAllMics = root.FindName("btnMenuSyncAllMics") as System.Windows.Controls.Button;
            tbMenuSyncAllMics = root.FindName("tbMenuSyncAllMics") as TextBlock;
            borderMenuSyncBadge = root.FindName("borderMenuSyncBadge") as Border;
            tbMenuSyncBadge = root.FindName("tbMenuSyncBadge") as TextBlock;
            btnMenuLockMute = root.FindName("btnMenuLockMute") as System.Windows.Controls.Button;
            tbMenuLockMute = root.FindName("tbMenuLockMute") as TextBlock;
            borderMenuLockBadge = root.FindName("borderMenuLockBadge") as Border;
            tbMenuLockBadge = root.FindName("tbMenuLockBadge") as TextBlock;
            btnMenuCheckUpdates = root.FindName("btnMenuCheckUpdates") as System.Windows.Controls.Button;
            btnMenuAbout = root.FindName("btnMenuAbout") as System.Windows.Controls.Button;
            tbMenuVersion = root.FindName("tbMenuVersion") as TextBlock;

            if (popupAppMenu != null)
            {
                popupAppMenu.Resources = this.Resources;
                popupAppMenu.Opened += (s, e) =>
                {
                    _menuWasOpened = true;
                    AnimateAppMenuOpenState(isOpen: true);
                };
                popupAppMenu.Closed += (s, e) =>
                {
                    if (_menuWasOpened)
                    {
                        _menuWasOpened = false;
                        _lastMenuClosedTicks = Environment.TickCount64;
                    }
                    AnimateAppMenuOpenState(isOpen: false);
                };
            }

            if (btnAppMenu != null)
            {
                btnAppMenu.Click += BtnAppMenu_Click;
                btnAppMenu.PreviewMouseLeftButtonDown += (s, e) => { if (!this.IsActive) this.Activate(); };
            }

            if (btnMenuStartupState != null)
            {
                btnMenuStartupState.Click += BtnMenuStartupState_Click;
            }

            if (btnMenuSyncAllMics != null)
            {
                btnMenuSyncAllMics.Click += BtnMenuSyncAllMics_Click;
            }

            if (btnMenuLockMute != null)
            {
                btnMenuLockMute.Click += BtnMenuLockMute_Click;
            }

            if (btnMenuCheckUpdates != null)
            {
                btnMenuCheckUpdates.Click += BtnMenuCheckUpdates_Click;
            }

            if (btnMenuAbout != null)
            {
                btnMenuAbout.Click += BtnMenuAbout_Click;
            }

            if (tbMenuVersion != null)
            {
                var ver = typeof(MainWindow).Assembly.GetName().Version;
                tbMenuVersion.Text = ver != null ? $"Version {ver.Major}.{ver.Minor}.{ver.Build}" : "Version 1.0.0";
            }

            cbDevices = (System.Windows.Controls.ComboBox)root.FindName("cbDevices");
            borderHotkey = (Border)root.FindName("borderHotkey");
            tbHotkey = (TextBlock)root.FindName("tbHotkey");
            tbHotkeySubtitle = root.FindName("tbHotkeySubtitle") as TextBlock;
            tbHeroHotkeyPrefix = root.FindName("tbHeroHotkeyPrefix") as TextBlock;
            tbHeroHotkey = root.FindName("tbHeroHotkey") as TextBlock;
            tbHeroHotkeySuffix = root.FindName("tbHeroHotkeySuffix") as TextBlock;
            btnHotkeyMode = root.FindName("btnHotkeyMode") as System.Windows.Controls.Button;
            btnRecordHotkey = (System.Windows.Controls.Button)root.FindName("btnRecordHotkey");
            cbEnableOsd = (System.Windows.Controls.CheckBox)root.FindName("cbEnableOsd");
            panelOsdDuration = root.FindName("panelOsdDuration") as Grid;
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
            rootBorder = root.FindName("rootBorder") as Border;
            rootScaleTransform = root.FindName("rootScaleTransform") as ScaleTransform;
            rootTranslateTransform = root.FindName("rootTranslateTransform") as TranslateTransform;
            themeTransitionOverlay = root.FindName("themeTransitionOverlay") as System.Windows.Shapes.Rectangle;
            themeIconRot = root.FindName("themeIconRot") as RotateTransform;
            panelHeroModeChips = root.FindName("panelHeroModeChips") as StackPanel;
            chipMuteLock = root.FindName("chipMuteLock") as Border;
            chipSyncAllMics = root.FindName("chipSyncAllMics") as Border;
            btnOsdPosCenter = root.FindName("btnOsdPosCenter") as System.Windows.Controls.Button;
            btnOsdPosBottom = root.FindName("btnOsdPosBottom") as System.Windows.Controls.Button;
            btnOsdPosTop = root.FindName("btnOsdPosTop") as System.Windows.Controls.Button;
            if (btnOsdPosCenter != null) btnOsdPosCenter.Click += BtnOsdPosCenter_Click;
            if (btnOsdPosBottom != null) btnOsdPosBottom.Click += BtnOsdPosBottom_Click;
            if (btnOsdPosTop != null) btnOsdPosTop.Click += BtnOsdPosTop_Click;
            warningTranslate = root.FindName("warningTranslate") as TranslateTransform;
            borderModalOverlay = root.FindName("borderModalOverlay") as Border;
            borderModalCard = root.FindName("borderModalCard") as Border;
            modalCardScale = root.FindName("modalCardScale") as ScaleTransform;
            tbModalTitle = root.FindName("tbModalTitle") as TextBlock;
            tbModalMessage = root.FindName("tbModalMessage") as TextBlock;
            btnModalCancel = root.FindName("btnModalCancel") as System.Windows.Controls.Button;
            btnModalConfirm = root.FindName("btnModalConfirm") as System.Windows.Controls.Button;
            if (btnModalCancel != null) btnModalCancel.Click += BtnModalCancel_Click;
            if (btnModalConfirm != null) btnModalConfirm.Click += BtnModalConfirm_Click;
            if (borderModalOverlay != null)
            {
                borderModalOverlay.MouseLeftButtonDown += (s, e) =>
                {
                    if (ReferenceEquals(e.OriginalSource, borderModalOverlay))
                    {
                        CloseConfirmationModal();
                        e.Handled = true;
                    }
                };
            }

            // Event hooks
            var titleBar = (Border)root.FindName("borderTitleBar");
            if (titleBar != null) titleBar.MouseLeftButtonDown += TitleBar_MouseLeftButtonDown;
            btnMin = (System.Windows.Controls.Button)root.FindName("btnMinimize");
            if (btnMin != null)
            {
                btnMin.Click += MinimizeButton_Click;
                btnMin.PreviewMouseLeftButtonDown += (s, e) => { if (!this.IsActive) this.Activate(); };
            }
            btnCls = (System.Windows.Controls.Button)root.FindName("btnClose");
            if (btnCls != null)
            {
                btnCls.Click += CloseButton_Click;
                btnCls.PreviewMouseLeftButtonDown += (s, e) => { if (!this.IsActive) this.Activate(); };
            }
            if (btnTitleTheme != null)
            {
                btnTitleTheme.Click += BtnTitleTheme_Click;
                btnTitleTheme.PreviewMouseLeftButtonDown += (s, e) => { if (!this.IsActive) this.Activate(); };
            }
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
                            var bestFrame = decoder.Frames.OrderByDescending(f => f.PixelWidth * f.PixelHeight).FirstOrDefault() ?? decoder.Frames[0];
                            imgIcon.Source = bestFrame;
                            this.Icon = bestFrame;
                        }
                    }
                }
                catch (Exception ex)
                {
                    DiagnosticLogger.LogError("Failed to load embedded window icon", ex);
                }
            }

            this.ShowInTaskbar = true;
            if (root.FontFamily != null)
            {
                this.FontFamily = root.FontFamily;
            }
            this.FontWeight = root.FontWeight;

            // Now safely detach content, attach to this Window, and close temporary XAML root
            var content = root.Content;
            root.Content = null;
            root.Resources = new ResourceDictionary();
            this.Content = content;
            try { root.Close(); } catch { }
        }

        if (btnStateToggle != null)
        {
            btnStateToggle.Click += BtnStateToggle_Click;
            btnStateToggle.PreviewMouseLeftButtonDown += (s, e) => { if (!this.IsActive) this.Activate(); };
        }
        if (cbDevices != null)
        {
            cbDevices.SelectionChanged += CbDevices_SelectionChanged;
            cbDevices.DropDownClosed += CbDevices_DropDownClosed;
            _dropDownOpenHandler = (s, e) =>
            {
                if (!cbDevices.IsDropDownOpen)
                {
                    CbDevices_DropDownClosed(s, e);
                }
            };
            System.ComponentModel.DependencyPropertyDescriptor.FromProperty(System.Windows.Controls.ComboBox.IsDropDownOpenProperty, typeof(System.Windows.Controls.ComboBox))
                ?.AddValueChanged(cbDevices, _dropDownOpenHandler);
        }
        if (btnHotkeyMode != null) btnHotkeyMode.Click += BtnHotkeyMode_Click;
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
                if (e.ClickCount >= 2)
                {
                    txtOsdDuration?.SelectAll();
                }
                else if (txtOsdDuration != null)
                {
                    txtOsdDuration.CaretIndex = txtOsdDuration.Text.Length;
                }
                e.Handled = true;
            };
        }
        if (txtOsdDuration != null)
        {
            txtOsdDuration.PreviewMouseLeftButtonDown += (s, e) =>
            {
                if (e.ClickCount >= 2)
                {
                    txtOsdDuration.SelectAll();
                    e.Handled = true;
                }
            };
            txtOsdDuration.GotFocus += (s, e) =>
            {
                _osdDurationBeforeEdit = SettingsManager.Load().OsdDuration;
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
        if (sliderSoundVolume != null)
        {
            sliderSoundVolume.ValueChanged += SliderSoundVolume_ValueChanged;
            sliderSoundVolume.AddHandler(UIElement.PreviewMouseLeftButtonDownEvent,
                new MouseButtonEventHandler((_, _) => _isPointerInteractingWithVolumeSlider = true),
                handledEventsToo: true);
            sliderSoundVolume.AddHandler(UIElement.PreviewMouseLeftButtonUpEvent,
                new MouseButtonEventHandler(SliderSoundVolume_PreviewMouseLeftButtonUp),
                handledEventsToo: true);
            sliderSoundVolume.LostMouseCapture += (_, _) =>
            {
                if (Mouse.LeftButton != MouseButtonState.Pressed)
                {
                    _isPointerInteractingWithVolumeSlider = false;
                    _soundVolumeChangedDuringDrag = false;
                }
            };
        }
        if (borderSoundVolumeKeycap != null)
        {
            borderSoundVolumeKeycap.MouseLeftButtonDown += (s, e) =>
            {
                txtSoundVolume?.Focus();
                if (e.ClickCount >= 2)
                {
                    txtSoundVolume?.SelectAll();
                }
                else if (txtSoundVolume != null)
                {
                    txtSoundVolume.CaretIndex = txtSoundVolume.Text.Length;
                }
                e.Handled = true;
            };
        }
        if (txtSoundVolume != null)
        {
            txtSoundVolume.PreviewMouseLeftButtonDown += (s, e) =>
            {
                if (e.ClickCount >= 2)
                {
                    txtSoundVolume.SelectAll();
                    e.Handled = true;
                }
            };
            txtSoundVolume.GotFocus += (s, e) =>
            {
                _soundVolumeBeforeEdit = SettingsManager.Load().SoundVolume;
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
        this.PreviewMouseLeftButtonDown += (s, e) =>
        {
            if (!this.IsActive)
            {
                this.Activate();
            }
        };
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
        xaml = XClassRegex.Replace(xaml, "");
        xaml = EventAttrRegex.Replace(xaml, "");
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
        IntPtr hwnd = windowInteropHelper.Handle;
        try
        {
            ChangeWindowMessageFilter((uint)WM_SHOWME, 1);
            ChangeWindowMessageFilterEx(hwnd, (uint)WM_SHOWME, 1, IntPtr.Zero);
            ChangeWindowMessageFilter((uint)WM_TASKBARCREATED, 1);
            ChangeWindowMessageFilterEx(hwnd, (uint)WM_TASKBARCREATED, 1, IntPtr.Zero);
        }
        catch (Exception ex)
        {
            DiagnosticLogger.LogError("ChangeWindowMessageFilter failed", ex);
        }

        try
        {
            using Stream? iconStream = typeof(MainWindow).Assembly.GetManifestResourceStream("MicMute.app.ico");
            if (iconStream != null)
            {
                using var ms = new MemoryStream();
                iconStream.CopyTo(ms);
                byte[] iconBytes = ms.ToArray();
                using (var ms1 = new MemoryStream(iconBytes))
                {
                    _icoSmall = new System.Drawing.Icon(ms1, 16, 16);
                    _hIconSmall = _icoSmall.Handle;
                }
                using (var ms2 = new MemoryStream(iconBytes))
                {
                    _icoBig = new System.Drawing.Icon(ms2, 32, 32);
                    _hIconBig = _icoBig.Handle;
                }
                if (_hIconSmall != IntPtr.Zero)
                {
                    SendMessage(hwnd, WM_SETICON, (IntPtr)ICON_SMALL, _hIconSmall);
                    try { SetClassLongPtr(hwnd, GCLP_HICONSM, _hIconSmall); } catch (Exception ex) { DiagnosticLogger.LogWarning("Failed to set small icon: " + ex.Message); }
                }
                if (_hIconBig != IntPtr.Zero)
                {
                    SendMessage(hwnd, WM_SETICON, (IntPtr)ICON_BIG, _hIconBig);
                    try { SetClassLongPtr(hwnd, GCLP_HICON, _hIconBig); } catch (Exception ex) { DiagnosticLogger.LogWarning("Failed to set big icon: " + ex.Message); }
                }
            }
        }
        catch (Exception ex) { DiagnosticLogger.LogError("Error setting window icons", ex); }

        HwndSource.FromHwnd(hwnd)?.AddHook(HwndMessageHook);
        _hotkeyManager = new HotkeyManager(hwnd);
        _hotkeyManager.HotkeyPressed += HotkeyManager_HotkeyPressed;
        _hotkeyManager.HotkeyReleased += HotkeyManager_HotkeyReleased;
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
        catch (Exception ex)
        {
            DiagnosticLogger.LogError("ClampWindowBoundsToWorkArea failed", ex);
        }
    }

    private IntPtr HwndMessageHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_GETICON)
        {
            int iconType = wParam.ToInt32();
            if (iconType == ICON_BIG && _hIconBig != IntPtr.Zero)
            {
                handled = true;
                return _hIconBig;
            }
            if ((iconType == ICON_SMALL || iconType == ICON_SMALL2) && _hIconSmall != IntPtr.Zero)
            {
                handled = true;
                return _hIconSmall;
            }
            if (_hIconBig != IntPtr.Zero)
            {
                handled = true;
                return _hIconBig;
            }
        }
        else if (msg == WM_DEVICECHANGE || msg == 0x0218)
        {
            if (msg == 0x0218)
            {
                _hotkeyManager?.RefreshHook();
            }
            TriggerDevicesChanged();
        }
        else if (msg == WM_SYSCOMMAND)
        {
            int command = wParam.ToInt32() & 0xFFF0;
            if (command == SC_MINIMIZE)
            {
                if (!_isMinimizing && WindowState != WindowState.Minimized)
                {
                    handled = true;
                    MinimizeWithAnimation();
                    return IntPtr.Zero;
                }
            }
            else if (command == SC_RESTORE)
            {
                if (!_isRestoring && WindowState == WindowState.Minimized)
                {
                    handled = true;
                    this.WindowState = WindowState.Normal;
                    PlayRestoreAnimation();
                    return IntPtr.Zero;
                }
            }
        }
        else if (msg == WM_SHOWME)
        {
            bool wasHiddenOrMin = !this.IsVisible || this.WindowState == WindowState.Minimized;
            if (wasHiddenOrMin && !_isRestoring && !_isOpenFromTrayActive)
            {
                PlayOpenFromTrayAnimation();
            }
            else
            {
                if (!this.IsVisible) this.Show();
                if (this.WindowState == WindowState.Minimized) this.WindowState = WindowState.Normal;
                CenterOnScreen();
                this.Activate();
                this.Focus();
                SetForegroundWindow(hwnd);
            }
            handled = true;
        }
        else if (msg == WM_TASKBARCREATED && WM_TASKBARCREATED != 0)
        {
            TaskbarCreated?.Invoke(this, EventArgs.Empty);
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
            UpdateOsdDurationUiState(appSettings.EnableOsd);
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
            UpdatePowerModeUi(appSettings);
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
        _audioController.SyncAllMicrophones = settings.SyncAllMicrophones;
        _audioController.LockMuteState = settings.LockMuteState;
        _audioController.SetTargetDevice(settings.SelectedDeviceId);
        StartupManager.SetStartup(settings.RunOnStartup, settings.RunAsAdmin);
        AdminManager.SetRunAsAdmin(settings.RunAsAdmin);
        _holdToggledActive = false;
        _hotkeyManager?.Unregister();
        if (!RegisterGlobalHotkey(settings.Hotkey, settings.HotkeyModifiers))
            throw new InvalidOperationException("The shortcut could not be configured. Choose a different key.");
    }

    private void UpdatePowerModeUi(AppSettings settings)
    {
        if (btnHotkeyMode != null)
        {
            btnHotkeyMode.Content = settings.HoldKeyMode ? "Hold" : "Toggle";
        }
        if (tbHeroHotkeyPrefix != null)
        {
            tbHeroHotkeyPrefix.Text = settings.HoldKeyMode ? "Hold " : "Press ";
        }
        if (tbHeroHotkeySuffix != null)
        {
            tbHeroHotkeySuffix.Text = " to toggle";
        }
        if (tbHotkeySubtitle != null)
        {
            tbHotkeySubtitle.Text = settings.HoldKeyMode ? "Hold key to toggle" : "Global toggle hotkey";
        }
        UpdateOsdPositionUi(settings.OsdPosition);
        if (tbMenuStartupState != null)
        {
            tbMenuStartupState.Text = settings.StartupMuteMode switch
            {
                1 => "Startup: Always Muted",
                2 => "Startup: Always Live",
                _ => "Startup: Keep Last"
            };
        }
        if (tbMenuStartupBadge != null)
        {
            tbMenuStartupBadge.Text = settings.StartupMuteMode switch
            {
                1 => "Muted",
                2 => "Live",
                _ => "Keep Last"
            };
        }
        StyleMenuBadge(borderMenuStartupBadge, tbMenuStartupBadge, settings.StartupMuteMode != 0);

        if (tbMenuSyncAllMics != null)
        {
            tbMenuSyncAllMics.Text = settings.SyncAllMicrophones ? "Sync All Mics: On" : "Sync All Mics: Off";
        }
        if (tbMenuSyncBadge != null)
        {
            tbMenuSyncBadge.Text = settings.SyncAllMicrophones ? "ON" : "OFF";
        }
        StyleMenuBadge(borderMenuSyncBadge, tbMenuSyncBadge, settings.SyncAllMicrophones);

        if (tbMenuLockMute != null)
        {
            tbMenuLockMute.Text = settings.LockMuteState ? "Mute Lock: On" : "Mute Lock: Off";
        }
        if (tbMenuLockBadge != null)
        {
            tbMenuLockBadge.Text = settings.LockMuteState ? "ON" : "OFF";
        }
        StyleMenuBadge(borderMenuLockBadge, tbMenuLockBadge, settings.LockMuteState);
        UpdateHeroModeChips(settings);
    }

    private void UpdateOsdPositionUi(string? osdPosition)
    {
        string normalized = SettingsCodec.NormalizeOsdPosition(osdPosition);
        if (btnOsdPosCenter != null) btnOsdPosCenter.Tag = normalized == "Center" ? "Selected" : null;
        if (btnOsdPosBottom != null) btnOsdPosBottom.Tag = normalized == "Bottom" ? "Selected" : null;
        if (btnOsdPosTop != null) btnOsdPosTop.Tag = normalized == "Top" ? "Selected" : null;
    }

    private void UpdateHeroModeChips(AppSettings settings)
    {
        bool showLock = settings.LockMuteState;
        bool showSync = settings.SyncAllMicrophones;
        if (chipMuteLock != null) chipMuteLock.Visibility = showLock ? Visibility.Visible : Visibility.Collapsed;
        if (chipSyncAllMics != null) chipSyncAllMics.Visibility = showSync ? Visibility.Visible : Visibility.Collapsed;
        if (panelHeroModeChips != null) panelHeroModeChips.Visibility = (showLock || showSync) ? Visibility.Visible : Visibility.Collapsed;
    }

    private static void StyleMenuBadge(Border? border, TextBlock? text, bool isActive)
    {
        if (border != null)
        {
            border.SetResourceReference(Border.BackgroundProperty, isActive ? "MenuBadgeActiveBgBrush" : "KeycapBgBrush");
            border.SetResourceReference(Border.BorderBrushProperty, isActive ? "MenuBadgeActiveBgBrush" : "KeycapBorderBrush");
        }
        if (text != null)
        {
            text.SetResourceReference(TextBlock.ForegroundProperty, isActive ? "MenuBadgeActiveTextBrush" : "TextGrayBrush");
        }
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

    private void CbDevices_DropDownClosed(object? sender, EventArgs e)
    {
        if (_pendingDeviceList != null)
        {
            var pending = _pendingDeviceList;
            _pendingDeviceList = null;
            ApplyDeviceList(pending);
        }
    }

    private void ApplyDeviceList(List<AudioDevice> captureDevices)
    {
        if (_isDisposed) return;

        if (cbDevices != null && cbDevices.IsDropDownOpen)
        {
            _pendingDeviceList = captureDevices;
            return;
        }

        _isUpdatingDeviceList = true;
        try
        {
            if (cbDevices != null)
            {
                var existing = cbDevices.ItemsSource as System.Collections.IEnumerable;
                if (!AreDeviceListsEquivalent(existing, captureDevices))
                {
                    cbDevices.ItemsSource = captureDevices;
                }

                string? currentTargetId = _audioController.CurrentDeviceId;
                if (string.IsNullOrEmpty(currentTargetId))
                {
                    if (cbDevices.SelectedItem != null)
                    {
                        cbDevices.SelectedItem = null;
                    }
                }
                else
                {
                    var selected = cbDevices.SelectedItem as AudioDevice;
                    if (selected?.Id != currentTargetId)
                    {
                        var newSelection = (cbDevices.ItemsSource as IEnumerable<AudioDevice>)?.FirstOrDefault(d => d.Id == currentTargetId);
                        if (cbDevices.SelectedItem != newSelection)
                        {
                            cbDevices.SelectedItem = newSelection;
                        }
                    }
                }
            }
        }
        finally { _isUpdatingDeviceList = false; }
    }

    private static bool AreDeviceListsEquivalent(System.Collections.IEnumerable? existing, List<AudioDevice>? incoming)
    {
        if (ReferenceEquals(existing, incoming)) return true;
        if (existing == null || incoming == null) return false;
        var existingList = (existing as IList<AudioDevice>) ?? (existing as IEnumerable<AudioDevice>)?.ToList();
        if (existingList == null) return false;
        if (existingList.Count != incoming.Count) return false;
        for (int i = 0; i < incoming.Count; i++)
        {
            if (existingList[i].Id != incoming[i].Id || existingList[i].Name != incoming[i].Name)
            {
                return false;
            }
        }
        return true;
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
        }
    }

    private void BtnStateToggle_Click(object sender, RoutedEventArgs e)
    {
        DiagnosticLogger.LogUi("Mute button clicked in UI");
        _holdToggledActive = false;
        if (string.IsNullOrEmpty(_audioController.CurrentDeviceId))
        {
            _audioController.ForceUpdateActiveDevice();
            RefreshDeviceList();
        }
        _audioController.ToggleMute();
    }

    private static readonly SolidColorBrush BrushNoMicCapsuleBg = Freeze(new SolidColorBrush(System.Windows.Media.Color.FromArgb(20, 100, 116, 139)));
    private static readonly SolidColorBrush BrushNoMicCapsuleBorderLight = Freeze(new SolidColorBrush(System.Windows.Media.Color.FromArgb(35, 100, 116, 139)));
    private static readonly SolidColorBrush BrushNoMicCapsuleBorderDark = Freeze(new SolidColorBrush(System.Windows.Media.Color.FromArgb(30, 255, 255, 255)));

    private static readonly System.Windows.Media.Color ColorMutedTextLight = System.Windows.Media.Color.FromRgb(220, 38, 38);
    private static readonly System.Windows.Media.Color ColorMutedTextDark = System.Windows.Media.Color.FromRgb(248, 113, 113);
    private static readonly System.Windows.Media.Color ColorMutedDotLight = System.Windows.Media.Color.FromRgb(220, 38, 38);
    private static readonly System.Windows.Media.Color ColorMutedDotDark = System.Windows.Media.Color.FromRgb(239, 68, 68);
    private static readonly SolidColorBrush BrushMutedTextLight = Freeze(new SolidColorBrush(ColorMutedTextLight));
    private static readonly SolidColorBrush BrushMutedTextDark = Freeze(new SolidColorBrush(ColorMutedTextDark));
    private static readonly SolidColorBrush BrushMutedDotLight = Freeze(new SolidColorBrush(ColorMutedDotLight));
    private static readonly SolidColorBrush BrushMutedDotDark = Freeze(new SolidColorBrush(ColorMutedDotDark));
    private static readonly SolidColorBrush BrushMutedCapsuleBgLight = Freeze(new SolidColorBrush(System.Windows.Media.Color.FromArgb(20, 220, 38, 38)));
    private static readonly SolidColorBrush BrushMutedCapsuleBgDark = Freeze(new SolidColorBrush(System.Windows.Media.Color.FromArgb(24, 239, 68, 68)));
    private static readonly SolidColorBrush BrushMutedCapsuleBorderLight = Freeze(new SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 220, 38, 38)));
    private static readonly SolidColorBrush BrushMutedCapsuleBorderDark = Freeze(new SolidColorBrush(System.Windows.Media.Color.FromArgb(45, 239, 68, 68)));

    private static readonly System.Windows.Media.Color ColorActiveLight = System.Windows.Media.Color.FromRgb(71, 85, 105);
    private static readonly System.Windows.Media.Color ColorActiveDark = System.Windows.Media.Color.FromRgb(244, 244, 245);
    private static readonly SolidColorBrush BrushActiveLight = Freeze(new SolidColorBrush(ColorActiveLight));
    private static readonly SolidColorBrush BrushActiveDark = Freeze(new SolidColorBrush(ColorActiveDark));
    private static readonly SolidColorBrush BrushActiveCapsuleBgLight = Freeze(new SolidColorBrush(System.Windows.Media.Color.FromArgb(24, 71, 85, 105)));
    private static readonly SolidColorBrush BrushActiveCapsuleBgDark = Freeze(new SolidColorBrush(System.Windows.Media.Color.FromArgb(28, 255, 255, 255)));
    private static readonly SolidColorBrush BrushActiveCapsuleBorderLight = Freeze(new SolidColorBrush(System.Windows.Media.Color.FromArgb(42, 71, 85, 105)));
    private static readonly SolidColorBrush BrushActiveCapsuleBorderDark = Freeze(new SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 255, 255, 255)));

    private void UpdateMuteStateUI(bool isMuted)
    {
        bool hasDevice = !string.IsNullOrEmpty(_audioController.CurrentDeviceId);
        btnStateToggle.IsEnabled = hasDevice;
        bool isLight = SettingsManager.Load().LightMode;

        if (!hasDevice)
        {
            _lastDisplayedMuteState = null;
            btnStateToggle.Tag = "Unavailable";
            tbStatusText.Text = "NO MICROPHONE";
            tbStatusText.SetResourceReference(TextBlock.ForegroundProperty, "TextDimBrush");
            if (dotStatus != null) dotStatus.Fill = (System.Windows.Media.Brush)FindResource("TextDimBrush");
            if (dotGlow != null) dotGlow.Opacity = 0;
            if (borderStatusCapsule != null)
            {
                borderStatusCapsule.Background = BrushNoMicCapsuleBg;
                borderStatusCapsule.BorderThickness = new Thickness(1);
                borderStatusCapsule.BorderBrush = isLight ? BrushNoMicCapsuleBorderLight : BrushNoMicCapsuleBorderDark;
            }
            ShowWarningMessage("No active audio capture devices found.", "");
            ShowWarningSurfaceAnimated();
            return;
        }

        bool stateFlipped = _isInitialized && _lastDisplayedMuteState.HasValue && _lastDisplayedMuteState.Value != isMuted;
        _lastDisplayedMuteState = isMuted;

        btnStateToggle.Tag = isMuted ? "Muted" : "Active";
        tbStatusText.Text = isMuted ? "MICROPHONE MUTED" : "MICROPHONE LIVE";

        if (isMuted)
        {
            tbStatusText.Foreground = isLight ? BrushMutedTextLight : BrushMutedTextDark;
            if (dotStatus != null) dotStatus.Fill = isLight ? BrushMutedDotLight : BrushMutedDotDark;
            if (dotGlow != null)
            {
                dotGlow.Color = isLight ? ColorMutedDotLight : ColorMutedDotDark;
                dotGlow.Opacity = isLight ? 0.35 : 0.55;
                dotGlow.BlurRadius = 6;
            }
            if (borderStatusCapsule != null)
            {
                borderStatusCapsule.Background = isLight ? BrushMutedCapsuleBgLight : BrushMutedCapsuleBgDark;
                borderStatusCapsule.BorderThickness = new Thickness(1);
                borderStatusCapsule.BorderBrush = isLight ? BrushMutedCapsuleBorderLight : BrushMutedCapsuleBorderDark;
            }
        }
        else
        {
            var activeBrush = isLight ? BrushActiveLight : BrushActiveDark;
            tbStatusText.Foreground = activeBrush;
            if (dotStatus != null) dotStatus.Fill = activeBrush;
            if (dotGlow != null)
            {
                dotGlow.Color = isLight ? ColorActiveLight : ColorActiveDark;
                dotGlow.Opacity = isLight ? 0.25 : 0.85;
                dotGlow.BlurRadius = 8;
            }
            if (borderStatusCapsule != null)
            {
                borderStatusCapsule.Background = isLight ? BrushActiveCapsuleBgLight : BrushActiveCapsuleBgDark;
                borderStatusCapsule.BorderThickness = new Thickness(1);
                borderStatusCapsule.BorderBrush = isLight ? BrushActiveCapsuleBorderLight : BrushActiveCapsuleBorderDark;
            }
        }

        if (stateFlipped && borderStatusCapsule != null && IsLoaded && IsVisible)
        {
            borderStatusCapsule.BeginAnimation(UIElement.OpacityProperty, null);
            DoubleAnimation pulse = new DoubleAnimation(0.72, 1.0, TimeSpan.FromMilliseconds(150))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            pulse.Completed += (_, _) =>
            {
                borderStatusCapsule.BeginAnimation(UIElement.OpacityProperty, null);
                borderStatusCapsule.Opacity = 1.0;
            };
            borderStatusCapsule.BeginAnimation(UIElement.OpacityProperty, pulse);
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
                ShowWarningSurfaceAnimated();
            }
            else
            {
                HideWarningSurfaceAnimated();
            }
            if (_isInitialized)
            {
                AppSettings appSettings = SettingsManager.Load();
                if (appSettings.EnableOsd && e.ShowOsd)
                {
                    OsdWindow.ShowOsd(e.IsMuted, appSettings.OsdDuration);
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
            ShowWarningSurfaceAnimated();
        });
    }

    internal void ClearHoldToggleState() => _holdToggledActive = false;

    private void HotkeyManager_HotkeyPressed()
    {
        bool holdMode = SettingsManager.Load().HoldKeyMode;
        _holdToggledActive = holdMode;
        DiagnosticLogger.LogHotkey(holdMode ? "Hold shortcut pressed -> Toggling mute" : "Global shortcut key triggered -> Toggling mute");
        _audioController.ToggleMute();
    }

    private void HotkeyManager_HotkeyReleased()
    {
        if (!_holdToggledActive) return;
        _holdToggledActive = false;
        if (SettingsManager.Load().HoldKeyMode)
        {
            DiagnosticLogger.LogHotkey("Hold shortcut released -> Reverting mute");
            _audioController.ToggleMute();
        }
    }

    private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
    {
        while (child != null)
        {
            if (child is T parent) return parent;
            if (child is Visual || child is System.Windows.Media.Media3D.Visual3D)
                child = VisualTreeHelper.GetParent(child);
            else
                child = LogicalTreeHelper.GetParent(child);
        }
        return null;
    }

    private void MainWindow_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_isRecordingHotkey)
        {
            Key? mouseKey = e.ChangedButton switch
            {
                MouseButton.Middle => HotkeyManager.MouseMiddleKey,
                MouseButton.XButton1 => HotkeyManager.MouseXButton1Key,
                MouseButton.XButton2 => HotkeyManager.MouseXButton2Key,
                _ => null
            };
            if (mouseKey.HasValue)
            {
                e.Handled = true;
                StopRecordingHotkey(success: true, mouseKey.Value, System.Windows.Input.Keyboard.Modifiers);
                return;
            }
            DependencyObject? dep = e.OriginalSource as DependencyObject;
            if (dep == null || FindVisualParent<System.Windows.Controls.Button>(dep) != btnRecordHotkey)
            {
                StopRecordingHotkey(success: false);
            }
        }

        if (popupAppMenu != null && popupAppMenu.IsOpen)
        {
            if (btnAppMenu != null)
            {
                Point p = e.GetPosition(btnAppMenu);
                if (p.X >= 0 && p.X <= btnAppMenu.ActualWidth && p.Y >= 0 && p.Y <= btnAppMenu.ActualHeight)
                {
                    // Click was directly on the app menu toggle button itself; let BtnAppMenu_Click handle it
                    return;
                }
            }

            popupAppMenu.IsOpen = false;
            _lastMenuDismissedByOutsideClickTicks = Environment.TickCount64;
        }
    }

    private void MainWindow_Deactivated(object? sender, EventArgs e)
    {
        if (popupAppMenu != null && popupAppMenu.IsOpen)
        {
            popupAppMenu.IsOpen = false;
        }
    }

    private void MainWindow_LocationChanged(object? sender, EventArgs e)
    {
        if (popupAppMenu != null && popupAppMenu.IsOpen)
        {
            popupAppMenu.IsOpen = false;
        }
    }

    public void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject dep && FindVisualParent<System.Windows.Controls.Primitives.ButtonBase>(dep) != null)
        {
            return;
        }

        if (popupAppMenu != null && popupAppMenu.IsOpen)
        {
            popupAppMenu.IsOpen = false;
            e.Handled = true;
            return;
        }

        if (_lastMenuDismissedByOutsideClickTicks > 0 && Environment.TickCount64 - _lastMenuDismissedByOutsideClickTicks < 150)
        {
            e.Handled = true;
            return;
        }

        if (e.LeftButton == MouseButtonState.Pressed)
        {
            ReleaseCapture();
            SendMessage(new WindowInteropHelper(this).Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
        }
    }

    // =========================================================================
    // DaVinci Resolve-style Top-Left App Menu Handlers (Placeholders for Future Features)
    // =========================================================================

    public void BtnAppMenu_Click(object sender, RoutedEventArgs e)
    {
        if (popupAppMenu == null) return;

        if (popupAppMenu.IsOpen)
        {
            popupAppMenu.IsOpen = false;
            return;
        }

        // If the popup was just closed by clicking the menu button outside the popup,
        // prevent immediately reopening it.
        if (_lastMenuClosedTicks > 0 && Environment.TickCount64 - _lastMenuClosedTicks < 250)
        {
            return;
        }

        popupAppMenu.IsOpen = true;
    }

    public void BtnHotkeyMode_Click(object sender, RoutedEventArgs e)
    {
        if (_isReloadingSettings) return;
        _holdToggledActive = false;
        AppSettings current = SettingsManager.Load();
        AppSettings updated = current with { HoldKeyMode = !current.HoldKeyMode };
        SettingsManager.Save(updated);
        UpdatePowerModeUi(updated);
        ShowTemporaryStatus(updated.HoldKeyMode
            ? "Shortcut mode: Hold (Push-to-Talk / Push-to-Mute)."
            : "Shortcut mode: Toggle.");
    }

    private void AnimateAppMenuOpenState(bool isOpen)
    {
        if (appMenuChevronRot != null)
        {
            DoubleAnimation chevAnim = new DoubleAnimation(isOpen ? 180.0 : 0.0, TimeSpan.FromMilliseconds(160))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            appMenuChevronRot.BeginAnimation(RotateTransform.AngleProperty, chevAnim);
        }

        if (isOpen && borderAppMenu != null)
        {
            CubicEase ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            Duration dur = TimeSpan.FromMilliseconds(160);

            borderAppMenu.BeginAnimation(UIElement.OpacityProperty, null);
            borderAppMenu.Opacity = 0.0;
            borderAppMenu.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.0, 1.0, dur) { EasingFunction = ease });

            if (appMenuTranslate != null)
            {
                appMenuTranslate.BeginAnimation(TranslateTransform.YProperty, null);
                appMenuTranslate.Y = -5.0;
                appMenuTranslate.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(-5.0, 0.0, dur) { EasingFunction = ease });
            }
            if (appMenuScale != null)
            {
                appMenuScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                appMenuScale.ScaleY = 0.97;
                appMenuScale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.97, 1.0, dur) { EasingFunction = ease });
            }
        }
    }

    public void BtnMenuStartupState_Click(object sender, RoutedEventArgs e)
    {
        AppSettings current = SettingsManager.Load();
        int nextMode = (current.StartupMuteMode + 1) % 3;
        AppSettings updated = current with { StartupMuteMode = nextMode };
        SettingsManager.Save(updated);
        UpdatePowerModeUi(updated);
        ShowTemporaryStatus(nextMode switch
        {
            1 => "Startup mic state: Always Start Muted.",
            2 => "Startup mic state: Always Start Live.",
            _ => "Startup mic state: Keep Last State."
        });
    }

    public void BtnMenuSyncAllMics_Click(object sender, RoutedEventArgs e)
    {
        AppSettings current = SettingsManager.Load();
        bool nextSync = !current.SyncAllMicrophones;
        AppSettings updated = current with { SyncAllMicrophones = nextSync };
        SettingsManager.Save(updated);
        _audioController.SyncAllMicrophones = nextSync;
        if (nextSync && !string.IsNullOrEmpty(_audioController.CurrentDeviceId))
        {
            _audioController.SetMute(_audioController.IsMuted, showOsd: false);
        }
        UpdatePowerModeUi(updated);
        ShowTemporaryStatus(nextSync
            ? "Sync All Microphones enabled."
            : "Sync All Microphones disabled.");
    }

    public void BtnMenuLockMute_Click(object sender, RoutedEventArgs e)
    {
        AppSettings current = SettingsManager.Load();
        bool nextLock = !current.LockMuteState;
        AppSettings updated = current with { LockMuteState = nextLock };
        SettingsManager.Save(updated);
        _audioController.LockMuteState = nextLock;
        UpdatePowerModeUi(updated);
        ShowTemporaryStatus(nextLock
            ? "Mute Lock enabled (external changes blocked)."
            : "Mute Lock disabled.");
    }

    public void BtnMenuCheckUpdates_Click(object sender, RoutedEventArgs e)
    {
        if (popupAppMenu != null)
        {
            popupAppMenu.IsOpen = false;
        }

        ShowTemporaryStatus("Checking GitHub for updates...");
        if (System.Windows.Application.Current != null)
        {
            _ = CheckGitHubReleaseAsync();
        }
    }

    private async Task CheckGitHubReleaseAsync()
    {
        try
        {
            using HttpResponseMessage response = await UpdateHttpClient.GetAsync(
                "https://api.github.com/repos/youssefhazem3-dot/MicMute/releases/latest").ConfigureAwait(false);
            var localVersion = typeof(MainWindow).Assembly.GetName().Version ?? new Version(1, 0, 0);
            string localDisplay = $"{localVersion.Major}.{localVersion.Minor}.{localVersion.Build}";

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _ = Dispatcher.BeginInvoke(() => { if (!_isDisposed) ShowTemporaryStatus($"MicMute v{localDisplay} is up to date."); });
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                _ = Dispatcher.BeginInvoke(() => { if (!_isDisposed) ShowTemporaryStatus("Could not check updates (GitHub returned " + (int)response.StatusCode + ")."); });
                return;
            }

            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            using JsonDocument doc = JsonDocument.Parse(json);
            string tag = doc.RootElement.TryGetProperty("tag_name", out JsonElement tagEl) && tagEl.ValueKind == JsonValueKind.String
                ? (tagEl.GetString() ?? string.Empty).Trim().TrimStart('v', 'V')
                : string.Empty;

            if (Version.TryParse(tag, out Version? remoteVersion) && remoteVersion > new Version(localVersion.Major, localVersion.Minor, localVersion.Build))
            {
                _ = Dispatcher.BeginInvoke(() => { if (!_isDisposed) ShowTemporaryStatus($"Update available: v{tag} on GitHub Releases!"); });
            }
            else
            {
                _ = Dispatcher.BeginInvoke(() => { if (!_isDisposed) ShowTemporaryStatus($"MicMute v{localDisplay} is up to date."); });
            }
        }
        catch
        {
            _ = Dispatcher.BeginInvoke(() => { if (!_isDisposed) ShowTemporaryStatus("Could not check updates (offline or timed out)."); });
        }
    }

    public void BtnMenuAbout_Click(object sender, RoutedEventArgs e)
    {
        if (popupAppMenu != null)
        {
            popupAppMenu.IsOpen = false;
        }

        var ver = typeof(MainWindow).Assembly.GetName().Version;
        string versionText = ver != null ? $"{ver.Major}.{ver.Minor}.{ver.Build}" : "1.0.0";
        ShowTemporaryStatus($"MicMute v{versionText} — Ultra-light Windows 11 Mic Controller (github.com/youssefhazem3-dot/MicMute)");
    }

    public void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        MinimizeWithAnimation();
    }

    public void MinimizeWithAnimation()
    {
        if (popupAppMenu != null)
        {
            popupAppMenu.IsOpen = false;
        }

        if (!IsLoaded || !IsVisible || ActualWidth <= 0 || rootBorder == null || rootScaleTransform == null || rootTranslateTransform == null)
        {
            WindowState = WindowState.Minimized;
            return;
        }

        if (_isMinimizing) return;
        _isMinimizing = true;
        _isRestoring = false;

        // Cancel any pending animations
        rootBorder.BeginAnimation(UIElement.OpacityProperty, null);
        rootScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        rootScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        rootTranslateTransform.BeginAnimation(TranslateTransform.XProperty, null);
        rootTranslateTransform.BeginAnimation(TranslateTransform.YProperty, null);

        const int animMs = 240;
        var duration = new Duration(TimeSpan.FromMilliseconds(animMs));
        var ease = new CubicEase { EasingMode = EasingMode.EaseIn };

        var opacityAnim = new DoubleAnimation(rootBorder.Opacity, 0.0, duration) { EasingFunction = ease };
        var scaleXAnim = new DoubleAnimation(rootScaleTransform.ScaleX, 0.92, duration) { EasingFunction = ease };
        var scaleYAnim = new DoubleAnimation(rootScaleTransform.ScaleY, 0.92, duration) { EasingFunction = ease };
        var transXAnim = new DoubleAnimation(rootTranslateTransform.X, 0.0, duration) { EasingFunction = ease };
        var transYAnim = new DoubleAnimation(rootTranslateTransform.Y, 20.0, duration) { EasingFunction = ease };

        opacityAnim.Completed += (s, ev) =>
        {
            try
            {
                WindowState = WindowState.Minimized;
            }
            finally
            {
                _isMinimizing = false;

                rootBorder.BeginAnimation(UIElement.OpacityProperty, null);
                rootScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                rootScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                rootTranslateTransform.BeginAnimation(TranslateTransform.XProperty, null);
                rootTranslateTransform.BeginAnimation(TranslateTransform.YProperty, null);

                // Keep Opacity at 0.0 while minimized to eliminate the single-frame flash before Windows completes hiding the HWND
                rootBorder.Opacity = 0.0;
                rootScaleTransform.ScaleX = 0.92;
                rootScaleTransform.ScaleY = 0.92;
                rootTranslateTransform.X = 0.0;
                rootTranslateTransform.Y = 20.0;
            }
        };

        rootBorder.BeginAnimation(UIElement.OpacityProperty, opacityAnim);
        rootScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnim);
        rootScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnim);
        rootTranslateTransform.BeginAnimation(TranslateTransform.XProperty, transXAnim);
        rootTranslateTransform.BeginAnimation(TranslateTransform.YProperty, transYAnim);
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        if (_isOpenFromTrayActive) return;

        if (WindowState == WindowState.Normal)
        {
            _isMinimizing = false;
            PlayRestoreAnimation();
        }
        else if (WindowState == WindowState.Minimized)
        {
            _isRestoring = false;
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(App.TrimWorkingSet));
        }
    }

    public void CenterOnScreen()
    {
        try
        {
            ApplyAdaptiveScreenConstraints(isInitialPlacement: true);
        }
        catch (Exception ex)
        {
            DiagnosticLogger.LogError("CenterOnScreen failed", ex);
        }
    }

    private void ResetRootTransformsToIdentity()
    {
        if (rootBorder != null)
        {
            rootBorder.BeginAnimation(UIElement.OpacityProperty, null);
            rootBorder.Opacity = 1.0;
        }
        if (rootScaleTransform != null)
        {
            rootScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            rootScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            rootScaleTransform.ScaleX = 1.0;
            rootScaleTransform.ScaleY = 1.0;
        }
        if (rootTranslateTransform != null)
        {
            rootTranslateTransform.BeginAnimation(TranslateTransform.XProperty, null);
            rootTranslateTransform.BeginAnimation(TranslateTransform.YProperty, null);
            rootTranslateTransform.X = 0.0;
            rootTranslateTransform.Y = 0.0;
        }
    }

    public void PlayRestoreAnimation()
    {
        if (_isOpenFromTrayActive) return;

        if (!IsLoaded || !IsVisible || ActualWidth <= 0 || rootBorder == null || rootScaleTransform == null || rootTranslateTransform == null)
        {
            ResetRootTransformsToIdentity();
            return;
        }

        if (_isRestoring) return;
        _isRestoring = true;
        _isMinimizing = false;

        // Cancel any pending animations
        rootBorder.BeginAnimation(UIElement.OpacityProperty, null);
        rootScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        rootScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        rootTranslateTransform.BeginAnimation(TranslateTransform.XProperty, null);
        rootTranslateTransform.BeginAnimation(TranslateTransform.YProperty, null);

        rootBorder.Opacity = 0.0;
        rootScaleTransform.ScaleX = 0.92;
        rootScaleTransform.ScaleY = 0.92;
        rootTranslateTransform.X = 0.0;
        rootTranslateTransform.Y = 20.0;

        const int animMs = 280;
        var duration = new Duration(TimeSpan.FromMilliseconds(animMs));
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

        var opacityAnim = new DoubleAnimation(0.0, 1.0, duration) { EasingFunction = ease };
        var scaleXAnim = new DoubleAnimation(0.92, 1.0, duration) { EasingFunction = ease };
        var scaleYAnim = new DoubleAnimation(0.92, 1.0, duration) { EasingFunction = ease };
        var transXAnim = new DoubleAnimation(0.0, 0.0, duration) { EasingFunction = ease };
        var transYAnim = new DoubleAnimation(20.0, 0.0, duration) { EasingFunction = ease };

        scaleXAnim.Completed += (s, ev) =>
        {
            _isRestoring = false;
            ResetRootTransformsToIdentity();
        };

        rootBorder.BeginAnimation(UIElement.OpacityProperty, opacityAnim);
        rootScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnim);
        rootScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnim);
        rootTranslateTransform.BeginAnimation(TranslateTransform.XProperty, transXAnim);
        rootTranslateTransform.BeginAnimation(TranslateTransform.YProperty, transYAnim);
    }

    public void PlayOpenFromTrayAnimation(System.Drawing.Point? trayPixelPoint = null)
    {
        if (_isRestoring || _isOpenFromTrayActive) return;
        _isOpenFromTrayActive = true;
        _isRestoring = true;
        _isMinimizing = false;

        // Immediately cancel any pending animations and make rootBorder invisible to avoid any 1-frame flash
        if (rootBorder != null)
        {
            rootBorder.BeginAnimation(UIElement.OpacityProperty, null);
            rootBorder.Opacity = 0.0;
        }
        if (rootScaleTransform != null)
        {
            rootScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            rootScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        }
        if (rootTranslateTransform != null)
        {
            rootTranslateTransform.BeginAnimation(TranslateTransform.XProperty, null);
            rootTranslateTransform.BeginAnimation(TranslateTransform.YProperty, null);
        }

        this.WindowState = WindowState.Normal;
        CenterOnScreen();

        // Update layout pass while opacity is 0 so ActualWidth/ActualHeight are properly measured
        this.UpdateLayout();

        if (rootBorder == null || rootScaleTransform == null || rootTranslateTransform == null)
        {
            _isOpenFromTrayActive = false;
            _isRestoring = false;
            ResetRootTransformsToIdentity();
            if (!this.IsVisible) this.Show();
            this.Activate();
            return;
        }

        // Convert tray point from physical pixels to WPF DIPs
        DpiScale dpi = VisualTreeHelper.GetDpi(this);
        double dpiX = dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0;
        double dpiY = dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1.0;

        double curWidth = this.ActualWidth > 0 ? this.ActualWidth : _preferredWidth;
        double curHeight = this.ActualHeight > 0 ? this.ActualHeight : 440.0;
        double winCenterDipX = this.Left + curWidth / 2.0;
        double winCenterDipY = this.Top + curHeight / 2.0;

        double trayDipX;
        double trayDipY;

        if (trayPixelPoint.HasValue && (trayPixelPoint.Value.X != 0 || trayPixelPoint.Value.Y != 0))
        {
            trayDipX = trayPixelPoint.Value.X / dpiX;
            trayDipY = trayPixelPoint.Value.Y / dpiY;
        }
        else
        {
            var screen = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point((int)(winCenterDipX * dpiX), (int)(winCenterDipY * dpiY))) ?? System.Windows.Forms.Screen.PrimaryScreen;
            var wa = screen?.WorkingArea ?? new System.Drawing.Rectangle(0, 0, (int)(1920 * dpiX), (int)(1080 * dpiY));
            trayDipX = (wa.Right - 40) / dpiX;
            trayDipY = (wa.Bottom - 20) / dpiY;
        }

        double startDeltaX = trayDipX - winCenterDipX;
        double startDeltaY = trayDipY - winCenterDipY;

        const double initialScale = 0.70;
        rootScaleTransform.ScaleX = initialScale;
        rootScaleTransform.ScaleY = initialScale;
        rootTranslateTransform.X = startDeltaX;
        rootTranslateTransform.Y = startDeltaY;
        rootBorder.Opacity = 0.0;

        if (!this.IsVisible)
        {
            this.Show();
        }
        this.Activate();
        this.Focus();

        IntPtr hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd != IntPtr.Zero)
        {
            SetForegroundWindow(hwnd);
        }

        const int animMs = 280;
        var duration = new Duration(TimeSpan.FromMilliseconds(animMs));
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

        var opacityAnim = new DoubleAnimation(0.0, 1.0, duration) { EasingFunction = ease };
        var scaleXAnim = new DoubleAnimation(initialScale, 1.0, duration) { EasingFunction = ease };
        var scaleYAnim = new DoubleAnimation(initialScale, 1.0, duration) { EasingFunction = ease };
        var transXAnim = new DoubleAnimation(startDeltaX, 0.0, duration) { EasingFunction = ease };
        var transYAnim = new DoubleAnimation(startDeltaY, 0.0, duration) { EasingFunction = ease };

        scaleXAnim.Completed += (s, ev) =>
        {
            _isOpenFromTrayActive = false;
            _isRestoring = false;

            rootBorder.BeginAnimation(UIElement.OpacityProperty, null);
            rootScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            rootScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            rootTranslateTransform.BeginAnimation(TranslateTransform.XProperty, null);
            rootTranslateTransform.BeginAnimation(TranslateTransform.YProperty, null);

            rootBorder.Opacity = 1.0;
            rootScaleTransform.ScaleX = 1.0;
            rootScaleTransform.ScaleY = 1.0;
            rootTranslateTransform.X = 0.0;
            rootTranslateTransform.Y = 0.0;
        };

        rootBorder.BeginAnimation(UIElement.OpacityProperty, opacityAnim);
        rootScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnim);
        rootScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnim);
        rootTranslateTransform.BeginAnimation(TranslateTransform.XProperty, transXAnim);
        rootTranslateTransform.BeginAnimation(TranslateTransform.YProperty, transYAnim);
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
        tbHotkey.SetResourceReference(TextBlock.ForegroundProperty, "AccentBrush");
        if (tbHeroHotkey != null) tbHeroHotkey.Text = "...";
        ApplyRecordingHotkeyBorderPulse();
        PreviewKeyDown += MainWindow_PreviewKeyDown;
    }

    private void ApplyRecordingHotkeyBorderPulse()
    {
        if (borderHotkey == null) return;
        System.Windows.Media.Color accent = TryFindResource("AccentColor") is System.Windows.Media.Color themeAccent
            ? themeAccent
            : (SettingsManager.Load().LightMode
                ? System.Windows.Media.Color.FromRgb(71, 85, 105)
                : System.Windows.Media.Color.FromRgb(255, 255, 255));
        SolidColorBrush pulseBrush = new SolidColorBrush(accent);
        borderHotkey.BorderBrush = pulseBrush;
        if (IsLoaded && IsVisible)
        {
            ColorAnimation pulse = new ColorAnimation(
                accent,
                System.Windows.Media.Color.FromArgb(80, accent.R, accent.G, accent.B),
                TimeSpan.FromMilliseconds(600))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };
            pulseBrush.BeginAnimation(SolidColorBrush.ColorProperty, pulse);
        }
    }

    protected override void OnDeactivated(EventArgs e)
    {
        if (_isRecordingHotkey) StopRecordingHotkey(success: false);
        base.OnDeactivated(e);
    }

    private void StopRecordingHotkey(bool success, Key key = Key.None, ModifierKeys modifiers = ModifierKeys.None)
    {
        _isRecordingHotkey = false;
        _holdToggledActive = false;
        btnRecordHotkey.Content = "Record";
        if (borderHotkey != null)
        {
            borderHotkey.SetResourceReference(Border.BorderBrushProperty, "KeycapBorderBrush");
        }
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
        => UiBehavior.FormatHotkeyText(key, modifiers);

    private void ShowWarningSurfaceAnimated()
    {
        if (borderWarning == null) return;
        int seq = ++_warningSequenceId;
        bool wasCollapsed = borderWarning.Visibility != Visibility.Visible;

        borderWarning.BeginAnimation(UIElement.OpacityProperty, null);
        borderWarning.Visibility = Visibility.Visible;

        if (IsLoaded && IsVisible)
        {
            CubicEase ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            Duration dur = TimeSpan.FromMilliseconds(160);
            borderWarning.Opacity = wasCollapsed ? 0.0 : borderWarning.Opacity;
            DoubleAnimation fadeIn = new DoubleAnimation(wasCollapsed ? 0.0 : borderWarning.Opacity, 1.0, dur) { EasingFunction = ease };
            fadeIn.Completed += (_, _) =>
            {
                if (seq == _warningSequenceId)
                {
                    borderWarning.BeginAnimation(UIElement.OpacityProperty, null);
                    borderWarning.Opacity = 1.0;
                }
            };
            borderWarning.BeginAnimation(UIElement.OpacityProperty, fadeIn);

            if (warningTranslate != null && wasCollapsed)
            {
                warningTranslate.BeginAnimation(TranslateTransform.YProperty, null);
                warningTranslate.Y = 6.0;
                DoubleAnimation slideIn = new DoubleAnimation(6.0, 0.0, dur) { EasingFunction = ease };
                slideIn.Completed += (_, _) =>
                {
                    if (seq == _warningSequenceId)
                    {
                        warningTranslate.BeginAnimation(TranslateTransform.YProperty, null);
                        warningTranslate.Y = 0.0;
                    }
                };
                warningTranslate.BeginAnimation(TranslateTransform.YProperty, slideIn);
            }
        }
        else
        {
            borderWarning.Opacity = 1.0;
            if (warningTranslate != null)
            {
                warningTranslate.BeginAnimation(TranslateTransform.YProperty, null);
                warningTranslate.Y = 0.0;
            }
        }
    }

    private void HideWarningSurfaceAnimated()
    {
        if (borderWarning == null || borderWarning.Visibility != Visibility.Visible) return;
        int seq = ++_warningSequenceId;

        if (!IsLoaded || !IsVisible)
        {
            borderWarning.BeginAnimation(UIElement.OpacityProperty, null);
            borderWarning.Opacity = 1.0;
            borderWarning.Visibility = Visibility.Collapsed;
            return;
        }

        DoubleAnimation fadeOut = new DoubleAnimation(borderWarning.Opacity, 0.0, TimeSpan.FromMilliseconds(140))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        fadeOut.Completed += (_, _) =>
        {
            if (seq == _warningSequenceId)
            {
                borderWarning.BeginAnimation(UIElement.OpacityProperty, null);
                borderWarning.Opacity = 1.0;
                borderWarning.Visibility = Visibility.Collapsed;
            }
        };
        borderWarning.BeginAnimation(UIElement.OpacityProperty, fadeOut);
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
        ShowWarningSurfaceAnimated();
        _statusDebouncer.Schedule(TimeSpan.FromSeconds(3), () =>
        {
            if (string.IsNullOrEmpty(_audioController.CurrentDeviceId))
            {
                ShowWarningMessage("No active audio capture devices found.", "");
                ShowWarningSurfaceAnimated();
            }
            else
            {
                HideWarningSurfaceAnimated();
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
        bool success = AdminManager.SetRunAsAdmin(true);
        if (!success && !AdminManager.IsRunAsAdminConfigured())
        {
            _isReloadingSettings = true;
            cbRunAsAdmin.IsChecked = false;
            _isReloadingSettings = false;
            ShowTemporaryStatus("Could not enable Administrator mode: registry access denied.");
            return;
        }
        var currentSettings = SettingsManager.Load();
        SettingsManager.Save(currentSettings with { RunAsAdmin = true });
        StartupManager.SetStartup(currentSettings.RunOnStartup, runAsAdmin: true);

        if (!AdminManager.IsRunningAsAdmin())
        {
            ShowConfirmationModal(
                "Restart as Administrator",
                "Run as Administrator has been enabled for elevated windows and game hotkey support.\n\nWould you like to restart MicMute as Administrator now to apply it immediately?",
                "Restart Now",
                () =>
                {
                    if (!AdminManager.RestartAsAdmin())
                    {
                        ShowTemporaryStatus("Restart was cancelled or could not be completed.");
                    }
                });
        }
    }

    private void CbRunAsAdmin_Unchecked(object sender, RoutedEventArgs e)
    {
        if (!_isInitialized || _isReloadingSettings) return;
        bool success = AdminManager.SetRunAsAdmin(false);
        if (!success && AdminManager.IsRunAsAdminConfigured())
        {
            _isReloadingSettings = true;
            cbRunAsAdmin.IsChecked = true;
            _isReloadingSettings = false;
            ShowTemporaryStatus("Could not disable Administrator mode: registry access denied.");
            return;
        }
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
        => SetPanelDimmedState(panelSoundVolume, soundFeedbackEnabled);

    private void SliderSoundVolume_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isPointerInteractingWithVolumeSlider = false;
        if (!_soundVolumeChangedDuringDrag) return;
        _soundVolumeChangedDuringDrag = false;
        if (!_isInitialized || _isReloadingSettings || sliderSoundVolume == null) return;
        if (!SettingsManager.Load().PlaySoundFeedback) return;
        int volume = (int)Math.Round(sliderSoundVolume.Value);
        if (volume <= 0) return;
        _volumeAudioDebouncer.Cancel();
        AudioFeedback.SetVolume(volume);
        AudioFeedback.Play(isMuted: false);
    }

    public void BtnTitleTheme_Click(object sender, RoutedEventArgs e)
    {
        if (cbLightMode != null)
        {
            cbLightMode.IsChecked = !cbLightMode.IsChecked;
        }
    }

    private static Dictionary<string, object> GetOrBuildThemeResources(bool isLight)
    {
        ref Dictionary<string, object>? cache = ref isLight ? ref _cachedLightTheme : ref _cachedDarkTheme;
        if (cache != null) return cache;

        static SolidColorBrush Solid(byte r, byte g, byte b) => Freeze(new SolidColorBrush(System.Windows.Media.Color.FromRgb(r, g, b)));
        static SolidColorBrush Alpha(byte a, byte r, byte g, byte b) => Freeze(new SolidColorBrush(System.Windows.Media.Color.FromArgb(a, r, g, b)));

        System.Windows.Media.Color accentColor = isLight
            ? System.Windows.Media.Color.FromRgb(71, 85, 105)
            : System.Windows.Media.Color.FromRgb(255, 255, 255);
        SolidColorBrush themeGlyphBrush = isLight ? Solid(55, 65, 81) : Solid(255, 255, 255);

        var dict = new Dictionary<string, object>(36)
        {
            ["AccentColor"] = accentColor,
            ["AccentBrush"] = Freeze(new SolidColorBrush(accentColor)),
            ["AccentHoverBrush"] = isLight ? Alpha(18, 71, 85, 105) : Alpha(34, 255, 255, 255),
            ["TitleTextBrush"] = themeGlyphBrush,
            ["TextWhiteBrush"] = themeGlyphBrush,
            ["SquircleIconBrush"] = themeGlyphBrush,
            ["KeycapTextBrush"] = themeGlyphBrush,
            ["StatusCapsuleTextBrush"] = themeGlyphBrush,
            ["StatusDotBrush"] = themeGlyphBrush,
            ["WarningTextBrush"] = themeGlyphBrush,
        };

        if (isLight)
        {
            dict["WindowBgBrush"] = Freeze(new LinearGradientBrush(
                System.Windows.Media.Color.FromArgb(174, 248, 249, 250),
                System.Windows.Media.Color.FromArgb(174, 233, 236, 239),
                new Point(0.0, 0.0), new Point(1.0, 1.0)));
            dict["CaptionButtonHoverBrush"] = Alpha(18, 0, 0, 0);
            dict["CaptionButtonPressedBrush"] = Alpha(35, 0, 0, 0);
            dict["CardBgBrush"] = Alpha(180, 255, 255, 255);
            dict["InputBgBrush"] = Solid(244, 244, 245);
            dict["TitleBarBgBrush"] = Alpha(0, 255, 255, 255);
            dict["TextGrayBrush"] = Solid(107, 114, 128);
            dict["TextDimBrush"] = Solid(156, 163, 175);
            dict["BorderBrush"] = Alpha(28, 0, 0, 0);
            dict["DividerBrush"] = Alpha(18, 0, 0, 0);

            dict["ToggleOnBrush"] = Solid(71, 85, 105);
            dict["ToggleOnBorderBrush"] = Solid(71, 85, 105);
            dict["ToggleOffBrush"] = Solid(228, 228, 231);
            dict["ToggleOffBorderBrush"] = Solid(212, 212, 216);
            dict["ToggleKnobBrush"] = Solid(71, 85, 105);
            dict["ToggleKnobBorderBrush"] = Solid(212, 212, 216);
            dict["ToggleKnobActiveBrush"] = Solid(255, 255, 255);
            dict["ToggleKnobActiveBorderBrush"] = Solid(228, 228, 231);

            dict["SliderTrackBrush"] = Solid(71, 85, 105);
            dict["SliderTrackEmptyBrush"] = Solid(228, 228, 231);
            dict["SliderTrackBorderBrush"] = Solid(212, 212, 216);
            dict["SliderThumbBgBrush"] = Solid(255, 255, 255);
            dict["SliderThumbBorderBrush"] = Solid(71, 85, 105);

            dict["JewelRingBgBrush"] = Freeze(new LinearGradientBrush(
                System.Windows.Media.Color.FromArgb(180, 255, 255, 255),
                System.Windows.Media.Color.FromArgb(120, 244, 244, 245),
                new Point(0.0, 0.0), new Point(1.0, 1.0)));
            dict["JewelRingBorderBrush"] = Alpha(180, 255, 255, 255);

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
            dict["HeroLensActiveBrush"] = Freeze(heroLensLight);

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
            dict["HeroGlowActiveBrush"] = Freeze(heroGlowLight);
            dict["HeroIconActiveBrush"] = Solid(255, 255, 255);

            dict["SquircleBgBrush"] = Alpha(14, 0, 0, 0);
            dict["SquircleBorderBrush"] = Alpha(24, 0, 0, 0);

            dict["KeycapBgBrush"] = Solid(244, 244, 245);
            dict["KeycapBorderBrush"] = Solid(228, 228, 231);

            dict["StatusCapsuleBgBrush"] = Alpha(24, 71, 85, 105);
            dict["StatusCapsuleBorderBrush"] = Alpha(42, 71, 85, 105);

            dict["WarningBgBrush"] = Alpha(12, 0, 0, 0);
            dict["WarningBorderBrush"] = Alpha(22, 0, 0, 0);

            dict["ScrollBarThumbBrush"] = Alpha(40, 0, 0, 0);
            dict["ScrollBarThumbHoverBrush"] = Alpha(70, 0, 0, 0);
            dict["ScrollBarThumbDragBrush"] = Alpha(100, 0, 0, 0);

            dict["MenuBadgeActiveBgBrush"] = Solid(71, 85, 105);
            dict["MenuBadgeActiveTextBrush"] = Solid(255, 255, 255);

            dict["ModalScrimBrush"] = Alpha(120, 15, 23, 42);
            dict["ModalCardBgBrush"] = Solid(255, 255, 255);
            dict["ModalTopHighlightBrush"] = Alpha(204, 255, 255, 255);
            dict["DangerBtnBgBrush"] = Alpha(24, 220, 38, 38);
            dict["DangerBtnHoverBgBrush"] = Alpha(42, 220, 38, 38);
            dict["DangerBtnPressedBgBrush"] = Alpha(60, 220, 38, 38);
            dict["DangerBtnBorderBrush"] = Alpha(128, 220, 38, 38);
            dict["DangerBtnTextBrush"] = Solid(220, 38, 38);
        }
        else
        {
            dict["WindowBgBrush"] = Freeze(new LinearGradientBrush(
                System.Windows.Media.Color.FromArgb(242, 24, 24, 27),
                System.Windows.Media.Color.FromArgb(242, 9, 9, 11),
                new Point(0.0, 0.0), new Point(1.0, 1.0)));
            dict["CaptionButtonHoverBrush"] = Alpha(24, 255, 255, 255);
            dict["CaptionButtonPressedBrush"] = Alpha(48, 255, 255, 255);
            dict["CardBgBrush"] = Alpha(20, 255, 255, 255);
            dict["InputBgBrush"] = Solid(24, 24, 27);
            dict["TitleBarBgBrush"] = Alpha(0, 0, 0, 0);
            dict["TextGrayBrush"] = Solid(193, 193, 204);
            dict["TextDimBrush"] = Solid(136, 136, 146);
            dict["BorderBrush"] = Alpha(32, 255, 255, 255);
            dict["DividerBrush"] = Alpha(20, 255, 255, 255);

            dict["ToggleOnBrush"] = Solid(184, 192, 204);
            dict["ToggleOnBorderBrush"] = Solid(184, 192, 204);
            dict["ToggleOffBrush"] = Solid(28, 28, 31);
            dict["ToggleOffBorderBrush"] = Solid(63, 63, 70);
            dict["ToggleKnobBrush"] = Solid(228, 228, 231);
            dict["ToggleKnobBorderBrush"] = Solid(193, 193, 204);
            dict["ToggleKnobActiveBrush"] = Solid(24, 24, 27);
            dict["ToggleKnobActiveBorderBrush"] = Solid(39, 39, 42);

            dict["SliderTrackBrush"] = Solid(184, 192, 204);
            dict["SliderTrackEmptyBrush"] = Solid(39, 39, 42);
            dict["SliderTrackBorderBrush"] = Solid(63, 63, 70);
            dict["SliderThumbBgBrush"] = Solid(255, 255, 255);
            dict["SliderThumbBorderBrush"] = Solid(24, 24, 27);

            dict["JewelRingBgBrush"] = Freeze(new LinearGradientBrush(
                System.Windows.Media.Color.FromArgb(34, 255, 255, 255),
                System.Windows.Media.Color.FromArgb(5, 255, 255, 255),
                new Point(0.0, 0.0), new Point(1.0, 1.0)));
            dict["JewelRingBorderBrush"] = Alpha(60, 255, 255, 255);

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
            heroLensDark.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromRgb(193, 193, 204), 1.0));
            dict["HeroLensActiveBrush"] = Freeze(heroLensDark);

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
            dict["HeroGlowActiveBrush"] = Freeze(heroGlowDark);
            dict["HeroIconActiveBrush"] = Solid(24, 24, 27);

            dict["SquircleBgBrush"] = Alpha(20, 255, 255, 255);
            dict["SquircleBorderBrush"] = Alpha(32, 255, 255, 255);

            dict["KeycapBgBrush"] = Solid(39, 39, 42);
            dict["KeycapBorderBrush"] = Solid(63, 63, 70);

            dict["StatusCapsuleBgBrush"] = Alpha(28, 255, 255, 255);
            dict["StatusCapsuleBorderBrush"] = Alpha(40, 255, 255, 255);

            dict["WarningBgBrush"] = Alpha(20, 255, 255, 255);
            dict["WarningBorderBrush"] = Alpha(32, 255, 255, 255);

            dict["ScrollBarThumbBrush"] = Alpha(36, 255, 255, 255);
            dict["ScrollBarThumbHoverBrush"] = Alpha(65, 255, 255, 255);
            dict["ScrollBarThumbDragBrush"] = Alpha(100, 255, 255, 255);

            dict["MenuBadgeActiveBgBrush"] = Solid(228, 228, 231);
            dict["MenuBadgeActiveTextBrush"] = Solid(24, 24, 27);

            dict["ModalScrimBrush"] = Alpha(184, 6, 6, 9);
            dict["ModalCardBgBrush"] = Solid(28, 28, 32);
            dict["ModalTopHighlightBrush"] = Alpha(38, 255, 255, 255);
            dict["DangerBtnBgBrush"] = Alpha(34, 239, 68, 68);
            dict["DangerBtnHoverBgBrush"] = Alpha(56, 239, 68, 68);
            dict["DangerBtnPressedBgBrush"] = Alpha(76, 239, 68, 68);
            dict["DangerBtnBorderBrush"] = Alpha(120, 248, 113, 113);
            dict["DangerBtnTextBrush"] = Solid(248, 113, 113);
        }

        cache = dict;
        return dict;
    }

    internal void SetLightMode(bool isLight)
    {
        if (popupAppMenu != null && popupAppMenu.IsOpen)
        {
            popupAppMenu.IsOpen = false;
        }

        int transitionVersion = ++_themeTransitionVersion;
        bool animateTransition = IsLoaded && IsVisible && ActualWidth > 0 && rootBorder != null && themeTransitionOverlay != null;
        if (animateTransition)
        {
            try
            {
                var dpi = VisualTreeHelper.GetDpi(this);
                int pixelW = Math.Max(1, (int)Math.Ceiling(rootBorder!.ActualWidth * dpi.DpiScaleX));
                int pixelH = Math.Max(1, (int)Math.Ceiling(rootBorder!.ActualHeight * dpi.DpiScaleY));
                var rtb = new RenderTargetBitmap(pixelW, pixelH, dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
                rtb.Render(rootBorder);
                rtb.Freeze();

                var imgBrush = new ImageBrush(rtb);
                imgBrush.Freeze();
                themeTransitionOverlay!.Fill = imgBrush;
                themeTransitionOverlay.Opacity = 1.0;
                themeTransitionOverlay.Visibility = Visibility.Visible;
            }
            catch
            {
                // Edge cases / headless fallback
            }
        }

        var theme = GetOrBuildThemeResources(isLight);
        foreach (var kv in theme)
        {
            Resources[kv.Key] = kv.Value;
        }

        var themeGlyphBrush = (System.Windows.Media.Brush)theme["TitleTextBrush"];
        if (pathTitleTheme != null)
        {
            pathTitleTheme.Fill = themeGlyphBrush;
            pathTitleTheme.Data = isLight ? MoonIconGeometry : SunIconGeometry;
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
        if (txtOsdDuration != null)
        {
            txtOsdDuration.CaretBrush = themeGlyphBrush;
        }
        if (txtSoundVolume != null)
        {
            txtSoundVolume.CaretBrush = themeGlyphBrush;
        }

        if (popupAppMenu != null)
        {
            popupAppMenu.Resources = this.Resources;
        }

        if (_isRecordingHotkey)
        {
            ApplyRecordingHotkeyBorderPulse();
        }

        if (_audioController != null)
        {
            UpdateMuteStateUI(_audioController.IsMuted);
        }
        OsdWindow.UpdateVisibleTheme(isLight);

        if (animateTransition && themeTransitionOverlay != null)
        {
            // Flush layout and resource changes synchronously under the opaque snapshot overlay
            // so there is zero layout work or resource thrashing during the crossfade animation.
            UpdateLayout();

            // Slower by 20%: 260ms * 1.20 = 312ms
            const int animMs = 312;
            if (themeIconRot != null)
            {
                var rotAnim = new DoubleAnimation(0, 360, new Duration(TimeSpan.FromMilliseconds(animMs)))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                themeIconRot.BeginAnimation(RotateTransform.AngleProperty, rotAnim);
            }

            var fadeOut = new DoubleAnimation(1.0, 0.0, new Duration(TimeSpan.FromMilliseconds(animMs)))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            fadeOut.Completed += (s, ev) =>
            {
                if (transitionVersion != _themeTransitionVersion) return;
                themeTransitionOverlay.Opacity = 0.0;
                themeTransitionOverlay.Visibility = Visibility.Collapsed;
                themeTransitionOverlay.Fill = null;
            };
            themeTransitionOverlay.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }
        else if (themeIconRot != null)
        {
            themeIconRot.BeginAnimation(RotateTransform.AngleProperty, null);
            themeIconRot.Angle = 0;
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
        UpdateOsdDurationUiState(true);
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
        OsdWindow.HideOsd();
        UpdateOsdDurationUiState(false);
    }

    public void BtnOsdPosCenter_Click(object sender, RoutedEventArgs e) => SelectOsdPosition("Center");
    public void BtnOsdPosBottom_Click(object sender, RoutedEventArgs e) => SelectOsdPosition("Bottom");
    public void BtnOsdPosTop_Click(object sender, RoutedEventArgs e) => SelectOsdPosition("Top");

    internal void SelectOsdPosition(string position)
    {
        if (_isReloadingSettings) return;
        string normalized = SettingsCodec.NormalizeOsdPosition(position);
        AppSettings current = SettingsManager.Load();
        AppSettings updated = string.Equals(current.OsdPosition, normalized, StringComparison.Ordinal)
            ? current
            : current with { OsdPosition = normalized };
        if (!ReferenceEquals(updated, current))
        {
            SettingsManager.Save(updated);
            ShowTemporaryStatus($"OSD position: {normalized}.");
        }
        UpdatePowerModeUi(updated);
        if (IsLoaded && IsVisible && updated.EnableOsd)
        {
            OsdWindow.ShowOsd(_audioController.IsMuted, updated.OsdDuration);
        }
    }

    private void UpdateOsdDurationUiState(bool enableOsd)
        => SetPanelDimmedState(panelOsdDuration, enableOsd);

    private void SetPanelDimmedState(Grid? panel, bool enabled)
    {
        if (panel == null) return;
        panel.IsEnabled = enabled;
        double targetOpacity = enabled ? 1.0 : 0.45;
        panel.BeginAnimation(UIElement.OpacityProperty, null);
        panel.Opacity = targetOpacity;
        if (IsLoaded && IsVisible && !_isReloadingSettings)
        {
            double fromOpacity = enabled ? 0.45 : 1.0;
            DoubleAnimation anim = new DoubleAnimation(fromOpacity, targetOpacity, TimeSpan.FromMilliseconds(180))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            anim.Completed += (_, _) =>
            {
                panel.BeginAnimation(UIElement.OpacityProperty, null);
                panel.Opacity = targetOpacity;
            };
            panel.BeginAnimation(UIElement.OpacityProperty, anim);
        }
    }

    private void SliderOsdDuration_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitialized && !_isReloadingSettings && !_isUpdatingOsdTextFromSlider)
        {
            double rounded = Math.Round(e.NewValue, 1);
            if (txtOsdDuration != null && !txtOsdDuration.IsFocused)
            {
                _isUpdatingOsdTextFromSlider = true;
                txtOsdDuration.Text = UiBehavior.FormatOsdDuration(rounded, CultureInfo.CurrentCulture);
                _isUpdatingOsdTextFromSlider = false;
            }
            AppSettings current = SettingsManager.Load();
            if (Math.Abs(current.OsdDuration - rounded) > 0.001)
            {
                SettingsManager.Save(current with
                {
                    OsdDuration = rounded
                });
            }
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
            _isUpdatingOsdTextFromSlider = true;
            txtOsdDuration.Text = UiBehavior.FormatOsdDuration(appSettings.OsdDuration, CultureInfo.CurrentCulture);
            _isUpdatingOsdTextFromSlider = false;
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
            AppSettings current = SettingsManager.Load();
            if (current.SoundVolume != volume)
            {
                if (_isPointerInteractingWithVolumeSlider)
                {
                    _soundVolumeChangedDuringDrag = true;
                }
                SettingsManager.Save(current with
                {
                    SoundVolume = volume
                });
            }
            _volumeAudioDebouncer.Schedule(TimeSpan.FromMilliseconds(35), () => AudioFeedback.SetVolume(volume));
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
            _volumeAudioDebouncer.Schedule(TimeSpan.FromMilliseconds(1), () => AudioFeedback.SetVolume(restoreVal));
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
                _volumeAudioDebouncer.Schedule(TimeSpan.FromMilliseconds(35), () => AudioFeedback.SetVolume(result));
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
            _volumeAudioDebouncer.Schedule(TimeSpan.FromMilliseconds(1), () => AudioFeedback.SetVolume(clamped));
            _isUpdatingSoundVolumeTextFromSlider = true;
            sliderSoundVolume.Value = clamped;
            txtSoundVolume.Text = UiBehavior.FormatSoundVolume(clamped);
            _isUpdatingSoundVolumeTextFromSlider = false;
        }
        else
        {
            AppSettings appSettings = SettingsManager.Load();
            _isUpdatingSoundVolumeTextFromSlider = true;
            txtSoundVolume.Text = UiBehavior.FormatSoundVolume(appSettings.SoundVolume);
            _isUpdatingSoundVolumeTextFromSlider = false;
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
        ShowConfirmationModal(
            "Reset Settings",
            "Are you sure you want to reset all settings to factory defaults?",
            "Reset",
            ExecuteResetSettings,
            isDanger: true);
    }

    private void ExecuteResetSettings()
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

    internal void ShowConfirmationModal(string title, string message, string confirmLabel, Action onConfirm, bool isDanger = false)
    {
        if (borderModalOverlay == null || tbModalTitle == null || tbModalMessage == null || btnModalConfirm == null)
        {
            onConfirm();
            return;
        }

        _modalSequenceId++;
        _pendingModalConfirmAction = onConfirm;
        tbModalTitle.Text = title;
        tbModalMessage.Text = message;
        btnModalConfirm.Content = confirmLabel;
        btnModalConfirm.Style = (Style)FindResource(isDanger ? "DangerActionButtonStyle" : "SecondaryActionButtonStyle");

        borderModalOverlay.BeginAnimation(UIElement.OpacityProperty, null);
        borderModalOverlay.Visibility = Visibility.Visible;
        if (contentScrollViewer != null)
        {
            contentScrollViewer.IsEnabled = false;
        }

        if (IsLoaded && IsVisible)
        {
            CubicEase ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            borderModalOverlay.Opacity = 0.0;
            borderModalOverlay.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(150)) { EasingFunction = ease });
            if (modalCardScale != null)
            {
                modalCardScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                modalCardScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                modalCardScale.ScaleX = 0.96;
                modalCardScale.ScaleY = 0.96;
                modalCardScale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.96, 1.0, TimeSpan.FromMilliseconds(160)) { EasingFunction = ease });
                modalCardScale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.96, 1.0, TimeSpan.FromMilliseconds(160)) { EasingFunction = ease });
            }
        }
        else
        {
            borderModalOverlay.Opacity = 1.0;
            if (modalCardScale != null)
            {
                modalCardScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                modalCardScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                modalCardScale.ScaleX = 1.0;
                modalCardScale.ScaleY = 1.0;
            }
        }

        btnModalCancel?.Focus();
    }

    internal void CloseConfirmationModal(bool immediate = false)
    {
        _pendingModalConfirmAction = null;
        if (contentScrollViewer != null)
        {
            contentScrollViewer.IsEnabled = true;
        }
        if (borderModalOverlay == null || borderModalOverlay.Visibility != Visibility.Visible) return;

        int seq = ++_modalSequenceId;
        if (immediate || !IsLoaded || !IsVisible)
        {
            borderModalOverlay.BeginAnimation(UIElement.OpacityProperty, null);
            borderModalOverlay.Opacity = 0.0;
            borderModalOverlay.Visibility = Visibility.Collapsed;
            return;
        }

        DoubleAnimation fadeOut = new DoubleAnimation(borderModalOverlay.Opacity, 0.0, TimeSpan.FromMilliseconds(120))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        fadeOut.Completed += (_, _) =>
        {
            if (seq == _modalSequenceId)
            {
                borderModalOverlay.BeginAnimation(UIElement.OpacityProperty, null);
                borderModalOverlay.Opacity = 0.0;
                borderModalOverlay.Visibility = Visibility.Collapsed;
            }
        };
        borderModalOverlay.BeginAnimation(UIElement.OpacityProperty, fadeOut);
    }

    private void MainWindow_ModalPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (borderModalOverlay != null && borderModalOverlay.Visibility == Visibility.Visible)
        {
            if (e.Key == Key.Escape)
            {
                CloseConfirmationModal();
                e.Handled = true;
            }
            else if (e.Key == Key.Tab)
            {
                if (btnModalCancel != null && btnModalConfirm != null)
                {
                    if (btnModalCancel.IsKeyboardFocusWithin)
                    {
                        btnModalConfirm.Focus();
                    }
                    else
                    {
                        btnModalCancel.Focus();
                    }
                    e.Handled = true;
                }
            }
        }
    }

    public void BtnModalCancel_Click(object sender, RoutedEventArgs e)
    {
        CloseConfirmationModal();
    }

    public void BtnModalConfirm_Click(object sender, RoutedEventArgs e)
    {
        Action? action = _pendingModalConfirmAction;
        CloseConfirmationModal(immediate: true);
        action?.Invoke();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        _isMinimizing = false;
        _isRestoring = false;
        CloseConfirmationModal(immediate: true);
        if (rootBorder != null)
        {
            rootBorder.BeginAnimation(UIElement.OpacityProperty, null);
            rootBorder.CacheMode = null;
            rootBorder.Opacity = 1.0;
        }
        if (rootScaleTransform != null)
        {
            rootScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            rootScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            rootScaleTransform.ScaleX = 1.0;
            rootScaleTransform.ScaleY = 1.0;
        }
        if (rootTranslateTransform != null)
        {
            rootTranslateTransform.BeginAnimation(TranslateTransform.YProperty, null);
            rootTranslateTransform.Y = 0.0;
        }
        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _isDisposed = true;
        _deviceRefreshDebouncer.Dispose();
        _statusDebouncer.Dispose();
        _volumeAudioDebouncer.Dispose();
        _deviceRefreshCoordinator.Dispose();
        SettingsManager.SaveFailed -= SettingsManager_SaveFailed;
        _audioController.MuteStateChanged -= AudioController_MuteStateChanged;
        _audioController.DevicesChanged -= AudioController_DevicesChanged;
        _audioController.WarningNotification -= AudioController_WarningNotification;

        if (cbDevices != null && _dropDownOpenHandler != null)
        {
            System.ComponentModel.DependencyPropertyDescriptor.FromProperty(System.Windows.Controls.ComboBox.IsDropDownOpenProperty, typeof(System.Windows.Controls.ComboBox))
                ?.RemoveValueChanged(cbDevices, _dropDownOpenHandler);
            _dropDownOpenHandler = null;
        }

        try
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero)
            {
                HwndSource.FromHwnd(hwnd)?.RemoveHook(HwndMessageHook);
                if (_hIconSmall != IntPtr.Zero)
                {
                    SendMessage(hwnd, WM_SETICON, (IntPtr)ICON_SMALL, IntPtr.Zero);
                    try { SetClassLongPtr(hwnd, GCLP_HICONSM, IntPtr.Zero); } catch { }
                }
                if (_hIconBig != IntPtr.Zero)
                {
                    SendMessage(hwnd, WM_SETICON, (IntPtr)ICON_BIG, IntPtr.Zero);
                    try { SetClassLongPtr(hwnd, GCLP_HICON, IntPtr.Zero); } catch { }
                }
            }
        }
        catch (Exception ex) { DiagnosticLogger.LogError("Error clearing window icons", ex); }

        base.OnClosed(e);
        if (_hotkeyManager != null)
        {
            _hotkeyManager.HotkeyPressed -= HotkeyManager_HotkeyPressed;
            _hotkeyManager.HotkeyReleased -= HotkeyManager_HotkeyReleased;
            _hotkeyManager.Dispose();
            _hotkeyManager = null;
        }
        try
        {
            _icoSmall?.Dispose();
            _icoSmall = null;
            _hIconSmall = IntPtr.Zero;

            _icoBig?.Dispose();
            _icoBig = null;
            _hIconBig = IntPtr.Zero;
        }
        catch (Exception ex) { DiagnosticLogger.LogError("Error disposing window icons", ex); }
    }
}
