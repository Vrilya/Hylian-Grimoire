namespace HylianGrimoire.Models;

using HylianGrimoire.Services;

/// <summary>
/// Represents a single decoded message entry.
/// </summary>
public class MessageEntry
{
    private string _text = string.Empty;
    private string? _displayTextCache;
    private IEditorTextSyntax? _displayTextSyntaxCacheKey;

    public int Id { get; set; }
    public int Type { get; set; }
    public int Position { get; set; }
    public int Bank { get; set; }
    public int Offset { get; set; }
    public int TableEndMarkerId { get; set; } = 0xfffd;
    public bool TableHasFinalEndMarker { get; set; } = true;
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
    public byte[]? EncodedBytesOverride { get; set; }
    public byte[]? OriginalTrailingMessageData { get; set; }
    public int? OriginalMessageDataSize { get; set; }
    public int? OriginalFinalTableEndMarkerBank { get; set; }
    public int? OriginalFinalTableEndMarkerOffset { get; set; }
    public object? OriginalCodecMetadata { get; set; }
    public object? CodecMetadata { get; set; }

    public MessageEntry(int id, int type, int position, int bank, int offset)
    {
        Id = id;
        Type = type;
        Position = position;
        Bank = bank;
        Offset = offset;
    }

    public string Label() => $"0x{Id:x4}";

    public string GetDisplayText(IEditorTextSyntax syntax)
    {
        if (_displayTextCache is null || !ReferenceEquals(_displayTextSyntaxCacheKey, syntax))
        {
            _displayTextSyntaxCacheKey = syntax;
            _displayTextCache = syntax.ToDisplay(Text);
        }

        return _displayTextCache;
    }

    public void InvalidateDisplayTextCache()
    {
        _displayTextCache = null;
        _displayTextSyntaxCacheKey = null;
    }

    public bool HasUnchangedEncodedBytes()
    {
        return OriginalEncodedBytes is not null
            && string.Equals(Text, OriginalText, StringComparison.Ordinal);
    }
}
