namespace HylianGrimoire.TextTextures;

public sealed record EndTitleTextParts(string Prefix, string Title, string Tm, string Suffix)
{
    public static EndTitleTextParts Empty { get; } = new(string.Empty, string.Empty, string.Empty, string.Empty);

    public static EndTitleTextParts Parse(string text)
    {
        string value = text.Trim();
        string prefix = string.Empty;
        string suffix = string.Empty;

        if (value.Length > 0 && IsDash(value[0]))
        {
            prefix = value[0].ToString();
            value = value[1..].TrimStart();
        }

        if (value.Length > 0 && IsDash(value[^1]))
        {
            suffix = value[^1].ToString();
            value = value[..^1].TrimEnd();
        }

        string tm = string.Empty;
        if (value.EndsWith('\u2122'))
        {
            tm = "TM";
            value = value[..^1].TrimEnd();
        }
        else if (value.EndsWith("TM", StringComparison.Ordinal))
        {
            tm = "TM";
            value = value[..^2].TrimEnd();
        }

        return new EndTitleTextParts(prefix, value, tm, suffix);
    }

    private static bool IsDash(char value)
        => value is '-' or '\u2013' or '\u2014';
}
