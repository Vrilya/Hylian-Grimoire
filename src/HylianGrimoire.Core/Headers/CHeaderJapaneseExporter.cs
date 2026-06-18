using System.Text;
using HylianGrimoire.Models;

namespace HylianGrimoire.Headers;

internal static partial class CHeaderJapaneseExporter
{
    public static string DecodeRawEntry(MessageEntry entry)
    {
        byte[] encoded = entry.EncodedBytesOverride
            ?? entry.OriginalEncodedBytes
            ?? throw new InvalidDataException($"Message 0x{entry.Id:x4}: Japanese ROM message has no encoded data to export.");

        try
        {
            return DecodeMessageHeader(encoded);
        }
        catch (Exception ex) when (ex is ArgumentException or DecoderFallbackException or InvalidDataException)
        {
            return FormatByteInitializer(TrimEnd(encoded));
        }
    }
}
