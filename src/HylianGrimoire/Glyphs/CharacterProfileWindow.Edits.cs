using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HylianGrimoire.Glyphs;

public sealed partial class CharacterProfileWindow
{
    private void OnCurrentCharChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating || _selectedValue is not byte value || CurrentCharBox.Text.Length == 0)
        {
            return;
        }

        GlyphInfo info = GetGlyphInfo(value);
        if (CurrentCharBox.Text[0] == info.CurrentChar)
        {
            return;
        }

        _characterProfileRuntime.SetDisplayChar(value, CurrentCharBox.Text[0]);
        RefreshSelected();
    }

    private void OnCurrentWidthChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_updating || _selectedValue is not byte value || double.IsNaN(sender.Value))
        {
            return;
        }

        double width = Math.Round(sender.Value, 2);
        GlyphInfo info = GetGlyphInfo(value);
        if (Math.Abs(width - info.CurrentWidth) < 0.001)
        {
            return;
        }

        SetSelectedGlyphWidth(value, width, info.DefaultWidth);
        RefreshSelected();
    }

    private async void OnReplaceImage(object sender, RoutedEventArgs e)
    {
        if (_selectedValue is not byte value)
        {
            return;
        }

        string? path = await PickImageAsync();
        if (path is null)
        {
            return;
        }

        bool sameSize;
        try
        {
            sameSize = HasSameSize(path, GameGlyphCatalog.GetOriginalGlyphPath(GlyphGameKind, value));
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or OutOfMemoryException)
        {
            await ShowDialogAsync("Invalid image", "The selected file could not be read as an image.");
            return;
        }

        if (!sameSize)
        {
            await ShowDialogAsync("Image size mismatch", "The replacement image must have the same pixel size as the original glyph.");
            return;
        }

        SetSelectedGlyphImage(value, path);
        RefreshSelected();
    }

    private void OnResetImage(object sender, RoutedEventArgs e)
    {
        if (_selectedValue is not byte value)
        {
            return;
        }

        ResetSelectedGlyphImage(value);
        RefreshSelected();
    }

    private void OnResetWidth(object sender, RoutedEventArgs e)
    {
        if (_selectedValue is not byte value)
        {
            return;
        }

        ResetSelectedGlyphWidth(value);
        RefreshSelected();
    }

    private void OnResetCharacter(object sender, RoutedEventArgs e)
    {
        if (_selectedValue is byte value)
        {
            _characterProfileRuntime.ResetDisplayChar(value);
            RefreshSelected();
        }
    }

    private void SetSelectedGlyphWidth(byte value, double width, double defaultWidth)
    {
        if (_romSession is null)
        {
            _characterProfileRuntime.SetWidth(value, width);
            return;
        }

        _romSession.SetWidth(value, width);
        if (_characterProfileRuntime.CanEditSelectedProfile)
        {
            _characterProfileRuntime.SetWidth(value, width, defaultWidth);
        }
    }

    private void SetSelectedGlyphImage(byte value, string path)
    {
        if (_romSession is null)
        {
            _characterProfileRuntime.SetImage(value, path);
            return;
        }

        _romSession.SetImage(value, path);
        if (_characterProfileRuntime.CanEditSelectedProfile)
        {
            _characterProfileRuntime.SetImage(value, path);
        }
    }

    private void ResetSelectedGlyphImage(byte value)
    {
        if (_romSession is null)
        {
            _characterProfileRuntime.ResetImage(value);
            return;
        }

        _romSession.ResetImage(value);
        if (_characterProfileRuntime.CanEditSelectedProfile)
        {
            _characterProfileRuntime.ResetImage(value);
        }
    }

    private void ResetSelectedGlyphWidth(byte value)
    {
        if (_romSession is null)
        {
            _characterProfileRuntime.ResetWidth(value);
            return;
        }

        _romSession.ResetWidth(value);
        if (_characterProfileRuntime.CanEditSelectedProfile)
        {
            _characterProfileRuntime.ResetWidth(value);
        }
    }
}
