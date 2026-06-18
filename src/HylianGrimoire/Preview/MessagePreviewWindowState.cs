namespace HylianGrimoire.Preview;

public sealed class MessagePreviewWindowState
{
    public const int DefaultRowsPerColumn = 5;

    public double ZoomScale { get; set; } = 1.0;

    public bool ShowAlignmentGuides { get; set; }

    public bool UseColumns { get; set; }

    public int RowsPerColumn { get; set; } = DefaultRowsPerColumn;

    public bool AlwaysOnTop { get; set; }

    public int? WindowWidth { get; set; }

    public int? WindowHeight { get; set; }
}
