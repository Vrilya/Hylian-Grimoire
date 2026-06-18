using HylianGrimoire.Codecs;
using HylianGrimoire.Games;
using HylianGrimoire.Models;

namespace HylianGrimoire.Services;

public static partial class MessageByteInspectorService
{
    public static bool CanInspect(GameKind gameKind)
        => gameKind is GameKind.OcarinaOfTime or GameKind.MajorasMask;

    public static MessageByteInspection Inspect(
        GameKind gameKind,
        MessageEntry entry,
        MessageEncodingProfile? encodingProfile = null)
    {
        encodingProfile ??= gameKind is GameKind.MajorasMask
            ? MessageEncodingProfile.MajorasMask
            : MessageEncodingProfile.Default;
        if (!CanInspect(gameKind))
        {
            throw new NotSupportedException($"{gameKind} message byte inspection is not supported yet.");
        }

        return gameKind switch
        {
            GameKind.OcarinaOfTime => InspectOcarinaOfTime(entry, encodingProfile),
            GameKind.MajorasMask => InspectMajorasMask(entry, encodingProfile),
            _ => throw new NotSupportedException($"{gameKind} message byte inspection is not supported yet."),
        };
    }
}
