using System.Text;
using HylianGrimoire.Codecs;
using HylianGrimoire.Codecs.MajorasMask;
using HylianGrimoire.Games;
using HylianGrimoire.Headers;
using HylianGrimoire.Models;
using HylianGrimoire.Services;
using Xunit;

namespace HylianGrimoire.Tests;

public sealed class MessageCodecRobustnessTests
{
    public static IEnumerable<object[]> OcarinaCanonicalEditorVectors()
    {
        yield return
        [
            "[quicktexton][quicktextoff][waitbutton][break]",
            new byte[] { 0x08, 0x09, 0x0d, 0x04, 0x02, 0x00, 0x00, 0x00 },
        ];
        yield return
        [
            "[color:red][color:default][color:7f]",
            new byte[] { 0x05, 0x41, 0x05, 0x40, 0x05, 0x7f, 0x02, 0x00 },
        ];
        yield return
        [
            "[shift:2a][textid:3456][breakdelay:20][fade:3c]",
            new byte[] { 0x06, 0x2a, 0x07, 0x34, 0x56, 0x0c, 0x20, 0x0e, 0x3c, 0x02, 0x00, 0x00 },
        ];
        yield return
        [
            "[endfade:013c][sfx:Laugh2][sfx:1234]",
            new byte[] { 0x11, 0x01, 0x3c, 0x12, 0x68, 0x6d, 0x12, 0x12, 0x34, 0x02, 0x00, 0x00 },
        ];
        yield return
        [
            "[item:2d][textspeed:01][background:123456][minigame:06]",
            new byte[] { 0x13, 0x2d, 0x14, 0x01, 0x15, 0x12, 0x34, 0x56, 0x1e, 0x06, 0x02, 0x00 },
        ];
        yield return
        [
            "[A-button][B-button][C-up][Triangle][Stick]",
            new byte[] { 0x9f, 0xa0, 0xa5, 0xa9, 0xaa, 0x02, 0x00, 0x00 },
        ];
    }

    public static IEnumerable<object[]> OcarinaTruncatedArgumentVectors()
    {
        yield return [new byte[] { (byte)'A', 0x05 }];
        yield return [new byte[] { (byte)'A', 0x06 }];
        yield return [new byte[] { (byte)'A', 0x07, 0x12 }];
        yield return [new byte[] { (byte)'A', 0x0c }];
        yield return [new byte[] { (byte)'A', 0x0e }];
        yield return [new byte[] { (byte)'A', 0x11, 0x12 }];
        yield return [new byte[] { (byte)'A', 0x12, 0x12 }];
        yield return [new byte[] { (byte)'A', 0x13 }];
        yield return [new byte[] { (byte)'A', 0x14 }];
        yield return [new byte[] { (byte)'A', 0x15, 0x12, 0x34 }];
        yield return [new byte[] { (byte)'A', 0x1e }];
    }

    public static IEnumerable<object[]> MajorasMaskCanonicalEditorVectors()
    {
        yield return
        [
            "[quicktexton]Hi\n[color:red][A-button][persistent]",
            new byte[] { 0x17, (byte)'H', (byte)'i', 0x11, 0x01, 0xb0, 0x1a, 0xbf },
        ];
        yield return
        [
            "[color:default][color:orange][break][break2][carriagereturn]",
            new byte[] { 0x00, 0x08, 0x10, 0x12, 0x13, 0xbf },
        ];
        yield return
        [
            "[shift:2a][breakdelay:0001][fade:003c][fadeskippable:0010][sfx:2913][delay:000a]",
            new byte[] { 0x14, 0x2a, 0x1b, 0x00, 0x01, 0x1c, 0x00, 0x3c, 0x1d, 0x00, 0x10, 0x1e, 0x29, 0x13, 0x1f, 0x00, 0x0a, 0xbf },
        ];
        yield return
        [
            "[timerpostman][inputbank][event2][hspointsunk1][dekuplaygroundname3]",
            new byte[] { 0xc4, 0xcc, 0xe0, 0xf1, 0xff, 0xbf },
        ];
        yield return
        [
            "[byte:09][byte:be]",
            new byte[] { 0x09, 0xbe, 0xbf },
        ];
    }

