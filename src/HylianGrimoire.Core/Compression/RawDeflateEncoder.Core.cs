namespace HylianGrimoire.Compression;

internal static partial class RawDeflateEncoder
{
    private const int MinMatch = 3;
    private const int MaxMatch = 258;
    private const int WindowSize = 32768;
    private const int MatchLookahead = MaxMatch + MinMatch + 1;
    private const int MatchDistanceLimit = WindowSize - MatchLookahead;
    private const int ShortMatchDistanceLimit = 4096;
    private const int MatchHashBits = 15;
    private const int MatchHashSize = 1 << MatchHashBits;
    private const int MatchHashMask = MatchHashSize - 1;
    private const int MatchHashShift = (MatchHashBits + MinMatch - 1) / MinMatch;
    private const int FastChainMatch = 32;
    private const int LazyMatchLimit = 258;
    private const int NoPos = -1;
    private const int TokenBlockLimit = 0x8000;
    private const int MatchBlockLimit = 0x8000;

    private static readonly int[] LenBase =
    [
        3, 4, 5, 6, 7, 8, 9, 10,
        11, 13, 15, 17, 19, 23, 27, 31,
        35, 43, 51, 59, 67, 83, 99, 115,
        131, 163, 195, 227, 258,
    ];

    private static readonly int[] LenExtra =
    [
        0, 0, 0, 0, 0, 0, 0, 0,
        1, 1, 1, 1, 2, 2, 2, 2,
        3, 3, 3, 3, 4, 4, 4, 4,
        5, 5, 5, 5, 0,
    ];

    private static readonly int[] DistBase =
    [
        1, 2, 3, 4, 5, 7, 9, 13,
        17, 25, 33, 49, 65, 97, 129, 193,
        257, 385, 513, 769, 1025, 1537, 2049, 3073,
        4097, 6145, 8193, 12289, 16385, 24577,
    ];

    private static readonly int[] DistExtra =
    [
        0, 0, 0, 0, 1, 1, 2, 2,
        3, 3, 4, 4, 5, 5, 6, 6,
        7, 7, 8, 8, 9, 9, 10, 10,
        11, 11, 12, 12, 13, 13,
    ];

    private static readonly int[] CodeLengthOrder =
    [
        16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15,
    ];

    private static readonly byte[] MatchProbeTail =
    [
        0x00, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0xb5, 0x2f, 0x05, 0x08,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x52, 0xd0, 0xff, 0xff,
        0xd0, 0x4a, 0x05, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    ];

    public static byte[] Encode(ReadOnlySpan<byte> source)
    {
        byte[] data = source.ToArray();
        List<Token> tokens = Tokenize(data);
        var writer = new BitWriter();
        int blockFirst = 0;
        uint blockFirstOut = 0;
        int blockTokens = 0;
        int blockMatches = 0;

        for (int i = 0; i < tokens.Count; i++)
        {
            bool flush = false;
            blockTokens++;
            if (tokens[i].Type == TokenType.Match)
            {
                blockMatches++;
            }

            if (blockTokens == TokenBlockLimit - 1 || blockMatches == MatchBlockLimit)
            {
                flush = true;
            }
            else if (blockTokens % 4096 == 0)
            {
                flush = ShouldSplitBlock(tokens, blockFirst, i + 1, blockFirstOut, tokens[i].EndPos);
            }

            if (flush && tokens[i].EndPos < data.Length)
            {
                WriteBestBlock(writer, tokens, blockFirst, i + 1, final: false);
                blockFirst = i + 1;
                blockFirstOut = tokens[i].EndPos;
                blockTokens = 0;
                blockMatches = 0;
            }
        }

        WriteBestBlock(writer, tokens, blockFirst, tokens.Count, final: true);
        return writer.Finish();
    }
}
