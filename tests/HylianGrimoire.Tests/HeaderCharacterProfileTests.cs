using HylianGrimoire.Codecs;
using HylianGrimoire.Games;
using HylianGrimoire.Glyphs;
using HylianGrimoire.Headers;
using HylianGrimoire.Models;
using HylianGrimoire.Rom;
using HylianGrimoire.Services;
using Xunit;

namespace HylianGrimoire.Tests;

public sealed class HeaderCharacterProfileTests
{
    [Fact]
    public void OotHeaderExportWritesCanonicalHeaderTextForCustomEditorGlyphs()
    {
        CharacterProfileStore store = CharacterProfileStore.Current;
        store.SetGameKind(GameKind.OcarinaOfTime);
        string profileName = $"Header export OoT {Guid.NewGuid():N}";
        string path = CreateTempHeaderPath();

        Assert.True(store.CreateProfile(profileName));
        try
        {
            store.SetDisplayChar(0x7b, '\u00e5');
            GameProfile profile = CreateProfileWithSnapshot(GameKind.OcarinaOfTime, store.CreateSnapshot());
            var workflow = new HeaderDocumentWorkflow();

            workflow.ExportCurrent(
                path,
                [CreateEntry(0x0001, "\u00e5")],
                profile,
                CHeaderExportFormat.Legacy);

            string content = File.ReadAllText(path);
            Assert.Contains("\"{\"", content);
            Assert.DoesNotContain('\u00e5', content);
        }
        finally
        {
            DeleteIfExists(path);
            store.ResetDisplayChar(0x7b);
            store.DeleteSelectedProfile();
        }
    }

    [Fact]
    public void OotMultiLanguageHeaderSaveWritesCanonicalHeaderTextForCustomEditorGlyphs()
    {
        CharacterProfileStore store = CharacterProfileStore.Current;
        store.SetGameKind(GameKind.OcarinaOfTime);
        string profileName = $"Header save OoT {Guid.NewGuid():N}";
        string path = CreateTempHeaderPath();

        Assert.True(store.CreateProfile(profileName));
        try
        {
            store.SetDisplayChar(0x7b, '\u00e5');
            GameProfile profile = CreateProfileWithSnapshot(GameKind.OcarinaOfTime, store.CreateSnapshot());
            var languages = new Dictionary<int, List<MessageEntry>>
            {
                [0] = [CreateEntry(0x0001, "\u00e5")],
                [1] = [CreateEntry(0x0001, "GER")],
            };
            var workflow = new HeaderDocumentWorkflow();

            workflow.Save(path, languages[0], profile, languages);

            string content = File.ReadAllText(path);
            Assert.Contains("\"{\"", content);
            Assert.DoesNotContain('\u00e5', content);
        }
        finally
        {
            DeleteIfExists(path);
            store.ResetDisplayChar(0x7b);
            store.DeleteSelectedProfile();
        }
    }

    [Fact]
    public void MajorasMaskHeaderExportWritesCanonicalHeaderTextForCustomEditorGlyphs()
    {
        CharacterProfileStore store = CharacterProfileStore.Current;
        store.SetGameKind(GameKind.MajorasMask);
        string profileName = $"Header export MM {Guid.NewGuid():N}";
        string path = CreateTempHeaderPath();

        Assert.True(store.CreateProfile(profileName));
        try
        {
            store.SetDisplayChar(0x7b, '\u00e5');
            GameProfile profile = CreateProfileWithSnapshot(GameKind.MajorasMask, store.CreateSnapshot());
            var workflow = new HeaderDocumentWorkflow();

            workflow.ExportCurrent(
                path,
                [CreateMmEntry(0x0200, "\u00e5")],
                profile,
                CHeaderExportFormat.Modern);

            string content = File.ReadAllText(path);
            Assert.Contains("\"{\"", content);
            Assert.DoesNotContain('\u00e5', content);
        }
        finally
        {
            DeleteIfExists(path);
            store.ResetDisplayChar(0x7b);
            store.DeleteSelectedProfile();
            store.SetGameKind(GameKind.OcarinaOfTime);
        }
    }

