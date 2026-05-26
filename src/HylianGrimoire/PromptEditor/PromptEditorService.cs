using System.Buffers.Binary;

namespace HylianGrimoire.PromptEditor;

public static class PromptEditorService
{
    public static IReadOnlyList<PromptEditorLine> Read(ReadOnlySpan<byte> rom, PromptEditorProfile profile, string languageKey)
    {
        PromptEditorLanguage language = PromptEditorProfileCatalog.Languages[languageKey];

        return
        [
            ReadLine(rom, profile, language, PromptEditorKind.File, "File panel", "ButtonA", "Confirm", "FileIcon", "FileText"),
            ReadLine(rom, profile, language, PromptEditorKind.Melody, "Melody panel", "ButtonA", "Melody", "MelodyIcon", "MelodyText"),
            ReadLine(rom, profile, language, PromptEditorKind.Gear, "Gear panel", "ButtonA", "Use", "GearIcon", "GearText"),
            ReadLine(rom, profile, language, PromptEditorKind.Item, "Item panel", "ButtonC", "Use", "ItemIcon", "ItemText"),
        ];
    }

    public static void Write(Span<byte> rom, PromptEditorProfile profile, string languageKey, IReadOnlyList<PromptEditorLine> lines)
    {
        PromptEditorLanguage language = PromptEditorProfileCatalog.Languages[languageKey];
        bool active = IsPatchActive(language, lines);
        ValidateWritable(rom, profile, lines);

        foreach (PromptEditorLine line in lines)
        {
            WritePatchSite(rom, profile, GetIconSiteName(line.Kind), active ? line.IconX : language.Defaults[line.Kind].IconX, active);
            WritePatchSite(rom, profile, GetTextSiteName(line.Kind), active ? line.TextX : language.Defaults[line.Kind].TextX, active);
        }

        foreach ((_, PromptEditorFixedPatch fixedPatch) in profile.FixedPatches)
        {
            WriteU32(rom, fixedPatch.Offset, active ? fixedPatch.Value : fixedPatch.Expected);
        }
    }

    public static void Reset(Span<byte> rom, PromptEditorProfile profile, string languageKey)
    {
        PromptEditorLanguage language = PromptEditorProfileCatalog.Languages[languageKey];
        Write(rom, profile, languageKey, CreateDefaultLines(language));
    }

    public static IReadOnlyList<PromptEditorLine> CreateDefaultLines(PromptEditorLanguage language) =>
    [
        DefaultLine(language, PromptEditorKind.File, "File panel", "ButtonA", "Confirm"),
        DefaultLine(language, PromptEditorKind.Melody, "Melody panel", "ButtonA", "Melody"),
        DefaultLine(language, PromptEditorKind.Gear, "Gear panel", "ButtonA", "Use"),
        DefaultLine(language, PromptEditorKind.Item, "Item panel", "ButtonC", "Use"),
    ];

    public static bool IsPatchActive(PromptEditorLanguage language, IReadOnlyList<PromptEditorLine> lines) =>
        lines.Any(line =>
            line.IconX != language.Defaults[line.Kind].IconX ||
            line.TextX != language.Defaults[line.Kind].TextX);

    public static int DecodePatchSiteValue(ReadOnlySpan<byte> rom, PromptEditorPatchSite site, int defaultValue)
    {
        EnsureRange(rom, site.Offset, 4);
        uint word = ReadU32(rom, site.Offset);
        if (word == site.Expected)
        {
            return defaultValue;
        }

        if ((word >> 26) != 0x09)
        {
            throw new InvalidDataException($"Patch site at 0x{site.Offset:x8} is not an addiu instruction.");
        }

        if (!IsAddiuForSite(word, site))
        {
            throw new InvalidDataException(
                $"Patch site at 0x{site.Offset:x8} is an addiu instruction for the wrong register.");
        }

        return SignExtend16((int)(word & 0xffff));
    }

    private static PromptEditorLine ReadLine(
        ReadOnlySpan<byte> rom,
        PromptEditorProfile profile,
        PromptEditorLanguage language,
        PromptEditorKind kind,
        string label,
        string icon,
        string text,
        string iconSiteName,
        string textSiteName)
    {
        PromptEditorDefaults defaults = language.Defaults[kind];
        int iconX = DecodePatchSiteValue(rom, profile.PatchSites[iconSiteName], defaults.IconX);
        int textX = DecodePatchSiteValue(rom, profile.PatchSites[textSiteName], defaults.TextX);
        return new PromptEditorLine(kind, label, icon, text, iconX, textX);
    }

