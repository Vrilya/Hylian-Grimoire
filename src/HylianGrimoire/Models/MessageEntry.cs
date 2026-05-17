namespace HylianGrimoire.Models;

using HylianGrimoire.Codecs;

/// <summary>
/// Represents a single message entry parsed from the OoT message table.
/// </summary>
public class MessageEntry
{
    private string _text = string.Empty;
    private string? _displayTextCache;

    public int Id { get; set; }
    public int Type { get; set; }
    public int Position { get; set; }
    public int Bank { get; set; }
    public int Offset { get; set; }
    public int TableEndMarkerId { get; set; } = 0xfffd;
    public bool PreserveOffsetWithoutMessageData { get; set; }
    public string Text
    {
        get => _text;
        set
        {
            if (string.Equals(_text, value, StringComparison.Ordinal))
            {
                return;
            }

            _text = value;
            InvalidateDisplayTextCache();
        }
    }

    // Parsed .bin bytes are preserved only while Text still matches OriginalText.
    // This keeps exact untouched messages intact, including padding/unknown byte details.
    public string? OriginalText { get; set; }
    public byte[]? OriginalEncodedBytes { get; set; }

    public MessageEntry(int id, int type, int position, int bank, int offset)
    {
        Id = id;
        Type = type;
        Position = position;
        Bank = bank;
        Offset = offset;
    }

    public string Label() => $"0x{Id:x4}";

    public string GetDisplayText()
    {
        _displayTextCache ??= MessageTextSyntax.ToDisplay(Text);
        return _displayTextCache;
    }

    public void InvalidateDisplayTextCache()
    {
        _displayTextCache = null;
    }

    public bool HasUnchangedEncodedBytes()
    {
        return OriginalEncodedBytes is not null
            && string.Equals(Text, OriginalText, StringComparison.Ordinal);
    }
}
