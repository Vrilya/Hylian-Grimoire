using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace HylianGrimoire.Glyphs;

public sealed partial class CharacterProfileWindow
{
    private void RefreshProfileAndGlyphViews()
    {
        ReloadProfiles();
        ReloadGlyphs();
        ShowSelectedGlyph();
    }

    private void ShowSelectedGlyph()
    {
        if (_selectedValue is byte value)
        {
            ShowGlyph(value);
        }
    }

    private void ReloadGlyphs()
    {
        byte? selected = _selectedValue;
        CharacterProfileSnapshot snapshot = CreateCharacterProfileSnapshot();
        _glyphs.Clear();
        foreach (byte value in GameGlyphCatalog.GetGlyphValues(GlyphGameKind))
        {
            GlyphInfo info = GetGlyphInfo(value, snapshot);
            _glyphs.Add(new GlyphListItem(info));
        }

        if (selected is byte selectedValue)
        {
            GlyphList.SelectedItem = _glyphs.FirstOrDefault(item => item.Value == selectedValue);
        }
    }

    private void OnGlyphSelected(object sender, SelectionChangedEventArgs e)
    {
        if (GlyphList.SelectedItem is not GlyphListItem item)
        {
            return;
        }

        _selectedValue = item.Value;
        ShowGlyph(item.Value);
    }

    private void ShowGlyph(byte value)
    {
        GlyphInfo info = GetGlyphInfo(value);
        using IDisposable update = BeginUpdate();
        DetailTitle.Text = $"{info.Hex} Glyph";
        DefaultCharText.Text = $"{info.DefaultChar}";
        CurrentCharBox.Text = $"{info.CurrentChar}";
        DefaultWidthText.Text = $"{info.DefaultWidth:0.##}";
        CurrentWidthBox.Value = info.CurrentWidth;
        ImageStatusText.Text = IsRomMode
            ? (info.HasImageOverride ? "Custom" : string.Empty)
            : (info.HasImageOverride ? "Custom image" : "Default image");
        GlyphImage.Source = File.Exists(info.CurrentPath) ? new BitmapImage(new Uri(info.CurrentPath)) : null;
        UpdateProfileControlState();
    }

    private void RefreshSelected()
    {
        if (_selectedValue is not byte value)
        {
            return;
        }

        GlyphInfo info = GetGlyphInfo(value);
        GlyphListItem? item = _glyphs.FirstOrDefault(item => item.Value == value);
        if (item is null)
        {
            ReloadGlyphs();
        }
        else
        {
            item.Update(info);
        }

        ShowGlyph(value);
    }

    private GlyphInfo GetGlyphInfo(byte value)
    {
        return GetGlyphInfo(value, CreateCharacterProfileSnapshot());
    }

    private GlyphInfo GetGlyphInfo(byte value, CharacterProfileSnapshot snapshot)
    {
        return _romSession?.GetGlyphInfo(value, snapshot) ?? GameGlyphCatalog.GetGlyphInfo(GlyphGameKind, value, snapshot);
    }

    private CharacterProfileSnapshot CreateCharacterProfileSnapshot()
    {
        return _characterProfileRuntime.CreateSnapshot(GlyphGameKind);
    }
}
