using System.Text;
using HylianGrimoire.Codecs;
using HylianGrimoire.Models;

namespace HylianGrimoire.Headers.MajorasMask;

public static partial class MmCHeaderExporter
{
    public static string Export(
        IReadOnlyList<MessageEntry> entries,
        MessageEncodingProfile? encodingProfile = null,
        MessageEncodingProfile? headerEncodingProfile = null)
    {
        encodingProfile ??= MessageEncodingProfile.MajorasMask;
        headerEncodingProfile ??= MessageEncodingProfile.MajorasMaskOriginal;
        var sb = new StringBuilder();
        foreach (MessageEntry entry in entries.Where(entry => !IsBuildGeneratedHelperEntry(entry)).OrderBy(entry => entry.Id))
        {
            if (MmStaffCreditsCHeaderExporter.IsEntry(entry))
            {
                MmStaffCreditsCHeaderExporter.Append(sb, entry);
                continue;
            }

            MajorasMaskMessageMetadata metadata = GetMetadata(entry);
            int tableType = (metadata.TableTypePosition >> 4) & 0x0f;
            int tablePosition = metadata.TableTypePosition & 0x0f;
            ushort textBoxProperties = BuildTextBoxProperties(metadata, entry);

            sb.Append(CultureInvariant($"DEFINE_MESSAGE(0x{entry.Id:X4}, 0x{tableType:X2}, 0x{tablePosition:X2},"));
            sb.AppendLine();
            sb.AppendLine("MSG(");
            sb.Append(CultureInvariant(
                $"HEADER(0x{textBoxProperties:X4}, 0x{metadata.IconId:X2}, 0x{metadata.NextTextId:X4}, {FormatSignedWord(metadata.FirstChoicePrice)}, {FormatSignedWord(metadata.SecondChoicePrice)}, 0x{metadata.Unknown:X4})"));
            string body = string.IsNullOrEmpty(entry.Text)
                ? string.Empty
                : FormatBody(entry.Text, encodingProfile, headerEncodingProfile);
            if (body.Length > 0)
            {
                sb.AppendLine();
                sb.Append(body);
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine();
            }

            sb.AppendLine(")");
            sb.AppendLine(")");
            sb.AppendLine();
        }

        string result = sb.ToString().ReplaceLineEndings("\n");
        return result.EndsWith("\n\n", StringComparison.Ordinal)
            ? result[..^1]
            : result;
    }
}
