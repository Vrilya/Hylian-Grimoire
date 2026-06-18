namespace HylianGrimoire.Games.MajorasMask;

public enum MmMessageIconDrawKind
{
    None,
    Image,
    Heart,
    Rupee,
    StrayFairy,
}

public sealed record MmMessageIconEntry(
    byte Value,
    string Label,
    string? RelativePath,
    MmMessageIconDrawKind DrawKind = MmMessageIconDrawKind.Image,
    byte? ItemId = null);

public static class MmMessageIconCatalog
{
    private const byte NoIcon = 0xfe;

    private static readonly Dictionary<byte, (string Name, string RelativePath)> ItemAssets = new()
    {
        [0x00] = ("Ocarina of Time", Icon("gItemIconOcarinaOfTimeTex")),
        [0x01] = ("Bow", Icon("gItemIconBowTex")),
        [0x02] = ("Fire Arrow", Icon("gItemIconFireArrowTex")),
        [0x03] = ("Ice Arrow", Icon("gItemIconIceArrowTex")),
        [0x04] = ("Light Arrow", Icon("gItemIconLightArrowTex")),
        [0x05] = ("Fairy Ocarina", Icon("gItemIconFairyOcarinaTex")),
        [0x06] = ("Bomb", Icon("gItemIconBombTex")),
        [0x07] = ("Bombchu", Icon("gItemIconBombchuTex")),
        [0x08] = ("Deku Stick", Icon("gItemIconDekuStickTex")),
        [0x09] = ("Deku Nut", Icon("gItemIconDekuNutTex")),
        [0x0A] = ("Magic Beans", Icon("gItemIconMagicBeansTex")),
        [0x0B] = ("Slingshot", Icon("gItemIconSlingshotTex")),
        [0x0C] = ("Powder Keg", Icon("gItemIconPowderKegTex")),
        [0x0D] = ("Pictograph Box", Icon("gItemIconPictographBoxTex")),
        [0x0E] = ("Lens of Truth", Icon("gItemIconLensofTruthTex")),
        [0x0F] = ("Hookshot", Icon("gItemIconHookshotTex")),
        [0x10] = ("Great Fairy's Sword", Icon("gItemIconGreatFairysSwordTex")),
        [0x11] = ("Longshot", Icon("gItemIconLongshotTex")),
        [0x12] = ("Bottle", Icon("gItemIconEmptyBottleTex")),
        [0x13] = ("Red Potion", Icon("gItemIconRedPotionTex")),
        [0x14] = ("Green Potion", Icon("gItemIconGreenPotionTex")),
        [0x15] = ("Blue Potion", Icon("gItemIconBluePotionTex")),
        [0x16] = ("Fairy", Icon("gItemIconBottledFairyTex")),
        [0x17] = ("Deku Princess", Icon("gItemIconBottledDekuPrincessTex")),
        [0x18] = ("Milk Bottle", Icon("gItemIconBottledFullMilkTex")),
        [0x19] = ("Half Milk", Icon("gItemIconBottledHalfMilkTex")),
        [0x1A] = ("Fish", Icon("gItemIconBottledFishTex")),
        [0x1B] = ("Bug", Icon("gItemIconBottledBugTex")),
        [0x1C] = ("Blue Fire", Icon("gItemIconBottledBlueFireTex")),
        [0x1D] = ("Poe", Icon("gItemIconBottledPoeTex")),
        [0x1E] = ("Big Poe", Icon("gItemIconBottledBigPoeTex")),
        [0x1F] = ("Spring Water", Icon("gItemIconSpringWaterTex")),
        [0x20] = ("Hot Spring Water", Icon("gItemIconHotSpringWaterTex")),
        [0x21] = ("Zora Egg", Icon("gItemIconBottledZoraEggTex")),
        [0x22] = ("Gold Dust", Icon("gItemIconBottledGoldDustTex")),
        [0x23] = ("Mushroom", Icon("gItemIconBottledMushroomTex")),
        [0x24] = ("Seahorse", Icon("gItemIconBottledSeahorseTex")),
        [0x25] = ("Chateau Romani", Icon("gItemIconChateauRomaniTex")),
        [0x26] = ("Hylian Loach", Icon("gItemIconBottledHylianLoachTex")),
        [0x27] = ("Obaba Drink", Icon("gItemIconEmptyBottle2Tex")),
        [0x28] = ("Moon's Tear", Icon("gItemIconMoonsTearTex")),
        [0x29] = ("Land Deed", Icon("gItemIconLandDeedTex")),
        [0x2A] = ("Swamp Deed", Icon("gItemIconSwampDeedTex")),
        [0x2B] = ("Mountain Deed", Icon("gItemIconMountainDeedTex")),
        [0x2C] = ("Ocean Deed", Icon("gItemIconOceanDeedTex")),
        [0x2D] = ("Room Key", Icon("gItemIconRoomKeyTex")),
        [0x2E] = ("Letter to Mama", Icon("gItemIconLetterToMamaTex")),
        [0x2F] = ("Letter to Kafei", Icon("gItemIconLetterToKafeiTex")),
        [0x30] = ("Pendant of Memories", Icon("gItemIconPendantOfMemoriesTex")),
        [0x31] = ("Tingle Map", Icon("gItemIconTingleMapTex")),
        [0x32] = ("Deku Mask", Icon("gItemIconDekuMaskTex")),
        [0x33] = ("Goron Mask", Icon("gItemIconGoronMaskTex")),
        [0x34] = ("Zora Mask", Icon("gItemIconZoraMaskTex")),
        [0x35] = ("Fierce Deity Mask", Icon("gItemIconFierceDeityMaskTex")),
        [0x36] = ("Mask of Truth", Icon("gItemIconMaskOfTruthTex")),
        [0x37] = ("Kafei's Mask", Icon("gItemIconKafeisMaskTex")),
        [0x38] = ("All-Night Mask", Icon("gItemIconAllNightMaskTex")),
        [0x39] = ("Bunny Hood", Icon("gItemIconBunnyHoodTex")),
        [0x3A] = ("Keaton Mask", Icon("gItemIconKeatonMaskTex")),
        [0x3B] = ("Garo Mask", Icon("gItemIconGaroMaskTex")),
        [0x3C] = ("Romani Mask", Icon("gItemIconRomaniMaskTex")),
        [0x3D] = ("Circus Leader's Mask", Icon("gItemIconCircusLeaderMaskTex")),
        [0x3E] = ("Postman's Hat", Icon("gItemIconPostmansHatTex")),
        [0x3F] = ("Couple's Mask", Icon("gItemIconCouplesMaskTex")),
        [0x40] = ("Great Fairy's Mask", Icon("gItemIconGreatFairyMaskTex")),
        [0x41] = ("Gibdo Mask", Icon("gItemIconGibdoMaskTex")),
        [0x42] = ("Don Gero's Mask", Icon("gItemIconDonGeroMaskTex")),
        [0x43] = ("Kamaro's Mask", Icon("gItemIconKamaroMaskTex")),
        [0x44] = ("Captain's Hat", Icon("gItemIconCaptainsHatTex")),
        [0x45] = ("Stone Mask", Icon("gItemIconStoneMaskTex")),
        [0x46] = ("Bremen Mask", Icon("gItemIconBremenMaskTex")),
        [0x47] = ("Blast Mask", Icon("gItemIconBlastMaskTex")),
        [0x48] = ("Mask of Scents", Icon("gItemIconMaskOfScentsTex")),
        [0x49] = ("Giant's Mask", Icon("gItemIconGiantsMaskTex")),
        [0x4A] = ("Fire Bow", Icon("gItemIconBowFireTex")),
        [0x4B] = ("Ice Bow", Icon("gItemIconBowIceTex")),
        [0x4C] = ("Light Bow", Icon("gItemIconBowLightTex")),
        [0x4D] = ("Kokiri Sword", Icon("gItemIconKokiriSwordTex")),
        [0x4E] = ("Razor Sword", Icon("gItemIconRazorSwordTex")),
        [0x4F] = ("Gilded Sword", Icon("gItemIconGildedSwordTex")),
        [0x50] = ("Fierce Deity Sword", Icon("gItemIconFierceDeitySwordTex")),
        [0x51] = ("Hero's Shield", Icon("gItemIconHerosShieldTex")),
        [0x52] = ("Mirror Shield", Icon("gItemIconMirrorShieldTex")),
        [0x53] = ("Quiver 30", Icon("gItemIconQuiver30Tex")),
        [0x54] = ("Quiver 40", Icon("gItemIconQuiver40Tex")),
        [0x55] = ("Quiver 50", Icon("gItemIconQuiver50Tex")),
        [0x56] = ("Bomb Bag 20", Icon("gItemIconBombBag20Tex")),
        [0x57] = ("Bomb Bag 30", Icon("gItemIconBombBag30Tex")),
        [0x58] = ("Bomb Bag 40", Icon("gItemIconBombBag40Tex")),
        [0x59] = ("Wallet", Icon("gItemIconDefaultWalletTex")),
        [0x5A] = ("Adult's Wallet", Icon("gItemIconAdultsWalletTex")),
        [0x5B] = ("Giant's Wallet", Icon("gItemIconGiantsWalletTex")),
        [0x5C] = ("Fishing Rod", Icon("gItemIconFishingRodTex")),
        [0x5D] = ("Odolwa's Remains", Icon("gItemIconOdolwasRemainsTex")),
        [0x5E] = ("Goht's Remains", Icon("gItemIconGohtsRemainsTex")),
        [0x5F] = ("Gyorg's Remains", Icon("gItemIconGyorgsRemainsTex")),
        [0x60] = ("Twinmold's Remains", Icon("gItemIconTwinmoldsRemainsTex")),
        [0x61] = ("Song Note", Icon("gItemIconSongNoteTex")),
        [0x62] = ("Song Note", Icon("gItemIconSongNoteTex")),
        [0x63] = ("Song Note", Icon("gItemIconSongNoteTex")),
        [0x64] = ("Song Note", Icon("gItemIconSongNoteTex")),
        [0x65] = ("Song Note", Icon("gItemIconSongNoteTex")),
        [0x66] = ("Song Note", Icon("gItemIconSongNoteTex")),
        [0x67] = ("Song Note", Icon("gItemIconSongNoteTex")),
        [0x68] = ("Song Note", Icon("gItemIconSongNoteTex")),
        [0x69] = ("Song Note", Icon("gItemIconSongNoteTex")),
        [0x6A] = ("Song Note", Icon("gItemIconSongNoteTex")),
        [0x6B] = ("Song Note", Icon("gItemIconSongNoteTex")),
        [0x6C] = ("Song Note", Icon("gItemIconSongNoteTex")),
        [0x6D] = ("Bombers' Notebook", Icon("gItemIconBombersNotebookTex")),
        [0x6E] = ("Skulltula Token", Quest("gQuestIconGoldSkulltulaTex")),
        [0x6F] = ("Heart Container", Quest("gQuestIconHeartContainerTex")),
        [0x70] = ("Piece of Heart", Quest("gQuestIconPieceOfHeartTex")),
        [0x74] = ("Boss Key", Quest("gQuestIconBossKeyTex")),
        [0x75] = ("Compass", Quest("gQuestIconCompassTex")),
        [0x76] = ("Dungeon Map", Quest("gQuestIconDungeonMapTex")),
        [0x77] = ("Stray Fairy", Quest("gQuestIconLinkHumanFaceTex")),
        [0x78] = ("Small Key", Quest("gQuestIconSmallKeyTex")),
        [0x79] = ("Small Magic Jar", Quest("gQuestIconSmallMagicJarTex")),
        [0x7A] = ("Big Magic Jar", Quest("gQuestIconBigMagicJarTex")),
        [0x9B] = ("Deku Stick Upgrade", Icon("gItemIconDekuStickTex")),
        [0xB8] = ("Anju", Schedule("gBombersNotebookPhotoAnjuTex")),
        [0xB9] = ("Kafei", Schedule("gBombersNotebookPhotoKafeiTex")),
        [0xBA] = ("Curiosity Shop Man", Schedule("gBombersNotebookPhotoCuriosityShopManTex")),
        [0xBB] = ("Bomb Shop Lady", Schedule("gBombersNotebookPhotoBombShopLadyTex")),
        [0xBC] = ("Romani", Schedule("gBombersNotebookPhotoRomaniTex")),
        [0xBD] = ("Cremia", Schedule("gBombersNotebookPhotoCremiaTex")),
        [0xBE] = ("Mayor Dotour", Schedule("gBombersNotebookPhotoMayorDotourTex")),
        [0xBF] = ("Madame Aroma", Schedule("gBombersNotebookPhotoMadameAromaTex")),
        [0xC0] = ("Toto", Schedule("gBombersNotebookPhotoTotoTex")),
        [0xC1] = ("Gorman", Schedule("gBombersNotebookPhotoGormanTex")),
        [0xC2] = ("Postman", Schedule("gBombersNotebookPhotoPostmanTex")),
        [0xC3] = ("Rosa Sisters", Schedule("gBombersNotebookPhotoRosaSistersTex")),
        [0xC4] = ("Toilet Hand", Schedule("gBombersNotebookPhotoToiletHandTex")),
        [0xC5] = ("Anju's Grandmother", Schedule("gBombersNotebookPhotoAnjusGrandmotherTex")),
        [0xC6] = ("Kamaro", Schedule("gBombersNotebookPhotoKamaroTex")),
        [0xC7] = ("Grog", Schedule("gBombersNotebookPhotoGrogTex")),
        [0xC8] = ("Gorman Brothers", Schedule("gBombersNotebookPhotoGormanBrothersTex")),
        [0xC9] = ("Shiro", Schedule("gBombersNotebookPhotoShiroTex")),
        [0xCA] = ("Guru-Guru", Schedule("gBombersNotebookPhotoGuruGuruTex")),
        [0xCB] = ("Bombers", Schedule("gBombersNotebookPhotoBombersTex")),
        [0xCC] = ("Notebook Mark", Schedule("gBombersNotebookEntryIconExclamationPointLargeTex")),
    };

