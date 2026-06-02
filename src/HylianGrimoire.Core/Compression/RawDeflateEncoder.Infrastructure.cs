namespace HylianGrimoire.Compression;

internal static partial class RawDeflateEncoder
{
    private static Code[] CanonicalCodes(byte[] lengths, int count)
    {
        Code[] codes = new Code[count];
        int[] bitLengthCount = new int[16];
        int[] nextCode = new int[16];
        int code = 0;

        for (int symbol = 0; symbol < count; symbol++)
        {
            codes[symbol] = new Code(0, lengths[symbol]);
            if (lengths[symbol] != 0)
            {
                bitLengthCount[lengths[symbol]]++;
            }
        }

        for (int bits = 1; bits <= 15; bits++)
        {
            code = (code + bitLengthCount[bits - 1]) << 1;
            nextCode[bits] = code;
        }

        for (int symbol = 0; symbol < count; symbol++)
        {
            int length = lengths[symbol];
            if (length != 0)
            {
                codes[symbol] = new Code(ReverseBits((uint)nextCode[length], length), (byte)length);
                nextCode[length]++;
            }
        }

        return codes;
    }

    private static uint ReverseBits(uint value, int count)
    {
        uint output = 0;
        for (int i = 0; i < count; i++)
        {
            output = (output << 1) | (value & 1u);
            value >>= 1;
        }

        return output;
    }

    private static void WriteSymbol(BitWriter writer, Code[] codes, int symbol)
    {
        Code code = codes[symbol];
        writer.Write(code.Bits, code.Length);
    }

    private readonly record struct Token(TokenType Type, ushort Literal, ushort Length, ushort Distance, uint EndPos)
    {
        public static Token CreateLiteral(byte value, uint endPos) =>
            new(TokenType.Literal, value, 0, 0, endPos);

        public static Token CreateMatch(ushort length, ushort distance, uint endPos) =>
            new(TokenType.Match, 0, length, distance, endPos);
    }

    private enum TokenType
    {
        Literal,
        Match,
    }

    private readonly record struct Code(uint Bits, byte Length);

    private readonly record struct CodeLengthItem(int Symbol, int Extra, int Bits);

    private sealed record BlockTables(
        byte[] LiteralLengths,
        int LiteralCount,
        byte[] DistanceLengths,
        int DistanceCount,
        byte[] CodeLengthLengths,
        List<CodeLengthItem> CodeLengthSymbols,
        int Hclen);

    private sealed class BitWriter
    {
        private readonly List<byte> _data = new(1024);
        private ulong _bitBuffer;
        private int _bitCount;

        public void Write(uint value, int bits)
        {
            ulong mask = bits == 32 ? uint.MaxValue : ((1UL << bits) - 1);
            _bitBuffer |= ((ulong)value & mask) << _bitCount;
            _bitCount += bits;

            while (_bitCount >= 8)
            {
                _data.Add((byte)(_bitBuffer & 0xff));
                _bitBuffer >>= 8;
                _bitCount -= 8;
            }
        }

        public byte[] Finish()
        {
            if (_bitCount != 0)
            {
                _data.Add((byte)(_bitBuffer & 0xff));
                _bitBuffer = 0;
                _bitCount = 0;
            }

            return _data.ToArray();
        }
    }
}
