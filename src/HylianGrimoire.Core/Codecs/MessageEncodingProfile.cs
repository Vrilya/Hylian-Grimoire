using System.Text;
using HylianGrimoire.Games;
using HylianGrimoire.Glyphs;

namespace HylianGrimoire.Codecs;

public sealed class MessageEncodingProfile
{
    public static MessageEncodingProfile Default { get; } = new(
        useCharacterProfiles: true,
        gameKind: GameKind.OcarinaOfTime,
        CreateOotEditorChars(),
        CreateOotHeaderChars());

    public static MessageEncodingProfile Original { get; } = new(
        useCharacterProfiles: false,
        gameKind: GameKind.OcarinaOfTime,
        CreateOotEditorChars(),
        CreateOotHeaderChars());

    public static MessageEncodingProfile MajorasMask { get; } = new(
        useCharacterProfiles: true,
        gameKind: GameKind.MajorasMask,
        CreateMajorasMaskEditorChars());

    public static MessageEncodingProfile MajorasMaskOriginal { get; } = new(
        useCharacterProfiles: false,
        gameKind: GameKind.MajorasMask,
        CreateMajorasMaskEditorChars());

    private readonly bool _useCharacterProfiles;
    private readonly GameKind _gameKind;
    private readonly IReadOnlyDictionary<byte, char> _editorChars;
    private readonly IReadOnlyDictionary<char, byte> _editorBytes;
    private readonly IReadOnlyDictionary<byte, string> _headerChars;
    private readonly IReadOnlyDictionary<string, byte> _headerBytes;
    private readonly CharacterProfileSnapshot? _characterProfileSnapshot;

    private MessageEncodingProfile(
        bool useCharacterProfiles,
        GameKind gameKind,
        IReadOnlyDictionary<byte, char> editorChars,
        IReadOnlyDictionary<byte, string>? headerChars = null,
        CharacterProfileSnapshot? characterProfileSnapshot = null)
    {
        _useCharacterProfiles = useCharacterProfiles;
        _gameKind = gameKind;
        _editorChars = editorChars;
        _editorBytes = DictionaryMaps.Reverse(_editorChars);
        _headerChars = headerChars ?? editorChars.ToDictionary(pair => pair.Key, pair => pair.Value.ToString());
        _headerBytes = DictionaryMaps.Reverse(_headerChars);
        _characterProfileSnapshot = characterProfileSnapshot;
    }

    private static IReadOnlyDictionary<byte, char> CreateOotEditorChars()
    {
        return new Dictionary<byte, char>
        {
            { 0x80, 'À' }, { 0x81, 'î' }, { 0x82, 'Â' }, { 0x83, 'Ä' }, { 0x84, 'Ç' },
            { 0x85, 'È' }, { 0x86, 'É' }, { 0x87, 'Ê' }, { 0x88, 'Ë' }, { 0x89, 'Ï' },
            { 0x8a, 'Ô' }, { 0x8b, 'Ö' }, { 0x8c, 'Ù' }, { 0x8d, 'Û' }, { 0x8e, 'Ü' },
            { 0x8f, 'ß' }, { 0x90, 'à' }, { 0x91, 'á' }, { 0x92, 'â' }, { 0x93, 'ä' },
            { 0x94, 'ç' }, { 0x95, 'è' }, { 0x96, 'é' }, { 0x97, 'ê' }, { 0x98, 'ë' },
            { 0x99, 'ï' }, { 0x9a, 'ô' }, { 0x9b, 'ö' }, { 0x9c, 'ù' }, { 0x9d, 'û' },
            { 0x9e, 'ü' },
        };
    }

    private static IReadOnlyDictionary<byte, string> CreateOotHeaderChars()
    {
        return CreateOotEditorChars().ToDictionary(pair => pair.Key, pair => pair.Value.ToString());
    }

    private static IReadOnlyDictionary<byte, char> CreateMajorasMaskEditorChars()
    {
        return new Dictionary<byte, char>
        {
            { 0x5c, '¥' }, { 0x7f, 'º' },
            { 0x80, 'À' }, { 0x81, 'Á' }, { 0x82, 'Â' }, { 0x83, 'Ä' }, { 0x84, 'Ç' },
            { 0x85, 'È' }, { 0x86, 'É' }, { 0x87, 'Ê' }, { 0x88, 'Ë' }, { 0x89, 'Ì' },
            { 0x8a, 'Í' }, { 0x8b, 'Î' }, { 0x8c, 'Ï' }, { 0x8d, 'Ñ' }, { 0x8e, 'Ò' },
            { 0x8f, 'Ó' }, { 0x90, 'Ô' }, { 0x91, 'Ö' }, { 0x92, 'Ù' }, { 0x93, 'Ú' },
            { 0x94, 'Û' }, { 0x95, 'Ü' }, { 0x96, 'β' }, { 0x97, 'à' }, { 0x98, 'á' },
            { 0x99, 'â' }, { 0x9a, 'ä' }, { 0x9b, 'ç' }, { 0x9c, 'è' }, { 0x9d, 'é' },
            { 0x9e, 'ê' }, { 0x9f, 'ë' }, { 0xa0, 'ì' }, { 0xa1, 'í' }, { 0xa2, 'î' },
            { 0xa3, 'ï' }, { 0xa4, 'ñ' }, { 0xa5, 'ò' }, { 0xa6, 'ó' }, { 0xa7, 'ô' },
            { 0xa8, 'ö' }, { 0xa9, 'ù' }, { 0xaa, 'ú' }, { 0xab, 'û' }, { 0xac, 'ü' },
            { 0xad, '¡' }, { 0xae, '¿' }, { 0xaf, 'ª' },
        };
    }

    public bool TryGetEditorChar(byte value, out char ch)
    {
        if (TryGetProfileDisplayChar(value, out ch))
        {
            return true;
        }

        return _editorChars.TryGetValue(value, out ch);
    }

    public bool TryGetByte(char ch, out byte value)
    {
        if (TryGetProfileByte(ch, out value))
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
        if (_headerChars.TryGetValue(value, out string? text))
        {
            return text;
        }

        return value is >= 0x20 and <= 0x7e ? ((char)value).ToString() : string.Empty;
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

    public MessageEncodingProfile WithCharacterProfileSnapshot(CharacterProfileSnapshot snapshot)
    {
        if (!_useCharacterProfiles)
        {
            return this;
        }

        if (snapshot.GameKind != _gameKind)
        {
            throw new InvalidOperationException(
                $"Cannot use a {snapshot.GameKind} character profile snapshot with a {_gameKind} encoding profile.");
        }

        return new MessageEncodingProfile(
            _useCharacterProfiles,
            _gameKind,
            _editorChars,
            _headerChars,
            snapshot);
    }

    private bool TryGetProfileDisplayChar(byte value, out char ch)
    {
        if (!CanUseCharacterProfile())
        {
            ch = default;
            return false;
        }

        return _characterProfileSnapshot!.TryGetDisplayChar(value, out ch);
    }

    private bool TryGetProfileByte(char ch, out byte value)
    {
        if (!CanUseCharacterProfile())
        {
            value = default;
            return false;
        }

        return _characterProfileSnapshot!.TryGetByte(ch, out value);
    }

    private bool CanUseCharacterProfile()
    {
        if (!_useCharacterProfiles)
        {
            return false;
        }

        return _characterProfileSnapshot is not null;
    }

}
