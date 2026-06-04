using HylianGrimoire.Games;
using HylianGrimoire.Models;
using HylianGrimoire.Rom;
using HylianGrimoire.Textures;

namespace HylianGrimoire.O2r;

public static class O2rModPortProfileCatalog
{
    private static readonly O2rModPortProfile ShipOfHarkinianProfile = new ShipOfHarkinianO2rProfile();
    private static readonly O2rModPortProfile TwoShipTwoHarkinianProfile = new TwoShipTwoHarkinianO2rProfile();

    private static readonly IReadOnlyDictionary<GameKind, O2rModPortProfile> Profiles = new Dictionary<GameKind, O2rModPortProfile>
    {
        [GameKind.OcarinaOfTime] = ShipOfHarkinianProfile,
        [GameKind.MajorasMask] = TwoShipTwoHarkinianProfile,
    };

    public static bool TryGetProfile(
        GameProfile? activeGameProfile,
        RomVersionProfile? romProfile,
        out O2rModPortProfile profile)
    {
        profile = null!;
        if (activeGameProfile is null || !Profiles.TryGetValue(activeGameProfile.Kind, out O2rModPortProfile? candidate))
        {
            return false;
        }

        if (romProfile is not null)
        {
            if (!candidate.SupportsRomProfile(romProfile))
            {
                return false;
            }

            profile = candidate;
            return true;
        }

        if (!candidate.SupportsCurrentDocumentTextResources)
        {
            return false;
        }

        profile = candidate;
        return true;
    }

    public static O2rModPortProfile GetProfile(GameProfile activeGameProfile, RomVersionProfile? romProfile)
        => TryGetProfile(activeGameProfile, romProfile, out O2rModPortProfile? profile)
            ? profile
            : throw new NotSupportedException("The active project cannot create O2R mods for the current ROM.");

    private sealed class ShipOfHarkinianO2rProfile : O2rModPortProfile
    {
        private const string EnglishMessageResourcePath = "text/nes_message_data_static/nes_message_data_static";
        private const string GermanMessageResourcePath = "text/ger_message_data_static/ger_message_data_static";
        private const string FrenchMessageResourcePath = "text/fra_message_data_static/fra_message_data_static";
        private const string CreditsResourcePath = "text/staff_message_data_static/staff_message_data_static";

        public ShipOfHarkinianO2rProfile()
            : base(
                O2rModPortKind.ShipOfHarkinian,
                GameProfiles.Get(GameKind.OcarinaOfTime),
                "Ship of Harkinian",
                "SoH Mod Maker",
                "HylianGrimoireMod",
                "SoH O2R mod")
        {
        }

        public override bool SupportsCurrentDocumentTextResources => true;

        public override bool SupportsRomProfile(RomVersionProfile profile)
            => profile.Game == GameKind.OcarinaOfTime;

        public override IReadOnlyList<O2rTextResourceDefinition> GetRomTextResources(RomMessageData romData)
        {
            var resources = new List<O2rTextResourceDefinition>();
            IReadOnlyList<MessageBankProfile> editableBanks = romData.Profile.GameProfile.MessageBankLayout.GetEditableBanks(romData.Profile);
            for (int bankIndex = 0; bankIndex < editableBanks.Count && bankIndex < 3; bankIndex++)
            {
                resources.Add(new O2rTextResourceDefinition(
                    editableBanks[bankIndex].Name,
                    GetMessageResourcePath(bankIndex),
                    O2rTextResourceKind.MessageBank,
                    bankIndex));
            }

            resources.Add(new O2rTextResourceDefinition(
                "Credits",
                CreditsResourcePath,
                O2rTextResourceKind.Credits,
                BankIndex: -1));

            return resources;
        }

        public override IReadOnlyList<O2rTextResourceDefinition> GetCurrentDocumentTextResources(
            IReadOnlyDictionary<int, List<MessageEntry>> languageEntries)
        {
            return
            [
                .. languageEntries
                    .Where(pair => pair.Key is >= 0 and <= 2 && pair.Value.Count > 0)
                    .OrderBy(pair => pair.Key)
                    .Select(pair => new O2rTextResourceDefinition(
                        $"Language {pair.Key + 1}",
                        GetMessageResourcePath(pair.Key),
                        O2rTextResourceKind.CurrentDocument,
                        pair.Key)),
            ];
        }

