using HylianGrimoire.Codecs;
using HylianGrimoire.Games;
using HylianGrimoire.Models;
using HylianGrimoire.Rom;
using HylianGrimoire.Textures;

namespace HylianGrimoire.O2r;

public abstract class O2rModPortProfile
{
    protected O2rModPortProfile(
        O2rModPortKind kind,
        GameProfile gameProfile,
        string displayName,
        string toolTitle,
        string suggestedFileName,
        string fileTypeDescription)
    {
        Kind = kind;
        GameProfile = gameProfile;
        DisplayName = displayName;
        ToolTitle = toolTitle;
        SuggestedFileName = suggestedFileName;
        FileTypeDescription = fileTypeDescription;
    }

    public O2rModPortKind Kind { get; }

    public GameProfile GameProfile { get; }

    public string DisplayName { get; }

    public string ToolTitle { get; }

    public string SuggestedFileName { get; }

    public string FileTypeDescription { get; }

    public virtual bool SupportsCurrentDocumentTextResources => false;

    public abstract bool SupportsRomProfile(RomVersionProfile profile);

    public virtual IReadOnlyList<O2rTextResourceDefinition> GetRomTextResources(RomMessageData romData) => [];

    public virtual IReadOnlyList<O2rTextResourceDefinition> GetCurrentDocumentTextResources(
        IReadOnlyDictionary<int, List<MessageEntry>> languageEntries) => [];

    public virtual bool SupportsTextureResource(TextureDefinition texture) => true;

    public virtual string GetTextureResourcePath(TextureDefinition texture)
        => $"{texture.Group.Replace('\\', '/')}/{texture.Name}";

    public virtual bool IsTextResourcePath(string resourcePath)
        => resourcePath.StartsWith("text/", StringComparison.Ordinal);

    public virtual (byte[] TableBytes, byte[] MessageBytes) BuildTextFiles(
        O2rTextResourceDefinition resource,
        List<MessageEntry> entries,
        MessageEncodingProfile encodingProfile)
        => GameProfile.MessageBankCodec.Build(entries, encodingProfile);

    public abstract byte[] PackTextResource(
        O2rTextResourceDefinition resource,
        ReadOnlySpan<byte> messageBytes,
        ReadOnlySpan<byte> tableBytes);
}
