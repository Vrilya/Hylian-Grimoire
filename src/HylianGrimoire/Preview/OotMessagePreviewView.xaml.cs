using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using HylianGrimoire.Codecs;
using HylianGrimoire.Glyphs;
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
    private readonly List<PreviewImageSlot> _previewSlots = [];

    public OotMessagePreviewView()
    {
        InitializeComponent();
        Render(OotPreviewStyle.Black, Array.Empty<MessageToken>(), showAlignmentGuides: false);
    }

    public void SetZoom(double zoomScale)
    {
        _zoomScale = zoomScale;
        foreach (PreviewImageSlot slot in _previewSlots)
        {
            ApplyImageSize(slot);
        }
    }

    public void SetRowsPerColumn(int rowsPerColumn)
    {
        _rowsPerColumn = Math.Max(1, rowsPerColumn);
        ArrangeImages();
    }

    public void Render(
        OotPreviewStyle style,
        IReadOnlyList<MessageToken> messageTokens,
        bool showAlignmentGuides,
        IGlyphSource? glyphSource = null,
        MessageEncodingProfile? encodingProfile = null)
    {
        glyphSource ??= OotGlyphSources.OriginalAssets;
        encodingProfile ??= MessageEncodingProfile.Default;
        bool darkText = style == OotPreviewStyle.NoneDarkText;
        var pages = OotPreviewTextPage.FromMessageTokensPages(messageTokens, encodingProfile);

        bool imageCountChanged = EnsurePreviewSlotCount(pages.Count);
        for (int i = 0; i < pages.Count; i++)
        {
            PreviewImageSlot slot = _previewSlots[i];
            slot.Tag = style;
            slot.SetSource(OotBitmapCache.RenderPreview(style, pages[i], darkText, i == pages.Count - 1, showAlignmentGuides, glyphSource));
            ApplyImageSize(slot);
        }

        if (imageCountChanged)
        {
            ArrangeImages();
        }
    }

    private bool EnsurePreviewSlotCount(int count)
    {
        bool changed = false;
        while (_previewSlots.Count < count)
        {
            _previewSlots.Add(new PreviewImageSlot());
            changed = true;
        }

        while (_previewSlots.Count > count)
        {
            _previewSlots.RemoveAt(_previewSlots.Count - 1);
            changed = true;
        }

        return changed;
    }

    private void ApplyImageSize(PreviewImageSlot slot)
    {
        bool credits = slot.Tag is OotPreviewStyle.Credits;
        slot.SetSize(
            (credits ? CreditsBaseWidth : BaseWidth) * _zoomScale,
            (credits ? CreditsBaseHeight : BaseHeight) * _zoomScale);
    }

    private void ArrangeImages()
    {
        PreviewGrid.Children.Clear();
        PreviewGrid.RowDefinitions.Clear();
        PreviewGrid.ColumnDefinitions.Clear();

        int rowCount = Math.Min(_rowsPerColumn, Math.Max(1, _previewSlots.Count));
        int columnCount = Math.Max(1, (int)Math.Ceiling(_previewSlots.Count / (double)_rowsPerColumn));

        for (int row = 0; row < rowCount; row++)
        {
            PreviewGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        for (int column = 0; column < columnCount; column++)
        {
            PreviewGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        }

        for (int i = 0; i < _previewSlots.Count; i++)
        {
            int row = i % _rowsPerColumn;
            int column = i / _rowsPerColumn;
            Grid root = _previewSlots[i].Root;
            Grid.SetRow(root, row);
            Grid.SetColumn(root, column);
            PreviewGrid.Children.Add(root);
        }
    }
}
