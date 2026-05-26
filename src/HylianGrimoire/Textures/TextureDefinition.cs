namespace HylianGrimoire.Textures;

public sealed record TextureDefinition(
    string Group,
    string Name,
    int RomAddress,
    int Width,
    int Height,
    TextureFormat Format);
