namespace HylianGrimoire.TitleText;

public sealed record TitleTextLine(
    TitleTextKind Kind,
    string Text,
    int GapAfterIndex,
    int X,
    int MaxCharacters);
