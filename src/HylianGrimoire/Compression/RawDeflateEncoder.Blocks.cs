namespace HylianGrimoire.Compression;

internal static partial class RawDeflateEncoder
{
    private static BlockTables BuildBlockTables(List<Token> tokens, int start, int end)
    {
        uint[] literalFrequency = new uint[286];
        uint[] distanceFrequency = new uint[30];
        FrequenciesForBlock(tokens, start, end, literalFrequency, distanceFrequency);

        byte[] literalLengths = HuffmanLengths(literalFrequency, 286, 15);
        byte[] distanceLengths = HuffmanLengths(distanceFrequency, 30, 15);
        (int literalCount, int distanceCount) = TrimLengths(literalLengths, distanceLengths);

        List<CodeLengthItem> codeLengthSymbols = EncodeCodeLengths(literalLengths, literalCount);
        codeLengthSymbols.AddRange(EncodeCodeLengths(distanceLengths, distanceCount));

        uint[] codeLengthFrequency = new uint[19];
        foreach (CodeLengthItem item in codeLengthSymbols)
        {
            codeLengthFrequency[item.Symbol]++;
        }

        byte[] codeLengthLengths = HuffmanLengths(codeLengthFrequency, 19, 7);
        int hclen = 4;
        for (int i = 0; i < 19; i++)
        {
            if (codeLengthLengths[CodeLengthOrder[i]] != 0)
            {
                hclen = i + 1;
            }
        }

        return new BlockTables(
            literalLengths,
            literalCount,
            distanceLengths,
            distanceCount,
            codeLengthLengths,
            codeLengthSymbols,
            hclen);
    }

    private static uint TokenBits(List<Token> tokens, int start, int end, byte[] literalLengths, byte[] distanceLengths)
    {
        uint bits = 0;
        for (int i = start; i < end; i++)
        {
            Token token = tokens[i];
            if (token.Type == TokenType.Literal)
            {
                bits += literalLengths[token.Literal];
            }
            else
            {
                int lc = LengthSymbol(token.Length);
                int dc = DistanceSymbol(token.Distance);
                bits += (uint)(literalLengths[lc] + LenExtra[lc - 257] + distanceLengths[dc] + DistExtra[dc]);
            }
        }

        return bits + literalLengths[256];
    }

    private static int ChooseKind(List<Token> tokens, int start, int end)
    {
        BlockTables tables = BuildBlockTables(tokens, start, end);
        uint dynamicBits = (uint)(5 + 5 + 4 + (3 * tables.Hclen));
        foreach (CodeLengthItem item in tables.CodeLengthSymbols)
        {
            dynamicBits += (uint)(tables.CodeLengthLengths[item.Symbol] + item.Bits);
        }

        dynamicBits += TokenBits(tokens, start, end, tables.LiteralLengths, tables.DistanceLengths);

        byte[] fixedLiteral = new byte[288];
        byte[] fixedDistance = new byte[32];
        FixedLengths(fixedLiteral, fixedDistance);
        uint fixedBits = TokenBits(tokens, start, end, fixedLiteral, fixedDistance);
        return (((fixedBits + 3 + 7) >> 3) <= ((dynamicBits + 3 + 7) >> 3)) ? 1 : 2;
    }

    private static bool ShouldSplitBlock(List<Token> tokens, int start, int end, uint startOut, uint endOut)
    {
        int blockTokens = end - start;
        int matches = 0;
        uint[] distanceFrequency = new uint[30];
        for (int i = start; i < end; i++)
        {
            Token token = tokens[i];
            if (token.Type == TokenType.Match)
            {
                matches++;
                distanceFrequency[DistanceSymbol(token.Distance)]++;
            }
        }

        if (matches >= blockTokens / 2)
        {
            return false;
        }

        uint outBits = (uint)blockTokens * 8;
        for (int i = 0; i < 30; i++)
        {
            outBits += distanceFrequency[i] * (uint)(5 + DistExtra[i]);
        }

        return (outBits >> 3) < (endOut - startOut) / 2;
    }

    private static void WriteBestBlock(BitWriter writer, List<Token> tokens, int start, int end, bool final)
    {
        if (ChooseKind(tokens, start, end) == 1)
        {
            WriteFixedBlock(writer, tokens, start, end, final);
        }
        else
        {
            WriteDynamicBlock(writer, tokens, start, end, final);
        }
    }

    private static void WriteTokenData(BitWriter writer, List<Token> tokens, int start, int end, Code[] literalCodes, Code[] distanceCodes)
    {
        for (int i = start; i < end; i++)
        {
            Token token = tokens[i];
            if (token.Type == TokenType.Literal)
            {
                WriteSymbol(writer, literalCodes, token.Literal);
            }
            else
            {
                int lc = LengthSymbol(token.Length);
                int dc = DistanceSymbol(token.Distance);
                WriteSymbol(writer, literalCodes, lc);
                if (LenExtra[lc - 257] != 0)
                {
                    writer.Write((uint)(token.Length - LenBase[lc - 257]), LenExtra[lc - 257]);
                }

                WriteSymbol(writer, distanceCodes, dc);
                if (DistExtra[dc] != 0)
                {
                    writer.Write((uint)(token.Distance - DistBase[dc]), DistExtra[dc]);
                }
            }
        }

        WriteSymbol(writer, literalCodes, 256);
    }

    private static void WriteFixedBlock(BitWriter writer, List<Token> tokens, int start, int end, bool final)
    {
        byte[] literalLengths = new byte[288];
        byte[] distanceLengths = new byte[32];
        FixedLengths(literalLengths, distanceLengths);
        Code[] literalCodes = CanonicalCodes(literalLengths, 288);
        Code[] distanceCodes = CanonicalCodes(distanceLengths, 32);

        writer.Write(final ? 1u : 0u, 1);
        writer.Write(1, 2);
        WriteTokenData(writer, tokens, start, end, literalCodes, distanceCodes);
    }

    private static void WriteDynamicBlock(BitWriter writer, List<Token> tokens, int start, int end, bool final)
    {
        BlockTables tables = BuildBlockTables(tokens, start, end);
        Code[] literalCodes = CanonicalCodes(tables.LiteralLengths, tables.LiteralCount);
        Code[] distanceCodes = CanonicalCodes(tables.DistanceLengths, tables.DistanceCount);
        Code[] codeLengthCodes = CanonicalCodes(tables.CodeLengthLengths, 19);

        writer.Write(final ? 1u : 0u, 1);
        writer.Write(2, 2);
        writer.Write((uint)(tables.LiteralCount - 257), 5);
        writer.Write((uint)(tables.DistanceCount - 1), 5);
        writer.Write((uint)(tables.Hclen - 4), 4);

        for (int i = 0; i < tables.Hclen; i++)
        {
            writer.Write(tables.CodeLengthLengths[CodeLengthOrder[i]], 3);
        }

        foreach (CodeLengthItem item in tables.CodeLengthSymbols)
        {
            WriteSymbol(writer, codeLengthCodes, item.Symbol);
            if (item.Bits != 0)
            {
                writer.Write((uint)item.Extra, item.Bits);
            }
        }

        WriteTokenData(writer, tokens, start, end, literalCodes, distanceCodes);
    }
}
