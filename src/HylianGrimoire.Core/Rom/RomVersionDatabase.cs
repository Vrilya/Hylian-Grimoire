using System.Text;

namespace HylianGrimoire.Rom;

public static partial class RomVersionDatabase
{
    private const int BuildDateLength = 17;

    private static readonly Lazy<IReadOnlyList<RomVersionProfile>> LazyProfiles = new(() =>
    [
        .. CreateOcarinaOfTimeProfiles(),
        .. CreateMajorasMaskProfiles(),
    ]);

    public static IReadOnlyList<RomVersionProfile> Profiles => LazyProfiles.Value;

    public static RomVersionProfile Detect(ReadOnlySpan<byte> rom)
    {
        foreach (RomVersionProfile profile in Profiles)
        {
            if (profile.BuildDateOffset + BuildDateLength > rom.Length)
            {
                continue;
            }

            string buildDate = Encoding.ASCII.GetString(rom.Slice(profile.BuildDateOffset, BuildDateLength));
            if (buildDate == profile.BuildDate)
            {
                return profile;
            }
        }

        throw new InvalidDataException("Unsupported or unrecognized ROM version.");
    }
}