    private static PromptEditorLine DefaultLine(
        PromptEditorLanguage language,
        PromptEditorKind kind,
        string label,
        string icon,
        string text)
    {
        PromptEditorDefaults defaults = language.Defaults[kind];
        return new PromptEditorLine(kind, label, icon, text, defaults.IconX, defaults.TextX);
    }

    private static void ValidateWritable(
        ReadOnlySpan<byte> rom,
        PromptEditorProfile profile,
        IReadOnlyList<PromptEditorLine> lines)
    {
        List<string> bad = [];

        foreach (PromptEditorLine line in lines)
        {
            ValidatePatchSite(rom, profile.PatchSites[GetIconSiteName(line.Kind)], GetIconSiteName(line.Kind), bad);
            ValidatePatchSite(rom, profile.PatchSites[GetTextSiteName(line.Kind)], GetTextSiteName(line.Kind), bad);
        }

        foreach ((string name, PromptEditorFixedPatch fixedPatch) in profile.FixedPatches)
        {
            ValidateFixedPatch(rom, fixedPatch, name, bad);
        }

        if (bad.Count > 0)
        {
            throw new InvalidDataException(
                $"Prompt patch locations did not match {profile.DisplayName}."
                + Environment.NewLine
                + Environment.NewLine
                + string.Join(Environment.NewLine, bad.Take(8)));
        }
    }

    private static void ValidatePatchSite(
        ReadOnlySpan<byte> rom,
        PromptEditorPatchSite site,
        string siteName,
        List<string> bad)
    {
        EnsureRange(rom, site.Offset, 4);
        uint current = ReadU32(rom, site.Offset);
        if (current == site.Expected || IsAddiuForSite(current, site))
        {
            return;
        }

        bad.Add(
            $"{siteName} at 0x{site.Offset:x8}: current 0x{current:x8}, "
            + $"expected original 0x{site.Expected:x8} or addiu r{site.Rt}, r{site.Rs}, imm");
    }

    private static void ValidateFixedPatch(
        ReadOnlySpan<byte> rom,
        PromptEditorFixedPatch fixedPatch,
        string patchName,
        List<string> bad)
    {
        EnsureRange(rom, fixedPatch.Offset, 4);
        uint current = ReadU32(rom, fixedPatch.Offset);
        if (current == fixedPatch.Expected || current == fixedPatch.Value)
        {
            return;
        }

        bad.Add(
            $"{patchName} at 0x{fixedPatch.Offset:x8}: current 0x{current:x8}, "
            + $"expected original 0x{fixedPatch.Expected:x8} or patched 0x{fixedPatch.Value:x8}");
    }

    private static void WritePatchSite(Span<byte> rom, PromptEditorProfile profile, string siteName, int value, bool active)
    {
        PromptEditorPatchSite site = profile.PatchSites[siteName];
        WriteU32(rom, site.Offset, active ? BuildAddiu(site, value) : site.Expected);
    }

    private static string GetIconSiteName(PromptEditorKind kind) => kind switch
    {
        PromptEditorKind.File => "FileIcon",
        PromptEditorKind.Melody => "MelodyIcon",
        PromptEditorKind.Gear => "GearIcon",
        PromptEditorKind.Item => "ItemIcon",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static string GetTextSiteName(PromptEditorKind kind) => kind switch
    {
        PromptEditorKind.File => "FileText",
        PromptEditorKind.Melody => "MelodyText",
        PromptEditorKind.Gear => "GearText",
        PromptEditorKind.Item => "ItemText",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static uint BuildAddiu(PromptEditorPatchSite site, int value) =>
        0x24000000u | ((uint)site.Rs << 21) | ((uint)site.Rt << 16) | ((uint)value & 0xffff);

    private static bool IsAddiuForSite(uint word, PromptEditorPatchSite site) =>
        (word >> 26) == 0x09 &&
        ((word >> 21) & 0x1f) == (uint)site.Rs &&
        ((word >> 16) & 0x1f) == (uint)site.Rt;

    private static int SignExtend16(int value)
    {
        value &= 0xffff;
        return (value & 0x8000) != 0 ? value - 0x10000 : value;
    }

    private static uint ReadU32(ReadOnlySpan<byte> data, int offset) =>
        BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));

    private static void WriteU32(Span<byte> data, int offset, uint value) =>
        BinaryPrimitives.WriteUInt32BigEndian(data.Slice(offset, 4), value);

    private static void EnsureRange(ReadOnlySpan<byte> data, int offset, int length)
    {
        if (offset < 0 || offset + length > data.Length)
        {
            throw new InvalidDataException("The loaded ROM is too small for prompt editing.");
        }
    }
}
