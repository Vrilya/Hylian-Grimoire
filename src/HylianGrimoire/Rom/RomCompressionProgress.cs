namespace HylianGrimoire.Rom;

public sealed record RomCompressionProgress(int CompletedFiles, int TotalFiles)
{
    public double Percent => TotalFiles == 0
        ? 100
        : Math.Clamp(CompletedFiles * 100.0 / TotalFiles, 0, 100);
}