    private static readonly Dictionary<byte, (string Name, string RelativePath, MmMessageIconDrawKind DrawKind)> SpecialItemAssets = new()
    {
        [0x77] = ("Stray Fairy", Parameter("gStrayFairyWoodfallIconTex"), MmMessageIconDrawKind.StrayFairy),
        [0x83] = ("Recovery Heart", Parameter("gHeartFullTex"), MmMessageIconDrawKind.Heart),
        [0x84] = ("Green Rupee", Parameter("gRupeeCounterIconTex"), MmMessageIconDrawKind.Rupee),
        [0x85] = ("Blue Rupee", Parameter("gRupeeCounterIconTex"), MmMessageIconDrawKind.Rupee),
        [0x86] = ("10 Rupees", Parameter("gRupeeCounterIconTex"), MmMessageIconDrawKind.Rupee),
        [0x87] = ("Red Rupee", Parameter("gRupeeCounterIconTex"), MmMessageIconDrawKind.Rupee),
        [0x88] = ("Purple Rupee", Parameter("gRupeeCounterIconTex"), MmMessageIconDrawKind.Rupee),
        [0x89] = ("Silver Rupee", Parameter("gRupeeCounterIconTex"), MmMessageIconDrawKind.Rupee),
        [0x8A] = ("Huge Rupee", Parameter("gRupeeCounterIconTex"), MmMessageIconDrawKind.Rupee),
    };

