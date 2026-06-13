using System.Text.RegularExpressions;
using HylianGrimoire.Rom;
using HylianGrimoire.Textures;

namespace HylianGrimoire.TextTextures;

public static partial class BossTitleCardTextureCatalog
{
    public const int Width = 128;
    public const int Height = 40;

    private const string BossGroupPrefix = "textures/object_";
    private const string EnglishTitleCardSuffix = "TitleCardENGTex";

    private static readonly IReadOnlyDictionary<string, BossTitleCardText> BossTextByTextureName =
        new Dictionary<string, BossTitleCardText>(StringComparer.Ordinal)
        {
            ["gBarinadeTitleCardENGTex"] = new("Bio-Electric Anemone", "BARINADE"),
            ["gVolvagiaBossTitleCardENGTex"] = new("Subterranean Lava Dragon", "VOLVAGIA"),
            ["gPhantomGanonTitleCardENGTex"] = new("Evil Spirit from Beyond", "PHANTOM GANON"),
            ["gGanondorfTitleCardENGTex"] = new("Great King of Evil", "GANONDORF"),
            ["gGanonTitleCardENGTex"] = new(string.Empty, "GANON"),
            ["gGohmaTitleCardENGTex"] = new("Parasitic Armored Arachnid", "GOHMA"),
            ["gKingDodongoTitleCardENGTex"] = new("Infernal Dinosaur", "KING DODONGO"),
            ["gMorphaTitleCardENGTex"] = new("Giant Aquatic Amoeba", "MORPHA"),
            ["gBongoTitleCardENGTex"] = new("Phantom Shadow Beast", "BONGO BONGO"),
            ["gTwinrovaTitleCardENGTex"] = new("Sorceress Sisters", "TWINROVA"),
        };

    public static bool TryGetTargets(RomVersionProfile profile, out IReadOnlyList<TextureDefinition> textures)
    {
        if (!TextureCatalog.TryGetTextures(profile, out IReadOnlyList<TextureDefinition>? catalog))
        {
            textures = [];
            return false;
        }

        textures = catalog
            .Where(IsBossTitleCardTexture)
            .OrderBy(texture => GetSortIndex(texture.Name))
            .ThenBy(texture => texture.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return textures.Count > 0;
    }

    public static IReadOnlyList<TextureDefinition> GetTargets(RomVersionProfile profile)
        => TryGetTargets(profile, out IReadOnlyList<TextureDefinition>? textures)
            ? textures
            : throw new NotSupportedException($"Boss title-card texture catalog is not available for {profile.Name}.");

    public static bool IsBossTitleCardTexture(TextureDefinition texture)
        => texture.Group.StartsWith(BossGroupPrefix, StringComparison.Ordinal)
            && texture.Name.EndsWith(EnglishTitleCardSuffix, StringComparison.Ordinal)
            && texture.Width == Width
            && texture.Height == Height
            && texture.Format == TextureFormat.IA8;

    public static BossTitleCardText GetDisplayText(TextureDefinition texture)
        => GetDisplayText(texture.Name);

    public static BossTitleCardText GetDisplayText(string textureName)
    {
        if (BossTextByTextureName.TryGetValue(textureName, out BossTitleCardText? text))
        {
            return text;
        }

        string name = LeadingGRegex().Replace(textureName, string.Empty);
        name = TitleCardSuffixRegex().Replace(name, string.Empty);
        MatchCollection matches = WordPartRegex().Matches(name);
        string bossText = matches.Count == 0
            ? name.ToUpperInvariant()
            : string.Join(" ", matches.Select(match => match.Value)).ToUpperInvariant();
        return new BossTitleCardText(string.Empty, bossText);
    }

    private static int GetSortIndex(string textureName)
    {
        int index = 0;
        foreach (string name in BossTextByTextureName.Keys)
        {
            if (string.Equals(textureName, name, StringComparison.Ordinal))
            {
                return index;
            }

            index++;
        }

        return int.MaxValue;
    }

    [GeneratedRegex("^g")]
    private static partial Regex LeadingGRegex();

    [GeneratedRegex("(Boss)?TitleCardENGTex$")]
    private static partial Regex TitleCardSuffixRegex();

    [GeneratedRegex("[A-Z]+(?=[A-Z][a-z]|$)|[A-Z]?[a-z]+|\\d+")]
    private static partial Regex WordPartRegex();
}
