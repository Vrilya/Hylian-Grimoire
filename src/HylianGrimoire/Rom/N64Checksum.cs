namespace HylianGrimoire.Rom;

public static class N64Checksum
{
    private const int HeaderSize = 0x40;
    private const int BootcodeSize = 0x1000 - HeaderSize;
    private const int Crc1Offset = 0x10;
    private const int Crc2Offset = 0x14;
    private const int ChecksumStart = 0x1000;
    private const int ChecksumLength = 0x100000;

    private const uint Cic6102Seed = 0xf8ca4ddc;
    private const uint Cic6103Seed = 0xa3886759;
    private const uint Cic6105Seed = 0xdf26f436;
    private const uint Cic6106Seed = 0x1fea617a;

    private static readonly uint[] CrcTable = BuildCrcTable();

    public static bool TryUpdate(Span<byte> rom)
    {
        int cic = GetCic(rom);
        if (cic == 0)
        {
            return false;
        }

        uint seed = cic switch
        {
            6101 or 6102 => Cic6102Seed,
            6103 => Cic6103Seed,
            6105 => Cic6105Seed,
            6106 => Cic6106Seed,
            _ => 0,
        };

        uint t1 = seed;
        uint t2 = seed;
        uint t3 = seed;
        uint t4 = seed;
        uint t5 = seed;
        uint t6 = seed;

        for (int i = ChecksumStart; i < ChecksumStart + ChecksumLength; i += 4)
        {
            uint value = DmaTable.ReadUInt32BigEndian(rom, i);
            if (t6 + value < t6)
            {
                t4++;
            }

            t6 += value;
            t3 ^= value;
            uint rotated = RotateLeft(value, (int)(value & 0x1f));
            t5 += rotated;
            t2 = t2 > value ? t2 ^ rotated : t2 ^ t6 ^ value;
            t1 += cic == 6105
                ? DmaTable.ReadUInt32BigEndian(rom, HeaderSize + 0x710 + (i & 0xff)) ^ value
                : t5 ^ value;
        }

        uint crc1;
        uint crc2;
        if (cic == 6103)
        {
            crc1 = (t6 ^ t4) + t3;
            crc2 = (t5 ^ t2) + t1;
        }
        else if (cic == 6106)
        {
            crc1 = (t6 * t4) + t3;
            crc2 = (t5 * t2) + t1;
        }
        else
        {
            crc1 = t6 ^ t4 ^ t3;
            crc2 = t5 ^ t2 ^ t1;
        }

        DmaTable.WriteUInt32BigEndian(rom, Crc1Offset, crc1);
        DmaTable.WriteUInt32BigEndian(rom, Crc2Offset, crc2);
        return true;
    }

    private static int GetCic(ReadOnlySpan<byte> rom)
    {
        if (rom.Length < ChecksumStart + ChecksumLength)
        {
            return 0;
        }

        uint crc = Crc32(rom, HeaderSize, BootcodeSize);
        return crc switch
        {
            0x6170a4a1 => 6101,
            0x90bb6cb5 => 6102,
            0x0b050ee0 => 6103,
            0x98bc2c86 => 6105,
            0xacc8580a => 6106,
            _ => 0,
        };
    }

    private static uint Crc32(ReadOnlySpan<byte> data, int offset, int length)
    {
        uint crc = 0xffffffff;
        for (int i = 0; i < length; i++)
        {
            crc = (crc >> 8) ^ CrcTable[(crc ^ data[offset + i]) & 0xff];
        }

        return crc ^ 0xffffffff;
    }

    private static uint[] BuildCrcTable()
    {
        var table = new uint[256];
        const uint polynomial = 0xedb88320;
        for (uint i = 0; i < table.Length; i++)
        {
            uint crc = i;
            for (int bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ polynomial : crc >> 1;
            }

            table[i] = crc;
        }

        return table;
    }

    private static uint RotateLeft(uint value, int bits) => (value << bits) | (value >> (32 - bits));
}
