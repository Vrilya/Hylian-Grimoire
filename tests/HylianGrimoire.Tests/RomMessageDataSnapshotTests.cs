using HylianGrimoire.Models;
using HylianGrimoire.Rom;
using Xunit;

namespace HylianGrimoire.Tests;

public sealed class RomMessageDataSnapshotTests
{
    [Fact]
    public void CreateSnapshotCopiesMutableRomAndEntryState()
    {
        var entry = new MessageEntry(0x1234, type: 1, position: 2, bank: 3, offset: 4)
        {
            Text = "Original",
            OriginalText = "Original",
            OriginalEncodedBytes = [0x01, 0x02],
            EncodedBytesOverride = [0x03, 0x04],
            OriginalTrailingMessageData = [0x05],
            OriginalMessageDataSize = 6,
            OriginalFinalTableEndMarkerBank = 7,
            OriginalFinalTableEndMarkerOffset = 8,
            CodecMetadata = new MajorasMaskMessageMetadata(0x0001, 0x02, 0x03, 0x0004, 0x0005, 0x0006, 0x0007),
        };
        entry.OriginalCodecMetadata = entry.CodecMetadata;

        RomVersionProfile profile = RomVersionDatabase.Profiles.First();
        var data = new RomMessageData(
            [entry],
            profile,
            WasCompressed: false,
            DecompressedRom: [0xaa, 0xbb],
            RomFontResources.Empty,
            ActiveMessageBankIndex: 0,
            RomMessageSection.Messages);

        RomMessageData snapshot = data.CreateSnapshot();

        Assert.NotSame(data.Entries, snapshot.Entries);
        Assert.NotSame(data.Entries[0], snapshot.Entries[0]);
        Assert.NotSame(data.DecompressedRom, snapshot.DecompressedRom);
        Assert.NotSame(entry.OriginalEncodedBytes, snapshot.Entries[0].OriginalEncodedBytes);
        Assert.NotSame(entry.EncodedBytesOverride, snapshot.Entries[0].EncodedBytesOverride);
        Assert.NotSame(entry.OriginalTrailingMessageData, snapshot.Entries[0].OriginalTrailingMessageData);

        data.DecompressedRom[0] = 0x00;
        entry.Text = "Changed";
        entry.OriginalEncodedBytes[0] = 0x00;
        entry.EncodedBytesOverride[0] = 0x00;
        entry.OriginalTrailingMessageData[0] = 0x00;

        Assert.Equal([0xaa, 0xbb], snapshot.DecompressedRom);
        Assert.Equal("Original", snapshot.Entries[0].Text);
        Assert.Equal([0x01, 0x02], snapshot.Entries[0].OriginalEncodedBytes);
        Assert.Equal([0x03, 0x04], snapshot.Entries[0].EncodedBytesOverride);
        Assert.Equal([0x05], snapshot.Entries[0].OriginalTrailingMessageData);
    }
}
