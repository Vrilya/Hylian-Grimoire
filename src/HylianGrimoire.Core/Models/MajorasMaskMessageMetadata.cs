namespace HylianGrimoire.Models;

public sealed record MajorasMaskMessageMetadata(
    byte TableTypePosition,
    ushort TextBoxProperties,
    byte IconId,
    ushort NextTextId,
    ushort FirstChoicePrice,
    ushort SecondChoicePrice,
    ushort Unknown)
{
    public int Type => (TextBoxProperties >> 8) & 0x0f;

    public int Position => (TextBoxProperties >> 4) & 0x0f;

    public bool IsCentered => ((TextBoxProperties >> 12) & 0x0f) == 1;

    public bool IsUnskippable => (TextBoxProperties & 0x0001) != 0;

    public bool DrawInstantly => (TextBoxProperties & 0x0003) == 0x0003;

    public short FirstChoicePriceSigned => unchecked((short)FirstChoicePrice);

    public short SecondChoicePriceSigned => unchecked((short)SecondChoicePrice);

    public MajorasMaskMessageMetadata With(
        byte? iconId = null,
        ushort? nextTextId = null,
        short? firstChoicePrice = null,
        short? secondChoicePrice = null,
        bool? centered = null,
        bool? unskippable = null,
        bool? drawInstantly = null)
    {
        return this with
        {
            TextBoxProperties = ApplyFlags(TextBoxProperties, centered, unskippable, drawInstantly),
            IconId = iconId ?? IconId,
            NextTextId = nextTextId ?? NextTextId,
            FirstChoicePrice = firstChoicePrice is null ? FirstChoicePrice : unchecked((ushort)firstChoicePrice.Value),
            SecondChoicePrice = secondChoicePrice is null ? SecondChoicePrice : unchecked((ushort)secondChoicePrice.Value),
        };
    }

    public byte[] BuildHeader(int type, int position)
    {
        ushort properties = (ushort)((TextBoxProperties & 0xf00f)
            | ((type & 0x0f) << 8)
            | ((position & 0x0f) << 4));

        return
        [
            (byte)((properties >> 8) & 0xff),
            (byte)(properties & 0xff),
            IconId,
            (byte)((NextTextId >> 8) & 0xff),
            (byte)(NextTextId & 0xff),
            (byte)((FirstChoicePrice >> 8) & 0xff),
            (byte)(FirstChoicePrice & 0xff),
            (byte)((SecondChoicePrice >> 8) & 0xff),
            (byte)(SecondChoicePrice & 0xff),
            (byte)((Unknown >> 8) & 0xff),
            (byte)(Unknown & 0xff),
        ];
    }

    private static ushort ApplyFlags(
        ushort properties,
        bool? centered,
        bool? unskippable,
        bool? drawInstantly)
    {
        int result = properties;

        if (centered is not null)
        {
            result = (result & ~0xf000) | (centered.Value ? 0x1000 : 0x0000);
        }

        if (unskippable is not null || drawInstantly is not null)
        {
            bool finalUnskippable = unskippable ?? ((result & 0x0001) != 0);
            bool finalDrawInstantly = drawInstantly ?? ((result & 0x0003) == 0x0003);
            int lowNibble = result & 0x000c;
            if (finalDrawInstantly)
            {
                lowNibble |= 0x0003;
            }
            else if (finalUnskippable)
            {
                lowNibble |= 0x0001;
            }

            result = (result & ~0x000f) | lowNibble;
        }

        return (ushort)result;
    }
}
