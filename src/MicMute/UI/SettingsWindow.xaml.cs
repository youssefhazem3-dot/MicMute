using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MicMute;

public partial class SettingsWindow : Window
{
    private const int WM_NCLBUTTONDOWN = 0xA1;
    private const int HTCAPTION = 0x2;

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    private static readonly System.Text.RegularExpressions.Regex XClassRegex =
        new(@"\s+x:Class=""[^""]+""", System.Text.RegularExpressions.RegexOptions.Compiled);

    private readonly MainWindow _ownerWindow;
    private bool _contentLoaded;
    private bool _allowForceClose;
    private bool _isClosingAnimationRunning;
    private double _preferredWidth = 412.0;
    private double _currentUiScale = 1.0;
    private int _openedSideDirection = 1; // +1 = opened to right of owner, -1 = opened to left
    private int _selectedOsdPosIndex;
    private int _themeTransitionVersion;
    private Action? _pendingModalConfirmAction;
    private int _modalSequenceId;

    internal Grid? settingsScaleHost;
    internal ScaleTransform? settingsLayoutScaleTransform;
    internal Border? settingsRootBorder;
    internal ScaleTransform? settingsOpenScale;
    internal TranslateTransform? settingsOpenTranslate;
    internal System.Windows.Shapes.Rectangle? settingsThemeOverlay;
    internal ScrollViewer settingsScrollViewer = null!;
    internal Border? borderSettingsTitleBar;
    internal System.Windows.Controls.Button? btnSettingsClose;

    internal System.Windows.Controls.CheckBox cbEnableOsd = null!;
    internal Grid? panelOsdDuration;
    internal Slider sliderOsdDuration = null!;
    internal Border? borderOsdDurationKeycap;
    internal System.Windows.Controls.TextBox txtOsdDuration = null!;
    internal Grid? gridOsdPosition;
    internal Border? borderOsdPosPill;
    internal TranslateTransform? osdPosPillTranslate;
    internal System.Windows.Controls.Button? btnOsdPosCenter;
    internal System.Windows.Controls.Button? btnOsdPosBottom;
    internal System.Windows.Controls.Button? btnOsdPosTop;

    internal System.Windows.Controls.CheckBox cbSoundFeedback = null!;
    internal Grid? panelSoundVolume;
    internal Slider sliderSoundVolume = null!;
    internal Border? borderSoundVolumeKeycap;
    internal System.Windows.Controls.TextBox txtSoundVolume = null!;

    internal System.Windows.Controls.CheckBox cbStartup = null!;
    internal System.Windows.Controls.CheckBox cbStartMinimized = null!;
    internal System.Windows.Controls.CheckBox cbRunAsAdmin = null!;

    internal System.Windows.Controls.Button btnOpenFolder = null!;
    internal System.Windows.Controls.Button btnChangeFolder = null!;
    internal System.Windows.Controls.Button btnResetData = null!;
    internal TextBlock tbStoragePath = null!;

    internal Border? borderSettingsModalOverlay;
    internal Border? borderSettingsModalCard;
    internal ScaleTransform? settingsModalCardScale;
    internal TextBlock? tbSettingsModalTitle;
    internal TextBlock? tbSettingsModalMessage;
    internal System.Windows.Controls.Button? btnSettingsModalCancel;
    internal System.Windows.Controls.Button? btnSettingsModalConfirm;

    internal double CurrentUiScale => _currentUiScale;
    internal bool IsSettingsOpen { get; private set; }

    internal event Action<bool>? OpenStateChanged;

    public SettingsWindow(MainWindow ownerWindow)
    {
        _ownerWindow = ownerWindow ?? throw new ArgumentNullException(nameof(ownerWindow));
        InitializeComponent();
        SyncThemeResources(ownerWindow.Resources);

        PreviewKeyDown += SettingsWindow_PreviewKeyDown;
        PreviewMouseLeftButtonDown += (s, e) =>
        {
            if (!IsActive)
            {
                Activate();
            }
        };
    }

