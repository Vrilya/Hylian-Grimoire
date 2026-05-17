using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using HylianGrimoire.Interop;
using HylianGrimoire.Models;
using WinRT.Interop;

namespace HylianGrimoire.Preview;

public sealed partial class OotPreviewWindow : Window
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
    private OotPreviewStyle _style = OotPreviewStyle.Black;
    private IReadOnlyList<MessageToken> _messageTokens = [];

    public OotPreviewWindow()
    {
        InitializeComponent();
        SystemBackdrop = new MicaBackdrop();
        AppWindow.Resize(new Windows.Graphics.SizeInt32(740, 700));
        WindowSizeLimits.SetMinimumSize(this, 740, 700);
        WindowIcon.Apply(this);
        WindowTheme.Register(this);
        RowsPerColumnBox.SelectedIndex = 4;
        ApplyColumnLayout();
        _initialized = true;
    }

    public void SetMessage(OotPreviewStyle style, IReadOnlyList<MessageToken> messageTokens)
    {
        _style = style;
        _messageTokens = messageTokens;
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
        PreviewView.Render(_style, _messageTokens, GuideOverlayButton.IsChecked == true);
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