        public override byte[] PackTextResource(
            O2rTextResourceDefinition resource,
            ReadOnlySpan<byte> messageBytes,
            ReadOnlySpan<byte> tableBytes)
            => O2rResourcePacker.PackSohText(messageBytes, tableBytes, addFontOrder: true);

        private static string GetMessageResourcePath(int bankIndex)
            => bankIndex switch
            {
                0 => EnglishMessageResourcePath,
                1 => GermanMessageResourcePath,
                2 => FrenchMessageResourcePath,
                _ => throw new ArgumentOutOfRangeException(nameof(bankIndex), bankIndex, "Unsupported SoH message bank."),
            };
    }

    private sealed class TwoShipTwoHarkinianO2rProfile : O2rModPortProfile
    {
        private const string MessageResourcePath = "text/message_data_static/message_data_static";
        private const string CreditsResourcePath = "text/staff_message_data_static/staff_message_data_static";

        private static readonly HashSet<string> SupportedProfileNames = new(StringComparer.Ordinal)
        {
            "Majora's Mask NTSC-U",
            "Majora's Mask NTSC-U GameCube",
        };

        public TwoShipTwoHarkinianO2rProfile()
            : base(
                O2rModPortKind.TwoShipTwoHarkinian,
                GameProfiles.Get(GameKind.MajorasMask),
                "2 Ship 2 Harkinian",
                "2S2H Mod Maker",
                "HylianGrimoireMmMod",
                "2S2H O2R mod")
        {
        }

        public override bool SupportsRomProfile(RomVersionProfile profile)
            => profile.Game == GameKind.MajorasMask && SupportedProfileNames.Contains(profile.Name);

        public override bool SupportsTextureResource(TextureDefinition texture)
        {
            string group = texture.Group.Replace('\\', '/');
            return !group.Equals("interface/kanji", StringComparison.Ordinal)
                && !IsUnsupportedCodeTexture(texture);
        }

        public override string GetTextureResourcePath(TextureDefinition texture)
        {
            string group = texture.Group.Replace('\\', '/');
            if (group.Equals("code", StringComparison.Ordinal))
            {
                return texture.Name switch
                {
                    "sCircleTex" => "code/fbdemo_circle/gCircleTex",
                    "sDebugDisplayBallTex" or
                    "sDebugDisplayCircleTex" or
                    "sDebugDisplayCrossTex" or
                    "sDebugDisplayCursorTex" => $"code/debug_display/{texture.Name}",
                    _ => base.GetTextureResourcePath(texture),
                };
            }

            if (group.StartsWith("scenes/", StringComparison.Ordinal))
            {
                return $"scenes/nonmq/{group["scenes/".Length..]}/{texture.Name}";
            }

            string path = base.GetTextureResourcePath(texture);
            return path.StartsWith("interface/", StringComparison.Ordinal)
                ? path["interface/".Length..]
                : path.StartsWith("archives/", StringComparison.Ordinal)
                    ? path["archives/".Length..]
                    : path;
        }

        private static bool IsUnsupportedCodeTexture(TextureDefinition texture)
            => texture.Group.Replace('\\', '/').Equals("code", StringComparison.Ordinal)
                && texture.Name is "sWhiteSquareTex";

        public override IReadOnlyList<O2rTextResourceDefinition> GetRomTextResources(RomMessageData romData)
        {
            if (!SupportsRomProfile(romData.Profile))
            {
                return [];
            }

            return
            [
                new O2rTextResourceDefinition(
                    romData.Profile.MessageBanks[0].Name,
                    MessageResourcePath,
                    O2rTextResourceKind.MessageBank,
                    BankIndex: 0),
                new O2rTextResourceDefinition(
                    "Credits",
                    CreditsResourcePath,
                    O2rTextResourceKind.Credits,
                    BankIndex: -1),
            ];
        }

        public override byte[] PackTextResource(
            O2rTextResourceDefinition resource,
            ReadOnlySpan<byte> messageBytes,
            ReadOnlySpan<byte> tableBytes)
            => O2rResourcePacker.PackMajorasMaskText(
                messageBytes,
                tableBytes,
                messageDataHasHeaders: resource.Kind == O2rTextResourceKind.MessageBank);
    }
}
