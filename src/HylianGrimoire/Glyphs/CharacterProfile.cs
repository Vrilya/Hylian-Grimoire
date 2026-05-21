namespace HylianGrimoire.Glyphs;

public sealed class CharacterProfile
{
    public string Name { get; set; } = string.Empty;

    public Dictionary<string, string> Characters { get; set; } = [];

    public Dictionary<string, double> Widths { get; set; } = [];

    public Dictionary<string, string> Images { get; set; } = [];

    public Dictionary<string, string> ImageData { get; set; } = [];
}
