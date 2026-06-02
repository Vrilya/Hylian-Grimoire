using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using HylianGrimoire.Codecs;
using HylianGrimoire.Glyphs;

namespace HylianGrimoire.Preview;

public sealed partial class MmMessagePreviewView : UserControl
{
    private const double BaseWidth = 448;
    private const double BaseHeight = 126;
    private const double StaffCreditsBaseWidth = 560;
    private const double StaffCreditsBaseHeight = 420;
    private double _zoomScale = 1.0;
    private int _rowsPerColumn = 5;
    private readonly List<Image> _previewImages = [];

    public MmMessagePreviewView()
    {
        InitializeComponent();
        Render(MmPreviewStyle.Black, string.Empty, MmPreviewRenderOptions.Default, showAlignmentGuides: false, MmGlyphSources.Assets, MessageEncodingProfile.MajorasMask);
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

    public void Render(
        MmPreviewStyle style,
        string editorText,
        MmPreviewRenderOptions options,
        bool showAlignmentGuides,
        IGlyphSource glyphSource,
        MessageEncodingProfile encodingProfile)
    {
        var pages = style == MmPreviewStyle.StaffCredits
            ? MmStaffCreditsPreviewTextPage.FromEditorTextPages(editorText, encodingProfile)
            : MmPreviewTextPage.FromEditorTextPages(editorText, encodingProfile);

        _previewImages.Clear();
        PreviewGrid.Children.Clear();
        for (int i = 0; i < pages.Count; i++)
        {
            var image = new Image
            {
                Stretch = Stretch.Uniform,
                Tag = style,
                Source = new BitmapImage(MmBitmapCache.RenderPreview(style, pages[i], i == pages.Count - 1, options, showAlignmentGuides, glyphSource)),
            };
            ApplyImageSize(image);
            _previewImages.Add(image);
        }

        ArrangeImages();
    }

    private void ApplyImageSize(Image image)
    {
        bool staffCredits = image.Tag is MmPreviewStyle.StaffCredits;
        image.Width = (staffCredits ? StaffCreditsBaseWidth : BaseWidth) * _zoomScale;
        image.Height = (staffCredits ? StaffCreditsBaseHeight : BaseHeight) * _zoomScale;
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
