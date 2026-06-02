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

public sealed partial class MmPreviewWindow : Window, IMessagePreviewWindow
{
    private const double MinZoom = 0.5;
    private const double MaxZoom = 2.5;
    private const double ZoomStep = 0.1;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoActivate = 0x0010;
    private static readonly IntPtr HwndTopmost = new(-1);
    private static readonly IntPtr HwndNoTopmost = new(-2);

    private double _zoomScale = 1.0;
    private bool _initialized;
    private MmPreviewStyle _style = MmPreviewStyle.Black;
    private MessageEncodingProfile _encodingProfile = MessageEncodingProfile.MajorasMask;
    private IGlyphSource _glyphSource = MmGlyphSources.Assets;
    private MmPreviewRenderOptions _renderOptions = MmPreviewRenderOptions.Default;
    private string _editorText = string.Empty;

    public event EventHandler? PreviewClosed;

    public MmPreviewWindow(MessageEncodingProfile encodingProfile)
    {
        _encodingProfile = encodingProfile;
        InitializeComponent();
        SystemBackdrop = new MicaBackdrop();
        AppWindow.Resize(new Windows.Graphics.SizeInt32(800, 730));
        WindowSizeLimits.SetMinimumSize(this, 740, 350);
        WindowIcon.Apply(this);
        WindowTheme.Register(this);
        RowsPerColumnBox.SelectedIndex = 4;
        ApplyColumnLayout();
        _initialized = true;
        Closed += (_, _) => PreviewClosed?.Invoke(this, EventArgs.Empty);
    }

    public void SetEmpty()
    {
        _style = MmPreviewStyle.Black;
        _editorText = string.Empty;
        _renderOptions = MmPreviewRenderOptions.Default;
        RenderCurrentMessage();
    }

    public void SetMessage(
        MessageEntry entry,
        string editorText,
        IGlyphSource glyphSource,
        MessageEncodingProfile encodingProfile)
    {
        _style = MmPreviewEntryClassifier.IsStaffCredits(entry)
            ? MmPreviewStyle.StaffCredits
            : MmMessageTypeCatalog.ToPreviewStyle(entry.Type);
        _editorText = editorText;
        _glyphSource = glyphSource;
        _encodingProfile = encodingProfile;
        _renderOptions = entry.CodecMetadata is MajorasMaskMessageMetadata metadata
            ? new MmPreviewRenderOptions(metadata.IconId, metadata.IsCentered)
            : MmPreviewRenderOptions.Default;
        RenderCurrentMessage();
    }

    private void OnTopmostToggled(object sender, RoutedEventArgs e)
    {
        SetAlwaysOnTop(TopmostButton.IsChecked == true);
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
    }

    private void RenderCurrentMessage()
    {
        PreviewView.Render(_style, _editorText, _renderOptions, GuideOverlayButton.IsChecked == true, _glyphSource, _encodingProfile);
    }

    private void SetZoom(double zoomScale)
    {
        _zoomScale = Math.Clamp(Math.Round(zoomScale, 2), MinZoom, MaxZoom);
        PreviewView.SetZoom(_zoomScale);
        ZoomText.Content = $"{_zoomScale:P0}";
    }

    private void ApplyColumnLayout()
    {
        bool useColumns = WrapColumnsBox.IsChecked == true;
        RowsPerColumnBox.IsEnabled = useColumns;

        if (!useColumns)
        {
            PreviewView.SetRowsPerColumn(int.MaxValue);
            return;
        }

        if (RowsPerColumnBox.SelectedItem is ComboBoxItem item
            && item.Content is string text
            && int.TryParse(text, out int rowsPerColumn))
        {
            PreviewView.SetRowsPerColumn(rowsPerColumn);
        }
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
