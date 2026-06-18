using System.Buffers.Binary;
using HylianGrimoire.PromptEditor;
using HylianGrimoire.Rom;
using Xunit;

namespace HylianGrimoire.Tests;

public sealed class PromptEditorTests
{
    [Fact]
    public void PromptEditorCanRemoveExistingCustomAddiuPatch()
    {
        PromptEditorProfile profile = CreateProfile();
        PromptEditorLanguage language = profile.Languages["eng"];
        byte[] rom = CreateOriginalRom(profile);

        WriteU32(rom, profile.PatchSites["FileText"].Offset, Addiu(profile.PatchSites["FileText"], -19));

        PromptEditorService.Write(rom, profile, language.Key, PromptEditorService.CreateDefaultLines(profile, language));

        Assert.Equal(profile.PatchSites["FileText"].Expected, ReadU32(rom, profile.PatchSites["FileText"].Offset));
        Assert.Equal(profile.FixedPatches["FileTextDirect"].Expected, ReadU32(rom, profile.FixedPatches["FileTextDirect"].Offset));
    }

    [Fact]
    public void PromptEditorRejectsAddiuPatchForWrongRegister()
    {
        PromptEditorProfile profile = CreateProfile();
        PromptEditorLanguage language = profile.Languages["eng"];
        byte[] rom = CreateOriginalRom(profile);

        WriteU32(rom, profile.PatchSites["FileText"].Offset, 0x2402ffedu);

        InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
            PromptEditorService.Write(rom, profile, language.Key, PromptEditorService.CreateDefaultLines(profile, language)));

