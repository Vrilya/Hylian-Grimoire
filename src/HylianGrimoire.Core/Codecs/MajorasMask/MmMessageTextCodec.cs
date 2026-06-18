using System.Text;
using System.Text.RegularExpressions;

namespace HylianGrimoire.Codecs.MajorasMask;

public static partial class MmMessageTextCodec
{
    public static string Decode(byte[] raw, int startOffset, int byteCount, MessageEncodingProfile encodingProfile)
    {
        var text = new StringBuilder();
        if (startOffset < 0 || startOffset >= raw.Length || byteCount <= 0)
        {
            return string.Empty;
        }

        int end = Math.Min(raw.Length, startOffset + byteCount);

        for (int i = startOffset; i < end; i++)
        {
            byte value = raw[i];
            if (value == 0xbf)
            {
                break;
            }

            if (value == 0x11)
            {
                text.Append('\n');
            }
            else if (MmMessageTokenMaps.ColorTags.TryGetValue(value, out string? color))
            {
                text.Append($"[color:{color}]");
            }
            else if (MmMessageTokenMaps.NoArgumentTags.TryGetValue(value, out string? tag))
            {
                text.Append($"[{tag}]");
            }
            else if (MmMessageTokenMaps.ButtonTags.TryGetValue(value, out string? button))
            {
                text.Append($"[{button}]");
            }
            else if (MmMessageTokenMaps.OneByteArgumentTags.TryGetValue(value, out string? oneByteArgumentName))
            {
                if (i + 1 >= end)
                    break;

                text.Append($"[{oneByteArgumentName}:{raw[++i]:x2}]");
            }
            else if (MmMessageTokenMaps.TwoByteArgumentTags.TryGetValue(value, out string? twoByteArgumentName))
            {
                if (i + 2 >= end)
                    break;

                ushort argument = (ushort)((raw[i + 1] << 8) | raw[i + 2]);
                i += 2;
                text.Append($"[{twoByteArgumentName}:{argument:x4}]");
            }
            else if (encodingProfile.TryGetEditorChar(value, out char special))
            {
                text.Append(special);
            }
            else if (value is >= 0x20 and <= 0x7e)
            {
                text.Append((char)value);
            }
            else
            {
                text.Append($"[byte:{value:x2}]");
            }
        }

        return text.ToString();
    }

    public static byte[] Encode(string editorText, MessageEncodingProfile encodingProfile)
    {
        var output = new List<byte>();
        int i = 0;

        while (i < editorText.Length)
        {
            char ch = editorText[i];
            if (ch == '[')
            {
                int end = editorText.IndexOf(']', i);
                if (end >= 0)
                {
                    string token = editorText[(i + 1)..end];
                    if (TryAppendTag(output, token))
                    {
                        i = end + 1;
                        continue;
                    }
                }
            }

            if (ch == '\n')
            {
                output.Add(0x11);
            }
            else if (encodingProfile.TryGetByte(ch, out byte specialByte))
            {
                output.Add(specialByte);
            }
            else if (ch is >= '\u0020' and <= '\u007e')
            {
                output.Add((byte)ch);
            }
            else
            {
                throw new InvalidDataException($"Unsupported character '{ch}' (U+{(int)ch:X4}).");
            }

            i++;
        }

        output.Add(0xbf);
        return output.ToArray();
    }

    private static bool TryAppendTag(List<byte> output, string token)
    {
        int colon = token.IndexOf(':');
        string name = colon >= 0 ? token[..colon] : token;
        string value = colon >= 0 ? token[(colon + 1)..] : string.Empty;

        if (name.Equals("color", StringComparison.OrdinalIgnoreCase)
            && MmMessageTokenMaps.ColorBytes.TryGetValue(value, out byte color))
        {
            output.Add(color);
            return true;
        }

        if (name.Equals("color", StringComparison.OrdinalIgnoreCase))
        {
            throw InvalidTagValue(token, "known color name");
        }

        if (MmMessageTokenMaps.NoArgumentBytes.TryGetValue(name, out byte command))
        {
            output.Add(command);
            return true;
        }

        if (MmMessageTokenMaps.ButtonBytes.TryGetValue(name, out byte button))
        {
            output.Add(button);
            return true;
        }

        if (name.Equals("byte", StringComparison.OrdinalIgnoreCase) && TryParseHex(value, 0xff, out int rawByte))
        {
            output.Add((byte)rawByte);
            return true;
        }

        if (name.Equals("byte", StringComparison.OrdinalIgnoreCase))
        {
            throw InvalidTagValue(token, "hexadecimal byte");
        }

        if (MmMessageTokenMaps.OneByteArgumentBytes.TryGetValue(name, out byte oneByteCommand)
            && TryParseHex(value, 0xff, out int byteArgument))
        {
            output.Add(oneByteCommand);
            output.Add((byte)byteArgument);
            return true;
        }

        if (MmMessageTokenMaps.OneByteArgumentBytes.ContainsKey(name))
        {
            throw InvalidTagValue(token, "hexadecimal byte");
        }

        if (MmMessageTokenMaps.TwoByteArgumentBytes.TryGetValue(name, out byte twoByteCommand)
            && TryParseHex(value, 0xffff, out int ushortArgument))
        {
            output.Add(twoByteCommand);
            output.Add((byte)((ushortArgument >> 8) & 0xff));
            output.Add((byte)(ushortArgument & 0xff));
            return true;
        }

        if (MmMessageTokenMaps.TwoByteArgumentBytes.ContainsKey(name))
        {
            throw InvalidTagValue(token, "hexadecimal 16-bit value");
        }

        return false;
    }

    private static InvalidDataException InvalidTagValue(string token, string expected)
    {
        return new InvalidDataException($"Invalid value for [{token}]. Expected {expected}.");
    }

    private static bool TryParseHex(string value, int maxValue, out int result)
    {
        result = 0;

        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            value = value[2..];
        }

        return Regex.IsMatch(value, "^[0-9a-fA-F]+$")
            && int.TryParse(value, System.Globalization.NumberStyles.HexNumber, null, out result)
            && result <= maxValue;
    }
}
