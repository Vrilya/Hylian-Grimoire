using HylianGrimoire.Games;

namespace HylianGrimoire.Rom;

public sealed record RomProfileValidationIssue(string ProfileName, string Rule, string Message);

public static class RomProfileValidator
{
    private const int BuildDateLength = 17;
    private const int DmaEntrySize = 16;

    public static IReadOnlyList<RomProfileValidationIssue> ValidateAll(IEnumerable<RomVersionProfile> profiles)
    {
        RomVersionProfile[] profileArray = profiles.ToArray();
        var issues = new List<RomProfileValidationIssue>();

        foreach (RomVersionProfile profile in profileArray)
        {
            issues.AddRange(Validate(profile));
        }

        foreach (IGrouping<string, RomVersionProfile> group in profileArray.GroupBy(profile => profile.Name))
        {
            if (group.Count() > 1)
            {
                issues.Add(new RomProfileValidationIssue(
                    group.Key,
                    "UniqueName",
                    $"ROM profile name '{group.Key}' is registered more than once."));
            }
        }

        foreach (IGrouping<(string BuildDate, int Offset), RomVersionProfile> group in profileArray.GroupBy(profile => (profile.BuildDate, profile.BuildDateOffset)))
        {
            if (group.Count() > 1)
            {
                issues.Add(new RomProfileValidationIssue(
                    string.Join(", ", group.Select(profile => profile.Name)),
                    "UniqueBuildSignature",
                    $"Build signature '{group.Key.BuildDate}' at 0x{group.Key.Offset:X} is registered more than once."));
            }
        }

        return issues;
    }

    public static IReadOnlyList<RomProfileValidationIssue> Validate(RomVersionProfile profile)
    {
        var issues = new List<RomProfileValidationIssue>();
        ValidateProfileHeader(profile, issues);
        ValidateCreditsProfile(profile, issues);
        ValidateMessageBanks(profile, issues);
        ValidateJapaneseBank(profile, issues);
        ValidateProfileRanges(profile, issues);
        ValidateFontProfile(profile, issues);
        ValidateCompressionProfile(profile, issues);
        ValidateCapabilities(profile, issues);
        return issues;
    }

    private static void ValidateProfileHeader(RomVersionProfile profile, List<RomProfileValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(profile.Name))
        {
            Add(issues, profile, "Name", "ROM profile name must not be empty.");
        }

        if (profile.BuildDate.Length != BuildDateLength)
        {
            Add(issues, profile, "BuildDate", $"Build date must be exactly {BuildDateLength} ASCII characters.");
        }

        if (profile.BuildDate.Any(ch => ch < 0x20 || ch > 0x7e))
        {
            Add(issues, profile, "BuildDate", "Build date must contain printable ASCII characters only.");
        }

        if (profile.BuildDateOffset < 0)
        {
            Add(issues, profile, "BuildDateOffset", "Build date offset must be non-negative.");
        }
        else if (RangeOverflows(profile.BuildDateOffset, BuildDateLength))
        {
            Add(issues, profile, "BuildDateOffset", "Build date range overflows.");
        }

        if (profile.DmaTableOffset < 0)
        {
            Add(issues, profile, "DmaTableOffset", "DMA table offset must be non-negative.");
        }

        if (profile.DmaEntryCount <= 0)
        {
            Add(issues, profile, "DmaEntryCount", "DMA entry count must be positive.");
        }
        else if (profile.DmaTableOffset >= 0 && RangeOverflows(profile.DmaTableOffset, profile.DmaEntryCount, DmaEntrySize))
        {
            Add(issues, profile, "DmaTableRange", "DMA table range overflows.");
        }

