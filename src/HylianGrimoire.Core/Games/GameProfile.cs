namespace HylianGrimoire.Games;

using HylianGrimoire.Codecs;
using HylianGrimoire.Rom;

public sealed record GameProfile(
    GameKind Kind,
    string DisplayName,
    string ShortName,
    GameAssetPaths Assets,
    Services.IMessageTypeCatalog MessageTypes,
    Services.IMessagePositionCatalog MessagePositions,
    Services.IEditorTextSyntax EditorTextSyntax,
    MessageEncodingProfile EncodingProfile,
    IMessageBankCodec MessageBankCodec,
    IMessageBankLayout MessageBankLayout,
    GameCapabilities Capabilities);
