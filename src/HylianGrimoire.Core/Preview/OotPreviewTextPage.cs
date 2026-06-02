using System.Text.RegularExpressions;
using HylianGrimoire.Codecs;
using HylianGrimoire.Models;

namespace HylianGrimoire.Preview;

public enum OotPreviewTokenKind
{
    Glyph,
    LineBreak,
    CarriageReturn,
    BoxBreak2,
    Shift,
    Icon,
    Color,
    Center,
    Choice,
    Background,
}

public readonly record struct OotPreviewToken(OotPreviewTokenKind Kind, byte Value);

public static partial class OotPreviewTextPage
{
    public static IReadOnlyList<IReadOnlyList<OotPreviewToken>> FromMessageTokensPages(
        IEnumerable<MessageToken> messageTokens,
        MessageEncodingProfile? encodingProfile = null)
    {
        encodingProfile ??= MessageEncodingProfile.Default;
        var tokens = new List<OotPreviewToken>();
        var pages = new List<IReadOnlyList<OotPreviewToken>>();

        foreach (MessageToken messageToken in messageTokens)
        {
            if (ShouldStartNewPage(messageToken))
            {
                AddPage(pages, tokens);
                tokens = [];
            }
            else
            {
                AddMessageToken(tokens, messageToken, encodingProfile);
            }
        }

        AddPage(pages, tokens);
        return pages.Count > 0 ? pages : [Array.Empty<OotPreviewToken>()];
    }

    public static IReadOnlyList<OotPreviewToken> FromMessageTokens(
        IEnumerable<MessageToken> messageTokens,
        MessageEncodingProfile? encodingProfile = null)
    {
        return FromMessageTokensPages(messageTokens, encodingProfile).FirstOrDefault() ?? [];
    }

    private static void AddStaticText(List<OotPreviewToken> tokens, string text, MessageEncodingProfile encodingProfile)
    {
        foreach (char ch in text)
        {
            if (TryGetGlyphByte(ch, encodingProfile, out byte glyph))
            {
                tokens.Add(new OotPreviewToken(OotPreviewTokenKind.Glyph, glyph));
            }
        }
    }

    private static void AddLineBreak(List<OotPreviewToken> tokens)
    {
        tokens.Add(new OotPreviewToken(OotPreviewTokenKind.LineBreak, 0));
    }

    private static void AddMessageToken(List<OotPreviewToken> tokens, MessageToken messageToken, MessageEncodingProfile encodingProfile)
    {
        switch (messageToken)
        {
            case TextToken text:
                AddStaticText(tokens, text.Text, encodingProfile);
                break;
            case LineBreakToken:
                AddLineBreak(tokens);
                break;
            case ButtonToken button:
                tokens.Add(new OotPreviewToken(OotPreviewTokenKind.Glyph, button.Code));
                break;
            case ShiftToken shift:
                tokens.Add(new OotPreviewToken(OotPreviewTokenKind.Shift, shift.Pixels));
                break;
            case ColorToken color:
                tokens.Add(new OotPreviewToken(OotPreviewTokenKind.Color, GetColorIndex(color.Index)));
                break;
            case IconToken icon:
                tokens.Add(new OotPreviewToken(OotPreviewTokenKind.Icon, icon.Id));
                break;
            case HighscoreToken highscore:
                AddStaticText(tokens, GetMinigamePreviewText(highscore.Id), encodingProfile);
                break;
            case CommandToken command:
                AddCommandToken(tokens, command.Code, encodingProfile);
                break;
        }
    }

    private static void AddCommandToken(List<OotPreviewToken> tokens, byte code, MessageEncodingProfile encodingProfile)
    {
        if (!MessageTokenMaps.CommandTags.TryGetValue(code, out string? tag))
            return;

        if (tag.Equals("center", StringComparison.OrdinalIgnoreCase))
            tokens.Add(new OotPreviewToken(OotPreviewTokenKind.Center, 0));
        else if (tag.Equals("twochoice", StringComparison.OrdinalIgnoreCase))
            tokens.Add(new OotPreviewToken(OotPreviewTokenKind.Choice, 2));
        else if (tag.Equals("threechoice", StringComparison.OrdinalIgnoreCase))
            tokens.Add(new OotPreviewToken(OotPreviewTokenKind.Choice, 3));
        else if (StaticTextTag().IsMatch(tag))
            AddStaticText(tokens, GetStaticText(tag), encodingProfile);
    }

    private static void AddPage(List<IReadOnlyList<OotPreviewToken>> pages, List<OotPreviewToken> tokens)
    {
        if (tokens.Count > 0)
        {
            pages.Add(tokens.ToArray());
        }
    }

    private static bool ShouldStartNewPage(MessageToken token)
    {
        return token is BreakDelayToken
            || token is CommandToken { Command: MessageCommand.Break };
    }

    private static string GetStaticText(string tag) => tag.ToLowerInvariant() switch
    {
        "name" => "Link",
        "points" => "1000",
        "fishinfo" => "10",
        "skulltulas" => "100",
        "marathontime" or "racetime" => "00\"00\"",
        "time" => "00:00",
        _ => string.Empty,
    };

    private static string GetMinigamePreviewText(byte value) => value switch
    {
        0x00 => "1500",
        0x01 => "1000",
        0x02 => "35",
        0x03 => "02:35",
        0x04 => "02:35",
        _ => string.Empty,
    };

    private static byte GetColorIndex(byte value)
    {
        return value is >= 0x40 and <= 0x47 ? (byte)(value - 0x40) : (byte)0;
    }

    private static bool TryGetGlyphByte(char ch, MessageEncodingProfile encodingProfile, out byte value)
    {
        if (ch >= 0x20 && ch <= 0x7f)
        {
            value = (byte)ch;
            return true;
        }

        return encodingProfile.TryGetByte(ch, out value);
    }

    [GeneratedRegex("^(name|points|fishinfo|skulltulas|marathontime|racetime|time)$", RegexOptions.IgnoreCase)]
    private static partial Regex StaticTextTag();
}
