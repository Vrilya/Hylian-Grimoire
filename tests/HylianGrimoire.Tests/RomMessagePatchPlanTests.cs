using HylianGrimoire.Models;
using HylianGrimoire.Rom;
using Xunit;

namespace HylianGrimoire.Tests;

public sealed class RomMessagePatchPlanTests
{
    [Fact]
    public void PatchPlanShowsDmaEndPatchWhenMessageDataExpandsWithinNextFile()
    {
        MessageBankProfile bank = new("Messages", 0x0200, 0x0100, 0x1000, 0x0020);
        var profile = new RomVersionProfile(
            "Patch Plan DMA Test",
            string.Empty,
            BuildDateOffset: 0,
            DmaTableOffset: 0x0020,
            DmaEntryCount: 2,
            RomCodecKind.Yaz0,
            RawDeflateHasNoHeader: false,
            TargetCompressedSizeMiB: 0,
            CreditsTableOffset: 0,
            CreditsTableSize: 0,
            CreditsDataOffset: 0,
            CreditsDataSize: 0,
            [bank],
            new HashSet<int>());
        byte[] rom = new byte[0x1300];
        WriteDmaEntry(rom, profile.DmaTableOffset, virtualStart: 0x1000, virtualEnd: 0x1020);
        WriteDmaEntry(rom, profile.DmaTableOffset + 16, virtualStart: 0x1100, virtualEnd: 0x1200);
        List<MessageEntry> entries =
        [
            new(0x6004, type: 0, position: 0, bank: 7, offset: 0)
            {
                Text = new string('A', 64),
            },
        ];

        RomMessagePatchPlan plan = RomMessageService.BuildActiveSectionPatchPlan(
            rom,
            profile,
            messageBankIndex: 0,
            RomMessageSection.Messages,
            entries);

        RomMessageUInt32WriteOperation dmaWrite = Assert.Single(plan.Operations.OfType<RomMessageUInt32WriteOperation>());
        Assert.Equal("message data DMA virtual end", dmaWrite.Name);
        Assert.Equal(profile.DmaTableOffset + 4, dmaWrite.Offset);
        Assert.True(dmaWrite.Value > 0x1020);
        Assert.True(dmaWrite.Value <= 0x1100);

        RomMessageSectionWriteOperation dataWrite = Assert.Single(
            plan.Operations.OfType<RomMessageSectionWriteOperation>(),
            operation => operation.Name == "message data");
        Assert.Equal(bank.MessageDataOffset, dataWrite.Offset);
        Assert.True(dataWrite.PayloadLength > bank.MessageDataSize);
        Assert.True(dataWrite.Length > bank.MessageDataSize);
    }

    private static void WriteDmaEntry(byte[] rom, int offset, uint virtualStart, uint virtualEnd)
    {
        WriteUInt32BigEndian(rom, offset, virtualStart);
        WriteUInt32BigEndian(rom, offset + 4, virtualEnd);
        WriteUInt32BigEndian(rom, offset + 8, virtualStart);
        WriteUInt32BigEndian(rom, offset + 12, 0);
    }

    private static void WriteUInt32BigEndian(byte[] data, int offset, uint value)
    {
        data[offset] = (byte)(value >> 24);
        data[offset + 1] = (byte)(value >> 16);
        data[offset + 2] = (byte)(value >> 8);
        data[offset + 3] = (byte)value;
    }
}
