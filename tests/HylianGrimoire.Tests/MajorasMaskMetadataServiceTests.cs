using HylianGrimoire.Models;
using HylianGrimoire.Services;
using Xunit;

namespace HylianGrimoire.Tests;

public sealed class MajorasMaskMetadataServiceTests
{
    [Fact]
    public void CreateFieldsFormatsEditableMetadataValues()
    {
        MajorasMaskMessageMetadata metadata = CreateMetadata(
            textBoxProperties: 0x1203,
            nextTextId: 0x1234,
            firstChoicePrice: 0xffff,
            secondChoicePrice: 0x0005);

        MajorasMaskMetadataFields fields = MajorasMaskMetadataService.CreateFields(metadata);

        Assert.Equal(0xfe, fields.IconId);
        Assert.Equal("0x1234", fields.NextTextId);
        Assert.Equal("-1", fields.FirstChoicePrice);
        Assert.Equal("5", fields.SecondChoicePrice);
        Assert.True(fields.IsUnskippable);
        Assert.True(fields.DrawInstantly);
        Assert.True(fields.IsCentered);
    }

    [Theory]
    [InlineData("0xABCD", 0xabcd)]
    [InlineData("abcd", 0xabcd)]
    [InlineData("FFFF", 0xffff)]
    public void TrySetNextTextIdAcceptsHexText(string text, int expected)
    {
        MajorasMaskMessageMetadata metadata = CreateMetadata(nextTextId: 0x0000);

        bool parsed = MajorasMaskMetadataService.TrySetNextTextId(metadata, text, out MajorasMaskMessageMetadata updated);

        Assert.True(parsed);
        Assert.Equal((ushort)expected, updated.NextTextId);
    }

    [Theory]
    [InlineData("-1", 0xffff)]
    [InlineData("5", 0x0005)]
    [InlineData("0x8000", 0x8000)]
    [InlineData("FFFF", 0xffff)]
    public void TrySetChoicePriceAcceptsSignedDecimalAndHexText(string text, int expected)
    {
        MajorasMaskMessageMetadata metadata = CreateMetadata(firstChoicePrice: 0x0000, secondChoicePrice: 0x0000);

        bool firstParsed = MajorasMaskMetadataService.TrySetFirstChoicePrice(metadata, text, out MajorasMaskMessageMetadata firstUpdated);
        bool secondParsed = MajorasMaskMetadataService.TrySetSecondChoicePrice(metadata, text, out MajorasMaskMessageMetadata secondUpdated);

        Assert.True(firstParsed);
        Assert.True(secondParsed);
        Assert.Equal((ushort)expected, firstUpdated.FirstChoicePrice);
        Assert.Equal((ushort)expected, secondUpdated.SecondChoicePrice);
    }

    [Fact]
    public void InvalidNumericTextLeavesMetadataUnchanged()
    {
        MajorasMaskMessageMetadata metadata = CreateMetadata(
            nextTextId: 0x1234,
            firstChoicePrice: 0x0005);

        bool nextParsed = MajorasMaskMetadataService.TrySetNextTextId(metadata, "not hex", out MajorasMaskMessageMetadata nextUpdated);
        bool priceParsed = MajorasMaskMetadataService.TrySetFirstChoicePrice(metadata, "65535", out MajorasMaskMessageMetadata priceUpdated);

        Assert.False(nextParsed);
        Assert.False(priceParsed);
        Assert.Equal(metadata, nextUpdated);
        Assert.Equal(metadata, priceUpdated);
    }

    [Fact]
    public void InstantTextForcesUnskippable()
    {
        MajorasMaskMessageMetadata metadata = CreateMetadata(textBoxProperties: 0x0000);

        MajorasMaskMessageMetadata updated = MajorasMaskMetadataService.SetInstantText(metadata, drawInstantly: true);

        Assert.True(updated.DrawInstantly);
        Assert.True(updated.IsUnskippable);
    }

    [Fact]
    public void ClearingInstantTextKeepsExistingUnskippableFlag()
    {
        MajorasMaskMessageMetadata metadata = CreateMetadata(textBoxProperties: 0x0003);

        MajorasMaskMessageMetadata updated = MajorasMaskMetadataService.SetInstantText(metadata, drawInstantly: false);

        Assert.False(updated.DrawInstantly);
        Assert.True(updated.IsUnskippable);
    }

    [Fact]
    public void UnskippableCannotBeClearedWhileInstantTextIsActive()
    {
        MajorasMaskMessageMetadata metadata = CreateMetadata(textBoxProperties: 0x0003);

        MajorasMaskMessageMetadata updated = MajorasMaskMetadataService.SetUnskippable(metadata, isUnskippable: false);

        Assert.True(updated.IsUnskippable);
        Assert.True(updated.DrawInstantly);
    }

    [Fact]
    public void SetCenteredTogglesCenteredFlagWithoutChangingTypeAndPosition()
    {
        MajorasMaskMessageMetadata metadata = CreateMetadata(textBoxProperties: 0x0230);

        MajorasMaskMessageMetadata centered = MajorasMaskMetadataService.SetCentered(metadata, isCentered: true);
        MajorasMaskMessageMetadata uncentered = MajorasMaskMetadataService.SetCentered(centered, isCentered: false);

        Assert.True(centered.IsCentered);
        Assert.Equal(metadata.Type, centered.Type);
        Assert.Equal(metadata.Position, centered.Position);
        Assert.False(uncentered.IsCentered);
        Assert.Equal(metadata.Type, uncentered.Type);
        Assert.Equal(metadata.Position, uncentered.Position);
    }

    private static MajorasMaskMessageMetadata CreateMetadata(
        ushort textBoxProperties = 0x0000,
        ushort nextTextId = 0xffff,
        ushort firstChoicePrice = 0xffff,
        ushort secondChoicePrice = 0xffff)
    {
        return new MajorasMaskMessageMetadata(
            TableTypePosition: 0,
            TextBoxProperties: textBoxProperties,
            IconId: 0xfe,
            NextTextId: nextTextId,
            FirstChoicePrice: firstChoicePrice,
            SecondChoicePrice: secondChoicePrice,
            Unknown: 0xffff);
    }
}