    public static IEnumerable<object[]> MajorasMaskAliasEditorVectors()
    {
        yield return
        [
            "[btn_a][btn_b][control_pad]",
            "[A-button][B-button][Control-Pad]",
            new byte[] { 0xb0, 0xb1, 0xbb, 0xbf },
        ];
        yield return
        [
            "[box_break_delayed:0001][fade_skippable:0002][TEXT_SPEED]",
            "[breakdelay:0001][fadeskippable:0002][textspeed]",
            new byte[] { 0x1b, 0x00, 0x01, 0x1d, 0x00, 0x02, 0x0a, 0xbf },
        ];
        yield return
        [
            "[HS_TIME_BOAT_ARCHERY][INPUT_DOGGY_RACETRACK_BET][EVENT2]",
            "[hsboatarcherytime][inputdogbet][event2]",
            new byte[] { 0xf3, 0xd0, 0xe0, 0xbf },
        ];
    }

    public static IEnumerable<object[]> MajorasMaskTruncatedArgumentVectors()
    {
        yield return [new byte[] { (byte)'A', 0x14 }];
        yield return [new byte[] { (byte)'A', 0x1b, 0x12 }];
        yield return [new byte[] { (byte)'A', 0x1c, 0x12 }];
        yield return [new byte[] { (byte)'A', 0x1d, 0x12 }];
        yield return [new byte[] { (byte)'A', 0x1e, 0x12 }];
        yield return [new byte[] { (byte)'A', 0x1f, 0x12 }];
    }

    [Theory]
    [MemberData(nameof(OcarinaCanonicalEditorVectors))]
    public void Ocarina_canonical_editor_control_code_vectors_roundtrip_bytes(string editorText, byte[] expected)
    {
        Assert.Equal(expected, EncodeOcarinaEditorText(editorText));
        Assert.Equal(editorText, DecodeOcarinaEditorText(expected));
    }

    [Fact]
    public void Ocarina_generated_editor_fragments_roundtrip_to_stable_bytes()
    {
        string[] fragments =
        [
            "Hi",
            " ",
            "\n",
            "[quicktexton]",
            "[quicktextoff]",
            "[waitbutton]",
            "[break]",
            "[color:red]",
            "[color:default]",
            "[color:7f]",
            "[shift:2a]",
            "[textid:3456]",
            "[breakdelay:20]",
            "[fade:3c]",
            "[endfade:013c]",
            "[sfx:Laugh2]",
            "[sfx:1234]",
            "[item:2d]",
            "[textspeed:01]",
            "[background:123456]",
            "[archery]",
            "[minigame:06]",
            "[A-button]",
            "[Triangle]",
            "[byte:ab]",
        ];

        uint state = 0x004f_6f54;
        for (int caseIndex = 0; caseIndex < 64; caseIndex++)
        {
            string editorText = BuildGeneratedEditorText(fragments, ref state, minFragments: 3, maxFragments: 12);

            AssertOcarinaEditorBytesStable(editorText);
        }
    }

    [Fact]
    public void Ocarina_generated_byte_streams_decode_to_stable_editor_text()
    {
        uint state = 0x1a2b_3c4d;
        for (int caseIndex = 0; caseIndex < 128; caseIndex++)
        {
            byte[] raw = BuildGeneratedBytes(ref state, minLength: 1, maxLength: 48);

            string decoded = DecodeOcarinaEditorText(raw);
            byte[] encoded = EncodeOcarinaEditorText(decoded);

            Assert.Equal(decoded, DecodeOcarinaEditorText(encoded));
        }
    }

    [Fact]
    public void Ocarina_decoder_preserves_unknown_raw_bytes()
    {
        byte[] raw = [(byte)'H', 0xab, (byte)'I', 0x02];

        List<MessageToken> tokens = MessageCodec.DecodeMessageTokens(raw, 0, raw.Length);
        string editorText = MessageTextSyntax.ToEditorText(tokens);

        Assert.Equal("H[byte:ab]I", editorText);
        Assert.Contains(new RawByteToken(0xab), tokens);
        Assert.Equal(raw, MessageCodec.EncodeMessageTokens(MessageTextSyntax.FromEditorText(editorText)));
    }

    [Fact]
    public void Ocarina_raw_bytes_roundtrip_through_c_headers()
    {
        var entries = new List<MessageEntry>
        {
            new(0x2000, 0, 0, 7, 0)
            {
                Text = "A[byte:ab]B",
            },
        };

        string exported = CHeaderExporter.Export(entries);
        List<MessageEntry> imported = CHeaderImporter.Import(exported);

        Assert.Contains("\"\\xAB\"", exported, StringComparison.Ordinal);
        Assert.Equal(entries[0].Text, imported[0].Text);
        Assert.Equal(
            [(byte)'A', 0xab, (byte)'B', 0x02],
            MessageCodec.EncodeMessageTokens(MessageTextSyntax.FromEditorText(imported[0].Text)));
    }

