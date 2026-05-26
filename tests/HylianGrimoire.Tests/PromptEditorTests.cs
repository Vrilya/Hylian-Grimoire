using System.Buffers.Binary;
using HylianGrimoire.PromptEditor;
using Xunit;

namespace HylianGrimoire.Tests;

public sealed class PromptEditorTests
{
    [Fact]
    public void PromptEditorCanRemoveExistingCustomAddiuPatch()
    {
        PromptEditorProfile profile = CreateProfile();
        PromptEditorLanguage language = PromptEditorProfileCatalog.Languages["eng"];
        byte[] rom = CreateOriginalRom(profile);

        WriteU32(rom, profile.PatchSites["FileText"].Offset, Addiu(profile.PatchSites["FileText"], -19));

        PromptEditorService.Write(rom, profile, language.Key, PromptEditorService.CreateDefaultLines(language));

        Assert.Equal(profile.PatchSites["FileText"].Expected, ReadU32(rom, profile.PatchSites["FileText"].Offset));
        Assert.Equal(profile.FixedPatches["FileTextDirect"].Expected, ReadU32(rom, profile.FixedPatches["FileTextDirect"].Offset));
    }

    [Fact]
    public void PromptEditorRejectsAddiuPatchForWrongRegister()
    {
        PromptEditorProfile profile = CreateProfile();
        PromptEditorLanguage language = PromptEditorProfileCatalog.Languages["eng"];
        byte[] rom = CreateOriginalRom(profile);

        WriteU32(rom, profile.PatchSites["FileText"].Offset, 0x2402ffedu);

        InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
            PromptEditorService.Write(rom, profile, language.Key, PromptEditorService.CreateDefaultLines(language)));

        Assert.Contains("FileText", exception.Message, StringComparison.Ordinal);
    }

    private static PromptEditorProfile CreateProfile()
    {
        var patchSites = new Dictionary<string, PromptEditorPatchSite>
        {
            ["FileIcon"] = new(0x00, 0x85e20e0e, 2, 0),
            ["MelodyIcon"] = new(0x04, 0x87020e02, 2, 0),
            ["GearIcon"] = new(0x08, 0x85e20e14, 2, 0),
            ["ItemIcon"] = new(0x0c, 0x85c20df6, 2, 0),
            ["FileText"] = new(0x10, 0x87190dfc, 25, 0),
            ["MelodyText"] = new(0x14, 0x85cf0dfc, 15, 0),
            ["GearText"] = new(0x18, 0x85d90dfc, 25, 0),
            ["ItemText"] = new(0x1c, 0x872f0e08, 15, 0),
        };

        var fixedPatches = new Dictionary<string, PromptEditorFixedPatch>
        {
            ["FileTextDirect"] = new(0x20, 0x032f1021, 0x03201025),
        };

        return new(
            "Test",
            ["eng"],
            0,
            0,
            null,
            null,
            new Dictionary<string, PromptEditorAsset>(),
            new Dictionary<string, IReadOnlyDictionary<string, PromptEditorAsset>>(),
            patchSites,
            fixedPatches);
    }

    private static byte[] CreateOriginalRom(PromptEditorProfile profile)
    {
        int length = Math.Max(profile.PatchSites.Values.Max(site => site.Offset), profile.FixedPatches.Values.Max(patch => patch.Offset)) + 4;
        byte[] rom = new byte[length];
        foreach (PromptEditorPatchSite site in profile.PatchSites.Values)
        {
            WriteU32(rom, site.Offset, site.Expected);
        }

        foreach (PromptEditorFixedPatch fixedPatch in profile.FixedPatches.Values)
        {
            WriteU32(rom, fixedPatch.Offset, fixedPatch.Expected);
        }

        return rom;
    }

    private static uint Addiu(PromptEditorPatchSite site, int value) =>
        0x24000000u | ((uint)site.Rs << 21) | ((uint)site.Rt << 16) | ((uint)value & 0xffff);

    private static uint ReadU32(ReadOnlySpan<byte> data, int offset) =>
        BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));

    private static void WriteU32(Span<byte> data, int offset, uint value) =>
        BinaryPrimitives.WriteUInt32BigEndian(data.Slice(offset, 4), value);
}
