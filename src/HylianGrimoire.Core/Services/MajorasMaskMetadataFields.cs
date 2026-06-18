namespace HylianGrimoire.Services;

public sealed record MajorasMaskMetadataFields(
    byte IconId,
    string NextTextId,
    string FirstChoicePrice,
    string SecondChoicePrice,
    bool IsUnskippable,
    bool DrawInstantly,
    bool IsCentered);
