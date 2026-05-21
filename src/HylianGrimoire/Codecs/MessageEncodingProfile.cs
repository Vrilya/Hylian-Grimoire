using System.Collections.Generic;
using System.Text;
using HylianGrimoire.Glyphs;

namespace HylianGrimoire.Codecs;

public sealed class MessageEncodingProfile
{
    public static MessageEncodingProfile Default { get; } = new(useCharacterProfiles: true);
    public static MessageEncodingProfile Original { get; } = new(useCharacterProfiles: false);

    private readonly bool _useCharacterProfiles;
    private readonly IReadOnlyDictionary<byte, char> _editorChars;
    private readonly IReadOnlyDictionary<char, byte> _editorBytes;
    private readonly IReadOnlyDictionary<byte, string> _headerChars;
    private readonly IReadOnlyDictionary<string, byte> _headerBytes;

    private MessageEncodingProfile(bool useCharacterProfiles)
    {
        _useCharacterProfiles = useCharacterProfiles;
        _editorChars = new Dictionary<byte, char>
        {
            { 0x80, 'À' }, { 0x81, 'î' }, { 0x82, 'Â' }, { 0x83, 'Ä' }, { 0x84, 'Ç' },
            { 0x85, 'È' }, { 0x86, 'É' }, { 0x87, 'Ê' }, { 0x88, 'Ë' }, { 0x89, 'Ï' },
            { 0x8a, 'Ô' }, { 0x8b, 'Ö' }, { 0x8c, 'Ù' }, { 0x8d, 'Û' }, { 0x8e, 'Ü' },
            { 0x8f, 'ß' }, { 0x90, 'à' }, { 0x91, 'á' }, { 0x92, 'â' }, { 0x93, 'ä' },
            { 0x94, 'ç' }, { 0x95, 'è' }, { 0x96, 'é' }, { 0x97, 'ê' }, { 0x98, 'ë' },
            { 0x99, 'ï' }, { 0x9a, 'ô' }, { 0x9b, 'ö' }, { 0x9c, 'ù' }, { 0x9d, 'û' },
            { 0x9e, 'ü' },
        };

        _editorBytes = DictionaryMaps.Reverse(_editorChars);
        _headerChars = new Dictionary<byte, string>
        {
            { 0x80, "À" }, { 0x81, "î" }, { 0x82, "Â" }, { 0x83, "Ä" }, { 0x84, "Ç" },
            { 0x85, "È" }, { 0x86, "É" }, { 0x87, "Ê" }, { 0x88, "Ë" }, { 0x89, "Ï" },
            { 0x8a, "Ô" }, { 0x8b, "Ö" }, { 0x8c, "Ù" }, { 0x8d, "Û" }, { 0x8e, "Ü" },
            { 0x8f, "ß" }, { 0x90, "à" }, { 0x91, "á" }, { 0x92, "â" }, { 0x93, "ä" },
            { 0x94, "ç" }, { 0x95, "è" }, { 0x96, "é" }, { 0x97, "ê" }, { 0x98, "ë" },
            { 0x99, "ï" }, { 0x9a, "ô" }, { 0x9b, "ö" }, { 0x9c, "ù" }, { 0x9d, "û" },
            { 0x9e, "ü" },
        };
        _headerBytes = DictionaryMaps.Reverse(_headerChars);
    }

    public bool TryGetEditorChar(byte value, out char ch)
    {
        if (_useCharacterProfiles && CharacterProfileStore.Current.TryGetDisplayChar(value, out ch))
        {
            return true;
        }

        return _editorChars.TryGetValue(value, out ch);
    }

    public bool TryGetByte(char ch, out byte value)
    {
        if (_useCharacterProfiles && CharacterProfileStore.Current.TryGetByte(ch, out value))
        {
            return true;
        }

        return _editorBytes.TryGetValue(ch, out value);
    }

    public char GetDefaultEditorChar(byte value)
    {
        if (_editorChars.TryGetValue(value, out char ch))
        {
            return ch;
        }

        return value is >= 0x20 and <= 0x7e ? (char)value : ' ';
    }

    public string GetHeaderText(byte value)
    {
        return _headerChars.TryGetValue(value, out string? text) ? text : string.Empty;
    }

    public string ToHeaderText(string editorText)
    {
        var result = new StringBuilder();
        foreach (char ch in editorText)
        {
            if (ch == '"')
            {
                result.Append("\\\"");
            }
            else if (TryGetByte(ch, out byte value))
            {
                result.Append(GetHeaderText(value));
            }
            else
            {
                result.Append(ch);
            }
        }

        return result.ToString();
    }

    public string HeaderTextToEditorText(string headerText)
    {
        var result = new StringBuilder();
        foreach (char ch in headerText)
        {
            string text = ch.ToString();
            if (_headerBytes.TryGetValue(text, out byte value) && TryGetEditorChar(value, out char editorChar))
            {
                result.Append(editorChar);
            }
            else
            {
                result.Append(ch);
            }
        }

        return result.ToString();
    }

}
