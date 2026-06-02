namespace HylianGrimoire.Rom;

// MM staff credits use the same 8-byte table shape as OoT messages, so this
// detector lives in neutral ROM code instead of either game's bank codec.
internal static class StaffCreditsMessageTableDetector
{
    private const int TableSegment = 0x07;
    private const int FirstMessageId = 0x4e20;
    private const int LastMessageId = 0x4e4c;

    public static bool LooksLikeTableFiles(byte[] tableBytes, byte[] messageBytes)
    {
        int tableOffset = FindTableStart(tableBytes);
        if (tableOffset < 0)
        {
            return false;
        }

        bool sawEntry = false;
        int previousOffset = -1;
        for (int i = tableOffset; i + 7 < tableBytes.Length; i += 8)
        {
            int id = (tableBytes[i] << 8) | tableBytes[i + 1];
            int bank = tableBytes[i + 4];
            int offset = (tableBytes[i + 5] << 16) | (tableBytes[i + 6] << 8) | tableBytes[i + 7];

            if (id == 0xffff)
            {
                return sawEntry && bank == 0 && offset == 0;
            }

            if (id == 0xfffd
                || id is < FirstMessageId or > LastMessageId
                || bank != TableSegment
                || offset < previousOffset
                || offset >= messageBytes.Length)
            {
                return false;
            }

            sawEntry = true;
            previousOffset = offset;
        }

        return false;
    }

    private static int FindTableStart(byte[] tableBytes)
    {
        for (int i = 0; i + 7 < tableBytes.Length; i += 8)
        {
            int id = (tableBytes[i] << 8) | tableBytes[i + 1];
            if (id is >= FirstMessageId and <= LastMessageId && tableBytes[i + 4] == TableSegment)
            {
                return i;
            }
        }

        return -1;
    }
}