        Assert.Contains("FileText", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PromptEditorCanPatchRelativeAddiuSiteAsDirectAddiu()
    {
        PromptEditorProfile profile = CreateRelativeAddiuProfile();
        PromptEditorLanguage language = profile.Languages["eng"];
        byte[] rom = CreateOriginalRom(profile);

        PromptEditorLine edited = PromptEditorService.CreateDefaultLines(profile, language).Single() with
        {
            IconX = -40,
            TextX = -25,
        };

        PromptEditorService.Write(rom, profile, language.Key, [edited]);

        Assert.Equal(0x2419ffd8u, ReadU32(rom, profile.PatchSites["EquipIcon"].Offset));
        Assert.Equal(0x2403ffe7u, ReadU32(rom, profile.PatchSites["EquipText"].Offset));

        PromptEditorService.Write(rom, profile, language.Key, PromptEditorService.CreateDefaultLines(profile, language));

        Assert.Equal(profile.PatchSites["EquipIcon"].Expected, ReadU32(rom, profile.PatchSites["EquipIcon"].Offset));
        Assert.Equal(profile.PatchSites["EquipText"].Expected, ReadU32(rom, profile.PatchSites["EquipText"].Offset));
    }

    [Fact]
    public void PromptEditorHasMajorasMaskGameCubeProfile()
    {
        RomVersionProfile romProfile = RomVersionDatabase.Profiles.Single(profile => profile.Name == "Majora's Mask NTSC-U GameCube");

        Assert.True(PromptEditorProfileCatalog.TryGetProfile(romProfile, out PromptEditorProfile? profile));
        Assert.Equal(0x00a45c10, profile.IconSegment.RomBase);
        Assert.Equal(PromptEditorSegmentFormat.CmpDmaArchive, profile.IconSegment.Format);
        Assert.Equal(0x00a23000, profile.TextSegments["eng"].RomBase);
        Assert.Equal(0x1000, profile.TextAssets["eng"]["Equip"].LocalOffset);
        Assert.Equal(System.Drawing.Color.FromArgb(0, 255, 100).ToArgb(), profile.IconAssets["ButtonA"].FallbackColor.ToArgb());
        Assert.Equal(0x00ca83e8, profile.PatchSites["EquipIcon"].Offset);
        Assert.Equal(0x00ca842c, profile.PatchSites["EquipText"].Offset);
    }

    private static PromptEditorProfile CreateProfile()
    {
        var lines = new List<PromptEditorLineDefinition>
        {
            new(PromptEditorKind.File, "File panel", "ButtonA", "Confirm", "FileIcon", "FileText"),
            new(PromptEditorKind.Melody, "Melody panel", "ButtonA", "Melody", "MelodyIcon", "MelodyText"),
            new(PromptEditorKind.Gear, "Gear panel", "ButtonA", "Use", "GearIcon", "GearText"),
            new(PromptEditorKind.Item, "Item panel", "ButtonC", "Use", "ItemIcon", "ItemText"),
        };

        var languages = new Dictionary<string, PromptEditorLanguage>(StringComparer.Ordinal)
        {
            ["eng"] = new(
                "eng",
                "English",
                new Dictionary<PromptEditorKind, PromptEditorDefaults>
                {
                    [PromptEditorKind.File] = new(-42, -20),
                    [PromptEditorKind.Melody] = new(-53, -31),
                    [PromptEditorKind.Gear] = new(-37, -15),
                    [PromptEditorKind.Item] = new(-48, -1),
                }),
        };

        var patchSites = new Dictionary<string, PromptEditorPatchSite>
        {
            ["FileIcon"] = DirectSite(0x00, 0x85e20e0e, 2),
            ["MelodyIcon"] = DirectSite(0x04, 0x87020e02, 2),
            ["GearIcon"] = DirectSite(0x08, 0x85e20e14, 2),
            ["ItemIcon"] = DirectSite(0x0c, 0x85c20df6, 2),
            ["FileText"] = DirectSite(0x10, 0x87190dfc, 25),
            ["MelodyText"] = DirectSite(0x14, 0x85cf0dfc, 15),
            ["GearText"] = DirectSite(0x18, 0x85d90dfc, 25),
            ["ItemText"] = DirectSite(0x1c, 0x872f0e08, 15),
        };

        var fixedPatches = new Dictionary<string, PromptEditorFixedPatch>
        {
            ["FileTextDirect"] = new(0x20, 0x032f1021, 0x03201025),
        };

        return new(
            "Test",
            ["eng"],
            languages,
            lines,
            RawSegment(0),
            new Dictionary<string, PromptEditorSegment>(StringComparer.Ordinal) { ["eng"] = RawSegment(0) },
            new Dictionary<string, PromptEditorAsset>(),
            new Dictionary<string, IReadOnlyDictionary<string, PromptEditorAsset>>(),
            patchSites,
            fixedPatches);
    }

    private static PromptEditorProfile CreateRelativeAddiuProfile()
    {
        var lines = new List<PromptEditorLineDefinition>
        {
            new(PromptEditorKind.Equip, "Equip panel", "ButtonC", "Equip", "EquipIcon", "EquipText"),
        };

        var languages = new Dictionary<string, PromptEditorLanguage>(StringComparer.Ordinal)
        {
            ["eng"] = new(
                "eng",
                "English",
                new Dictionary<PromptEditorKind, PromptEditorDefaults>
                {
                    [PromptEditorKind.Equip] = new(-33, -17),
                }),
        };

        var patchSites = new Dictionary<string, PromptEditorPatchSite>
        {
            ["EquipIcon"] = DirectSite(0x00, 0x2419ffdf, 25),
            ["EquipText"] = RelativeToDirectSite(0x04, 0x24630010, 3, 3, 0),
        };

        return new(
            "MM Test",
            ["eng"],
            languages,
            lines,
            RawSegment(0),
            new Dictionary<string, PromptEditorSegment>(StringComparer.Ordinal) { ["eng"] = RawSegment(0) },
            new Dictionary<string, PromptEditorAsset>(),
            new Dictionary<string, IReadOnlyDictionary<string, PromptEditorAsset>>(),
            patchSites,
            new Dictionary<string, PromptEditorFixedPatch>());
    }

    private static byte[] CreateOriginalRom(PromptEditorProfile profile)
    {
        int lastPatch = profile.PatchSites.Values.Select(site => site.Offset).DefaultIfEmpty(0).Max();
        int lastFixed = profile.FixedPatches.Values.Select(patch => patch.Offset).DefaultIfEmpty(0).Max();
        int length = Math.Max(lastPatch, lastFixed) + 4;
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
        0x24000000u | ((uint)site.WriteRs << 21) | ((uint)site.WriteRt << 16) | ((uint)value & 0xffff);

    private static PromptEditorPatchSite DirectSite(int offset, uint expected, int rt) =>
        new(offset, expected, rt, 0, rt, 0);

    private static PromptEditorPatchSite RelativeToDirectSite(int offset, uint expected, int expectedRt, int expectedRs, int writeRs) =>
        new(offset, expected, expectedRt, expectedRs, expectedRt, writeRs);

    private static PromptEditorSegment RawSegment(int romBase) =>
        new(romBase, PromptEditorSegmentFormat.Raw);

    private static uint ReadU32(ReadOnlySpan<byte> data, int offset) =>
        BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));

    private static void WriteU32(Span<byte> data, int offset, uint value) =>
        BinaryPrimitives.WriteUInt32BigEndian(data.Slice(offset, 4), value);
}
