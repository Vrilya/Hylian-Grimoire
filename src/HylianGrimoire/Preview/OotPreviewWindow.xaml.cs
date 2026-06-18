using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using HylianGrimoire.Codecs;
using HylianGrimoire.Glyphs;
using HylianGrimoire.Interop;
using HylianGrimoire.Models;
using HylianGrimoire.Services;
using WinRT.Interop;

namespace HylianGrimoire.Preview;

public sealed partial class OotPreviewWindow : Window, IMessagePreviewWindow
{
    private const double MinZoom = 0.5;
    private const double MaxZoom = 2.5;
    private const double ZoomStep = 0.1;
    private const int InitialWindowWidth = 800;
    private const int InitialWindowHeight = 730;
    private const int MinimumWindowWidth = 740;
    private const int MinimumWindowHeight = 350;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoActivate = 0x0010;
    private static readonly IntPtr HwndTopmost = new(-1);
    private static readonly IntPtr HwndNoTopmost = new(-2);

    private double _zoomScale = 1.0;
    private bool _initialized;
    private readonly MessagePreviewWindowState _state;
    private OotPreviewStyle _style = OotPreviewStyle.Black;
    private MessageEncodingProfile _encodingProfile = MessageEncodingProfile.Default;
    private IGlyphSource _glyphSource = OotGlyphSources.OriginalAssets;
    private IReadOnlyList<MessageToken> _messageTokens = [];

    public event EventHandler? PreviewClosed;

    public OotPreviewWindow()
        : this(MessageEncodingProfile.Default, new MessagePreviewWindowState())
    {
    }

    public OotPreviewWindow(MessageEncodingProfile encodingProfile, MessagePreviewWindowState state)
    {
        _encodingProfile = encodingProfile;
        _state = state;
        InitializeComponent();
        SystemBackdrop = new MicaBackdrop();
        WindowSizeLimits.SetMinimumSize(this, MinimumWindowWidth, MinimumWindowHeight);
        ApplyWindowSizeFromState();
        WindowIcon.Apply(this);
        WindowTheme.Register(this);
        ApplyStateToControls();
        ApplyColumnLayout();
        _initialized = true;
        Closed += (_, _) =>
        {
            StoreWindowSize();
            PreviewClosed?.Invoke(this, EventArgs.Empty);
        };
    }

    public void SetMessage(OotPreviewStyle style, IReadOnlyList<MessageToken> messageTokens)
    {
        _style = style;
        _messageTokens = messageTokens;
        RenderCurrentMessage();
    }

    public void SetEmpty()
    {
        SetMessage(OotPreviewStyle.Black, Array.Empty<MessageToken>());
    }

    public void SetMessage(
        MessageEntry entry,
        string editorText,
        IGlyphSource glyphSource,
        MessageEncodingProfile encodingProfile)
    {
        SetMessage(
            OotMessageTypeCatalog.ToPreviewStyle(entry.Type),
            MessageTextSyntax.FromEditorText(editorText),
            glyphSource,
            encodingProfile);
    }

    public void SetMessage(
        OotPreviewStyle style,
        IReadOnlyList<MessageToken> messageTokens,
        IGlyphSource glyphSource,
        MessageEncodingProfile encodingProfile)
    {
        _style = style;
        _messageTokens = messageTokens;
        _glyphSource = glyphSource;
        _encodingProfile = encodingProfile;
        RenderCurrentMessage();
    }

    public void SetGlyphSource(IGlyphSource glyphSource)
    {
        _glyphSource = glyphSource;
        RenderCurrentMessage();
    }

    public void SetEncodingProfile(MessageEncodingProfile encodingProfile)
    {
        _encodingProfile = encodingProfile;
        RenderCurrentMessage();
    }

    private void OnTopmostToggled(object sender, RoutedEventArgs e)
    {
        SetAlwaysOnTop(TopmostButton.IsChecked == true);
        UpdateState();
    }

    private void OnZoomOut(object sender, RoutedEventArgs e)
    {
        SetZoom(_zoomScale - ZoomStep);
    }

    private void OnZoomIn(object sender, RoutedEventArgs e)
    {
        SetZoom(_zoomScale + ZoomStep);
    }

