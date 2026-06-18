using HylianGrimoire.Codecs;
using HylianGrimoire.Codecs.MajorasMask;
using HylianGrimoire.Models;

namespace HylianGrimoire.Headers.MajorasMask;

public static partial class MmCHeaderExporter
{
    private static bool IsBuildGeneratedHelperEntry(MessageEntry entry)
        => entry.Id is FontOrderCodec.MessageId or MmMessageTableCodec.DebuggerEndMessageId;

    private static MajorasMaskMessageMetadata GetMetadata(MessageEntry entry)
    {
        if (entry.CodecMetadata is MajorasMaskMessageMetadata metadata)
        {
            return metadata;
        }

        ushort properties = (ushort)(((entry.Type & 0x0f) << 8) | ((entry.Position & 0x0f) << 4));
        return new MajorasMaskMessageMetadata(
            TableTypePosition: 0,
            TextBoxProperties: properties,
            IconId: 0xfe,
            NextTextId: 0xffff,
            FirstChoicePrice: 0xffff,
            SecondChoicePrice: 0xffff,
            Unknown: 0xffff);
    }

    private static ushort BuildTextBoxProperties(MajorasMaskMessageMetadata metadata, MessageEntry entry)
    {
        return (ushort)((metadata.TextBoxProperties & 0xf00f)
            | ((entry.Type & 0x0f) << 8)
            | ((entry.Position & 0x0f) << 4));
    }
}
