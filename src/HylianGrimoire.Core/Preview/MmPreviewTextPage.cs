using HylianGrimoire.Codecs;
using HylianGrimoire.Codecs.MajorasMask;

namespace HylianGrimoire.Preview;

public static class MmPreviewTextPage
{
    public static IReadOnlyList<IReadOnlyList<OotPreviewToken>> FromEditorTextPages(
        string editorText,
        MessageEncodingProfile encodingProfile)
    {
        byte[] encoded = MmMessageTextCodec.Encode(editorText, encodingProfile);
        return FromEncodedPages(encoded, encodingProfile);
    }

    private static IReadOnlyList<IReadOnlyList<OotPreviewToken>> FromEncodedPages(
        byte[] encoded,
        MessageEncodingProfile encodingProfile)
    {
        var pages = new List<IReadOnlyList<OotPreviewToken>>();
        var tokens = new List<OotPreviewToken>();

        for (int i = 0; i < encoded.Length; i++)
        {
            byte value = encoded[i];
            if (value == 0xbf)
            {
                break;
            }

            if (value == 0x12)
            {
                tokens.Add(new OotPreviewToken(OotPreviewTokenKind.BoxBreak2, 0));
                AddPage(pages, tokens);
                tokens = [];
                continue;
            }

            if (value is 0x10 or 0x15)
            {
                AddPage(pages, tokens);
                tokens = [];
                continue;
            }

            if (value is 0x1b)
            {
                i += 2;
                AddPage(pages, tokens);
                tokens = [];
                continue;
            }

            if (MmMessageTokenMaps.ColorTags.ContainsKey(value))
            {
                tokens.Add(new OotPreviewToken(OotPreviewTokenKind.Color, value));
            }
            else if (value == 0x11)
            {
                tokens.Add(new OotPreviewToken(OotPreviewTokenKind.LineBreak, 0));
            }
            else if (value == 0x13)
            {
                tokens.Add(new OotPreviewToken(OotPreviewTokenKind.CarriageReturn, 0));
            }
            else if (value == 0x14)
            {
                if (i + 1 < encoded.Length)
                {
                    tokens.Add(new OotPreviewToken(OotPreviewTokenKind.Shift, encoded[++i]));
                }
            }
            else if (value == 0x16)
            {
                AddStaticText(tokens, "Link", encodingProfile);
            }
            else if (value is 0xc2 or 0xc3)
            {
                tokens.Add(new OotPreviewToken(OotPreviewTokenKind.Choice, value == 0xc2 ? (byte)2 : (byte)3));
            }
            else if (MmMessageTokenMaps.ButtonTags.ContainsKey(value))
            {
                tokens.Add(new OotPreviewToken(OotPreviewTokenKind.Glyph, value));
            }
            else if (MmMessageTokenMaps.OneByteArgumentTags.ContainsKey(value))
            {
                i += 1;
            }
            else if (MmMessageTokenMaps.TwoByteArgumentTags.ContainsKey(value))
            {
                i += 2;
            }
            else if (MmMessageTokenMaps.NoArgumentTags.TryGetValue(value, out string? tag))
            {
                if (tag.Equals("background", StringComparison.OrdinalIgnoreCase))
                {
                    tokens.Add(new OotPreviewToken(OotPreviewTokenKind.Background, 0));
                }
                else
                {
                    AddDynamicPlaceholder(tokens, tag, encodingProfile);
                }
            }
            else if (value is >= 0x20 and <= 0xbb)
            {
                tokens.Add(new OotPreviewToken(OotPreviewTokenKind.Glyph, value));
            }
        }

        AddPage(pages, tokens);
        return pages.Count > 0 ? pages : [Array.Empty<OotPreviewToken>()];
    }

    private static void AddPage(List<IReadOnlyList<OotPreviewToken>> pages, List<OotPreviewToken> tokens)
    {
        if (tokens.Count > 0)
        {
            pages.Add(tokens.ToArray());
        }
    }

    private static void AddStaticText(
        List<OotPreviewToken> tokens,
        string text,
        MessageEncodingProfile encodingProfile)
    {
        foreach (char ch in text)
        {
            if (TryGetGlyphByte(ch, encodingProfile, out byte glyph))
            {
                tokens.Add(new OotPreviewToken(OotPreviewTokenKind.Glyph, glyph));
            }
        }
    }

    private static void AddDynamicPlaceholder(
        List<OotPreviewToken> tokens,
        string tag,
        MessageEncodingProfile encodingProfile)
    {
        string normalizedTag = tag.ToLowerInvariant();
        if (normalizedTag is "spiderhousemask1" or "spiderhousemask2" or "spiderhousemask3"
            or "spiderhousemask4" or "spiderhousemask5" or "spiderhousemask6")
        {
            AddSpiderHouseMaskColorPreview(tokens, encodingProfile);
            return;
        }

        string text = normalizedTag switch
        {
            "hsboatarchery" => "20",
            "timerpostman" => "9\"59",
            "timerminigame2" => "9\"59",
            "time" or "timeuntilnewday" => "00:00",
            "timeuntilmooncrash" => "72:00",
            "chestflags" => "9999",
            "inputbank" => "0 0 0  Rupee(s)",
            "inputdogbet" => "0 0  Rupees",
            "inputbombercode" => "1 2 3 4 5",
            "inputlotterycode" => "1 2 3",
            "rupeesselected" => "500",
            "rupeestotal" => "5000",
            "helditemprice" => "500",
            "tokens" => "99",
            "owlwarp" => "Great Bay Coast",
            "strayfairies" or "fairieswoodfall" or "fairiessnowhead" or "fairiesgreatbay" or "fairiesstonetower" => "15",
            "bombercode" => "12345",
            "lotterycode" or "lotterycodeguess" => "123",
            "pointsboatarchery" => "45",
            "pointstens" or "pointsthousands" => "1000",
            "hoursuntilmooncrash" => "72 hours",
            "hstownshooting" => "50",
            "hsdekuplayground1" or "hsdekuplayground2" or "hsdekuplayground3" => "99'99\"99",
            "hshorseballoon" or "hshorseballoontime" => "99\"99\"99",
            "dekuplaygroundname1" or "dekuplaygroundname2" or "dekuplaygroundname3" => "Link",
            _ => string.Empty,
        };

        AddStaticText(tokens, text, encodingProfile);
    }

    private static void AddSpiderHouseMaskColorPreview(
        List<OotPreviewToken> tokens,
        MessageEncodingProfile encodingProfile)
    {
        tokens.Add(new OotPreviewToken(OotPreviewTokenKind.Color, 0x04));
        AddStaticText(tokens, "YELLOW", encodingProfile);
        tokens.Add(new OotPreviewToken(OotPreviewTokenKind.Color, 0x00));
    }

    private static bool TryGetGlyphByte(char ch, MessageEncodingProfile encodingProfile, out byte value)
    {
        if (ch >= 0x20 && ch <= 0x7e)
        {
            value = (byte)ch;
            return true;
        }

        return encodingProfile.TryGetByte(ch, out value);
    }
}
