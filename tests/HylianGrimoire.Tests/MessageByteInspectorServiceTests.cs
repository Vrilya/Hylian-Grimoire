using HylianGrimoire.Codecs;
using HylianGrimoire.Games;
using HylianGrimoire.Models;
using HylianGrimoire.Services;
using Xunit;

namespace HylianGrimoire.Tests;

public sealed class MessageByteInspectorServiceTests
{
    [Fact]
    public void Ocarina_inspection_matches_message_codec_bytes()
    {
        var entry = new MessageEntry(0x1234, 0, 0, 7, 0)
        {
            Text = "Hi\n[color:red]A[waitbutton][textid:3456]",
        };

        MessageByteInspection inspection = MessageByteInspectorService.Inspect(GameKind.OcarinaOfTime, entry);
        byte[] encoded = MessageCodec.EncodeMessageTokens(MessageTextSyntax.FromEditorText(entry.Text));

        Assert.Equal(encoded, inspection.Bytes);
        Assert.Equal(GameKind.OcarinaOfTime, inspection.GameKind);
        Assert.Equal(0x1234, inspection.MessageId);
        Assert.Contains(inspection.Sections, section => section.Kind == MessageByteSectionKind.MessageData && section.Bytes.SequenceEqual(encoded));
    }

    [Fact]
    public void Ocarina_inspection_includes_message_table_entry_bytes()
    {
        var entry = new MessageEntry(0x1234, 2, 3, 7, 0x4567)
        {
            Text = "Hi",
        };

        MessageByteInspection inspection = MessageByteInspectorService.Inspect(GameKind.OcarinaOfTime, entry);
        MessageByteSection tableSection = Assert.Single(
            inspection.Sections,
            section => section.Kind == MessageByteSectionKind.MessageTableEntry);

        Assert.Equal(new byte[] { 0x12, 0x34, 0x23, 0x00, 0x07, 0x00, 0x45, 0x67 }, tableSection.Bytes);
        Assert.Contains(tableSection.Segments, segment => segment.Offset == 0 && segment.Length == 2 && segment.Label == "Message ID");
        Assert.Contains(tableSection.Segments, segment => segment.Offset == 2 && segment.Length == 1 && segment.Label == "Type / Position");
        Assert.Contains(tableSection.Segments, segment => segment.Offset == 5 && segment.Length == 3 && segment.Label == "Message Offset");
    }

    [Fact]
    public void MajorasMask_inspection_includes_table_header_and_message_data_sections()
    {
        var metadata = new MajorasMaskMessageMetadata(
            TableTypePosition: 0x23,
            TextBoxProperties: 0x1201,
            IconId: 0xfe,
            NextTextId: 0x3456,
            FirstChoicePrice: 0x0005,
            SecondChoicePrice: 0xffff,
            Unknown: 0xabcd);
        var entry = new MessageEntry(0x1234, 2, 3, 8, 0x4567)
        {
            Text = "[quicktexton]Hi\n[color:red]A[sfx:2913]",
            CodecMetadata = metadata,
        };

        MessageByteInspection inspection = MessageByteInspectorService.Inspect(
            GameKind.MajorasMask,
            entry,
            MessageEncodingProfile.MajorasMask);

        MessageByteSection tableSection = Assert.Single(
            inspection.Sections,
            section => section.Kind == MessageByteSectionKind.MessageTableEntry);
        MessageByteSection headerSection = Assert.Single(
            inspection.Sections,
            section => section.Kind == MessageByteSectionKind.MessageHeader);
        MessageByteSection dataSection = Assert.Single(
            inspection.Sections,
            section => section.Kind == MessageByteSectionKind.MessageData);

        Assert.Equal(new byte[] { 0x12, 0x34, 0x23, 0x00, 0x08, 0x00, 0x45, 0x67 }, tableSection.Bytes);
        Assert.Equal(metadata.BuildHeader(entry.Type, entry.Position), headerSection.Bytes);
        Assert.Equal(headerSection.Bytes.Concat(dataSection.Bytes), inspection.Bytes);
        Assert.Contains(headerSection.Segments, segment => segment.Offset == 0 && segment.Length == 2 && segment.Kind == MessageByteSegmentKind.HeaderField);
        Assert.Contains(headerSection.Segments, segment => segment.Label == "Textbox Properties" && segment.Description.Contains("centered yes", StringComparison.Ordinal));
        Assert.Contains(headerSection.Segments, segment => segment.Label == "First Choice Price" && segment.Description.Contains("5 (0x0005)", StringComparison.Ordinal));
        Assert.Contains(headerSection.Segments, segment => segment.Label == "Second Choice Price" && segment.Description.Contains("-1 (0xffff)", StringComparison.Ordinal));
        Assert.Contains(dataSection.Segments, segment => segment.Kind == MessageByteSegmentKind.ControlCode && segment.EditorSyntax == "[quicktexton]");
        Assert.Contains(dataSection.Segments, segment => segment.Kind == MessageByteSegmentKind.LineBreak);
        Assert.Contains(dataSection.Segments, segment => segment.Kind == MessageByteSegmentKind.ControlCode && segment.EditorSyntax == "[color:red]");
        Assert.Contains(dataSection.Segments, segment => segment.Kind == MessageByteSegmentKind.Parameter && segment.EditorSyntax == "[sfx:2913]");
        Assert.Contains(dataSection.Segments, segment => segment.Kind == MessageByteSegmentKind.Terminator);
        Assert.Contains(dataSection.Segments, segment => segment.Kind == MessageByteSegmentKind.Padding);
    }

