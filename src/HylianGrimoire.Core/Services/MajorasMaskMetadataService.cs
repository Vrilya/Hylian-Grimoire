using System.Globalization;
using HylianGrimoire.Models;

namespace HylianGrimoire.Services;

public static class MajorasMaskMetadataService
{
    public static MajorasMaskMetadataFields CreateFields(MajorasMaskMessageMetadata metadata)
    {
        return new MajorasMaskMetadataFields(
            IconId: metadata.IconId,
            NextTextId: $"0x{metadata.NextTextId:X4}",
            FirstChoicePrice: metadata.FirstChoicePriceSigned.ToString(CultureInfo.InvariantCulture),
            SecondChoicePrice: metadata.SecondChoicePriceSigned.ToString(CultureInfo.InvariantCulture),
            IsUnskippable: metadata.IsUnskippable,
            DrawInstantly: metadata.DrawInstantly,
            IsCentered: metadata.IsCentered);
    }

    public static MajorasMaskMessageMetadata SetIcon(
        MajorasMaskMessageMetadata metadata,
        byte iconId)
        => metadata.With(iconId: iconId);

    public static bool TrySetNextTextId(
        MajorasMaskMessageMetadata metadata,
        string text,
        out MajorasMaskMessageMetadata updated)
    {
        if (TryParseHexU16(text, out ushort nextTextId))
        {
            updated = metadata.With(nextTextId: nextTextId);
            return true;
        }

        updated = metadata;
        return false;
    }

    public static bool TrySetFirstChoicePrice(
        MajorasMaskMessageMetadata metadata,
        string text,
        out MajorasMaskMessageMetadata updated)
    {
        if (TryParseSignedU16(text, out short price))
        {
            updated = metadata.With(firstChoicePrice: price);
            return true;
        }

        updated = metadata;
        return false;
    }

    public static bool TrySetSecondChoicePrice(
        MajorasMaskMessageMetadata metadata,
        string text,
        out MajorasMaskMessageMetadata updated)
    {
        if (TryParseSignedU16(text, out short price))
        {
            updated = metadata.With(secondChoicePrice: price);
            return true;
        }

        updated = metadata;
        return false;
    }

    public static MajorasMaskMessageMetadata SetUnskippable(
        MajorasMaskMessageMetadata metadata,
        bool isUnskippable)
        => metadata.With(unskippable: isUnskippable || metadata.DrawInstantly);

    public static MajorasMaskMessageMetadata SetInstantText(
        MajorasMaskMessageMetadata metadata,
        bool drawInstantly)
        => metadata.With(
            unskippable: drawInstantly ? true : null,
            drawInstantly: drawInstantly);

    public static MajorasMaskMessageMetadata SetCentered(
        MajorasMaskMessageMetadata metadata,
        bool isCentered)
        => metadata.With(centered: isCentered);

    public static bool TryParseHexU16(string text, out ushort value)
    {
        text = text.Trim();
        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            text = text[2..];
        }

        return ushort.TryParse(
            text,
            NumberStyles.HexNumber,
            CultureInfo.InvariantCulture,
            out value);
    }

    public static bool TryParseSignedU16(string text, out short value)
    {
        text = text.Trim();
        if (short.TryParse(
            text,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out value))
        {
            return true;
        }

        if (TryParseHexU16(text, out ushort unsignedValue))
        {
            value = unchecked((short)unsignedValue);
            return true;
        }

        value = 0;
        return false;
    }
}
