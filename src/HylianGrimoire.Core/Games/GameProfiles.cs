namespace HylianGrimoire.Games;

using HylianGrimoire.Codecs;
using HylianGrimoire.Rom;
using HylianGrimoire.Rom.MajorasMask;
using HylianGrimoire.Services;

public static class GameProfiles
{
    private static readonly IReadOnlyDictionary<GameKind, GameProfile> Profiles = new Dictionary<GameKind, GameProfile>
    {
        [GameKind.OcarinaOfTime] = new(
            GameKind.OcarinaOfTime,
            "Ocarina of Time",
            "OoT",
            new GameAssetPaths(
                "Assets/Games/Oot",
                "Assets/Games/Oot/Preview/Oot",
                "Assets/Games/Oot/TextureCatalog",
                "Assets/Games/Oot/Title"),
            OotMessageTypeCatalog.Instance,
            OotMessagePositionCatalog.Instance,
            OotEditorTextSyntax.Instance,
            MessageEncodingProfile.Default,
            OotMessageBankCodec.Instance,
            OotMessageBankLayout.Instance,
            GameCapabilities.OcarinaOfTime),
        [GameKind.MajorasMask] = new(
            GameKind.MajorasMask,
            "Majora's Mask",
            "MM",
            new GameAssetPaths(
                "Assets/Games/MM",
                "Assets/Games/MM/Preview",
                "Assets/Games/MM/TextureCatalog",
                "Assets/Games/MM/Title"),
            MmMessageTypeCatalog.Instance,
            MmMessagePositionCatalog.Instance,
            MmEditorTextSyntax.Instance,
            MessageEncodingProfile.MajorasMask,
            MmMessageBankCodec.Instance,
            MmMessageBankLayout.Instance,
            GameCapabilities.MajorasMask),
    };

    public static GameProfile Get(GameKind kind) => Profiles[kind];

    public static MessageEncodingProfile GetOriginalEncodingProfile(GameKind kind)
    {
        return kind switch
        {
            GameKind.OcarinaOfTime => MessageEncodingProfile.Original,
            GameKind.MajorasMask => MessageEncodingProfile.MajorasMaskOriginal,
            _ => throw new NotSupportedException($"No original encoding profile is registered for {kind}.")
        };
    }

    public static IReadOnlyCollection<GameProfile> All { get; } = Profiles.Values.ToArray();
}
