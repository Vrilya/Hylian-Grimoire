using HylianGrimoire.Games;

namespace HylianGrimoire.Preview;

public static class MessagePreviewWindowFactory
{
    public static IMessagePreviewWindow Create(GameProfile profile, MessagePreviewWindowState state)
    {
        return profile.Kind switch
        {
            GameKind.OcarinaOfTime => new OotPreviewWindow(profile.EncodingProfile, state),
            GameKind.MajorasMask => new MmPreviewWindow(profile.EncodingProfile, state),
            _ => throw new NotSupportedException($"{profile.DisplayName} does not have a message preview window."),
        };
    }
}
