using HylianGrimoire.Rom;
using Xunit;

namespace HylianGrimoire.Tests;

public sealed class RomProfileValidatorTests
{
    [Fact]
    public void RegisteredRomProfilesAreStructurallyValid()
    {
        IReadOnlyList<RomProfileValidationIssue> issues = RomProfileValidator.ValidateAll(RomVersionDatabase.Profiles);

        Assert.True(
            issues.Count == 0,
            string.Join(Environment.NewLine, issues.Select(issue => $"{issue.ProfileName} [{issue.Rule}]: {issue.Message}")));
    }

    [Fact]
    public void ValidatorReportsBrokenOcarinaPalPointerBank()
    {
        RomVersionProfile profile = RomVersionDatabase.Profiles.Single(profile => profile.Name == "PAL 1.0");
        MessageBankProfile brokenLanguage2 = profile.MessageBanks[1] with
        {
            OffsetMode = MessageBankOffsetMode.Table,
            ExcludesFontMessage = false,
            PointerTableOffset = 0,
        };
        RomVersionProfile broken = profile with
        {
            MessageBanks =
            [
                profile.MessageBanks[0],
                brokenLanguage2,
                profile.MessageBanks[2],
            ],
        };

        IReadOnlyList<RomProfileValidationIssue> issues = RomProfileValidator.Validate(broken);

        Assert.Contains(issues, issue => issue.Rule == "OotPalPointerBank");
    }

    [Fact]
    public void ValidatorReportsBasicStructuralProblems()
    {
        var profile = new RomVersionProfile(
            "Broken Profile",
            "bad",
            BuildDateOffset: -1,
            DmaTableOffset: -1,
            DmaEntryCount: 1,
            RomCodecKind.Yaz0,
            RawDeflateHasNoHeader: true,
            TargetCompressedSizeMiB: -1,
            CreditsTableOffset: 0x100,
            CreditsTableSize: 0,
            CreditsDataOffset: 0,
            CreditsDataSize: 0,
            MessageBanks:
            [
                new("Broken Bank", 0x1000, 0x100, 0x1080, 0x100),
            ],
            UncompressedEntryIndices: new HashSet<int> { 2 });

        IReadOnlyList<RomProfileValidationIssue> issues = RomProfileValidator.Validate(profile);
        string[] rules = issues.Select(issue => issue.Rule).ToArray();

        Assert.Contains("BuildDate", rules);
        Assert.Contains("BuildDateOffset", rules);
        Assert.Contains("DmaTableOffset", rules);
        Assert.Contains("UncompressedEntryIndices", rules);
        Assert.Contains("CreditsBank", rules);
        Assert.Contains("MessageBanks[0]", rules);
        Assert.Contains("RawDeflateHasNoHeader", rules);
        Assert.Contains("TargetCompressedSizeMiB", rules);
    }

    [Fact]
    public void ValidatorReportsHeaderRangeProblems()
    {
        RomVersionProfile profile = RomVersionDatabase.Profiles.Single(profile => profile.Name == "NTSC 1.2");
        RomVersionProfile overlappingHeader = profile with
        {
            BuildDateOffset = profile.DmaTableOffset,
        };
        RomVersionProfile overflowingDma = profile with
        {
            DmaTableOffset = int.MaxValue - 8,
            DmaEntryCount = 2,
        };

        IReadOnlyList<RomProfileValidationIssue> headerIssues = RomProfileValidator.Validate(overlappingHeader);
        IReadOnlyList<RomProfileValidationIssue> dmaIssues = RomProfileValidator.Validate(overflowingDma);

        Assert.Contains(headerIssues, issue => issue.Rule == "ProfileHeaderRanges");
        Assert.Contains(dmaIssues, issue => issue.Rule == "DmaTableRange");
    }

    [Fact]
    public void ValidatorReportsGameSpecificFontBaselineProblems()
    {
        RomVersionProfile ocarinaProfile = RomVersionDatabase.Profiles.Single(profile => profile.Name == "NTSC 1.2");
        RomVersionProfile majoraProfile = RomVersionDatabase.Profiles.Single(profile => profile.Name == "Majora's Mask NTSC-U");

        IReadOnlyList<RomProfileValidationIssue> ootIssues = RomProfileValidator.Validate(ocarinaProfile with
        {
            FontBaseline = RomFontBaseline.MajorasMask,
        });
        IReadOnlyList<RomProfileValidationIssue> ootPalIssues = RomProfileValidator.Validate(ocarinaProfile with
        {
            FontBaseline = RomFontBaseline.PalMultiLanguage,
        });
        IReadOnlyList<RomProfileValidationIssue> mmIssues = RomProfileValidator.Validate(majoraProfile with
        {
            FontBaseline = RomFontBaseline.Standard,
        });

        Assert.Contains(ootIssues, issue => issue.Rule == "OotFontBaseline");
        Assert.Contains(ootPalIssues, issue => issue.Rule == "OotFontBaseline");
        Assert.Contains(mmIssues, issue => issue.Rule == "MmFontBaseline");
    }

    [Fact]
    public void ValidatorReportsCrossBankAndSpecialBankRangeOverlap()
    {
        RomVersionProfile majoraProfile = RomVersionDatabase.Profiles.Single(profile => profile.Name == "Majora's Mask EU 1.0");
        MessageBankProfile overlappingLanguage2 = majoraProfile.MessageBanks[1] with
        {
            MessageDataOffset = majoraProfile.MessageBanks[0].MessageDataOffset,
        };
        RomVersionProfile overlappingMessageBanks = majoraProfile with
        {
            MessageBanks =
            [
                majoraProfile.MessageBanks[0],
                overlappingLanguage2,
                majoraProfile.MessageBanks[2],
                majoraProfile.MessageBanks[3],
            ],
        };

        RomVersionProfile ocarinaProfile = RomVersionDatabase.Profiles.Single(profile => profile.Name == "NTSC 1.2");
        RomVersionProfile overlappingCredits = ocarinaProfile with
        {
            CreditsDataOffset = ocarinaProfile.MessageBanks[0].MessageDataOffset,
            CreditsDataSize = 0x20,
        };

        IReadOnlyList<RomProfileValidationIssue> bankIssues = RomProfileValidator.Validate(overlappingMessageBanks);
        IReadOnlyList<RomProfileValidationIssue> creditsIssues = RomProfileValidator.Validate(overlappingCredits);

        Assert.Contains(bankIssues, issue => issue.Rule == "ProfileRanges");
        Assert.Contains(creditsIssues, issue => issue.Rule == "ProfileRanges");
    }
}
