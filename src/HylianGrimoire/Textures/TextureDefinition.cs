namespace HylianGrimoire.Textures;

public sealed record TextureDefinition(
    string Group,
    string Name,
    int RomAddress,
    int Width,
    int Height,
    TextureFormat Format,
    int? TlutRomAddress = null,
    int? TlutColorCount = null,
    string? OutputName = null)
{
    public string ExportName => Name;

    public bool UsesTlut => Format is TextureFormat.CI4 or TextureFormat.CI8;

    public int EffectiveTlutColorCount => TlutColorCount ?? Format switch
    {
        TextureFormat.CI4 => 16,
        TextureFormat.CI8 => 256,
        _ => 0,
    };
}
