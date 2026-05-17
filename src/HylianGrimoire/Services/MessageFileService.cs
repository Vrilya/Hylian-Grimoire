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

    public static List<MessageEntry> ImportHeader(string path)
    {
        string content = File.ReadAllText(path);
        return CHeaderImporter.Import(content);
    }

    public static void ExportHeader(List<MessageEntry> entries, string path)
    {
        string content = CHeaderExporter.Export(entries);
        File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
