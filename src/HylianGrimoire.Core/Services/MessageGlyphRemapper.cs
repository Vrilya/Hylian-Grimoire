using System.Text;
using HylianGrimoire.Codecs;
using HylianGrimoire.Models;

namespace HylianGrimoire.Services;

public static class MessageGlyphRemapper
{
    public const byte FirstGlyph = 0x20;
    public const byte LastGlyph = 0x9e;

    public static int CountOccurrences(
        IEnumerable<MessageEntry> entries,
        byte value,
        MessageEncodingProfile? encodingProfile = null)
    {
        encodingProfile ??= MessageEncodingProfile.Default;
        return entries.Sum(entry => CountOccurrences(entry.Text, value, encodingProfile));
    }

    public static int Replace(
        IEnumerable<MessageEntry> entries,
        byte source,
        byte target,
        MessageEncodingProfile? encodingProfile = null)
    {
        if (source == target)
        {
            return 0;
        }

        encodingProfile ??= MessageEncodingProfile.Default;
        int total = 0;
        foreach (MessageEntry entry in entries)
        {
            string remapped = Replace(entry.Text, source, target, encodingProfile, out int replacements);
            if (replacements == 0)
            {
                continue;
            }

            entry.Text = remapped;
            total += replacements;
        }

        return total;
    }

    public static string GetDisplayChar(byte value, MessageEncodingProfile? encodingProfile = null)
    {
        encodingProfile ??= MessageEncodingProfile.Default;
        return TryGetEditorChar(value, encodingProfile, out char ch) ? ch.ToString() : string.Empty;
    }

    private static int CountOccurrences(string editorText, byte value, MessageEncodingProfile encodingProfile)
    {
        int count = 0;
        for (int i = 0; i < editorText.Length;)
        {
            if (TrySkipEditorTag(editorText, ref i))
            {
                continue;
            }

            char ch = editorText[i++];
            if (TryGetByte(ch, encodingProfile, out byte current) && current == value)
            {
                count++;
            }
        }

        return count;
    }

    private static string Replace(
        string editorText,
        byte source,
        byte target,
        MessageEncodingProfile encodingProfile,
        out int replacements)
    {
        replacements = 0;
        if (!TryGetEditorChar(target, encodingProfile, out char targetChar))
        {
            return editorText;
        }

        var result = new StringBuilder(editorText.Length);
        for (int i = 0; i < editorText.Length;)
        {
            if (TryCopyEditorTag(editorText, ref i, result))
            {
                continue;
            }

            char ch = editorText[i++];
            if (TryGetByte(ch, encodingProfile, out byte value) && value == source)
            {
                result.Append(targetChar);
                replacements++;
            }
            else
            {
                result.Append(ch);
            }
        }

        return replacements == 0 ? editorText : result.ToString();
    }

    private static bool TrySkipEditorTag(string text, ref int index)
    {
        if (text[index] != '[')
        {
            return false;
        }

        int end = text.IndexOf(']', index + 1);
        if (end < 0)
        {
            return false;
        }

        index = end + 1;
        return true;
    }

    private static bool TryCopyEditorTag(string text, ref int index, StringBuilder result)
    {
        if (text[index] != '[')
        {
            return false;
        }

        int end = text.IndexOf(']', index + 1);
        if (end < 0)
        {
            return false;
        }

        result.Append(text, index, end - index + 1);
        index = end + 1;
        return true;
    }

    private static bool TryGetByte(char ch, MessageEncodingProfile encodingProfile, out byte value)
    {
        if (encodingProfile.TryGetByte(ch, out value))
        {
            return true;
        }

        if (ch is >= (char)FirstGlyph and <= (char)LastGlyph)
        {
            value = (byte)ch;
            return true;
        }

        value = 0;
        return false;
    }

    private static bool TryGetEditorChar(byte value, MessageEncodingProfile encodingProfile, out char ch)
    {
        if (encodingProfile.TryGetEditorChar(value, out ch))
        {
            return true;
        }

        if (value is >= FirstGlyph and <= LastGlyph)
        {
            ch = (char)value;
            return true;
        }

        ch = default;
        return false;
    }
}
