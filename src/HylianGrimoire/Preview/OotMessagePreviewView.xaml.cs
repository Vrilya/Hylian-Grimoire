using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using HylianGrimoire.Codecs;
using HylianGrimoire.Models;

namespace HylianGrimoire.Preview;

public sealed partial class OotMessagePreviewView : UserControl
{
    private const double BaseWidth = 448;
    private const double BaseHeight = 126;
    private const double CreditsBaseWidth = 560;
    private const double CreditsBaseHeight = 420;
    private double _zoomScale = 1.0;
    private int _rowsPerColumn = 5;
    private readonly List<Image> _previewImages = [];

    public OotMessagePreviewView()
    {
        InitializeComponent();
        Render(OotPreviewStyle.Black, Array.Empty<MessageToken>(), showAlignmentGuides: false);
    }

    public void SetZoom(double zoomScale)
    {
        _zoomScale = zoomScale;
        foreach (Image image in _previewImages)
        {
            ApplyImageSize(image);
        }
    }

    public void SetRowsPerColumn(int rowsPerColumn)
    {
        _rowsPerColumn = Math.Max(1, rowsPerColumn);
        ArrangeImages();
    }

    public void Render(OotPreviewStyle style, IReadOnlyList<MessageToken> messageTokens, bool showAlignmentGuides)
    {
        bool darkText = style == OotPreviewStyle.NoneDarkText;
        var pages = OotPreviewTextPage.FromMessageTokensPages(messageTokens);

        _previewImages.Clear();
        PreviewGrid.Children.Clear();
        for (int i = 0; i < pages.Count; i++)
        {
            var image = new Image
            {
                Stretch = Stretch.Uniform,
                Tag = style,
                Source = new BitmapImage(OotBitmapCache.RenderPreview(style, pages[i], darkText, i == pages.Count - 1, showAlignmentGuides)),
            };
            ApplyImageSize(image);
            _previewImages.Add(image);
        }

        ArrangeImages();
    }

    private void ApplyImageSize(Image image)
    {
        bool credits = image.Tag is OotPreviewStyle.Credits;
        image.Width = (credits ? CreditsBaseWidth : BaseWidth) * _zoomScale;
        image.Height = (credits ? CreditsBaseHeight : BaseHeight) * _zoomScale;
    }

    private void ArrangeImages()
    {
        PreviewGrid.Children.Clear();
        PreviewGrid.RowDefinitions.Clear();
        PreviewGrid.ColumnDefinitions.Clear();

        int rowCount = Math.Min(_rowsPerColumn, Math.Max(1, _previewImages.Count));
        int columnCount = Math.Max(1, (int)Math.Ceiling(_previewImages.Count / (double)_rowsPerColumn));

        for (int row = 0; row < rowCount; row++)
        {
            PreviewGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        for (int column = 0; column < columnCount; column++)
        {
            PreviewGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        }

        for (int i = 0; i < _previewImages.Count; i++)
        {
            int row = i % _rowsPerColumn;
            int column = i / _rowsPerColumn;
            Image image = _previewImages[i];
            Grid.SetRow(image, row);
            Grid.SetColumn(image, column);
            PreviewGrid.Children.Add(image);
        }
    }
}
