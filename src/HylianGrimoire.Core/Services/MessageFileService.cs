using System.Text;
using HylianGrimoire.Codecs;
using HylianGrimoire.Games;
using HylianGrimoire.Headers;
using HylianGrimoire.Headers.MajorasMask;
using HylianGrimoire.Models;
using HylianGrimoire.Rom;

namespace HylianGrimoire.Services;

public sealed record MessageFileDocument(List<MessageEntry> Entries, GameProfile GameProfile);

public static class MessageFileService
{
    public static MessageFileDocument LoadTableFiles(string tblPath, string binPath)
    {
        byte[] tblData = File.ReadAllBytes(tblPath);
        byte[] binData = File.ReadAllBytes(binPath);
        GameProfile gameProfile = DetectTableFileGame(tblData, binData);
        List<MessageEntry> entries = gameProfile.MessageBankCodec.Parse(
            tblData,
            binData,
            new MessageBankProfile("Data Files", 0, tblData.Length, 0, binData.Length),
            null,
            gameProfile.EncodingProfile);
        return new MessageFileDocument(entries, gameProfile);
    }

    public static void SaveTableFiles(List<MessageEntry> entries, string tblPath, string binPath, GameProfile gameProfile)
    {
        var (tblBytes, msgBytes) = gameProfile.MessageBankCodec.Build(entries, gameProfile.EncodingProfile);
        AtomicFileWriter.WriteAllBytesBatch(
        [
            new AtomicFileWrite(tblPath, tblBytes),
            new AtomicFileWrite(binPath, msgBytes),
        ]);
    }

    public static List<MessageEntry> ImportHeader(
        string path,
        CHeaderMessageSlot preferredSlot = CHeaderMessageSlot.Nes,
        bool allowWesternFallback = true,
        GameEncodingProfileResolver? getEncodingProfile = null)
    {
        string content = File.ReadAllText(path);
        return ImportHeaderContent(content, preferredSlot, allowWesternFallback, getEncodingProfile);
    }

    public static List<MessageEntry> ImportHeaderContent(
        string content,
        CHeaderMessageSlot preferredSlot = CHeaderMessageSlot.Nes,
        bool allowWesternFallback = true,
        GameEncodingProfileResolver? getEncodingProfile = null)
    {
        if (MmCHeaderImporter.LooksLikeMajorasMask(content))
        {
            return MmCHeaderImporter.Import(content, ResolveEncodingProfile(GameKind.MajorasMask, getEncodingProfile));
        }

        return CHeaderImporter.Import(
            content,
            preferredSlot,
            allowWesternFallback,
            ResolveEncodingProfile(GameKind.OcarinaOfTime, getEncodingProfile));
    }

    public static void ExportHeader(
        List<MessageEntry> entries,
        string path,
        GameProfile gameProfile,
        CHeaderExportFormat format = CHeaderExportFormat.Legacy,
        MessageEncodingProfile? headerEncodingProfile = null)
    {
        headerEncodingProfile ??= GameProfiles.GetOriginalEncodingProfile(gameProfile.Kind);
        string content = gameProfile.Kind switch
        {
            GameKind.OcarinaOfTime => CHeaderExporter.Export(entries, format, gameProfile.EncodingProfile, headerEncodingProfile),
            GameKind.MajorasMask => MmCHeaderExporter.Export(entries, gameProfile.EncodingProfile, headerEncodingProfile),
            _ => throw new NotSupportedException($"No C header exporter is registered for {gameProfile.DisplayName}.")
        };
        AtomicFileWriter.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    public static void ExportHeaderLanguages(
        IReadOnlyList<MessageEntry>? jpnEntries,
        IReadOnlyList<MessageEntry>? nesEntries,
        IReadOnlyList<MessageEntry>? gerEntries,
        IReadOnlyList<MessageEntry>? fraEntries,
        string path,
        MessageEncodingProfile? encodingProfile = null,
        MessageEncodingProfile? headerEncodingProfile = null)
    {
        encodingProfile ??= MessageEncodingProfile.Default;
        headerEncodingProfile ??= MessageEncodingProfile.Original;
        string content = CHeaderExporter.ExportModernLanguages(
            jpnEntries,
            nesEntries,
            gerEntries,
            fraEntries,
            encodingProfile,
            headerEncodingProfile);
        AtomicFileWriter.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static GameProfile DetectTableFileGame(byte[] tblData, byte[] binData)
    {
        var matches = GameProfiles.All
            .Where(profile => profile.MessageBankCodec.CanParseTableFiles(tblData, binData))
            .ToList();

        if (matches.Count == 1)
        {
            return matches[0];
        }

        if (matches.Count > 1)
        {
            string gameNames = string.Join(", ", matches.Select(profile => profile.DisplayName));
            throw new InvalidDataException($"The message files match more than one game profile: {gameNames}.");
        }

        throw new InvalidDataException("The message files do not match any supported game profile.");
    }

    private static MessageEncodingProfile ResolveEncodingProfile(
        GameKind gameKind,
        GameEncodingProfileResolver? getEncodingProfile)
    {
        return getEncodingProfile?.Invoke(gameKind)
            ?? GameProfiles.Get(gameKind).EncodingProfile;
    }
}
