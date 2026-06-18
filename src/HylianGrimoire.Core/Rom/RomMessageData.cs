using HylianGrimoire.Models;

namespace HylianGrimoire.Rom;

public sealed record RomMessageData(
    List<MessageEntry> Entries,
    RomVersionProfile Profile,
    bool WasCompressed,
    byte[] DecompressedRom,
    RomFontResources FontResources,
    int ActiveMessageBankIndex,
    RomMessageSection ActiveSection)
{
    public RomMessageData CreateSnapshot()
    {
        return this with
        {
            Entries = Entries.Select(entry => entry.CreateSnapshot()).ToList(),
            DecompressedRom = DecompressedRom.ToArray(),
        };
    }
}