    [Fact]
    public void MajorasMask_control_code_menu_entries_can_be_inspected()
    {
        var metadata = new MajorasMaskMessageMetadata(
            TableTypePosition: 0,
            TextBoxProperties: 0,
            IconId: 0xfe,
            NextTextId: 0xffff,
            FirstChoicePrice: 0xffff,
            SecondChoicePrice: 0xffff,
            Unknown: 0xffff);

        foreach (MessageControlCodeEntry controlCode in MessageControlCodeCatalog.GetGroups(GameKind.MajorasMask).SelectMany(group => group.Entries))
        {
            var entry = new MessageEntry(0x0100, 0, 0, 8, 0)
            {
                Text = controlCode.InsertText,
                CodecMetadata = metadata,
            };

            MessageByteInspection inspection = MessageByteInspectorService.Inspect(
                GameKind.MajorasMask,
                entry,
                MessageEncodingProfile.MajorasMask);

            Assert.NotEmpty(inspection.Bytes);
            Assert.NotEmpty(inspection.Segments);
            Assert.Contains(inspection.Sections, section => section.Kind == MessageByteSectionKind.MessageHeader);
        }
    }

    [Fact]
    public void Ocarina_inspection_segments_text_controls_parameters_terminator_and_padding()
    {
        var entry = new MessageEntry(0x0001, 0, 0, 7, 0)
        {
            Text = "Hi\n[color:red]A[waitbutton][textid:3456]",
        };

        MessageByteInspection inspection = MessageByteInspectorService.Inspect(GameKind.OcarinaOfTime, entry);

        Assert.Contains(inspection.Segments, segment => segment.Offset == 0 && segment.Length == 2 && segment.Kind == MessageByteSegmentKind.Text);
        Assert.Contains(inspection.Segments, segment => segment.Offset == 2 && segment.Length == 1 && segment.Kind == MessageByteSegmentKind.LineBreak);
        Assert.Contains(inspection.Segments, segment => segment.Offset == 3 && segment.Length == 1 && segment.Kind == MessageByteSegmentKind.ControlCode && segment.EditorSyntax == "[color:red]");
        Assert.Contains(inspection.Segments, segment => segment.Offset == 4 && segment.Length == 1 && segment.Kind == MessageByteSegmentKind.Parameter && segment.Description.Contains("Red", StringComparison.Ordinal));
        Assert.Contains(inspection.Segments, segment => segment.Kind == MessageByteSegmentKind.ControlCode && segment.EditorSyntax == "[waitbutton]");
        Assert.Contains(inspection.Segments, segment => segment.Kind == MessageByteSegmentKind.ControlCode && segment.Label == "Next Text ID");
        Assert.Contains(inspection.Segments, segment => segment.Kind == MessageByteSegmentKind.Parameter && segment.EditorSyntax == "[textid:3456]");
        Assert.Contains(inspection.Segments, segment => segment.Kind == MessageByteSegmentKind.Terminator && segment.EditorSyntax is null);
        Assert.Contains(inspection.Segments, segment => segment.Kind == MessageByteSegmentKind.Padding);
    }

    [Fact]
    public void MajorasMask_inspection_uses_decomp_aligned_control_descriptions()
    {
        var entry = new MessageEntry(0x0001, 0, 0, 8, 0)
        {
            Text = "[textspeed][continue][inputbank][event2]",
        };

        MessageByteInspection inspection = MessageByteInspectorService.Inspect(
            GameKind.MajorasMask,
            entry,
            MessageEncodingProfile.MajorasMask);

        Assert.Contains(inspection.Segments, segment => segment.EditorSyntax == "[textspeed]" && segment.Description.Contains("without an explicit encoded argument", StringComparison.Ordinal));
        Assert.Contains(inspection.Segments, segment => segment.EditorSyntax == "[continue]" && segment.Description.Contains("continue icon", StringComparison.Ordinal));
        Assert.Contains(inspection.Segments, segment => segment.EditorSyntax == "[inputbank]" && segment.Description.Contains("bank rupee input behavior", StringComparison.Ordinal));
        Assert.Contains(inspection.Segments, segment => segment.EditorSyntax == "[event2]" && segment.Description.Contains("alternate event-controlled textbox state", StringComparison.Ordinal));
        Assert.DoesNotContain(inspection.Segments, segment => segment.Description == "Prints a Majora's Mask dynamic value.");
    }

    [Fact]
    public void Ocarina_control_code_menu_entries_can_be_inspected()
    {
        foreach (MessageControlCodeEntry controlCode in MessageControlCodeCatalog.GetGroups(GameKind.OcarinaOfTime).SelectMany(group => group.Entries))
        {
            var entry = new MessageEntry(0x0100, 0, 0, 7, 0)
            {
                Text = controlCode.InsertText,
            };

            MessageByteInspection inspection = MessageByteInspectorService.Inspect(GameKind.OcarinaOfTime, entry);

            Assert.NotEmpty(inspection.Bytes);
            Assert.NotEmpty(inspection.Segments);
        }
    }

    [Fact]
    public void Inspector_support_is_explicit_per_game()
    {
        Assert.True(MessageByteInspectorService.CanInspect(GameKind.OcarinaOfTime));
        Assert.True(MessageByteInspectorService.CanInspect(GameKind.MajorasMask));

        var entry = new MessageEntry(0x0001, 0, 0, 7, 0) { Text = "Hello" };
        Assert.NotEmpty(MessageByteInspectorService.Inspect(GameKind.MajorasMask, entry).Bytes);
    }
}
