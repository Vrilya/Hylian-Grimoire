using HylianGrimoire.Games;

namespace HylianGrimoire.Rom;

public static partial class RomVersionDatabase
{
    private static readonly HashSet<int> SkipMajorasMaskUs =
    [
        0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
        15, 16, 17, 18, 19, 20, 21, 22,
        25, 26, 27, 28, 29, 30,
        652, 1127,
        1539, 1540, 1541, 1542, 1543, 1544, 1545, 1546, 1547, 1548,
        1549, 1550, 1551, 1552, 1553, 1554, 1555, 1556, 1557, 1558,
        1559, 1560, 1561, 1562, 1563, 1564, 1565, 1566, 1567,
    ];

    private static readonly HashSet<int> SkipMajorasMaskUsGameCube =
    [
        0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
        15, 16, 17, 18, 19, 20, 21, 22,
        25, 26, 27, 28, 29, 30,
        651, 1124,
        1536, 1537, 1538, 1539, 1540, 1541, 1542, 1543, 1544, 1545,
        1546, 1547, 1548, 1549, 1550, 1551, 1552, 1553, 1554, 1555,
        1556, 1557, 1558, 1559, 1560, 1561, 1562, 1563, 1564,
    ];

    private static readonly HashSet<int> SkipMajorasMaskEu =
    [
        0, 1, 2, 3, 4, 5, 6, 7, 8,
        17, 18, 19, 20, 21, 22, 23, 24, 25, 26,
        27, 28, 29, 30,
        33, 34, 35, 36, 37, 38, 39, 40,
        41, 42, 43, 44, 45, 46, 47, 48,
        669, 1148,
        1560, 1561, 1562, 1563, 1564, 1565, 1566, 1567, 1568, 1569,
        1570, 1571, 1572, 1573, 1574, 1575, 1576, 1577, 1578, 1579,
        1580, 1581, 1582, 1583, 1584, 1585, 1586, 1587, 1588,
    ];

    private static readonly HashSet<int> SkipMajorasMaskEuGameCube =
    [
        0, 1, 2, 3, 4, 5, 6, 7, 8,
        17, 18, 19, 20, 21, 22, 23, 24, 25, 26,
        27, 28, 29, 30,
        33, 34, 35, 36, 37, 38, 39, 40,
        41, 42, 43, 44, 45, 46, 47, 48,
        669, 1146,
        1558, 1559, 1560, 1561, 1562, 1563, 1564, 1565, 1566, 1567,
        1568, 1569, 1570, 1571, 1572, 1573, 1574, 1575, 1576, 1577,
        1578, 1579, 1580, 1581, 1582, 1583, 1584, 1585, 1586,
    ];

