using HylianGrimoire.Models;

namespace HylianGrimoire.Preview;

public static class MmPreviewEntryClassifier
{
    public static bool IsStaffCredits(MessageEntry entry)
    {
        return entry.CodecMetadata is null
            && entry.TableEndMarkerId == 0xffff
            && entry.Bank == 0x07
            && entry.Id is >= 0x4e20 and <= 0x4e4c;
    }
}