    public void InitializeComponent()
    {
        if (_contentLoaded) return;
        _contentLoaded = true;

        string localFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SettingsWindow.xaml");
        Window root = LoadWindowRoot(localFile);

        Title = root.Title;
        Width = root.Width;
        if (!double.IsNaN(root.Height)) Height = root.Height;
        SizeToContent = root.SizeToContent;
        WindowStyle = root.WindowStyle;
        AllowsTransparency = root.AllowsTransparency;
        Background = root.Background;
        ResizeMode = root.ResizeMode;
        ShowInTaskbar = root.ShowInTaskbar;
        WindowStartupLocation = root.WindowStartupLocation;
        FontFamily = root.FontFamily;
        FontWeight = root.FontWeight;
        SnapsToDevicePixels = root.SnapsToDevicePixels;
        UseLayoutRounding = root.UseLayoutRounding;
        TextOptions.SetTextFormattingMode(this, TextOptions.GetTextFormattingMode(root));
        TextOptions.SetTextRenderingMode(this, TextOptions.GetTextRenderingMode(root));
        TextOptions.SetTextHintingMode(this, TextOptions.GetTextHintingMode(root));
        RenderOptions.SetClearTypeHint(this, RenderOptions.GetClearTypeHint(root));
        RenderOptions.SetEdgeMode(this, RenderOptions.GetEdgeMode(root));
        RenderOptions.SetBitmapScalingMode(this, RenderOptions.GetBitmapScalingMode(root));

        settingsScaleHost = root.FindName("settingsScaleHost") as Grid;
        settingsLayoutScaleTransform = root.FindName("settingsLayoutScaleTransform") as ScaleTransform;
        settingsRootBorder = root.FindName("settingsRootBorder") as Border;
        settingsOpenScale = root.FindName("settingsOpenScale") as ScaleTransform;
        settingsOpenTranslate = root.FindName("settingsOpenTranslate") as TranslateTransform;
        settingsThemeOverlay = root.FindName("settingsThemeOverlay") as System.Windows.Shapes.Rectangle;
        settingsScrollViewer = (ScrollViewer)root.FindName("settingsScrollViewer");
        borderSettingsTitleBar = root.FindName("borderSettingsTitleBar") as Border;
        btnSettingsClose = root.FindName("btnSettingsClose") as System.Windows.Controls.Button;

        cbEnableOsd = (System.Windows.Controls.CheckBox)root.FindName("cbEnableOsd");
        panelOsdDuration = root.FindName("panelOsdDuration") as Grid;
        sliderOsdDuration = (Slider)root.FindName("sliderOsdDuration");
        borderOsdDurationKeycap = root.FindName("borderOsdDurationKeycap") as Border;
        txtOsdDuration = (System.Windows.Controls.TextBox)root.FindName("txtOsdDuration");
        gridOsdPosition = root.FindName("gridOsdPosition") as Grid;
        borderOsdPosPill = root.FindName("borderOsdPosPill") as Border;
        osdPosPillTranslate = root.FindName("osdPosPillTranslate") as TranslateTransform;
        if (gridOsdPosition != null)
        {
            gridOsdPosition.SizeChanged += (_, _) => UpdateOsdPositionPill(_selectedOsdPosIndex, animate: false);
        }
        btnOsdPosCenter = root.FindName("btnOsdPosCenter") as System.Windows.Controls.Button;
        btnOsdPosBottom = root.FindName("btnOsdPosBottom") as System.Windows.Controls.Button;
        btnOsdPosTop = root.FindName("btnOsdPosTop") as System.Windows.Controls.Button;

        cbSoundFeedback = (System.Windows.Controls.CheckBox)root.FindName("cbSoundFeedback");
        panelSoundVolume = root.FindName("panelSoundVolume") as Grid;
        sliderSoundVolume = (Slider)root.FindName("sliderSoundVolume");
        borderSoundVolumeKeycap = root.FindName("borderSoundVolumeKeycap") as Border;
        txtSoundVolume = (System.Windows.Controls.TextBox)root.FindName("txtSoundVolume");

        cbStartup = (System.Windows.Controls.CheckBox)root.FindName("cbStartup");
        cbStartMinimized = (System.Windows.Controls.CheckBox)root.FindName("cbStartMinimized");
        cbRunAsAdmin = (System.Windows.Controls.CheckBox)root.FindName("cbRunAsAdmin");

        btnOpenFolder = (System.Windows.Controls.Button)root.FindName("btnOpenFolder");
        btnChangeFolder = (System.Windows.Controls.Button)root.FindName("btnChangeFolder");
        btnResetData = (System.Windows.Controls.Button)root.FindName("btnResetData");
        tbStoragePath = (TextBlock)root.FindName("tbStoragePath");

        borderSettingsModalOverlay = root.FindName("borderSettingsModalOverlay") as Border;
        borderSettingsModalCard = root.FindName("borderSettingsModalCard") as Border;
        settingsModalCardScale = root.FindName("settingsModalCardScale") as ScaleTransform;
        tbSettingsModalTitle = root.FindName("tbSettingsModalTitle") as TextBlock;
        tbSettingsModalMessage = root.FindName("tbSettingsModalMessage") as TextBlock;
        btnSettingsModalCancel = root.FindName("btnSettingsModalCancel") as System.Windows.Controls.Button;
        btnSettingsModalConfirm = root.FindName("btnSettingsModalConfirm") as System.Windows.Controls.Button;

        if (borderSettingsTitleBar != null)
        {
            borderSettingsTitleBar.MouseLeftButtonDown += TitleBar_MouseLeftButtonDown;
        }
        if (btnSettingsClose != null)
        {
            btnSettingsClose.Click += (_, _) => HideSettings(immediate: false);
        }
        if (sliderOsdDuration != null)
        {
            sliderOsdDuration.PreviewMouseWheel += ForwardSliderWheelToScrollViewer;
        }
        if (sliderSoundVolume != null)
        {
            sliderSoundVolume.PreviewMouseWheel += ForwardSliderWheelToScrollViewer;
        }
        if (btnSettingsModalCancel != null)
        {
            btnSettingsModalCancel.Click += (_, _) => CloseConfirmationModal();
        }
        if (btnSettingsModalConfirm != null)
        {
            btnSettingsModalConfirm.Click += (_, _) =>
            {
                Action? action = _pendingModalConfirmAction;
                CloseConfirmationModal(immediate: true);
                action?.Invoke();
            };
        }
        if (borderSettingsModalOverlay != null)
        {
            borderSettingsModalOverlay.MouseLeftButtonDown += (_, e) =>
            {
                if (ReferenceEquals(e.OriginalSource, borderSettingsModalOverlay))
                {
                    CloseConfirmationModal();
                    e.Handled = true;
                }
            };
        }

        if (double.IsFinite(Width) && Width > 0)
        {
            _preferredWidth = Width;
        }

        object content = root.Content;
        root.Content = null;
        root.Resources = new ResourceDictionary();
        Content = content;
        try { root.Close(); } catch { }
    }

