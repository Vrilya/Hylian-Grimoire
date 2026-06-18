namespace HylianGrimoire.O2r;

public sealed record O2rTextResourceDefinition(
    string DisplayName,
    string ResourcePath,
    O2rTextResourceKind Kind,
    int BankIndex);
