using System.Text;
using HylianGrimoire.Codecs;
using HylianGrimoire.Models;

namespace HylianGrimoire.Services;

public static class MessageGlyphRemapper
{
    public const byte FirstGlyph = 0x20;
    public const byte LastGlyph = 0x9e;

    public static int CountOccurrences(IEnumerable<MessageEntry> entries, byte value)
    {
        return entries.Sum(entry => CountOccurrences(entry.Text, value));
    }

    public static int Replace(IEnumerable<MessageEntry> entries, byte source, byte target)
    {
        if (source == target)
        {
            return 0;
        }

        int total = 0;
        foreach (MessageEntry entry in entries)
        {
            string remapped = Replace(entry.Text, source, target, out int replacements);
            if (replacements == 0)
            {
                continue;
            }

            entry.Text = remapped;
            total += replacements;
        }

        return total;
    }

    public static string GetDisplayChar(byte value)
    {
        return TryGetEditorChar(value, out char ch) ? ch.ToString() : string.Empty;
    }

    private static int CountOccurrences(string editorText, byte value)
    {
        int count = 0;
        foreach (MessageToken token in MessageTextSyntax.FromEditorText(editorText))
        {
            if (token is TextToken text)
            {
                count += text.Text.Count(ch => TryGetByte(ch, out byte current) && current == value);
            }
        }

        return count;
    }

    private static string Replace(string editorText, byte source, byte target, out int replacements)
    {
        replacements = 0;
        if (!TryGetEditorChar(target, out char targetChar))
        {
            return editorText;
        }

        List<MessageToken> tokens = MessageTextSyntax.FromEditorText(editorText);
        var remapped = new List<MessageToken>(tokens.Count);
        foreach (MessageToken token in tokens)
        {
            if (token is not TextToken text)
            {
                remapped.Add(token);
                continue;
            }

            string replacedText = ReplaceText(text.Text, source, targetChar, ref replacements);
            remapped.Add(new TextToken(replacedText));
        }

        return replacements == 0 ? editorText : MessageTextSyntax.ToEditorText(remapped);
    }

    private static string ReplaceText(string text, byte source, char targetChar, ref int replacements)
    {
        var result = new StringBuilder(text.Length);
        foreach (char ch in text)
        {
            if (TryGetByte(ch, out byte value) && value == source)
            {
                result.Append(targetChar);
                replacements++;
            }
            else
            {
                result.Append(ch);
            }
        }

        return result.ToString();
    }

    private static bool TryGetByte(char ch, out byte value)
    {
        if (MessageEncodingProfile.Default.TryGetByte(ch, out value))
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

    private static bool TryGetEditorChar(byte value, out char ch)
    {
        if (MessageEncodingProfile.Default.TryGetEditorChar(value, out ch))
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
