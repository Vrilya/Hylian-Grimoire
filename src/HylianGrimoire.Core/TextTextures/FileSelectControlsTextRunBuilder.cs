namespace HylianGrimoire.TextTextures;

public static class FileSelectControlsTextRunBuilder
{
    public const char Bullet = '\u2022';

    public static IReadOnlyList<CompactTextTextureTextRun> Create(
        string text,
        TextTextureFont regularFont,
        TextTextureFont boldFont,
        double bulletScale,
        double bulletYOffset)
    {
        var runs = new List<CompactTextTextureTextRun>();
        for (int i = 0; i < text.Length; i++)
        {
            bool isHyphen = text[i] == '-';
            bool isBullet = text[i] == Bullet;
            TextTextureFont font = UsesBoldFont(text[i]) ? boldFont : regularFont;
            double scale = isBullet ? bulletScale / 100d : 1d;
            AddRun(
                runs,
                text[i].ToString(),
                font,
                scale,
                isBullet ? CompactTextTextureTextRunKind.Bullet : CompactTextTextureTextRunKind.Text,
                isBullet ? bulletYOffset : isHyphen ? -1 : 0,
                isHyphen ? 1 : 0,
                0,
                isHyphen ? 1 : 0);
        }

        return runs;
    }

    public static bool UsesBoldFont(char character)
        => char.IsLetter(character) || character == '-';

    private static void AddRun(
        List<CompactTextTextureTextRun> runs,
        string text,
        TextTextureFont font,
        double fontScale,
        CompactTextTextureTextRunKind kind,
        double yOffset,
        double xOffset,
        double leadingSpacing,
        double trailingSpacing)
    {
        if (runs.Count > 0
            && runs[^1].Kind == kind
            && runs[^1].Font == font
            && Math.Abs(runs[^1].FontScale - fontScale) < 0.001
            && Math.Abs(runs[^1].YOffset - yOffset) < 0.001
            && Math.Abs(runs[^1].XOffset - xOffset) < 0.001
            && Math.Abs(runs[^1].LeadingSpacing - leadingSpacing) < 0.001
            && Math.Abs(runs[^1].TrailingSpacing - trailingSpacing) < 0.001)
        {
            runs[^1] = runs[^1] with { Text = runs[^1].Text + text };
            return;
        }

        runs.Add(new CompactTextTextureTextRun(
            text,
            font,
            fontScale,
            kind,
            yOffset,
            xOffset,
            leadingSpacing,
            trailingSpacing));
    }
}
