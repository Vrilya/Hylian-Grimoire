namespace HylianGrimoire.Textures;

public enum TextureStorageKind
{
    Rom,
    CmpDmaArchive,
}

public sealed record TextureDefinition(
    string Group,
    string Name,
    int RomAddress,
    int Width,
    int Height,
    TextureFormat Format,
    int? TlutRomAddress = null,
    int? TlutColorCount = null,
    string? OutputName = null,
    TextureStorageKind StorageKind = TextureStorageKind.Rom,
    int? ArchiveRomAddress = null,
    int? ArchiveLength = null)
{
    public string ExportName => string.IsNullOrWhiteSpace(OutputName) ? Name : OutputName;

    public bool UsesArchive => StorageKind == TextureStorageKind.CmpDmaArchive;

    public bool UsesTlut => Format is TextureFormat.CI4 or TextureFormat.CI8;

    public int EffectiveTlutColorCount => TlutColorCount ?? Format switch
    {
        TextureFormat.CI4 => 16,
        TextureFormat.CI8 => 256,
        _ => 0,
    };
}
