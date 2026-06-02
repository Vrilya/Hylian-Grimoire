using System.Text;
using HylianGrimoire.Codecs;
using HylianGrimoire.Games;

namespace HylianGrimoire.Glyphs;

public static class CharacterProfileTextRemapper
{
    public static string Remap(
        GameKind gameKind,
        string editorText,
        CharacterProfile? sourceProfile,
        CharacterProfile? targetProfile)
    {
        MessageEncodingProfile originalEncodingProfile = GameProfiles.GetOriginalEncodingProfile(gameKind);
        var result = new StringBuilder(editorText.Length);
        var textRun = new StringBuilder();
        int index = 0;

        while (index < editorText.Length)
        {
            if (editorText[index] == '[')
            {
                int tagEnd = editorText.IndexOf(']', index + 1);
                if (tagEnd >= 0)
                {
                    AppendRemappedTextRun(result, textRun, sourceProfile, targetProfile, originalEncodingProfile);
                    result.Append(editorText, index, tagEnd - index + 1);
                    index = tagEnd + 1;
                    continue;
                }
            }

            textRun.Append(editorText[index]);
            index++;
        }

        AppendRemappedTextRun(result, textRun, sourceProfile, targetProfile, originalEncodingProfile);
        return result.ToString();
    }

    private static string RemapPlainText(
        string text,
        CharacterProfile? sourceProfile,
        CharacterProfile? targetProfile,
        MessageEncodingProfile originalEncodingProfile)
    {
        var result = new char[text.Length];
        for (int i = 0; i < text.Length; i++)
        {
            result[i] = TryGetByteFromProfile(sourceProfile, text[i], originalEncodingProfile, out byte value)
                ? GetEditorCharFromProfile(targetProfile, value, originalEncodingProfile)
                : text[i];
        }

        return new string(result);
    }

    private static void AppendRemappedTextRun(
        StringBuilder result,
        StringBuilder textRun,
        CharacterProfile? sourceProfile,
        CharacterProfile? targetProfile,
        MessageEncodingProfile originalEncodingProfile)
    {
        if (textRun.Length == 0)
        {
            return;
        }

        result.Append(RemapPlainText(textRun.ToString(), sourceProfile, targetProfile, originalEncodingProfile));
        textRun.Clear();
    }

    private static bool TryGetByteFromProfile(
        CharacterProfile? profile,
        char displayChar,
        MessageEncodingProfile originalEncodingProfile,
        out byte value)
    {
        if (profile is not null)
        {
            string text = displayChar.ToString();
            foreach ((string key, string mappedText) in profile.Characters)
            {
                if (mappedText == text && TryParseKey(key, out value))
                {
                    return true;
                }
            }
        }

        if (originalEncodingProfile.TryGetByte(displayChar, out value))
        {
            return true;
        }

        if (displayChar is >= (char)0x20 and <= (char)0x7e)
        {
            value = (byte)displayChar;
            return true;
        }

        value = 0;
        return false;
    }

    private static char GetEditorCharFromProfile(
        CharacterProfile? profile,
        byte value,
        MessageEncodingProfile originalEncodingProfile)
    {
        if (profile is not null
            && profile.Characters.TryGetValue(ToKey(value), out string? text)
            && text.Length > 0)
        {
            return text[0];
        }

        return originalEncodingProfile.GetDefaultEditorChar(value);
    }

    private static string ToKey(byte value) => $"0x{value:X2}";

    private static bool TryParseKey(string key, out byte value)
    {
        string text = key.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? key[2..] : key;
        return byte.TryParse(text, System.Globalization.NumberStyles.HexNumber, null, out value);
    }
}
