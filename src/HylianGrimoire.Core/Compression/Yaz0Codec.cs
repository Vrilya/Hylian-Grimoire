namespace HylianGrimoire.Compression;

public static class Yaz0Codec
{
    private const int HeaderSize = 16;
    private const int WindowSize = 0x1000;
    private const int MaxMatchLength = 0x111;

    public static bool IsYaz0(ReadOnlySpan<byte> data) =>
        data.Length >= HeaderSize
        && data[0] == (byte)'Y'
        && data[1] == (byte)'a'
        && data[2] == (byte)'z'
        && data[3] == (byte)'0';

    public static byte[] Decode(ReadOnlySpan<byte> source)
    {
        if (!IsYaz0(source))
        {
            throw new InvalidDataException("Invalid Yaz0 data.");
        }

        int uncompressedSize = checked((int)ReadUInt32BigEndian(source, 4));
        var output = new byte[uncompressedSize];
        DecodeInto(source, output);
        return output;
    }

    public static int DecodeInto(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        if (!IsYaz0(source))
        {
            throw new InvalidDataException("Invalid Yaz0 data.");
        }

        int uncompressedSize = checked((int)ReadUInt32BigEndian(source, 4));
        if (destination.Length < uncompressedSize)
        {
            throw new InvalidDataException("Yaz0 destination buffer is too small.");
        }

        int sourceIndex = HeaderSize;
        int destIndex = 0;
        int validBitCount = 0;
        byte codeByte = 0;

        while (destIndex < uncompressedSize)
        {
            if (validBitCount == 0)
            {
                if (sourceIndex >= source.Length)
                {
                    throw new InvalidDataException("Unexpected end of Yaz0 stream.");
                }

                codeByte = source[sourceIndex++];
                validBitCount = 8;
            }

            if ((codeByte & 0x80) != 0)
            {
                if (sourceIndex >= source.Length)
                {
                    throw new InvalidDataException("Unexpected end of Yaz0 literal.");
                }

                destination[destIndex++] = source[sourceIndex++];
            }
            else
            {
                if (sourceIndex + 1 >= source.Length)
                {
                    throw new InvalidDataException("Unexpected end of Yaz0 back-reference.");
                }

                byte byte1 = source[sourceIndex++];
                byte byte2 = source[sourceIndex++];
                int distance = ((byte1 & 0x0f) << 8) | byte2;
                int copySource = destIndex - (distance + 1);
                if (copySource < 0)
                {
                    throw new InvalidDataException("Invalid Yaz0 back-reference.");
                }

                int count = byte1 >> 4;
                if (count == 0)
                {
                    if (sourceIndex >= source.Length)
                    {
                        throw new InvalidDataException("Unexpected end of long Yaz0 back-reference.");
                    }

                    count = source[sourceIndex++] + 0x12;
                }
                else
                {
                    count += 2;
                }

                for (int i = 0; i < count && destIndex < uncompressedSize; i++)
                {
                    destination[destIndex++] = destination[copySource++];
                }
            }

            validBitCount--;
            codeByte <<= 1;
        }

        return uncompressedSize;
    }

    public static byte[] Encode(ReadOnlySpan<byte> data)
    {
        if (data.Length == 0)
        {
            byte[] header = new byte[HeaderSize];
            header[0] = (byte)'Y';
            header[1] = (byte)'a';
            header[2] = (byte)'z';
            header[3] = (byte)'0';
            return header;
        }

        const int cap = MaxMatchLength;
        int size = data.Length;
        int position = 0;
        uint flag = 0x80000000u;

        var raws = new List<byte>(data.Length / 2);
        var control = new List<ushort>(data.Length / 8);
        var commands = new List<uint>(data.Length / 32 + 1) { 0 };

        while (position < size)
        {
            EncSearch(data, position, size, cap, out int hitPosition, out int hitLength);

            if (hitLength < 3)
            {
                raws.Add(data[position]);
                commands[^1] |= flag;
                position += 1;
            }
            else
            {
                EncSearch(data, position + 1, size, cap, out int testPosition, out int testLength);
                if (hitLength + 1 < testLength)
                {
                    raws.Add(data[position]);
                    commands[^1] |= flag;
                    position += 1;
                    flag >>= 1;
                    if (flag == 0)
                    {
                        flag = 0x80000000u;
                        commands.Add(0);
                    }

                    hitLength = testLength;
                    hitPosition = testPosition;
                }

                int distance = position - hitPosition - 1;
                position += hitLength;

                if (hitLength < 0x12)
                {
                    hitLength -= 2;
                    control.Add((ushort)((hitLength << 12) | distance));
                }
                else
                {
                    control.Add((ushort)distance);
                    raws.Add((byte)(hitLength - 0x12));
                }
            }

            flag >>= 1;
            if (flag == 0)
            {
                flag = 0x80000000u;
                commands.Add(0);
            }
        }

        if (flag == 0x80000000u)
        {
            commands.RemoveAt(commands.Count - 1);
        }

        byte[] controlBytes = new byte[commands.Count * 4];
        for (int i = 0; i < commands.Count; i++)
        {
            uint command = commands[i];
            controlBytes[i * 4] = (byte)(command >> 24);
            controlBytes[i * 4 + 1] = (byte)(command >> 16);
            controlBytes[i * 4 + 2] = (byte)(command >> 8);
            controlBytes[i * 4 + 3] = (byte)command;
        }

        int byteIndex = 0;
        int rawIndex = 0;
        int controlIndex = 0;
        uint bit = 0x10000;
        int remaining = data.Length;

        var stream = new List<byte>(data.Length);
        while (remaining > 0)
        {
            if (bit > 0xffff)
            {
                bit = controlBytes[byteIndex++];
                stream.Add((byte)(bit & 0xff));
                bit |= 0x100;
            }

            if ((bit & 0x80) != 0)
            {
                stream.Add(raws[rawIndex++]);
                remaining -= 1;
            }
            else
            {
                ushort value = control[controlIndex++];
                stream.Add((byte)(value >> 8));
                stream.Add((byte)value);
                int lengthNibble = (value >> 12) & 0x0f;
                if (lengthNibble == 0)
                {
                    byte extra = raws[rawIndex++];
                    stream.Add(extra);
                    lengthNibble = extra + 16;
                }

                remaining -= lengthNibble + 2;
            }

            bit <<= 1;
        }

        var output = new byte[HeaderSize + stream.Count];
        output[0] = (byte)'Y';
        output[1] = (byte)'a';
        output[2] = (byte)'z';
        output[3] = (byte)'0';
        WriteUInt32BigEndian(output, 4, (uint)data.Length);
        stream.CopyTo(output.AsSpan(HeaderSize));
        return output;
    }