    private void OnResetZoom(object sender, RoutedEventArgs e)
    {
        SetZoom(1.0);
    }

    private void OnRowsPerColumnChanged(object sender, RoutedEventArgs e)
    {
        if (!_initialized)
        {
            return;
        }

        ApplyColumnLayout();
    }

    private void OnColumnLayoutChanged(object sender, RoutedEventArgs e)
    {
        if (!_initialized)
        {
            return;
        }

        ApplyColumnLayout();
    }

    private void OnGuideOverlayChanged(object sender, RoutedEventArgs e)
    {
        if (!_initialized)
        {
            return;
        }

        RenderCurrentMessage();
        UpdateState();
    }

    private void RenderCurrentMessage()
    {
        PreviewView.Render(_style, _messageTokens, GuideOverlayButton.IsChecked == true, _glyphSource, _encodingProfile);
    }

    private void SetZoom(double zoomScale)
    {
        _zoomScale = Math.Clamp(Math.Round(zoomScale, 2), MinZoom, MaxZoom);
        PreviewView.SetZoom(_zoomScale);
        ZoomText.Content = $"{_zoomScale:P0}";
        UpdateState();
    }

    private void ApplyColumnLayout()
    {
        bool useColumns = WrapColumnsBox.IsChecked == true;
        RowsPerColumnBox.IsEnabled = useColumns;

        if (!useColumns)
        {
            PreviewView.SetRowsPerColumn(int.MaxValue);
            UpdateState();
            return;
        }

        if (RowsPerColumnBox.SelectedItem is ComboBoxItem item
            && item.Content is string text
            && int.TryParse(text, out int rowsPerColumn))
        {
            PreviewView.SetRowsPerColumn(rowsPerColumn);
            UpdateState();
        }
    }

    private void ApplyStateToControls()
    {
        SetZoom(_state.ZoomScale);
        GuideOverlayButton.IsChecked = _state.ShowAlignmentGuides;
        WrapColumnsBox.IsChecked = _state.UseColumns;
        SelectRowsPerColumn(_state.RowsPerColumn);
        TopmostButton.IsChecked = _state.AlwaysOnTop;
        SetAlwaysOnTop(_state.AlwaysOnTop);
    }

    private void UpdateState()
    {
        if (!_initialized)
        {
            return;
        }

        _state.ZoomScale = _zoomScale;
        _state.ShowAlignmentGuides = GuideOverlayButton.IsChecked == true;
        _state.UseColumns = WrapColumnsBox.IsChecked == true;
        _state.RowsPerColumn = GetSelectedRowsPerColumn();
        _state.AlwaysOnTop = TopmostButton.IsChecked == true;
    }

    private void SelectRowsPerColumn(int rowsPerColumn)
    {
        int clampedRows = Math.Clamp(rowsPerColumn, 1, 8);
        RowsPerColumnBox.SelectedIndex = clampedRows - 1;
    }

    private void ApplyWindowSizeFromState()
    {
        int width = Math.Max(MinimumWindowWidth, _state.WindowWidth ?? InitialWindowWidth);
        int height = Math.Max(MinimumWindowHeight, _state.WindowHeight ?? InitialWindowHeight);
        AppWindow.Resize(new Windows.Graphics.SizeInt32(width, height));
    }

    private void StoreWindowSize()
    {
        Windows.Graphics.SizeInt32 size = AppWindow.Size;
        _state.WindowWidth = Math.Max(MinimumWindowWidth, size.Width);
        _state.WindowHeight = Math.Max(MinimumWindowHeight, size.Height);
    }

    private int GetSelectedRowsPerColumn()
    {
        if (RowsPerColumnBox.SelectedItem is ComboBoxItem item
            && item.Content is string text
            && int.TryParse(text, out int rowsPerColumn))
        {
            return rowsPerColumn;
        }

        return MessagePreviewWindowState.DefaultRowsPerColumn;
    }

    private void SetAlwaysOnTop(bool enabled)
    {
        IntPtr hwnd = WindowNative.GetWindowHandle(this);
        _ = SetWindowPos(
            hwnd,
            enabled ? HwndTopmost : HwndNoTopmost,
            0,
            0,
            0,
            0,
            SwpNoMove | SwpNoSize | SwpNoActivate);
    }

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags);
}
