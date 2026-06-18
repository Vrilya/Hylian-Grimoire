namespace HylianGrimoire.Headers;

internal static partial class CHeaderJapaneseExporter
{
    private static ReadOnlySpan<byte> TrimEnd(byte[] encoded)
    {
        int end = encoded.Length;
        bool foundEnd = false;
        for (int i = 0; i + 1 < encoded.Length; i += 2)
        {
            if (encoded[i] == 0x81 && encoded[i + 1] == 0x70)
            {
                end = i;
                foundEnd = true;
                break;
            }
        }

        if (!foundEnd)
        {
            while (end > 0 && encoded[end - 1] == 0)
            {
                end--;
            }
        }

        return encoded.AsSpan(0, end);
    }

    private static int ReadWord(ReadOnlySpan<byte> data, int offset)
    {
        if (offset + 1 >= data.Length)
        {
            throw new InvalidDataException("Japanese message ended in the middle of a wide value.");
        }

        return (data[offset] << 8) | data[offset + 1];
    }

    private static int ReadWord(ReadOnlySpan<byte> data, ref int offset)
    {
        int value = ReadWord(data, offset);
        offset += 2;
        return value;
    }

    private static int ReadSkippedByte(ReadOnlySpan<byte> data, ref int offset)
    {
        int value = ReadWord(data, ref offset);
        if ((value & 0xff00) != 0)
        {
            throw new InvalidDataException("Japanese message argument expected a zero high byte.");
        }

        return value & 0xff;
    }

    private static int ReadByte(ReadOnlySpan<byte> data, ref int offset)
    {
        if (offset >= data.Length)
        {
            throw new InvalidDataException("Japanese message ended in the middle of a byte argument.");
        }

        return data[offset++];
    }
}
