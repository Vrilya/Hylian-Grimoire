using System.Text;
using HylianGrimoire.Codecs;
using HylianGrimoire.Headers;
using HylianGrimoire.Models;

namespace HylianGrimoire.Services;

public static class MessageFileService
{
    public static List<MessageEntry> LoadTableFiles(string tblPath, string binPath)
    {
        byte[] tblData = File.ReadAllBytes(tblPath);
        byte[] binData = File.ReadAllBytes(binPath);
        return MessageTableCodec.ParseTable(tblData, binData);
    }

    public static void SaveTableFiles(List<MessageEntry> entries, string tblPath, string binPath)
    {
        var (tblBytes, msgBytes) = MessageTableCodec.BuildFiles(entries);
        File.WriteAllBytes(tblPath, tblBytes);
        File.WriteAllBytes(binPath, msgBytes);
    }

    public static List<MessageEntry> ImportHeader(
        string path,
        CHeaderMessageSlot preferredSlot = CHeaderMessageSlot.Nes,
        bool allowWesternFallback = true)
    {
        string content = File.ReadAllText(path);
        return CHeaderImporter.Import(content, preferredSlot, allowWesternFallback);
    }

    public static void ExportHeader(
        List<MessageEntry> entries,
        string path,
        CHeaderExportFormat format = CHeaderExportFormat.Legacy)
    {
        string content = CHeaderExporter.Export(entries, format);
        File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    public static void ExportHeaderLanguages(
        IReadOnlyList<MessageEntry>? jpnEntries,
        IReadOnlyList<MessageEntry>? nesEntries,
        IReadOnlyList<MessageEntry>? gerEntries,
        IReadOnlyList<MessageEntry>? fraEntries,
        string path)
    {
        string content = CHeaderExporter.ExportModernLanguages(jpnEntries, nesEntries, gerEntries, fraEntries);
        File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
