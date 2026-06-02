using System.Text;

namespace HylianGrimoire.Codecs;

public static class FontOrderCodec
{
    public const int MessageId = 0xfffc;

    private static readonly byte[] StandardBytes =
    [
        (byte)'0', (byte)'1', (byte)'2', (byte)'3', (byte)'4', (byte)'5', (byte)'6', (byte)'7', (byte)'8', (byte)'9', 0x01,
        (byte)'A', (byte)'B', (byte)'C', (byte)'D', (byte)'E', (byte)'F', (byte)'G', (byte)'H', (byte)'I', (byte)'J', (byte)'K', (byte)'L', (byte)'M', (byte)'N', 0x01,
        (byte)'O', (byte)'P', (byte)'Q', (byte)'R', (byte)'S', (byte)'T', (byte)'U', (byte)'V', (byte)'W', (byte)'X', (byte)'Y', (byte)'Z', 0x01,
        (byte)'a', (byte)'b', (byte)'c', (byte)'d', (byte)'e', (byte)'f', (byte)'g', (byte)'h', (byte)'i', (byte)'j', (byte)'k', (byte)'l', (byte)'m', (byte)'n', 0x01,
        (byte)'o', (byte)'p', (byte)'q', (byte)'r', (byte)'s', (byte)'t', (byte)'u', (byte)'v', (byte)'w', (byte)'x', (byte)'y', (byte)'z', 0x01,
        (byte)' ', (byte)'-', (byte)'.', 0x01,
        0x02,
    ];

    public static byte[] GetStandardBytes() => StandardBytes.ToArray();

    public static string GetStandardEditorText() => ToEditorText(StandardBytes);

    public static string ToEditorText(ReadOnlySpan<byte> raw, MessageEncodingProfile? encodingProfile = null)
    {
        encodingProfile ??= MessageEncodingProfile.Default;
        var text = new StringBuilder();

        foreach (byte value in raw)
        {
            if (value == 0x02)
            {
                break;
            }

            if (value is 0x00 or 0x03)
            {
                continue;
            }

            if (value == 0x01)
            {
                text.Append('\n');
                continue;
            }

            text.Append(encodingProfile.TryGetEditorChar(value, out char ch) ? ch : (char)value);
        }

        return text.ToString().TrimEnd('\n');
    }

    public static byte[] FromEditorText(string text, MessageEncodingProfile? encodingProfile = null)
    {
        encodingProfile ??= MessageEncodingProfile.Default;
        var bytes = new List<byte>();

        foreach (char ch in text.Replace("\r\n", "\n").Replace('\r', '\n'))
        {
            if (ch == '\n')
            {
                bytes.Add(0x01);
                continue;
            }

            if (ch is >= (char)0x20 and <= (char)0x7e)
            {
                bytes.Add((byte)ch);
                continue;
            }

            if (!encodingProfile.TryGetByte(ch, out byte value))
            {
                throw new InvalidDataException($"Font order contains a character that cannot be encoded: {ch}");
            }

            bytes.Add(value);
        }

        if (bytes.Count == 0 || bytes[^1] != 0x01)
        {
            bytes.Add(0x01);
        }

        bytes.Add(0x02);
        return bytes.ToArray();
    }
}
