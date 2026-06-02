using HylianGrimoire.Codecs;
using HylianGrimoire.Games;
using HylianGrimoire.Glyphs;

namespace HylianGrimoire;

public sealed partial class MainWindow
{
    private MessageEncodingProfile CreateCurrentEncodingProfile()
        => _characterProfileRuntime.CreateEncodingProfile(CurrentGameProfile);

    private MessageEncodingProfile CreateEncodingProfile(GameKind gameKind)
        => _characterProfileRuntime.CreateEncodingProfile(gameKind);

    private MessageEncodingProfile CreateEncodingProfile(GameProfile profile)
        => _characterProfileRuntime.CreateEncodingProfile(profile);

    private MessageEncodingProfile CreateEncodingProfile(GameProfile profile, CharacterProfileSnapshot snapshot)
        => _characterProfileRuntime.CreateEncodingProfile(profile, snapshot);

    private GameProfile CreateCurrentEncodingGameProfile()
        => _characterProfileRuntime.CreateEncodingGameProfile(CurrentGameProfile);

    private GameProfile CreateEncodingGameProfile(GameProfile profile)
        => _characterProfileRuntime.CreateEncodingGameProfile(profile);

    private GameProfile CreateEncodingGameProfile(GameProfile profile, CharacterProfileSnapshot snapshot)
        => _characterProfileRuntime.CreateEncodingGameProfile(profile, snapshot);

    private CharacterProfileSnapshot CreateCharacterProfileSnapshot(GameProfile profile)
        => _characterProfileRuntime.CreateSnapshot(profile);
}