        if (profile.BuildDateOffset >= 0
            && profile.DmaTableOffset >= 0
            && profile.DmaEntryCount > 0
            && !RangeOverflows(profile.DmaTableOffset, profile.DmaEntryCount, DmaEntrySize)
            && Overlaps(profile.BuildDateOffset, BuildDateLength, profile.DmaTableOffset, profile.DmaEntryCount * DmaEntrySize))
        {
            Add(issues, profile, "ProfileHeaderRanges", "Build date and DMA table ranges must not overlap.");
        }

        foreach (int index in profile.UncompressedEntryIndices)
        {
            if (index < 0 || index >= profile.DmaEntryCount)
            {
                Add(issues, profile, "UncompressedEntryIndices", $"Uncompressed DMA entry index {index} is outside the DMA table.");
            }
        }
    }

    private static void ValidateCreditsProfile(RomVersionProfile profile, List<RomProfileValidationIssue> issues)
    {
        bool hasAnyCreditsField = profile.CreditsTableOffset != 0
            || profile.CreditsTableSize != 0
            || profile.CreditsDataOffset != 0
            || profile.CreditsDataSize != 0;
        bool hasCompleteCreditsFields = profile.CreditsTableOffset > 0
            && profile.CreditsTableSize > 0
            && profile.CreditsDataOffset > 0
            && profile.CreditsDataSize > 0;

        if (hasAnyCreditsField && !hasCompleteCreditsFields)
        {
            Add(issues, profile, "CreditsBank", "Credits offsets and sizes must be either all unset or all positive.");
            return;
        }

        if (hasCompleteCreditsFields)
        {
            ValidateBankRange(profile, profile.CreditsBank, "CreditsBank", issues);
        }
    }

    private static void ValidateMessageBanks(RomVersionProfile profile, List<RomProfileValidationIssue> issues)
    {
        if (profile.MessageBanks.Count == 0)
        {
            Add(issues, profile, "MessageBanks", "At least one editable message bank must be registered.");
            return;
        }

        for (int i = 0; i < profile.MessageBanks.Count; i++)
        {
            ValidateBank(profile, profile.MessageBanks[i], $"MessageBanks[{i}]", issues);
        }

        foreach (IGrouping<string, MessageBankProfile> group in profile.MessageBanks.GroupBy(bank => bank.Name))
        {
            if (group.Count() > 1)
            {
                Add(issues, profile, "MessageBanks", $"Message bank name '{group.Key}' is registered more than once.");
            }
        }

        ValidateGameBankShape(profile, issues);
    }

    private static void ValidateGameBankShape(RomVersionProfile profile, List<RomProfileValidationIssue> issues)
    {
        if (profile.Game == GameKind.OcarinaOfTime)
        {
            if (IsMajorasMaskFontBaseline(profile.FontBaseline))
            {
                Add(issues, profile, "OotFontBaseline", "Ocarina of Time profiles must not use a Majora's Mask font baseline.");
            }

            if (profile.MessageBanks.Count is not (1 or 3))
            {
                Add(issues, profile, "OotMessageBanks", "Ocarina of Time profiles must have either one bank or three PAL banks.");
                return;
            }

            if (profile.MessageBanks.Count == 1 && profile.FontBaseline is RomFontBaseline.PalMultiLanguage or RomFontBaseline.PalGameCube)
            {
                Add(issues, profile, "OotFontBaseline", "Single-bank Ocarina of Time profiles must use the standard font baseline.");
            }

            if (profile.MessageBanks.Count == 3)
            {
                ValidateOcarinaPalBanks(profile, issues);
            }

            return;
        }

        if (profile.Game == GameKind.MajorasMask)
        {
            if (!IsMajorasMaskFontBaseline(profile.FontBaseline))
            {
                Add(issues, profile, "MmFontBaseline", "Majora's Mask profiles must use a Majora's Mask font baseline.");
            }

            if (profile.MessageBanks.Count is not (1 or 4))
            {
                Add(issues, profile, "MmMessageBanks", "Majora's Mask profiles must have either one bank or four EU banks.");
            }

            if (profile.MessageBanks.Any(bank => bank.OffsetMode != MessageBankOffsetMode.Table || bank.PointerTableOffset != 0 || bank.ExcludesFontMessage))
            {
                Add(issues, profile, "MmMessageBanks", "Majora's Mask message banks must use direct table offsets without OoT PAL pointer tables.");
            }
        }
    }

    private static void ValidateProfileRanges(RomVersionProfile profile, List<RomProfileValidationIssue> issues)
    {
        var ranges = new List<ProfileRange>();
        for (int i = 0; i < profile.MessageBanks.Count; i++)
        {
            MessageBankProfile bank = profile.MessageBanks[i];
            AddRange(ranges, $"MessageBanks[{i}]", "message table", bank.MessageTableOffset, bank.MessageTableSize, isEditableMessageTable: true);
            AddRange(ranges, $"MessageBanks[{i}]", "message data", bank.MessageDataOffset, bank.MessageDataSize, isEditableMessageTable: false);
        }

        if (profile.CreditsTableOffset > 0
            && profile.CreditsTableSize > 0
            && profile.CreditsDataOffset > 0
            && profile.CreditsDataSize > 0)
        {
            AddRange(ranges, "CreditsBank", "message table", profile.CreditsTableOffset, profile.CreditsTableSize, isEditableMessageTable: false);
            AddRange(ranges, "CreditsBank", "message data", profile.CreditsDataOffset, profile.CreditsDataSize, isEditableMessageTable: false);
        }

        if (profile.JapaneseMessageBank is MessageBankProfile japaneseBank)
        {
            AddRange(
                ranges,
                "JapaneseMessageBank",
                "message table",
                japaneseBank.MessageTableOffset,
                japaneseBank.MessageTableSize,
                isEditableMessageTable: false,
                isJapaneseMessageTable: true);
            AddRange(ranges, "JapaneseMessageBank", "message data", japaneseBank.MessageDataOffset, japaneseBank.MessageDataSize, isEditableMessageTable: false);
        }

        foreach ((ProfileRange Left, ProfileRange Right) in Pairwise(ranges))
        {
            if (AllowedSharedOcarinaPalTable(profile, Left, Right)
                || AllowedOcarinaJapaneseTableOverlap(profile, Left, Right))
            {
                continue;
            }

            if (Overlaps(Left.Offset, Left.Size, Right.Offset, Right.Size))
            {
                Add(
                    issues,
                    profile,
                    "ProfileRanges",
                    $"{Left.Label} {Left.RangeName} and {Right.Label} {Right.RangeName} ranges overlap.");
            }
        }
    }

    private static void AddRange(
        List<ProfileRange> ranges,
        string label,
        string rangeName,
        int offset,
        int size,
        bool isEditableMessageTable,
        bool isJapaneseMessageTable = false)
    {
        if (offset >= 0 && size > 0)
        {
            ranges.Add(new ProfileRange(label, rangeName, offset, size, isEditableMessageTable, isJapaneseMessageTable));
        }
    }

    private static bool AllowedSharedOcarinaPalTable(RomVersionProfile profile, ProfileRange left, ProfileRange right)
        => profile.Game == GameKind.OcarinaOfTime
            && profile.MessageBanks.Count == 3
            && left.IsEditableMessageTable
            && right.IsEditableMessageTable
            && left.Offset == right.Offset
            && left.Size == right.Size;

    private static bool AllowedOcarinaJapaneseTableOverlap(RomVersionProfile profile, ProfileRange left, ProfileRange right)
        => profile.Game == GameKind.OcarinaOfTime
            && left.RangeName == "message table"
            && right.RangeName == "message table"
            && ((left.IsEditableMessageTable && right.IsJapaneseMessageTable)
                || (left.IsJapaneseMessageTable && right.IsEditableMessageTable));

    private static void ValidateOcarinaPalBanks(RomVersionProfile profile, List<RomProfileValidationIssue> issues)
    {
        MessageBankProfile language1 = profile.MessageBanks[0];
        MessageBankProfile language2 = profile.MessageBanks[1];
        MessageBankProfile language3 = profile.MessageBanks[2];

        if (profile.FontBaseline is not (RomFontBaseline.PalMultiLanguage or RomFontBaseline.PalGameCube))
        {
            Add(issues, profile, "OotPalFontBaseline", "Three-bank Ocarina of Time profiles must use a PAL font baseline.");
        }

        if (language1.OffsetMode != MessageBankOffsetMode.Table || language1.PointerTableOffset != 0 || language1.ExcludesFontMessage)
        {
            Add(issues, profile, "OotPalLanguage1", "PAL language 1 must use the shared table directly and keep the font-order message.");
        }

        ValidateOcarinaPalSecondaryBank(profile, language2, "Language 2", issues);
        ValidateOcarinaPalSecondaryBank(profile, language3, "Language 3", issues);

        if (language2.MessageTableOffset != language1.MessageTableOffset
            || language3.MessageTableOffset != language1.MessageTableOffset
            || language2.MessageTableSize != language1.MessageTableSize
            || language3.MessageTableSize != language1.MessageTableSize)
        {
            Add(issues, profile, "OotPalSharedTable", "PAL language banks must share the language 1 message table.");
        }

        foreach ((MessageBankProfile Left, MessageBankProfile Right) in Pairwise(profile.MessageBanks))
        {
            if (Overlaps(Left.MessageDataOffset, Left.MessageDataSize, Right.MessageDataOffset, Right.MessageDataSize))
            {
                Add(issues, profile, "OotPalDataBanks", $"{Left.Name} and {Right.Name} message data ranges overlap.");
            }
        }
    }

    private static void ValidateOcarinaPalSecondaryBank(
        RomVersionProfile profile,
        MessageBankProfile bank,
        string label,
        List<RomProfileValidationIssue> issues)
    {
        if (bank.OffsetMode != MessageBankOffsetMode.Sequential || !bank.ExcludesFontMessage || bank.PointerTableOffset <= 0)
        {
            Add(issues, profile, "OotPalPointerBank", $"{label} must use sequential text data, exclude 0xFFFC, and provide a pointer table.");
        }
    }

    private static void ValidateJapaneseBank(RomVersionProfile profile, List<RomProfileValidationIssue> issues)
    {
        if (profile.JapaneseMessageBank is not MessageBankProfile japaneseBank)
        {
            return;
        }

        if (profile.Game != GameKind.OcarinaOfTime)
        {
            Add(issues, profile, "JapaneseBank", "Only Ocarina of Time profiles may register a Japanese export bank.");
        }

        ValidateBank(profile, japaneseBank, "JapaneseMessageBank", issues);
        if (japaneseBank.TableSegment != 0x08)
        {
            Add(issues, profile, "JapaneseBank", "Japanese export banks must use table segment 0x08.");
        }
    }

    private static void ValidateFontProfile(RomVersionProfile profile, List<RomProfileValidationIssue> issues)
    {
        if (profile.FontDmaEntryIndex is int fontDmaEntryIndex
            && (fontDmaEntryIndex < 0 || fontDmaEntryIndex >= profile.DmaEntryCount))
        {
            Add(issues, profile, "FontDmaEntryIndex", $"Font DMA entry index {fontDmaEntryIndex} is outside the DMA table.");
        }

        if (profile.FontWidthTableOffset is int fontWidthTableOffset && fontWidthTableOffset <= 0)
        {
            Add(issues, profile, "FontWidthTableOffset", "Font width table offset must be positive when fixed.");
        }

        if (profile.Game == GameKind.MajorasMask
            && profile.Capabilities.SupportsRomFontResources
            && (profile.FontDmaEntryIndex is null || profile.FontWidthTableOffset is null))
        {
            Add(issues, profile, "MajorasMaskFontProfile", "Majora's Mask ROM font resources must have fixed font DMA and width-table locations.");
        }
    }

    private static void ValidateCompressionProfile(RomVersionProfile profile, List<RomProfileValidationIssue> issues)
    {
        if (profile.RawDeflateHasNoHeader && profile.Codec != RomCodecKind.RawDeflate)
        {
            Add(issues, profile, "RawDeflateHasNoHeader", "Raw deflate header flags may only be set on raw deflate profiles.");
        }

        if (profile.TargetCompressedSizeMiB < 0)
        {
            Add(issues, profile, "TargetCompressedSizeMiB", "Target compressed size must not be negative.");
        }
    }

    private static void ValidateCapabilities(RomVersionProfile profile, List<RomProfileValidationIssue> issues)
    {
        bool supportsMessageEditing = profile.MessageBanks.Count > 0;
        bool supportsMultipleMessageBanks = profile.MessageBanks.Count > 1;
        bool supportsCreditsEditing = profile.CreditsTableOffset > 0
            && profile.CreditsTableSize > 0
            && profile.CreditsDataOffset > 0
            && profile.CreditsDataSize > 0;
        bool supportsJapaneseMessageExport = profile.JapaneseMessageBank is not null;
        bool supportsRomFontResources = profile.GameProfile.Capabilities.SupportsRomGlyphEditor
            && profile.MessageBanks.Count > 0;

        if (profile.Capabilities.SupportsMessageEditing != supportsMessageEditing)
        {
            Add(issues, profile, "Capabilities", "SupportsMessageEditing does not match registered message banks.");
        }

        if (profile.Capabilities.SupportsMultipleMessageBanks != supportsMultipleMessageBanks)
        {
            Add(issues, profile, "Capabilities", "SupportsMultipleMessageBanks does not match registered message banks.");
        }

        if (profile.Capabilities.SupportsCreditsEditing != supportsCreditsEditing)
        {
            Add(issues, profile, "Capabilities", "SupportsCreditsEditing does not match credits profile data.");
        }

        if (profile.Capabilities.SupportsJapaneseMessageExport != supportsJapaneseMessageExport)
        {
            Add(issues, profile, "Capabilities", "SupportsJapaneseMessageExport does not match Japanese bank data.");
        }

        if (profile.Capabilities.SupportsRomFontResources != supportsRomFontResources)
        {
            Add(issues, profile, "Capabilities", "SupportsRomFontResources does not match game and message-bank data.");
        }
    }

    private static void ValidateBank(
        RomVersionProfile profile,
        MessageBankProfile bank,
        string context,
        List<RomProfileValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(bank.Name))
        {
            Add(issues, profile, context, $"{context} name must not be empty.");
        }

        ValidateBankRange(profile, bank, context, issues);

        if (bank.TableSegment is not (0x07 or 0x08))
        {
            Add(issues, profile, context, $"{context} table segment must be 0x07 or 0x08.");
        }

        if (bank.OffsetMode == MessageBankOffsetMode.Table)
        {
            if (bank.PointerTableOffset != 0)
            {
                Add(issues, profile, context, $"{context} must not have a pointer table when using table offsets.");
            }

            if (bank.ExcludesFontMessage)
            {
                Add(issues, profile, context, $"{context} must not exclude 0xFFFC when using table offsets.");
            }
        }
        else if (bank.PointerTableOffset <= 0)
        {
            Add(issues, profile, context, $"{context} must provide a pointer table when using sequential offsets.");
        }

        if (bank.ExcludesFontMessage && bank.OffsetMode != MessageBankOffsetMode.Sequential)
        {
            Add(issues, profile, context, $"{context} may only exclude 0xFFFC when using sequential offsets.");
        }

        if (bank.PointerTableOffset < 0)
        {
            Add(issues, profile, context, $"{context} pointer table offset must be non-negative.");
        }

        if (bank.PointerTableOffset > 0
            && (Contains(bank.MessageTableOffset, bank.MessageTableSize, bank.PointerTableOffset)
                || Contains(bank.MessageDataOffset, bank.MessageDataSize, bank.PointerTableOffset)))
        {
            Add(issues, profile, context, $"{context} pointer table offset must not point inside its table or data ranges.");
        }
    }

    private static void ValidateBankRange(
        RomVersionProfile profile,
        MessageBankProfile bank,
        string context,
        List<RomProfileValidationIssue> issues)
    {
        ValidateRange(profile, context, "message table", bank.MessageTableOffset, bank.MessageTableSize, issues);
        ValidateRange(profile, context, "message data", bank.MessageDataOffset, bank.MessageDataSize, issues);

        if (Overlaps(bank.MessageTableOffset, bank.MessageTableSize, bank.MessageDataOffset, bank.MessageDataSize))
        {
            Add(issues, profile, context, $"{context} table and data ranges overlap.");
        }
    }

    private static void ValidateRange(
        RomVersionProfile profile,
        string context,
        string rangeName,
        int offset,
        int size,
        List<RomProfileValidationIssue> issues)
    {
        if (offset < 0)
        {
            Add(issues, profile, context, $"{context} {rangeName} offset must be non-negative.");
        }

        if (size <= 0)
        {
            Add(issues, profile, context, $"{context} {rangeName} size must be positive.");
        }

        if (offset >= 0 && size > 0 && offset > int.MaxValue - size)
        {
            Add(issues, profile, context, $"{context} {rangeName} range overflows.");
        }
    }

    private static IEnumerable<(MessageBankProfile Left, MessageBankProfile Right)> Pairwise(IReadOnlyList<MessageBankProfile> banks)
    {
        for (int i = 0; i < banks.Count; i++)
        {
            for (int j = i + 1; j < banks.Count; j++)
            {
                yield return (banks[i], banks[j]);
            }
        }
    }

    private static bool Overlaps(int leftOffset, int leftSize, int rightOffset, int rightSize)
    {
        if (leftOffset < 0 || leftSize <= 0 || rightOffset < 0 || rightSize <= 0)
        {
            return false;
        }

        long leftEnd = (long)leftOffset + leftSize;
        long rightEnd = (long)rightOffset + rightSize;
        return leftOffset < rightEnd && rightOffset < leftEnd;
    }

    private static bool RangeOverflows(int offset, int size)
        => offset < 0 || size < 0 || offset > int.MaxValue - size;

    private static bool RangeOverflows(int offset, int count, int elementSize)
    {
        if (offset < 0 || count < 0 || elementSize < 0)
        {
            return true;
        }

        long size = (long)count * elementSize;
        return size > int.MaxValue || offset > int.MaxValue - size;
    }

    private static bool Contains(int offset, int size, int value)
    {
        if (offset < 0 || size <= 0)
        {
            return false;
        }

        long end = (long)offset + size;
        return value >= offset && value < end;
    }

    private static bool IsMajorasMaskFontBaseline(RomFontBaseline baseline)
        => baseline is RomFontBaseline.MajorasMask
            or RomFontBaseline.MajorasMaskUsGameCube
            or RomFontBaseline.MajorasMaskEu;

    private static IEnumerable<(ProfileRange Left, ProfileRange Right)> Pairwise(IReadOnlyList<ProfileRange> ranges)
    {
        for (int i = 0; i < ranges.Count; i++)
        {
            for (int j = i + 1; j < ranges.Count; j++)
            {
                yield return (ranges[i], ranges[j]);
            }
        }
    }

    private static void Add(
        List<RomProfileValidationIssue> issues,
        RomVersionProfile profile,
        string rule,
        string message) =>
        issues.Add(new RomProfileValidationIssue(profile.Name, rule, message));

    private sealed record ProfileRange(
        string Label,
        string RangeName,
        int Offset,
        int Size,
        bool IsEditableMessageTable,
        bool IsJapaneseMessageTable);
}
