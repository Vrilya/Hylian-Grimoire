using System.Buffers.Binary;

namespace HylianGrimoire.PromptEditor;

public static class PromptEditorService
{
    public static IReadOnlyList<PromptEditorLine> Read(ReadOnlySpan<byte> rom, PromptEditorProfile profile, string languageKey)
    {
        PromptEditorLanguage language = profile.Languages[languageKey];
        var lines = new List<PromptEditorLine>(profile.Lines.Count);

        foreach (PromptEditorLineDefinition line in profile.Lines)
        {
            lines.Add(ReadLine(rom, profile, language, line));
        }

        return lines;
    }

    public static void Write(Span<byte> rom, PromptEditorProfile profile, string languageKey, IReadOnlyList<PromptEditorLine> lines)
    {
        PromptEditorLanguage language = profile.Languages[languageKey];
        bool active = IsPatchActive(language, lines);
        ValidateWritable(rom, profile, lines);

        foreach (PromptEditorLine line in lines)
        {
            PromptEditorLineDefinition definition = GetDefinition(profile, line.Kind);
            WritePatchSite(rom, profile, definition.IconSiteName, active ? line.IconX : language.Defaults[line.Kind].IconX, active);
            WritePatchSite(rom, profile, definition.TextSiteName, active ? line.TextX : language.Defaults[line.Kind].TextX, active);
        }

        foreach ((_, PromptEditorFixedPatch fixedPatch) in profile.FixedPatches)
        {
            WriteU32(rom, fixedPatch.Offset, active ? fixedPatch.Value : fixedPatch.Expected);
        }
    }

    public static void Reset(Span<byte> rom, PromptEditorProfile profile, string languageKey)
    {
        PromptEditorLanguage language = profile.Languages[languageKey];
        Write(rom, profile, languageKey, CreateDefaultLines(profile, language));
    }

    public static IReadOnlyList<PromptEditorLine> CreateDefaultLines(
        PromptEditorProfile profile,
        PromptEditorLanguage language) =>
        profile.Lines
            .Select(line => DefaultLine(language, line))
            .ToArray();

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

        if (!IsPatchedAddiuForSite(word, site))
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
        PromptEditorLineDefinition definition)
    {
        PromptEditorDefaults defaults = language.Defaults[definition.Kind];
        int iconX = DecodePatchSiteValue(rom, profile.PatchSites[definition.IconSiteName], defaults.IconX);
        int textX = DecodePatchSiteValue(rom, profile.PatchSites[definition.TextSiteName], defaults.TextX);
        return new PromptEditorLine(
            definition.Kind,
            definition.Label,
            definition.IconKey,
            definition.TextKey,
            iconX,
            textX);
    }

    private static PromptEditorLine DefaultLine(
        PromptEditorLanguage language,
        PromptEditorLineDefinition definition)
    {
        PromptEditorDefaults defaults = language.Defaults[definition.Kind];
        return new PromptEditorLine(
            definition.Kind,
            definition.Label,
            definition.IconKey,
            definition.TextKey,
            defaults.IconX,
            defaults.TextX);
    }

    private static void ValidateWritable(
        ReadOnlySpan<byte> rom,
        PromptEditorProfile profile,
        IReadOnlyList<PromptEditorLine> lines)
    {
        List<string> bad = [];

        foreach (PromptEditorLine line in lines)
        {
            PromptEditorLineDefinition definition = GetDefinition(profile, line.Kind);
            ValidatePatchSite(rom, profile.PatchSites[definition.IconSiteName], definition.IconSiteName, bad);
            ValidatePatchSite(rom, profile.PatchSites[definition.TextSiteName], definition.TextSiteName, bad);
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
        if (current == site.Expected || IsPatchedAddiuForSite(current, site))
        {
            return;
        }

        bad.Add(
            $"{siteName} at 0x{site.Offset:x8}: current 0x{current:x8}, "
            + $"expected original 0x{site.Expected:x8} or addiu r{site.WriteRt}, r{site.WriteRs}, imm");
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

    private static PromptEditorLineDefinition GetDefinition(PromptEditorProfile profile, PromptEditorKind kind) =>
        profile.Lines.First(line => line.Kind == kind);

    private static uint BuildAddiu(PromptEditorPatchSite site, int value) =>
        0x24000000u | ((uint)site.WriteRs << 21) | ((uint)site.WriteRt << 16) | ((uint)value & 0xffff);

    private static bool IsPatchedAddiuForSite(uint word, PromptEditorPatchSite site) =>
        (word >> 26) == 0x09 &&
        ((word >> 21) & 0x1f) == (uint)site.WriteRs &&
        ((word >> 16) & 0x1f) == (uint)site.WriteRt;

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
