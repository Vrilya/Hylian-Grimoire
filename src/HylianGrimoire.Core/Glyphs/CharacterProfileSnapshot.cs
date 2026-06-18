using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using HylianGrimoire.Games;

namespace HylianGrimoire.Glyphs;

public sealed class CharacterProfileSnapshot
{
    private readonly IReadOnlyDictionary<byte, char> _displayChars;
    private readonly IReadOnlyDictionary<char, byte> _displayBytes;
    private readonly IReadOnlyDictionary<byte, double> _widths;
    private readonly IReadOnlyDictionary<byte, string> _imagePaths;

    internal CharacterProfileSnapshot(
        GameKind gameKind,
        string profileName,
        int version,
        IReadOnlyDictionary<byte, char> displayChars,
        IReadOnlyDictionary<byte, double> widths,
        IReadOnlyDictionary<byte, string> imagePaths)
    {
        GameKind = gameKind;
        ProfileName = profileName;
        Version = version;
        _displayChars = Copy(displayChars);
        _displayBytes = CopyReverseFirstWins(displayChars);
        _widths = Copy(widths);
        _imagePaths = Copy(imagePaths);
        CacheKey = CreateCacheKey();
    }

    public GameKind GameKind { get; }

    public string ProfileName { get; }

    public int Version { get; }

    public string CacheKey { get; }

    public IReadOnlyDictionary<byte, char> DisplayChars => _displayChars;

    public IReadOnlyDictionary<byte, double> Widths => _widths;

    public IReadOnlyDictionary<byte, string> ImagePaths => _imagePaths;

    public bool TryGetDisplayChar(byte value, out char displayChar)
        => _displayChars.TryGetValue(value, out displayChar);

    public bool TryGetByte(char displayChar, out byte value)
        => _displayBytes.TryGetValue(displayChar, out value);

    public bool TryGetWidth(byte value, out double width)
        => _widths.TryGetValue(value, out width);

    public bool TryGetImagePath(byte value, out string? path)
    {
        if (_imagePaths.TryGetValue(value, out string? snapshotPath))
        {
            path = snapshotPath;
            return true;
        }

        path = null;
        return false;
    }

    internal static CharacterProfileSnapshot Empty(GameKind gameKind, int version)
        => new(
            gameKind,
            CharacterProfileStore.DefaultProfileName,
            version,
            new Dictionary<byte, char>(),
            new Dictionary<byte, double>(),
            new Dictionary<byte, string>());

    private static IReadOnlyDictionary<TKey, TValue> Copy<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> source)
        where TKey : notnull
        => new ReadOnlyDictionary<TKey, TValue>(source.ToDictionary(pair => pair.Key, pair => pair.Value));

    private static IReadOnlyDictionary<char, byte> CopyReverseFirstWins(IReadOnlyDictionary<byte, char> source)
    {
        var result = new Dictionary<char, byte>();
        foreach ((byte value, char displayChar) in source)
        {
            result.TryAdd(displayChar, value);
        }

        return new ReadOnlyDictionary<char, byte>(result);
    }

    private string CreateCacheKey()
    {
        var text = new StringBuilder();
        text.Append(GameKind).Append('|').Append(Version).Append('|');
        foreach ((byte value, char displayChar) in _displayChars.OrderBy(pair => pair.Key))
        {
            text.Append("c:").Append(value).Append('=').Append((int)displayChar).Append('|');
        }

        foreach ((byte value, double width) in _widths.OrderBy(pair => pair.Key))
        {
            text.Append("w:").Append(value).Append('=').Append(width.ToString("R", System.Globalization.CultureInfo.InvariantCulture)).Append('|');
        }

        foreach ((byte value, string path) in _imagePaths.OrderBy(pair => pair.Key))
        {
            text.Append("i:").Append(value).Append('=').Append(path).Append('|');
        }

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString()));
        return Convert.ToHexString(hash)[..16].ToLowerInvariant();
    }
}
