namespace HylianGrimoire.TitleText;

internal static class TitleTextCodec
{
    internal static (string Text, string TextClean, int GapAfterIndex) ParseInput(
        string text,
        int maxCharacters,
        int defaultGapAfterIndex)
    {
        text = text.Trim().ToUpperInvariant();
        int spaces = text.Count(char.IsWhiteSpace);
        if (spaces > 1)
        {
            throw new InvalidDataException("Use at most one space to control the title-text gap.");
        }

        int gapAfterIndex = defaultGapAfterIndex;
        string textClean = text;
        if (spaces == 1)
        {
            int gapIndex = text.IndexOf(' ');
            if (gapIndex <= 0)
            {
                throw new InvalidDataException("The gap must come after at least one character.");
            }

            gapAfterIndex = gapIndex - 1;
            textClean = text.Remove(gapIndex, 1);
        }

        if (textClean.Length == 0)
        {
            throw new InvalidDataException("Title text must contain at least one character.");
        }

        if (textClean.Length > maxCharacters)
        {
            throw new InvalidDataException($"Title text can contain at most {maxCharacters} visible characters.");
        }

        if (spaces == 0)
        {
            gapAfterIndex = textClean.Length - 1;
        }

        foreach (char ch in textClean)
        {
            if (ch is < 'A' or > 'Z')
            {
                throw new InvalidDataException("Title text supports A-Z characters only.");
            }
        }

        return (text, textClean, gapAfterIndex);
    }

    internal static (string CleanText, int[] GapAfterIndexes) ParseLocalizedInput(
        string text,
        TitleTextLocalizedLineProfile profile)
    {
        text = text.Trim().ToUpperInvariant();
        var clean = new System.Text.StringBuilder(text.Length);
        var gapAfterIndexes = new List<int>(profile.DefaultGapAfterIndexes.Length);

        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];
            if (char.IsWhiteSpace(ch))
            {
                if (clean.Length == 0 || gapAfterIndexes.Count >= profile.DefaultGapAfterIndexes.Length)
                {
                    throw new InvalidDataException(GetLocalizedGapError(profile));
                }

                int gapAfterIndex = clean.Length - 1;
                if (gapAfterIndexes.Count > 0 && gapAfterIndexes[^1] == gapAfterIndex)
                {
                    throw new InvalidDataException(GetLocalizedGapError(profile));
                }

                gapAfterIndexes.Add(gapAfterIndex);
                continue;
            }

            if (!CanEncodeLocalizedCharacter(ch, profile))
            {
                throw new InvalidDataException(profile.LanguageIndex == 1
                    ? "Title text supports A-Z and \u00dc for this language."
                    : "Title text supports A-Z characters only.");
            }

            clean.Append(ch);
            if (clean.Length > profile.MaxVisibleCharacters)
            {
                throw new InvalidDataException($"Title text can contain at most {profile.MaxVisibleCharacters} visible characters.");
            }
        }

        if (clean.Length == 0)
        {
            throw new InvalidDataException("Title text must contain at least one character.");
        }

        return (clean.ToString(), gapAfterIndexes.ToArray());
    }

    internal static string DecodeText(ReadOnlySpan<byte> encoded, int fontBase)
    {
        Span<char> chars = encoded.Length <= 128 ? stackalloc char[encoded.Length] : new char[encoded.Length];
        for (int i = 0; i < encoded.Length; i++)
        {
            int letter = encoded[i] - fontBase;
            chars[i] = letter is >= 0 and < 26 ? (char)('A' + letter) : '?';
        }

        return new string(chars);
    }

    internal static byte[] EncodeText(string text, int fontBase)
    {
        byte[] output = new byte[text.Length];
        for (int i = 0; i < text.Length; i++)
        {
            output[i] = checked((byte)(text[i] - 'A' + fontBase));
        }

        return output;
    }

    internal static char DecodeLocalizedCharacter(byte value)
    {
        if (value is >= 0x0A and <= 0x23)
        {
            return (char)('A' + value - 0x0A);
        }

        return value == 0x57 ? '\u00dc' : '?';
    }

    internal static byte[] EncodeLocalizedText(string text)
    {
        byte[] output = new byte[text.Length];
        for (int i = 0; i < text.Length; i++)
        {
            output[i] = EncodeLocalizedCharacter(text[i]);
        }

        return output;
    }

    internal static byte EncodeLocalizedCharacter(char ch)
    {
        if (ch is >= 'A' and <= 'Z')
        {
            return checked((byte)(0x0A + ch - 'A'));
        }

        if (ch == '\u00dc')
        {
            return 0x57;
        }

        throw new InvalidDataException("Title text contains a character that cannot be encoded.");
    }

    internal static byte GetLocalizedPreviewGlyph(byte encoded) =>
        encoded == 0x57
            ? (byte)0x95
            : checked((byte)('A' + encoded - 0x0A));

    internal static int GetLocalizedBaseWidth(TitleTextLocalizedLineProfile profile, byte encoded)
    {
        int width = int.MaxValue;
        for (int i = 0; i < profile.DefaultString.Length && i < profile.DefaultWidths.Length; i++)
        {
            if (profile.DefaultString[i] != encoded)
            {
                continue;
            }

            int candidate = profile.DefaultWidths[i];
            if (profile.DefaultGapAfterIndexes.Contains(i))
            {
                candidate -= 5;
            }

            if (candidate > 0)
            {
                width = Math.Min(width, candidate);
            }
        }

        return width == int.MaxValue ? 7 : width;
    }

    internal static int GetLocalizedGapExtraWidth(TitleTextLocalizedLineProfile profile, byte encoded)
    {
        int baseWidth = GetLocalizedBaseWidth(profile, encoded);
        int extra = 5;
        for (int i = 0; i < profile.DefaultString.Length && i < profile.DefaultWidths.Length; i++)
        {
            if (profile.DefaultString[i] == encoded && profile.DefaultGapAfterIndexes.Contains(i))
            {
                extra = Math.Max(extra, profile.DefaultWidths[i] - baseWidth);
            }
        }

        return extra;
    }

    private static string GetLocalizedGapError(TitleTextLocalizedLineProfile profile)
    {
        int maxSpaces = profile.DefaultGapAfterIndexes.Length;
        return maxSpaces == 1
            ? "Use at most one space to control the title-text gap."
            : $"Use at most {maxSpaces} spaces to control the title-text gaps.";
    }

    private static bool CanEncodeLocalizedCharacter(char ch, TitleTextLocalizedLineProfile profile) =>
        ch is >= 'A' and <= 'Z' || (profile.LanguageIndex == 1 && ch == '\u00dc');
}
