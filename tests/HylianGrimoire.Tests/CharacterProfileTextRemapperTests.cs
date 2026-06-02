using HylianGrimoire.Codecs;
using HylianGrimoire.Games;
using HylianGrimoire.Glyphs;
using Xunit;

namespace HylianGrimoire.Tests;

public sealed class CharacterProfileTextRemapperTests
{
    [Fact]
    public void RemapKeepsControlTagsAndRemapsPlainText()
    {
        var source = new CharacterProfile
        {
            GameKind = GameKind.OcarinaOfTime,
            Characters = new Dictionary<string, string>
            {
                ["0x92"] = "Q",
            },
        };
        char defaultChar = GameProfiles.GetOriginalEncodingProfile(GameKind.OcarinaOfTime).GetDefaultEditorChar(0x92);

        string remapped = CharacterProfileTextRemapper.Remap(
            GameKind.OcarinaOfTime,
            "Q[color:red]Q",
            source,
            targetProfile: null);

        Assert.Equal($"{defaultChar}[color:red]{defaultChar}", remapped);
    }

    [Fact]
    public void RemapCanTargetCustomProfileCharacter()
    {
        MessageEncodingProfile original = GameProfiles.GetOriginalEncodingProfile(GameKind.OcarinaOfTime);
        char sourceChar = original.GetDefaultEditorChar(0x92);
        var target = new CharacterProfile
        {
            GameKind = GameKind.OcarinaOfTime,
            Characters = new Dictionary<string, string>
            {
                ["0x92"] = "Q",
            },
        };

        string remapped = CharacterProfileTextRemapper.Remap(
            GameKind.OcarinaOfTime,
            sourceChar.ToString(),
            sourceProfile: null,
            target);

        Assert.Equal("Q", remapped);
    }
}
