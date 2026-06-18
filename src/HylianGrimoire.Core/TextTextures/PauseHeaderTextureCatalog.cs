using HylianGrimoire.Games;
using HylianGrimoire.Rom;
using HylianGrimoire.Textures;

namespace HylianGrimoire.TextTextures;

public static class PauseHeaderTextureCatalog
{
    public const int Width = 240;
    public const int Height = 32;
    public const int TileWidth = 80;
    public const int TileHeight = 32;

    public static readonly IReadOnlyList<PauseHeaderTextureSpec> Specs =
    [
        new(
            "Equipment",
            "Equipment",
            ["gPauseEquipment00Tex", "gPauseEquipment10ENGTex", "gPauseEquipment20Tex"],
            "EQUIPMENT",
            new(
                new(10, 50, 40),
                new(90, 100, 60),
                new(90, 100, 60),
                new(10, 50, 40))),
        new(
            "Map",
            "Map",
            ["gPauseMap00Tex", "gPauseMap10ENGTex", "gPauseMap20Tex"],
            "MAP",
            new(
                new(80, 40, 30),
                new(140, 60, 60),
                new(140, 60, 60),
                new(80, 40, 30))),
        new(
            "QuestStatus",
            "Quest Status",
            ["gPauseQuestStatus00ENGTex", "gPauseQuestStatus10ENGTex", "gPauseQuestStatus20ENGTex"],
            "QUEST STATUS",
            new(
                new(80, 80, 50),
                new(120, 120, 70),
                new(120, 120, 70),
                new(80, 80, 50))),
        new(
            "Save",
            "Save",
            ["gPauseSave00Tex", "gPauseSave10ENGTex", "gPauseSave20Tex"],
            "SAVE",
            new(
                new(50, 50, 50),
                new(110, 110, 110),
                new(110, 110, 110),
                new(50, 50, 50))),
        new(
            "SelectItem",
            "Select Item",
            ["gPauseSelectItem00ENGTex", "gPauseSelectItem10ENGTex", "gPauseSelectItem20ENGTex"],
            "SELECT ITEM",
            new(
                new(10, 50, 80),
                new(70, 100, 130),
                new(70, 100, 130),
                new(10, 50, 80))),
    ];

    private static readonly HashSet<string> TextureNames = Specs
        .SelectMany(spec => spec.TextureNames)
        .ToHashSet(StringComparer.Ordinal);

    public static bool TryGetTargets(RomVersionProfile profile, out IReadOnlyList<PauseHeaderTextureTarget> targets)
    {
        if (profile.Game != GameKind.OcarinaOfTime || !TextureCatalog.TryGetTextures(profile, out IReadOnlyList<TextureDefinition>? catalog))
        {
            targets = [];
            return false;
        }

        Dictionary<string, TextureDefinition> byName = catalog
            .Where(IsPauseHeaderTexture)
            .GroupBy(texture => texture.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var result = new List<PauseHeaderTextureTarget>(Specs.Count);
        foreach (PauseHeaderTextureSpec spec in Specs)
        {
            if (!byName.TryGetValue(spec.TextureNames[0], out TextureDefinition? left)
                || !byName.TryGetValue(spec.TextureNames[1], out TextureDefinition? middle)
                || !byName.TryGetValue(spec.TextureNames[2], out TextureDefinition? right))
            {
                continue;
            }

            result.Add(new PauseHeaderTextureTarget(spec, left, middle, right));
        }

        targets = result;
        return targets.Count > 0;
    }

    public static IReadOnlyList<PauseHeaderTextureTarget> GetTargets(RomVersionProfile profile)
        => TryGetTargets(profile, out IReadOnlyList<PauseHeaderTextureTarget>? targets)
            ? targets
            : throw new NotSupportedException($"Pause-header texture catalog is not available for {profile.Name}.");

    public static bool IsPauseHeaderTexture(TextureDefinition texture)
        => TextureNames.Contains(texture.Name)
            && texture.Width == TileWidth
            && texture.Height == TileHeight
            && texture.Format == TextureFormat.IA8;
}

public sealed record PauseHeaderTextureSpec(
    string Key,
    string Label,
    IReadOnlyList<string> TextureNames,
    string SampleText,
    PauseHeaderColorRamp OriginalColorRamp)
{
    public IReadOnlyList<string> TemplateFileNames { get; } = TextureNames
        .Select(name => $"{name}.png")
        .ToArray();
}

public readonly record struct PauseHeaderPageColor(byte Red, byte Green, byte Blue);

public sealed record PauseHeaderColorRamp(
    PauseHeaderPageColor Column0,
    PauseHeaderPageColor Column1,
    PauseHeaderPageColor Column2,
    PauseHeaderPageColor Column3)
{
    public PauseHeaderPageColor GetColumn(int index)
        => index switch
        {
            0 => Column0,
            1 => Column1,
            2 => Column2,
            3 => Column3,
            _ => throw new ArgumentOutOfRangeException(nameof(index)),
        };
}

public sealed record PauseHeaderTextureTarget(
    PauseHeaderTextureSpec Spec,
    TextureDefinition Left,
    TextureDefinition Middle,
    TextureDefinition Right)
{
    public IReadOnlyList<TextureDefinition> Textures { get; } = [Left, Middle, Right];
}
