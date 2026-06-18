using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HylianGrimoire.Glyphs;

internal sealed class GlyphListItem : INotifyPropertyChanged
{
    private string _hex = string.Empty;
    private string _currentChar = string.Empty;
    private string _defaultChar = string.Empty;
    private string _currentWidth = string.Empty;
    private string _overrideLabel = string.Empty;

    public GlyphListItem(GlyphInfo info)
    {
        Value = info.Value;
        Update(info);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public byte Value { get; }

    public string Hex
    {
        get => _hex;
        private set => SetField(ref _hex, value);
    }

    public string CurrentChar
    {
        get => _currentChar;
        private set => SetField(ref _currentChar, value);
    }

    public string DefaultChar
    {
        get => _defaultChar;
        private set => SetField(ref _defaultChar, value);
    }

    public string CurrentWidth
    {
        get => _currentWidth;
        private set => SetField(ref _currentWidth, value);
    }

    public string OverrideLabel
    {
        get => _overrideLabel;
        private set => SetField(ref _overrideLabel, value);
    }

    public void Update(GlyphInfo info)
    {
        Hex = info.Hex;
        CurrentChar = info.CurrentChar.ToString();
        DefaultChar = $"Def {info.DefaultChar}";
        CurrentWidth = info.CurrentWidth.ToString("0.##");
        OverrideLabel = info.HasDisplayOverride || info.HasWidthOverride || info.HasImageOverride
            ? "Custom"
            : string.Empty;
    }

    private void SetField(ref string field, string value, [CallerMemberName] string? propertyName = null)
    {
        if (field == value)
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
