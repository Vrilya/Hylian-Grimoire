using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
    private readonly List<PreviewImageSlot> _previewSlots = [];

    public MmMessagePreviewView()
    {
        InitializeComponent();
        Render(MmPreviewStyle.Black, string.Empty, MmPreviewRenderOptions.Default, showAlignmentGuides: false, MmGlyphSources.Assets, MessageEncodingProfile.MajorasMask);
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

        bool imageCountChanged = EnsurePreviewSlotCount(pages.Count);
        for (int i = 0; i < pages.Count; i++)
        {
            PreviewImageSlot slot = _previewSlots[i];
            slot.Tag = style;
            slot.SetSource(MmBitmapCache.RenderPreview(style, pages[i], i == pages.Count - 1, options, showAlignmentGuides, glyphSource));
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
        bool staffCredits = slot.Tag is MmPreviewStyle.StaffCredits;
        slot.SetSize(
            (staffCredits ? StaffCreditsBaseWidth : BaseWidth) * _zoomScale,
            (staffCredits ? StaffCreditsBaseHeight : BaseHeight) * _zoomScale);
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
