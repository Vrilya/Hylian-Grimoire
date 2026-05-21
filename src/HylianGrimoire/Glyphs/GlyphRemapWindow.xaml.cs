using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using HylianGrimoire.Interop;
using HylianGrimoire.Models;
using HylianGrimoire.Services;

namespace HylianGrimoire.Glyphs;

public sealed partial class GlyphRemapWindow : Window
{
    private readonly IReadOnlyList<MessageEntry> _entries;
    private readonly IOotGlyphSource _glyphSource;
    private readonly Func<byte, byte, int> _apply;
    private readonly List<GlyphRemapItem> _items;

    public GlyphRemapWindow(
        IReadOnlyList<MessageEntry> entries,
        IOotGlyphSource glyphSource,
        Func<byte, byte, int> apply)
    {
        InitializeComponent();
        _entries = entries;
        _glyphSource = glyphSource;
        _apply = apply;
        _items = Enumerable
            .Range(MessageGlyphRemapper.FirstGlyph, MessageGlyphRemapper.LastGlyph - MessageGlyphRemapper.FirstGlyph + 1)
            .Select(value => new GlyphRemapItem((byte)value))
            .ToList();

        SourceCombo.ItemsSource = _items;
        TargetCombo.ItemsSource = _items;
        SourceCombo.SelectedIndex = 0;
        TargetCombo.SelectedIndex = Math.Min(_items.Count - 1, 1);

        SystemBackdrop = new MicaBackdrop();
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.IsResizable = false;
        }

        AppWindow.Resize(new Windows.Graphics.SizeInt32(640, 545));
        WindowSizeLimits.SetFixedSize(this, 640, 545);
        WindowIcon.Apply(this);
        AppWindow.TitleBar.ResetToDefault();
        WindowTheme.Register(this);
        UpdateView();
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateView();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnApply(object sender, RoutedEventArgs e)
    {
        if (SourceCombo.SelectedItem is not GlyphRemapItem source
            || TargetCombo.SelectedItem is not GlyphRemapItem target
            || source.Value == target.Value)
        {
            return;
        }

        _ = _apply(source.Value, target.Value);
        Close();
    }

    private void UpdateView()
    {
        if (SourceCombo.SelectedItem is not GlyphRemapItem source
            || TargetCombo.SelectedItem is not GlyphRemapItem target)
        {
            return;
        }

        int sourceCount = MessageGlyphRemapper.CountOccurrences(_entries, source.Value);
        int targetCount = MessageGlyphRemapper.CountOccurrences(_entries, target.Value);
        SourceCountText.Text = $"{sourceCount} occurrences";
        TargetCountText.Text = $"{targetCount} occurrences";
        SourceImage.Source = LoadGlyphImage(source.Value);
        TargetImage.Source = LoadGlyphImage(target.Value);
        ApplyButton.IsEnabled = source.Value != target.Value && sourceCount > 0;
    }

    private BitmapImage? LoadGlyphImage(byte value)
    {
        string path = _glyphSource.GetGlyphPath(value);
        return File.Exists(path) ? new BitmapImage(new Uri(path)) : null;
    }

    private sealed class GlyphRemapItem(byte value)
    {
        public byte Value { get; } = value;

        public string Label => $"0x{Value:X2}  {MessageGlyphRemapper.GetDisplayChar(Value)}";
    }
}