    private void ForwardSliderWheelToScrollViewer(object sender, MouseWheelEventArgs e)
    {
        if (sender is Slider slider && !slider.IsFocused && settingsScrollViewer != null)
        {
            e.Handled = true;
            var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            {
                RoutedEvent = UIElement.MouseWheelEvent,
                Source = sender
            };
            settingsScrollViewer.RaiseEvent(eventArg);
        }
    }

    internal static Window LoadWindowRoot(string localFile)
    {
        if (File.Exists(localFile))
        {
            try { return ParseWindowRoot(File.ReadAllText(localFile)); }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("Ignoring invalid loose SettingsWindow.xaml: " + ex);
            }
        }

        using Stream stream = typeof(SettingsWindow).Assembly.GetManifestResourceStream("MicMute.SettingsWindow.xaml")
            ?? throw new FileNotFoundException("Embedded SettingsWindow.xaml was not found.");
        using StreamReader reader = new(stream);
        return ParseWindowRoot(reader.ReadToEnd());
    }

    private static Window ParseWindowRoot(string xaml)
    {
        xaml = XClassRegex.Replace(xaml, "");
        Window root = (Window)XamlReader.Parse(xaml);
        bool valid = root.FindName("cbEnableOsd") is System.Windows.Controls.CheckBox
            && root.FindName("sliderOsdDuration") is Slider
            && root.FindName("txtOsdDuration") is System.Windows.Controls.TextBox
            && root.FindName("cbSoundFeedback") is System.Windows.Controls.CheckBox
            && root.FindName("sliderSoundVolume") is Slider
            && root.FindName("txtSoundVolume") is System.Windows.Controls.TextBox
            && root.FindName("cbStartup") is System.Windows.Controls.CheckBox
            && root.FindName("cbStartMinimized") is System.Windows.Controls.CheckBox
            && root.FindName("cbRunAsAdmin") is System.Windows.Controls.CheckBox
            && root.FindName("btnOpenFolder") is System.Windows.Controls.Button
            && root.FindName("btnChangeFolder") is System.Windows.Controls.Button
            && root.FindName("btnResetData") is System.Windows.Controls.Button
            && root.FindName("tbStoragePath") is TextBlock
            && root.FindName("settingsScrollViewer") is ScrollViewer;
        if (valid) return root;
        root.Close();
        throw new InvalidDataException("SettingsWindow.xaml is missing required controls.");
    }

    internal void SyncThemeResources(ResourceDictionary sharedResources)
    {
        if (sharedResources != null)
        {
            Resources = sharedResources;
        }
    }

    internal void CaptureThemeSnapshotIfVisible()
    {
        if (!IsLoaded || !IsVisible || ActualWidth <= 0 || settingsRootBorder == null || settingsThemeOverlay == null)
        {
            return;
        }
        try
        {
            DpiScale dpi = VisualTreeHelper.GetDpi(this);
            int pixelW = Math.Max(1, (int)Math.Ceiling(settingsRootBorder.ActualWidth * dpi.DpiScaleX));
            int pixelH = Math.Max(1, (int)Math.Ceiling(settingsRootBorder.ActualHeight * dpi.DpiScaleY));
            var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(pixelW, pixelH, dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
            rtb.Render(settingsRootBorder);
            rtb.Freeze();
            var imgBrush = new ImageBrush(rtb);
            imgBrush.Freeze();
            settingsThemeOverlay.BeginAnimation(UIElement.OpacityProperty, null);
            settingsThemeOverlay.Fill = imgBrush;
            settingsThemeOverlay.Opacity = 1.0;
            settingsThemeOverlay.Visibility = Visibility.Visible;
        }
        catch { }
    }

    internal void PlayThemeCrossfadeIfVisible()
    {
        if (!IsLoaded || !IsVisible || settingsThemeOverlay == null || settingsThemeOverlay.Visibility != Visibility.Visible)
        {
            return;
        }
        int version = ++_themeTransitionVersion;
        UpdateLayout();
        DoubleAnimation fadeOut = new DoubleAnimation(1.0, 0.0, UiMotion.Standard)
        {
            EasingFunction = UiMotion.Decelerate
        };
        fadeOut.Completed += (_, _) =>
        {
            if (version != _themeTransitionVersion) return;
            ClearThemeOverlay();
        };
        settingsThemeOverlay.BeginAnimation(UIElement.OpacityProperty, fadeOut);
    }

    private void ClearThemeOverlay()
    {
        _themeTransitionVersion++;
        if (settingsThemeOverlay != null)
        {
            settingsThemeOverlay.BeginAnimation(UIElement.OpacityProperty, null);
            settingsThemeOverlay.Opacity = 0.0;
            settingsThemeOverlay.Visibility = Visibility.Collapsed;
            settingsThemeOverlay.Fill = null;
        }
    }

    internal void UpdateOsdPositionPill(int index, bool animate)
    {
        _selectedOsdPosIndex = Math.Clamp(index, 0, 2);
        if (gridOsdPosition == null || borderOsdPosPill == null || osdPosPillTranslate == null) return;

        double totalW = gridOsdPosition.ActualWidth;
        if (!double.IsFinite(totalW) || totalW <= 4.0) return;

        // 3 equal * columns separated by two 2px gap columns
        const double gap = 2.0;
        double segmentW = Math.Max(1.0, (totalW - (gap * 2.0)) / 3.0);
        borderOsdPosPill.Width = segmentW;
        double targetX = _selectedOsdPosIndex * (segmentW + gap);

        if (animate && IsLoaded && IsVisible)
        {
            DoubleAnimation glide = new DoubleAnimation(targetX, UiMotion.Standard)
            {
                EasingFunction = UiMotion.Decelerate
            };
            osdPosPillTranslate.BeginAnimation(TranslateTransform.XProperty, glide);
        }
        else
        {
            osdPosPillTranslate.BeginAnimation(TranslateTransform.XProperty, null);
            osdPosPillTranslate.X = targetX;
        }
    }

    private double MeasureUnscaledContentHeight()
    {
        if (settingsRootBorder != null)
        {
            settingsRootBorder.Measure(new System.Windows.Size(_preferredWidth, double.PositiveInfinity));
            if (double.IsFinite(settingsRootBorder.DesiredSize.Height) && settingsRootBorder.DesiredSize.Height > 0)
            {
                return settingsRootBorder.DesiredSize.Height;
            }
        }
        return 560.0;
    }

    private bool ApplyUiScale(double scale)
    {
        double clampedScale = Math.Clamp(scale, UiBehavior.MinimumUiScale, 1.0);
        bool changed = Math.Abs(_currentUiScale - clampedScale) > 0.0005;
        _currentUiScale = clampedScale;
        if (settingsLayoutScaleTransform != null)
        {
            if (Math.Abs(settingsLayoutScaleTransform.ScaleX - clampedScale) > 0.0005)
                settingsLayoutScaleTransform.ScaleX = clampedScale;
            if (Math.Abs(settingsLayoutScaleTransform.ScaleY - clampedScale) > 0.0005)
                settingsLayoutScaleTransform.ScaleY = clampedScale;
        }
        if (changed)
        {
            settingsScaleHost?.InvalidateMeasure();
            InvalidateMeasure();
        }
        return changed;
    }

    internal void ApplyAdaptiveBounds(DipRect workArea, DipRect? anchorBounds)
    {
        double maxAllowedHeight = UiBehavior.CalculateAdaptiveMaxHeight(workArea.Height, UiBehavior.DefaultAdaptiveVerticalMargin);
        if (Math.Abs(MaxHeight - maxAllowedHeight) > 0.5) MaxHeight = maxAllowedHeight;

        double naturalHeight = MeasureUnscaledContentHeight();
        double scale = UiBehavior.CalculateAdaptiveScale(
            workArea,
            _preferredWidth,
            naturalHeight,
            UiBehavior.DefaultAdaptiveVerticalMargin,
            UiBehavior.DefaultAdaptiveHorizontalMargin,
            UiBehavior.MinimumUiScale);
        ApplyUiScale(scale);

        double scaledWidth = Math.Min(workArea.Width, Math.Ceiling(_preferredWidth * scale));
        double scaledHeight = Math.Min(maxAllowedHeight, Math.Ceiling(naturalHeight * scale));

        DipRect placed = anchorBounds.HasValue
            ? UiBehavior.CalculateAdjacentPopupBounds(
                workArea,
                anchorBounds.Value,
                scaledWidth,
                scaledHeight,
                gap: 12.0,
                verticalMargin: UiBehavior.DefaultAdaptiveVerticalMargin)
            : UiBehavior.ClampWindowBoundsToWorkArea(
                workArea,
                new DipRect(Left, Top, scaledWidth, scaledHeight),
                UiBehavior.DefaultAdaptiveVerticalMargin);

        Width = placed.Width;
        Left = placed.Left;
        Top = placed.Top;

        if (anchorBounds.HasValue)
        {
            double anchorCenter = anchorBounds.Value.Left + (anchorBounds.Value.Width / 2.0);
            double popupCenter = placed.Left + (placed.Width / 2.0);
            _openedSideDirection = popupCenter >= anchorCenter ? 1 : -1;
        }
    }

    internal void ShowSettings(DipRect workArea, DipRect anchorBounds)
    {
        _isClosingAnimationRunning = false;
        SyncThemeResources(_ownerWindow.Resources);

        if (Owner == null && _ownerWindow.IsVisible)
        {
            try { Owner = _ownerWindow; } catch { }
        }

        ApplyAdaptiveBounds(workArea, anchorBounds);

        bool wasOpen = IsSettingsOpen;
        IsSettingsOpen = true;

        if (!IsVisible)
        {
            if (settingsRootBorder != null)
            {
                settingsRootBorder.BeginAnimation(UIElement.OpacityProperty, null);
                settingsRootBorder.Opacity = 0.0;
            }
            Show();
        }

        Activate();
        UpdateOsdPositionPill(_selectedOsdPosIndex, animate: false);
        PlayOpenAnimation();

        if (!wasOpen)
        {
            OpenStateChanged?.Invoke(true);
        }
    }

    internal void HideSettings(bool immediate = false)
    {
        CloseConfirmationModal(immediate: true);

        bool wasOpen = IsSettingsOpen;
        IsSettingsOpen = false;

        if (!IsVisible)
        {
            if (wasOpen) OpenStateChanged?.Invoke(false);
            return;
        }

        if (immediate || !IsLoaded || settingsRootBorder == null)
        {
            _isClosingAnimationRunning = false;
            ClearThemeOverlay();
            ResetOpenTransforms();
            Hide();
            if (wasOpen) OpenStateChanged?.Invoke(false);
            return;
        }

        if (_isClosingAnimationRunning)
        {
            if (wasOpen) OpenStateChanged?.Invoke(false);
            return;
        }

        _isClosingAnimationRunning = true;
        if (wasOpen) OpenStateChanged?.Invoke(false);

        UiMotion.EnableTransientCache(settingsRootBorder, this);
        Duration dur = UiMotion.Exit;
        IEasingFunction ease = UiMotion.Accelerate;
        DoubleAnimation fadeOut = new DoubleAnimation(settingsRootBorder.Opacity, 0.0, dur) { EasingFunction = ease };
        fadeOut.Completed += (_, _) =>
        {
            if (_isClosingAnimationRunning && !IsSettingsOpen)
            {
                _isClosingAnimationRunning = false;
                ClearThemeOverlay();
                ResetOpenTransforms();
                Hide();
            }
        };
        settingsRootBorder.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        if (settingsOpenScale != null)
        {
            settingsOpenScale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(settingsOpenScale.ScaleX, 0.96, dur) { EasingFunction = ease });
            settingsOpenScale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(settingsOpenScale.ScaleY, 0.96, dur) { EasingFunction = ease });
        }
        if (settingsOpenTranslate != null)
        {
            double exitX = -10.0 * _openedSideDirection;
            settingsOpenTranslate.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(settingsOpenTranslate.X, exitX, dur) { EasingFunction = ease });
        }
    }

    internal void ForceClose()
    {
        _allowForceClose = true;
        _isClosingAnimationRunning = false;
        IsSettingsOpen = false;
        CloseConfirmationModal(immediate: true);
        ClearThemeOverlay();
        ResetOpenTransforms();
        osdPosPillTranslate?.BeginAnimation(TranslateTransform.XProperty, null);
        OpenStateChanged = null;
        try { Close(); } catch { }
    }

    private void PlayOpenAnimation()
    {
        if (settingsRootBorder == null) return;

        settingsRootBorder.BeginAnimation(UIElement.OpacityProperty, null);
        if (!IsLoaded)
        {
            ResetOpenTransforms();
            return;
        }

        UiMotion.EnableTransientCache(settingsRootBorder, this);
        Duration dur = UiMotion.Standard;
        IEasingFunction ease = UiMotion.Decelerate;
        double startX = -14.0 * _openedSideDirection;

        settingsRootBorder.Opacity = 0.0;
        DoubleAnimation fadeIn = new DoubleAnimation(0.0, 1.0, dur) { EasingFunction = ease };
        fadeIn.Completed += (_, _) =>
        {
            if (IsSettingsOpen && !_isClosingAnimationRunning)
            {
                ResetOpenTransforms();
            }
        };
        settingsRootBorder.BeginAnimation(UIElement.OpacityProperty, fadeIn);

        if (settingsOpenScale != null)
        {
            settingsOpenScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            settingsOpenScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            settingsOpenScale.ScaleX = 0.96;
            settingsOpenScale.ScaleY = 0.96;
            settingsOpenScale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.96, 1.0, dur) { EasingFunction = ease });
            settingsOpenScale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.96, 1.0, dur) { EasingFunction = ease });
        }
        if (settingsOpenTranslate != null)
        {
            settingsOpenTranslate.BeginAnimation(TranslateTransform.XProperty, null);
            settingsOpenTranslate.BeginAnimation(TranslateTransform.YProperty, null);
            settingsOpenTranslate.X = startX;
            settingsOpenTranslate.Y = 0.0;
            settingsOpenTranslate.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(startX, 0.0, dur) { EasingFunction = ease });
        }
    }

    private void ResetOpenTransforms()
    {
        UiMotion.ClearTransientCache(settingsRootBorder);
        if (settingsRootBorder != null)
        {
            settingsRootBorder.BeginAnimation(UIElement.OpacityProperty, null);
            settingsRootBorder.Opacity = 1.0;
        }
        if (settingsOpenScale != null)
        {
            settingsOpenScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            settingsOpenScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            settingsOpenScale.ScaleX = 1.0;
            settingsOpenScale.ScaleY = 1.0;
        }
        if (settingsOpenTranslate != null)
        {
            settingsOpenTranslate.BeginAnimation(TranslateTransform.XProperty, null);
            settingsOpenTranslate.BeginAnimation(TranslateTransform.YProperty, null);
            settingsOpenTranslate.X = 0.0;
            settingsOpenTranslate.Y = 0.0;
        }
    }

    internal void ShowConfirmationModal(string title, string message, string confirmLabel, Action onConfirm, bool isDanger = false)
    {
        if (borderSettingsModalOverlay == null || tbSettingsModalTitle == null || tbSettingsModalMessage == null || btnSettingsModalConfirm == null)
        {
            onConfirm();
            return;
        }

        int seq = ++_modalSequenceId;
        _pendingModalConfirmAction = onConfirm;
        tbSettingsModalTitle.Text = title;
        tbSettingsModalMessage.Text = message;
        btnSettingsModalConfirm.Content = confirmLabel;
        btnSettingsModalConfirm.Style = (Style)FindResource(isDanger ? "DangerActionButtonStyle" : "SecondaryActionButtonStyle");

        if (btnSettingsModalCancel != null)
        {
            btnSettingsModalCancel.Visibility = Visibility.Visible;
        }
        Grid.SetColumn(btnSettingsModalConfirm, 2);
        Grid.SetColumnSpan(btnSettingsModalConfirm, 1);

        borderSettingsModalOverlay.BeginAnimation(UIElement.OpacityProperty, null);
        borderSettingsModalOverlay.Visibility = Visibility.Visible;
        if (settingsScrollViewer != null)
        {
            settingsScrollViewer.IsEnabled = false;
        }

        if (IsLoaded && IsVisible)
        {
            UiMotion.EnableTransientCache(borderSettingsModalCard, this);
            borderSettingsModalOverlay.Opacity = 0.0;
            DoubleAnimation fadeIn = new DoubleAnimation(0.0, 1.0, UiMotion.Fast) { EasingFunction = UiMotion.Decelerate };
            fadeIn.Completed += (_, _) =>
            {
                if (seq == _modalSequenceId)
                {
                    UiMotion.ClearTransientCache(borderSettingsModalCard);
                    borderSettingsModalOverlay.BeginAnimation(UIElement.OpacityProperty, null);
                    borderSettingsModalOverlay.Opacity = 1.0;
                    if (settingsModalCardScale != null)
                    {
                        settingsModalCardScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                        settingsModalCardScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                        settingsModalCardScale.ScaleX = 1.0;
                        settingsModalCardScale.ScaleY = 1.0;
                    }
                }
            };
            borderSettingsModalOverlay.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            if (settingsModalCardScale != null)
            {
                settingsModalCardScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                settingsModalCardScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                settingsModalCardScale.ScaleX = 0.94;
                settingsModalCardScale.ScaleY = 0.94;
                settingsModalCardScale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.94, 1.0, UiMotion.Fast) { EasingFunction = UiMotion.Decelerate });
                settingsModalCardScale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.94, 1.0, UiMotion.Fast) { EasingFunction = UiMotion.Decelerate });
            }
        }
        else
        {
            borderSettingsModalOverlay.Opacity = 1.0;
            if (settingsModalCardScale != null)
            {
                settingsModalCardScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                settingsModalCardScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                settingsModalCardScale.ScaleX = 1.0;
                settingsModalCardScale.ScaleY = 1.0;
            }
        }

        btnSettingsModalCancel?.Focus();
    }

    internal void CloseConfirmationModal(bool immediate = false)
    {
        _pendingModalConfirmAction = null;
        if (settingsScrollViewer != null)
        {
            settingsScrollViewer.IsEnabled = true;
        }
        if (borderSettingsModalOverlay == null || borderSettingsModalOverlay.Visibility != Visibility.Visible) return;

        int seq = ++_modalSequenceId;
        if (immediate || !IsLoaded || !IsVisible)
        {
            UiMotion.ClearTransientCache(borderSettingsModalCard);
            borderSettingsModalOverlay.BeginAnimation(UIElement.OpacityProperty, null);
            borderSettingsModalOverlay.Opacity = 0.0;
            borderSettingsModalOverlay.Visibility = Visibility.Collapsed;
            if (settingsModalCardScale != null)
            {
                settingsModalCardScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                settingsModalCardScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                settingsModalCardScale.ScaleX = 1.0;
                settingsModalCardScale.ScaleY = 1.0;
            }
            return;
        }

        UiMotion.EnableTransientCache(borderSettingsModalCard, this);
        DoubleAnimation fadeOut = new DoubleAnimation(borderSettingsModalOverlay.Opacity, 0.0, UiMotion.Exit)
        {
            EasingFunction = UiMotion.Accelerate
        };
        fadeOut.Completed += (_, _) =>
        {
            if (seq == _modalSequenceId)
            {
                UiMotion.ClearTransientCache(borderSettingsModalCard);
                borderSettingsModalOverlay.BeginAnimation(UIElement.OpacityProperty, null);
                borderSettingsModalOverlay.Opacity = 0.0;
                borderSettingsModalOverlay.Visibility = Visibility.Collapsed;
                if (settingsModalCardScale != null)
                {
                    settingsModalCardScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                    settingsModalCardScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                    settingsModalCardScale.ScaleX = 1.0;
                    settingsModalCardScale.ScaleY = 1.0;
                }
            }
        };
        if (settingsModalCardScale != null)
        {
            settingsModalCardScale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(settingsModalCardScale.ScaleX, 0.96, UiMotion.Exit) { EasingFunction = UiMotion.Accelerate });
            settingsModalCardScale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(settingsModalCardScale.ScaleY, 0.96, UiMotion.Exit) { EasingFunction = UiMotion.Accelerate });
        }
        borderSettingsModalOverlay.BeginAnimation(UIElement.OpacityProperty, fadeOut);
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject dep && FindVisualParent<ButtonBase>(dep) != null)
        {
            return;
        }
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            ReleaseCapture();
            SendMessage(new WindowInteropHelper(this).Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
        }
    }

    private void SettingsWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (borderSettingsModalOverlay != null && borderSettingsModalOverlay.Visibility == Visibility.Visible)
        {
            if (e.Key == Key.Escape)
            {
                CloseConfirmationModal();
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Tab && btnSettingsModalCancel != null && btnSettingsModalConfirm != null)
            {
                if (btnSettingsModalCancel.IsKeyboardFocusWithin)
                    btnSettingsModalConfirm.Focus();
                else
                    btnSettingsModalCancel.Focus();
                e.Handled = true;
                return;
            }
            return;
        }

        if (e.Key == Key.Escape)
        {
            // Let focused duration/volume textboxes handle Escape revert first if focused;
            // otherwise close the Settings window cleanly.
            if (Keyboard.FocusedElement is System.Windows.Controls.TextBox)
            {
                return;
            }
            HideSettings(immediate: false);
            e.Handled = true;
        }
    }

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        if (IsVisible)
        {
            DipRect? workArea = _ownerWindow.GetWorkAreaForWindow(this);
            if (workArea.HasValue)
            {
                ApplyAdaptiveBounds(workArea.Value, anchorBounds: null);
            }
        }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (!_allowForceClose)
        {
            e.Cancel = true;
            HideSettings(immediate: false);
            return;
        }
        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _isClosingAnimationRunning = false;
        IsSettingsOpen = false;
        CloseConfirmationModal(immediate: true);
        ClearThemeOverlay();
        ResetOpenTransforms();
        osdPosPillTranslate?.BeginAnimation(TranslateTransform.XProperty, null);
        OpenStateChanged = null;
        try { Owner = null; } catch { }
        Resources = new ResourceDictionary();
        base.OnClosed(e);
    }

    private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
    {
        DependencyObject? current = child;
        while (current != null)
        {
            if (current is T match) return match;
            current = current is Visual || current is System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(current)
                : LogicalTreeHelper.GetParent(current);
        }
        return null;
    }
}