    private static readonly Dictionary<byte, byte> MessageIconToItemId = BuildMessageIconMap();

    public static IReadOnlyList<MmMessageIconEntry> Items { get; } = Enumerable
        .Range(0, 0x100)
        .Select(value =>
        {
            byte iconId = (byte)value;
            if (iconId == NoIcon)
            {
                return new MmMessageIconEntry(iconId, "0xFE None", null, MmMessageIconDrawKind.None);
            }

            if (MessageIconToItemId.TryGetValue(iconId, out byte itemId)
                && TryGetAsset(itemId, out var asset))
            {
                return new MmMessageIconEntry(iconId, $"0x{iconId:X2} {asset.Name}", asset.RelativePath, asset.DrawKind, itemId);
            }

            return new MmMessageIconEntry(iconId, $"0x{iconId:X2}", null, MmMessageIconDrawKind.None);
        })
        .ToArray();

    public static MmMessageIconEntry Get(byte iconId) => Items[iconId];

    private static Dictionary<byte, byte> BuildMessageIconMap()
    {
        var map = new Dictionary<byte, byte>();

        UseSequentialRange(0x01, 0x07, 0x84);
        Use(0x08, 0x5A);
        Use(0x09, 0x5B);
        UseRange(0x0A, 0x0B, 0x83);
        Use(0x0C, 0x70);
        Use(0x0D, 0x6F);
        Use(0x0E, 0x79);
        Use(0x0F, 0x7A);
        Use(0x10, 0x83);
        Use(0x11, 0x77);
        UseRange(0x12, 0x13, 0x83);
        UseRange(0x14, 0x18, 0x06);
        Use(0x19, 0x08);
        Use(0x1A, 0x07);
        Use(0x1B, 0x56);
        Use(0x1C, 0x57);
        Use(0x1D, 0x58);
        UseRange(0x1E, 0x21, 0x01);
        Use(0x22, 0x53);
        Use(0x23, 0x54);
        Use(0x24, 0x55);
        Use(0x25, 0x02);
        Use(0x26, 0x03);
        Use(0x27, 0x04);
        UseRange(0x28, 0x2A, 0x09);
        Use(0x2F, 0x9B);
        Use(0x32, 0x51);
        Use(0x33, 0x52);
        Use(0x34, 0x0C);
        Use(0x35, 0x0A);
        Use(0x36, 0x0D);
        Use(0x37, 0x4D);
        Use(0x38, 0x4E);
        Use(0x39, 0x4F);
        Use(0x3A, 0x50);
        Use(0x3B, 0x10);
        Use(0x3C, 0x78);
        Use(0x3D, 0x74);
        Use(0x3E, 0x76);
        Use(0x3F, 0x75);
        Use(0x40, 0x0C);
        Use(0x41, 0x0F);
        Use(0x42, 0x0E);
        Use(0x43, 0x0D);
        Use(0x44, 0x5C);
        Use(0x4C, 0x00);
        Use(0x50, 0x6D);
        Use(0x52, 0x6E);
        Use(0x55, 0x5D);
        Use(0x56, 0x5E);
        Use(0x57, 0x5F);
        Use(0x58, 0x60);
        Use(0x59, 0x13);
        Use(0x5A, 0x12);
        Use(0x5B, 0x13);
        Use(0x5C, 0x14);
        Use(0x5D, 0x15);
        Use(0x5E, 0x16);
        Use(0x5F, 0x17);
        Use(0x60, 0x18);
        Use(0x61, 0x19);
        Use(0x62, 0x1A);
        Use(0x63, 0x1B);
        Use(0x64, 0x1C);
        Use(0x65, 0x1D);
        Use(0x66, 0x1E);
        Use(0x67, 0x1F);
        Use(0x68, 0x20);
        Use(0x69, 0x21);
        Use(0x6A, 0x22);
        Use(0x6B, 0x23);
        Use(0x6E, 0x24);
        Use(0x6F, 0x25);
        Use(0x70, 0x26);
        Use(0x78, 0x32);
        Use(0x79, 0x33);
        Use(0x7A, 0x34);
        Use(0x7B, 0x35);
        Use(0x7C, 0x36);
        Use(0x7D, 0x37);
        Use(0x7E, 0x38);
        Use(0x7F, 0x39);
        Use(0x80, 0x3A);
        Use(0x81, 0x3B);
        Use(0x82, 0x3C);
        Use(0x83, 0x3D);
        Use(0x84, 0x3E);
        Use(0x85, 0x3F);
        Use(0x86, 0x40);
        Use(0x87, 0x41);
        Use(0x88, 0x42);
        Use(0x89, 0x43);
        Use(0x8A, 0x44);
        Use(0x8B, 0x45);
        Use(0x8C, 0x46);
        Use(0x8D, 0x47);
        Use(0x8E, 0x48);
        Use(0x8F, 0x49);
        Use(0x91, 0x25);
        Use(0x92, 0x18);
        Use(0x93, 0x22);
        Use(0x94, 0x26);
        Use(0x95, 0x24);
        Use(0x96, 0x28);
        Use(0x97, 0x29);
        Use(0x98, 0x2A);
        Use(0x99, 0x2B);
        Use(0x9A, 0x2C);
        Use(0xA0, 0x2D);
        Use(0xA1, 0x2E);
        Use(0xAA, 0x2F);
        Use(0xAB, 0x30);
        UseRange(0xB3, 0xB9, 0x31);
        UseRange(0xC8, 0xCA, 0x61);
        Use(0xCB, 0x62);
        Use(0xCC, 0x63);
        Use(0xCD, 0x64);
        Use(0xCE, 0x65);
        Use(0xCF, 0x66);
        Use(0xD0, 0x67);
        Use(0xD1, 0x68);
        Use(0xD2, 0x69);
        Use(0xD3, 0x6A);
        Use(0xD4, 0x6B);
        Use(0xD5, 0x6C);
        Use(0xD6, 0x62);
        Use(0xD7, 0x61);
        UseRange(0xD8, 0xD9, 0x61);
        Use(0xDA, 0x62);
        Use(0xDB, 0x63);
        UseSequentialRange(0xDC, 0xEF, 0xB8);
        Use(0xF0, 0xCC);

        return map;

        void Use(byte iconId, byte itemId) => map[iconId] = itemId;

        void UseRange(byte firstIconId, byte lastIconId, byte itemId)
        {
            for (byte iconId = firstIconId; iconId <= lastIconId; iconId++)
            {
                Use(iconId, itemId);
            }
        }

        void UseSequentialRange(byte firstIconId, byte lastIconId, byte firstItemId)
        {
            for (byte iconId = firstIconId, itemId = firstItemId; iconId <= lastIconId; iconId++, itemId++)
            {
                Use(iconId, itemId);
            }
        }
    }

    private static bool TryGetAsset(
        byte itemId,
        out (string Name, string RelativePath, MmMessageIconDrawKind DrawKind) asset)
    {
        if (SpecialItemAssets.TryGetValue(itemId, out asset))
        {
            return true;
        }

        if (ItemAssets.TryGetValue(itemId, out var itemAsset))
        {
            asset = (itemAsset.Name, itemAsset.RelativePath, MmMessageIconDrawKind.Image);
            return true;
        }

        asset = default;
        return false;
    }

    private static string Icon(string textureName)
        => Path.Combine("icon_item_static_yar", $"{textureName}.png");

    private static string Quest(string textureName)
        => Path.Combine("icon_item_24_static_yar", $"{textureName}.png");

    private static string Schedule(string textureName)
        => Path.Combine("schedule_dma_static_yar", $"{textureName}.png");

    private static string Parameter(string textureName)
        => Path.Combine("parameter_static", $"{textureName}.png");
}