    [Fact]
    public void Ocarina_byte_inspector_labels_raw_bytes_explicitly()
    {
        var entry = new MessageEntry(0x2000, 0, 0, 7, 0)
        {
            Text = "A[byte:ab]B",
        };

        MessageByteInspection inspection = MessageByteInspectorService.Inspect(GameKind.OcarinaOfTime, entry);

        Assert.Contains(
            inspection.Segments,
            segment => segment.Offset == 1
                && segment.Length == 1
                && segment.Kind == MessageByteSegmentKind.RawByte
                && segment.EditorSyntax == "[byte:ab]");
    }

    [Theory]
    [MemberData(nameof(OcarinaTruncatedArgumentVectors))]
    public void Ocarina_decoder_stops_at_truncated_arguments_without_reading_past_buffer(byte[] raw)
    {
        string editorText = MessageTextSyntax.ToEditorText(MessageCodec.DecodeMessageTokens(raw, 0, raw.Length));

        Assert.Equal("A", editorText);
    }

    [Theory]
    [InlineData("[byte:]")]
    [InlineData("[byte:100]")]
    [InlineData("[byte:nothex]")]
    public void Ocarina_raw_byte_syntax_requires_a_valid_byte(string text)
    {
        InvalidDataException ex = Assert.Throws<InvalidDataException>(() => MessageTextSyntax.FromEditorText(text));

        Assert.Contains(text, ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [MemberData(nameof(MajorasMaskCanonicalEditorVectors))]
    public void MajorasMask_canonical_editor_control_code_vectors_roundtrip_bytes(string editorText, byte[] expected)
    {
        Assert.Equal(expected, EncodeMajorasMaskEditorText(editorText));
        Assert.Equal(editorText, DecodeMajorasMaskEditorText(expected));
    }

    [Fact]
    public void MajorasMask_generated_editor_fragments_roundtrip_to_stable_bytes()
    {
        string[] fragments =
        [
            "Hi",
            " ",
            "\n",
            "[quicktexton]",
            "[quicktextoff]",
            "[persistent]",
            "[break]",
            "[break2]",
            "[carriagereturn]",
            "[color:red]",
            "[color:orange]",
            "[A-button]",
            "[btn_b]",
            "[control_pad]",
            "[shift:2a]",
            "[breakdelay:0001]",
            "[box_break_delayed:0002]",
            "[fade:003c]",
            "[fadeskippable:0010]",
            "[fade_skippable:0011]",
            "[sfx:2913]",
            "[delay:000a]",
            "[timerpostman]",
            "[inputbank]",
            "[event2]",
            "[HS_TIME_BOAT_ARCHERY]",
            "[dekuplaygroundname3]",
            "[byte:09]",
        ];

        uint state = 0x004d_4d21;
        for (int caseIndex = 0; caseIndex < 64; caseIndex++)
        {
            string editorText = BuildGeneratedEditorText(fragments, ref state, minFragments: 3, maxFragments: 12);

            AssertMajorasMaskEditorBytesStable(editorText);
        }
    }

    [Fact]
    public void MajorasMask_generated_byte_streams_decode_to_stable_editor_text()
    {
        uint state = 0x5566_7788;
        for (int caseIndex = 0; caseIndex < 128; caseIndex++)
        {
            byte[] raw = BuildGeneratedBytes(ref state, minLength: 1, maxLength: 48);

            string decoded = DecodeMajorasMaskEditorText(raw);
            byte[] encoded = EncodeMajorasMaskEditorText(decoded);

            Assert.Equal(decoded, DecodeMajorasMaskEditorText(encoded));
        }
    }

    [Theory]
    [MemberData(nameof(MajorasMaskAliasEditorVectors))]
    public void MajorasMask_alias_control_code_vectors_encode_to_canonical_bytes(
        string aliasText,
        string canonicalText,
        byte[] expected)
    {
        Assert.Equal(expected, EncodeMajorasMaskEditorText(aliasText));
        Assert.Equal(canonicalText, DecodeMajorasMaskEditorText(expected));
    }

    [Fact]
    public void MajorasMask_unknown_raw_bytes_roundtrip_through_editor_syntax()
    {
        byte[] raw = [(byte)'H', 0x09, 0x1e, 0x12, 0x34, 0xbf];

        string editorText = MmMessageTextCodec.Decode(raw, 0, raw.Length, MessageEncodingProfile.MajorasMask);

        Assert.Equal("H[byte:09][sfx:1234]", editorText);
        Assert.Equal(raw, MmMessageTextCodec.Encode(editorText, MessageEncodingProfile.MajorasMask));
    }

    [Theory]
    [MemberData(nameof(MajorasMaskTruncatedArgumentVectors))]
    public void MajorasMask_decoder_stops_at_truncated_arguments_without_reading_past_buffer(byte[] raw)
    {
        string editorText = MmMessageTextCodec.Decode(raw, 0, raw.Length, MessageEncodingProfile.MajorasMask);

        Assert.Equal("A", editorText);
    }

    [Fact]
    public void MajorasMask_decoder_treats_invalid_ranges_as_empty_input()
    {
        byte[] raw = [(byte)'A', 0xbf];

        Assert.Equal(string.Empty, MmMessageTextCodec.Decode(raw, -1, raw.Length, MessageEncodingProfile.MajorasMask));
        Assert.Equal(string.Empty, MmMessageTextCodec.Decode(raw, raw.Length, 1, MessageEncodingProfile.MajorasMask));
        Assert.Equal(string.Empty, MmMessageTextCodec.Decode(raw, 0, 0, MessageEncodingProfile.MajorasMask));
    }

    [Theory]
    [InlineData("[byte:]")]
    [InlineData("[byte:100]")]
    [InlineData("[byte:nothex]")]
    [InlineData("[color:notacolor]")]
    [InlineData("[shift:100]")]
    [InlineData("[breakdelay:nothex]")]
    [InlineData("[sfx:10000]")]
    public void MajorasMask_known_argument_tags_require_valid_values(string editorText)
    {
        InvalidDataException ex = Assert.Throws<InvalidDataException>(
            () => EncodeMajorasMaskEditorText(editorText));

        Assert.Contains(editorText, ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static string DecodeOcarinaEditorText(byte[] raw)
        => MessageTextSyntax.ToEditorText(MessageCodec.DecodeMessageTokens(raw, 0, raw.Length));

    private static byte[] EncodeOcarinaEditorText(string editorText)
        => MessageCodec.EncodeMessageTokens(MessageTextSyntax.FromEditorText(editorText));

    private static void AssertOcarinaEditorBytesStable(string editorText)
    {
        byte[] encoded = EncodeOcarinaEditorText(editorText);
        string decoded = DecodeOcarinaEditorText(encoded);

        Assert.Equal(encoded, EncodeOcarinaEditorText(decoded));
    }

    private static string DecodeMajorasMaskEditorText(byte[] raw)
        => MmMessageTextCodec.Decode(raw, 0, raw.Length, MessageEncodingProfile.MajorasMask);

    private static byte[] EncodeMajorasMaskEditorText(string editorText)
        => MmMessageTextCodec.Encode(editorText, MessageEncodingProfile.MajorasMask);

    private static void AssertMajorasMaskEditorBytesStable(string editorText)
    {
        byte[] encoded = EncodeMajorasMaskEditorText(editorText);
        string decoded = DecodeMajorasMaskEditorText(encoded);

        Assert.Equal(encoded, EncodeMajorasMaskEditorText(decoded));
    }

    private static string BuildGeneratedEditorText(
        IReadOnlyList<string> fragments,
        ref uint state,
        int minFragments,
        int maxFragments)
    {
        int count = minFragments + (int)(NextDeterministicValue(ref state) % (uint)(maxFragments - minFragments + 1));
        var builder = new StringBuilder();
        for (int i = 0; i < count; i++)
        {
            int fragmentIndex = (int)(NextDeterministicValue(ref state) % (uint)fragments.Count);
            builder.Append(fragments[fragmentIndex]);
        }

        return builder.ToString();
    }

    private static byte[] BuildGeneratedBytes(ref uint state, int minLength, int maxLength)
    {
        int length = minLength + (int)(NextDeterministicValue(ref state) % (uint)(maxLength - minLength + 1));
        byte[] bytes = new byte[length];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)(NextDeterministicValue(ref state) >> 24);
        }

        return bytes;
    }

    private static uint NextDeterministicValue(ref uint state)
    {
        state = unchecked((state * 1_664_525u) + 1_013_904_223u);
        return state;
    }
}
