using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace HylianGrimoire.Preview;

internal sealed class PreviewImageSlot
{
    private readonly Image _first = new() { Stretch = Stretch.Uniform };
    private readonly Image _second = new() { Stretch = Stretch.Uniform, Opacity = 0 };
    private int _visibleIndex;
    private int _pendingVersion;
    private Uri? _visibleUri;
    private Uri? _pendingUri;

    public PreviewImageSlot()
    {
        Root.Children.Add(_first);
        Root.Children.Add(_second);
        _first.ImageOpened += OnImageOpened;
        _second.ImageOpened += OnImageOpened;
        _first.ImageFailed += OnImageFailed;
        _second.ImageFailed += OnImageFailed;
    }

    public Grid Root { get; } = new();

    public object? Tag { get; set; }

    public void SetSize(double width, double height)
    {
        Root.Width = width;
        Root.Height = height;
        _first.Width = width;
        _first.Height = height;
        _second.Width = width;
        _second.Height = height;
    }

    public void SetSource(Uri uri)
    {
        if (uri == _visibleUri || uri == _pendingUri)
        {
            return;
        }

        Image next = GetHiddenImage();
        int version = ++_pendingVersion;
        _pendingUri = uri;
        next.Tag = version;
        next.Opacity = 0;
        next.Source = new BitmapImage(uri);
    }

    private Image GetVisibleImage()
    {
        return _visibleIndex == 0 ? _first : _second;
    }

    private Image GetHiddenImage()
    {
        return _visibleIndex == 0 ? _second : _first;
    }

    private void OnImageOpened(object sender, RoutedEventArgs e)
    {
        if (sender is not Image opened || opened.Tag is not int version || version != _pendingVersion)
        {
            return;
        }

        Image current = GetVisibleImage();
        current.Opacity = 0;
        opened.Opacity = 1;
        if (!ReferenceEquals(current, opened))
        {
            current.Source = null;
        }

        _visibleIndex = ReferenceEquals(opened, _first) ? 0 : 1;
        _visibleUri = _pendingUri;
        _pendingUri = null;
    }

    private void OnImageFailed(object sender, ExceptionRoutedEventArgs e)
    {
        if (sender is not Image failed || failed.Tag is not int version || version != _pendingVersion)
        {
            return;
        }

        failed.Source = null;
        _pendingUri = null;
    }
}