    private static IReadOnlyList<RomVersionProfile> CreateMajorasMaskProfiles() =>
    [
        new(
            "Majora's Mask NTSC-U",
            "00-07-31 17:04:16",
            0x1a4dc,
            0x1a500,
            1568,
            RomCodecKind.Yaz0,
            RawDeflateHasNoHeader: false,
            TargetCompressedSizeMiB: 32,
            CreditsTableOffset: 0xc66048,
            CreditsTableSize: 0x170,
            CreditsDataOffset: 0xb3b000,
            CreditsDataSize: 0xe60,
            MessageBanks: MajorasMaskUsBanks(),
            SkipMajorasMaskUs,
            FontBaseline: RomFontBaseline.MajorasMask,
            Game: GameKind.MajorasMask,
            FontDmaEntryIndex: 28,
            FontWidthTableOffset: 0xc669b0),
        new(
            "Majora's Mask NTSC-U GameCube",
            "03-08-26 04:20:25",
            0x1ae70,
            0x1ae90,
            1565,
            RomCodecKind.Yaz0,
            RawDeflateHasNoHeader: false,
            TargetCompressedSizeMiB: 32,
            CreditsTableOffset: 0xc735b8,
            CreditsTableSize: 0x170,
            CreditsDataOffset: 0xb4a000,
            CreditsDataSize: 0xe60,
            MessageBanks: MajorasMaskUsGameCubeBanks(),
            SkipMajorasMaskUsGameCube,
            FontBaseline: RomFontBaseline.MajorasMaskUsGameCube,
            Game: GameKind.MajorasMask,
            FontDmaEntryIndex: 28,
            FontWidthTableOffset: 0xc73f10),
        new(
            "Majora's Mask EU 1.0",
            "00-09-25 11:16:53",
            0x1a62c,
            0x1a650,
            1589,
            RomCodecKind.Yaz0,
            RawDeflateHasNoHeader: false,
            TargetCompressedSizeMiB: 32,
            CreditsTableOffset: 0xdabf58,
            CreditsTableSize: 0x170,
            CreditsDataOffset: 0xc65000,
            CreditsDataSize: 0xe80,
            MessageBanks: MajorasMaskEu10Banks(),
            SkipMajorasMaskEu,
            FontBaseline: RomFontBaseline.MajorasMaskEu,
            Game: GameKind.MajorasMask,
            FontDmaEntryIndex: 39,
            FontWidthTableOffset: 0xdac8b0),
        new(
            "Majora's Mask EU 1.1",
            "00-09-29 09:29:41",
            0x1a8ac,
            0x1a8d0,
            1589,
            RomCodecKind.Yaz0,
            RawDeflateHasNoHeader: false,
            TargetCompressedSizeMiB: 32,
            CreditsTableOffset: 0xdac078,
            CreditsTableSize: 0x170,
            CreditsDataOffset: 0xc65000,
            CreditsDataSize: 0xe80,
            MessageBanks: MajorasMaskEu11Banks(),
            SkipMajorasMaskEu,
            FontBaseline: RomFontBaseline.MajorasMaskEu,
            Game: GameKind.MajorasMask,
            FontDmaEntryIndex: 39,
            FontWidthTableOffset: 0xdac9d0),
        new(
            "Majora's Mask EU GameCube",
            "03-10-04 00:40:20",
            0x1ae70,
            0x1ae90,
            1587,
            RomCodecKind.Yaz0,
            RawDeflateHasNoHeader: false,
            TargetCompressedSizeMiB: 32,
            CreditsTableOffset: 0xdb9078,
            CreditsTableSize: 0x170,
            CreditsDataOffset: 0xc74000,
            CreditsDataSize: 0xe80,
            MessageBanks: MajorasMaskEuGameCubeBanks(),
            SkipMajorasMaskEuGameCube,
            FontBaseline: RomFontBaseline.MajorasMaskEu,
            Game: GameKind.MajorasMask,
            FontDmaEntryIndex: 39,
            FontWidthTableOffset: 0xdb99d0),
    ];

    private static IReadOnlyList<MessageBankProfile> MajorasMaskUsBanks() =>
    [
        new("Language 1", 0xc5d0d8, 0x8f70, 0xad1000, 0x699f0, TableSegment: 0x08),
    ];

    private static IReadOnlyList<MessageBankProfile> MajorasMaskUsGameCubeBanks() =>
    [
        new("Language 1", 0xc6a648, 0x8f70, 0xae0000, 0x69ae0, TableSegment: 0x08),
    ];

    private static IReadOnlyList<MessageBankProfile> MajorasMaskEu10Banks() =>
    [
        new("Language 1", 0xc66000, 0x8f70, 0xaa5000, 0x69a80, TableSegment: 0x07),
        new("Language 2", 0xc6f000, 0x8d00, 0xb0f000, 0x74df0, TableSegment: 0x07),
        new("Language 3", 0xc78000, 0x8ce0, 0xb84000, 0x71130, TableSegment: 0x07),
        new("Language 4", 0xc81000, 0x8ce0, 0xbf6000, 0x6e5c0, TableSegment: 0x07),
    ];

    private static IReadOnlyList<MessageBankProfile> MajorasMaskEu11Banks() =>
    [
        new("Language 1", 0xc66000, 0x8f70, 0xaa5000, 0x69a80, TableSegment: 0x07),
        new("Language 2", 0xc6f000, 0x8d00, 0xb0f000, 0x74df0, TableSegment: 0x07),
        new("Language 3", 0xc78000, 0x8ce0, 0xb84000, 0x71120, TableSegment: 0x07),
        new("Language 4", 0xc81000, 0x8ce0, 0xbf6000, 0x6e5c0, TableSegment: 0x07),
    ];

    private static IReadOnlyList<MessageBankProfile> MajorasMaskEuGameCubeBanks() =>
    [
        new("Language 1", 0xc75000, 0x8f70, 0xab4000, 0x69ae0, TableSegment: 0x07),
        new("Language 2", 0xc7e000, 0x8d00, 0xb1e000, 0x74e00, TableSegment: 0x07),
        new("Language 3", 0xc87000, 0x8ce0, 0xb93000, 0x710d0, TableSegment: 0x07),
        new("Language 4", 0xc90000, 0x8ce0, 0xc05000, 0x6e600, TableSegment: 0x07),
    ];
}