    [Fact]
    public void MajorasMaskHeaderImportIntoRomEncodesVisualGlyphsWithActiveProfile()
    {
        CharacterProfileStore store = CharacterProfileStore.Current;
        store.SetGameKind(GameKind.MajorasMask);
        string profileName = $"Header import ROM MM {Guid.NewGuid():N}";
        string path = CreateTempHeaderPath();

        Assert.True(store.CreateProfile(profileName));
        try
        {
            store.SetDisplayChar(0x7b, '\u00e5');
            store.SetDisplayChar(0x7d, '\u00c5');
            MessageEncodingProfile encodingProfile = MessageEncodingProfile.MajorasMask
                .WithCharacterProfileSnapshot(store.CreateSnapshot());
            File.WriteAllText(path, """
                DEFINE_MESSAGE(0x0002, 0x00, 0x00,
                MSG(
                HEADER(0x0200, 0xFE, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF)
                "}}{{"
                )
                )
                """);
            RomMessageData romData = CreateMmRomData();
            List<MessageEntry> currentEntries =
            [
                CreateMmEntry(0x0002, "\u00c5"),
            ];
            var workflow = new HeaderDocumentWorkflow();

            HeaderRomImportResult result = workflow.ImportIntoRom(
                path,
                romData,
                currentEntries,
                allWesternLanguages: false,
                CHeaderMessageSlot.Nes,
                _ => encodingProfile);

            Assert.Equal([0x7d, 0x7d, 0x7b, 0x7b], result.RomData.DecompressedRom.Skip(0x200 + 11).Take(4).ToArray());
        }
        finally
        {
            DeleteIfExists(path);
            store.ResetDisplayChar(0x7b);
            store.ResetDisplayChar(0x7d);
            store.DeleteSelectedProfile();
            store.SetGameKind(GameKind.OcarinaOfTime);
        }
    }

    private static GameProfile CreateProfileWithSnapshot(GameKind gameKind, CharacterProfileSnapshot snapshot)
    {
        GameProfile profile = GameProfiles.Get(gameKind);
        return profile with
        {
            EncodingProfile = profile.EncodingProfile.WithCharacterProfileSnapshot(snapshot),
        };
    }

    private static MessageEntry CreateEntry(int id, string text)
        => new(id, type: 0, position: 0, bank: 7, offset: 0)
        {
            Text = text,
        };

    private static MessageEntry CreateMmEntry(int id, string text)
    {
        var metadata = new MajorasMaskMessageMetadata(
            TableTypePosition: 0,
            TextBoxProperties: 0,
            IconId: 0xfe,
            NextTextId: 0xffff,
            FirstChoicePrice: 0xffff,
            SecondChoicePrice: 0xffff,
            Unknown: 0xffff);
        return new MessageEntry(id, type: 0, position: 0, bank: 0x08, offset: 0)
        {
            Text = text,
            CodecMetadata = metadata,
        };
    }

    private static RomMessageData CreateMmRomData()
    {
        var bank = new MessageBankProfile("Messages", 0x000, 0x100, 0x200, 0x1000, TableSegment: 0x08);
        var profile = new RomVersionProfile(
            "Majora's Mask Test ROM",
            string.Empty,
            0,
            0,
            0,
            RomCodecKind.Yaz0,
            false,
            0,
            0,
            0,
            0,
            0,
            [bank],
            new HashSet<int>(),
            Game: GameKind.MajorasMask);
        return new RomMessageData([], profile, false, new byte[0x2000], RomFontResources.Empty, 0, RomMessageSection.Messages);
    }

    private static string CreateTempHeaderPath()
        => Path.Combine(Path.GetTempPath(), $"hylian-grimoire-header-export-{Guid.NewGuid():N}.h");

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
