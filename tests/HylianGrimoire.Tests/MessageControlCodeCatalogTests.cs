using HylianGrimoire.Codecs;
using HylianGrimoire.Codecs.MajorasMask;
using HylianGrimoire.Games;
using HylianGrimoire.Models;
using HylianGrimoire.Services;
using Xunit;

namespace HylianGrimoire.Tests;

public sealed class MessageControlCodeCatalogTests
{
    [Theory]
    [InlineData(GameKind.OcarinaOfTime)]
    [InlineData(GameKind.MajorasMask)]
    public void Message_control_code_catalog_has_well_formed_groups(GameKind gameKind)
    {
        IReadOnlyList<MessageControlCodeGroup> groups = MessageControlCodeCatalog.GetGroups(gameKind);

        Assert.NotEmpty(groups);
        Assert.Equal(groups.Count, groups.Select(group => group.Name).Distinct(StringComparer.Ordinal).Count());
        foreach (MessageControlCodeGroup group in groups)
        {
            Assert.NotEmpty(group.Entries);
            Assert.Equal(group.Entries.Count, group.Entries.Select(entry => entry.Label).Distinct(StringComparer.Ordinal).Count());
            foreach (MessageControlCodeEntry entry in group.Entries)
            {
                Assert.False(string.IsNullOrWhiteSpace(entry.Label));
                Assert.False(string.IsNullOrEmpty(entry.InsertText));
                Assert.InRange(entry.SelectionStartOffset, -1, entry.InsertText.Length);
                if (entry.SelectionStartOffset >= 0)
                {
                    Assert.InRange(entry.SelectionLength, 1, entry.InsertText.Length - entry.SelectionStartOffset);
                }
            }
        }
    }

    [Fact]
    public void Message_control_code_catalog_keeps_ocarina_and_majora_entries_separate()
    {
        IReadOnlyList<MessageControlCodeEntry> ootEntries = Flatten(GameKind.OcarinaOfTime);
        IReadOnlyList<MessageControlCodeEntry> mmEntries = Flatten(GameKind.MajorasMask);

        Assert.Contains(ootEntries, entry => entry.InsertText == "[ocarina]");
        Assert.DoesNotContain(mmEntries, entry => entry.InsertText == "[ocarina]");
        Assert.Contains(mmEntries, entry => entry.InsertText == "[owlwarp]");
        Assert.DoesNotContain(ootEntries, entry => entry.InsertText == "[owlwarp]");
    }

    [Fact]
    public void Message_control_code_catalog_uses_precise_flow_descriptions()
    {
        IReadOnlyList<MessageControlCodeEntry> ootEntries = Flatten(GameKind.OcarinaOfTime);
        IReadOnlyList<MessageControlCodeEntry> mmEntries = Flatten(GameKind.MajorasMask);

        Assert.Contains(ootEntries, entry => entry.InsertText == "[textid:0000]" && entry.Label == "Next Text ID");
        Assert.Contains(ootEntries, entry => entry.InsertText == "[event]" && entry.Description.Contains("event-controlled textbox state", StringComparison.Ordinal));
        Assert.Contains(mmEntries, entry => entry.InsertText == "[textspeed]" && entry.Description.Contains("no explicit argument", StringComparison.Ordinal));
        Assert.Contains(mmEntries, entry => entry.InsertText == "[continue]" && entry.Description.Contains("continue icon", StringComparison.Ordinal));
        Assert.Contains(mmEntries, entry => entry.InsertText == "[inputbank]" && entry.Description.Contains("bank rupee input behavior", StringComparison.Ordinal));
        Assert.DoesNotContain(mmEntries, entry => entry.Description.Contains("next configured message", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Ocarina_control_code_menu_entries_encode_as_control_tokens()
    {
        foreach (MessageControlCodeEntry entry in Flatten(GameKind.OcarinaOfTime))
        {
            List<MessageToken> tokens = MessageTextSyntax.FromEditorText(entry.InsertText);
            Assert.NotEmpty(tokens);
            Assert.DoesNotContain(tokens, token => token is TextToken text && text.Text.Contains('['));

            byte[] encoded = MessageCodec.EncodeMessageTokens(tokens, MessageEncodingProfile.Default);
            Assert.NotEmpty(encoded);
            Assert.NotEqual((byte)'[', encoded[0]);
        }
    }

    [Fact]
    public void Majoras_mask_control_code_menu_entries_encode_as_control_bytes()
    {
        foreach (MessageControlCodeEntry entry in Flatten(GameKind.MajorasMask))
        {
            byte[] encoded = MmMessageTextCodec.Encode(entry.InsertText, MessageEncodingProfile.MajorasMask);

            Assert.NotEmpty(encoded);
            Assert.NotEqual((byte)'[', encoded[0]);
        }
    }

    private static IReadOnlyList<MessageControlCodeEntry> Flatten(GameKind gameKind)
        => MessageControlCodeCatalog.GetGroups(gameKind)
            .SelectMany(group => group.Entries)
            .ToArray();
}
