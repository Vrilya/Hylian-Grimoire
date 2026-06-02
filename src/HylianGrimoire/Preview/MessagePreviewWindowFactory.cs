using HylianGrimoire.Games;

namespace HylianGrimoire.Preview;

public static class MessagePreviewWindowFactory
{
    public static IMessagePreviewWindow Create(GameProfile profile)
    {
        return profile.Kind switch
        {
            GameKind.OcarinaOfTime => new OotPreviewWindow(profile.EncodingProfile),
            GameKind.MajorasMask => new MmPreviewWindow(profile.EncodingProfile),
            _ => throw new NotSupportedException($"{profile.DisplayName} does not have a message preview window."),
        };
    }
}