    private static int EncFind(
        ReadOnlySpan<byte> data,
        int arrayOffset,
        int needleOffset,
        int needleLength,
        int startIndex,
        int sourceLength)
    {
        int limit = sourceLength - needleLength + 1;
        if (limit <= 0)
        {
            return -1;
        }

        byte needleFirst = data[needleOffset];
        while (startIndex < limit)
        {
            int index = -1;
            for (int r = startIndex; r < limit; r++)
            {
                if (data[arrayOffset + r] == needleFirst)
                {
                    index = r;
                    break;
                }
            }

            if (index == -1)
            {
                return -1;
            }

            bool found = true;
            for (int i = 0; i < needleLength; i++)
            {
                if (data[arrayOffset + index + i] != data[needleOffset + i])
                {
                    found = false;
                    break;
                }
            }

            if (found)
            {
                return index;
            }

            startIndex = index + 1;
        }

        return -1;
    }

    private static void EncSearch(
        ReadOnlySpan<byte> data,
        int position,
        int size,
        int cap,
        out int hitPosition,
        out int hitLength)
    {
        int matchPosition = position > WindowSize ? position - WindowSize : 0;
        int matchLengthLimit = Math.Min(cap, size - position);
        if (matchLengthLimit < 3)
        {
            hitPosition = 0;
            hitLength = 0;
            return;
        }

        int foundPosition = 0;
        int foundLength = 3;

        if (matchPosition < position)
        {
            int matchOffset = EncFind(
                data,
                matchPosition,
                position,
                foundLength,
                startIndex: 0,
                sourceLength: position + foundLength - matchPosition);

            while (matchOffset >= 0 && matchOffset < position - matchPosition)
            {
                while (foundLength < matchLengthLimit
                    && data[position + foundLength] == data[matchPosition + matchOffset + foundLength])
                {
                    foundLength++;
                }

                matchPosition += matchOffset;
                foundPosition = matchPosition;
                if (foundLength == matchLengthLimit)
                {
                    hitPosition = foundPosition;
                    hitLength = foundLength;
                    return;
                }

                matchPosition += 1;
                foundLength += 1;
                if (matchPosition >= position)
                {
                    break;
                }

                matchOffset = EncFind(
                    data,
                    matchPosition,
                    position,
                    foundLength,
                    startIndex: 0,
                    sourceLength: position + foundLength - matchPosition);
            }
        }

        if (foundLength < 4)
        {
            foundLength = 1;
        }

        hitPosition = foundPosition;
        hitLength = foundLength - 1;
    }

    private static uint ReadUInt32BigEndian(ReadOnlySpan<byte> data, int offset) =>
        ((uint)data[offset] << 24)
        | ((uint)data[offset + 1] << 16)
        | ((uint)data[offset + 2] << 8)
        | data[offset + 3];

    private static void WriteUInt32BigEndian(List<byte> output, uint value)
    {
        output.Add((byte)(value >> 24));
        output.Add((byte)(value >> 16));
        output.Add((byte)(value >> 8));
        output.Add((byte)value);
    }

    private static void WriteUInt32BigEndian(Span<byte> output, int offset, uint value)
    {
        output[offset] = (byte)(value >> 24);
        output[offset + 1] = (byte)(value >> 16);
        output[offset + 2] = (byte)(value >> 8);
        output[offset + 3] = (byte)value;
    }
}
