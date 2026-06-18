using HylianGrimoire.Codecs;
using HylianGrimoire.Games;
using HylianGrimoire.Interop;
using HylianGrimoire.Models;
using HylianGrimoire.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace HylianGrimoire.MessageInspector;

public sealed partial class MessageByteInspectorWindow : Window
{
    private const int BytesPerRow = 16;
    private const double OffsetColumnWidth = 72;
    private const double HexCellWidth = 31;
    private const double DecodedCellWidth = 18;
    private const double CellHeight = 24;
    private const double CellGap = 2;
    private const double ColumnSlack = 12;
    private const double HexColumnWidth = (HexCellWidth * BytesPerRow) + (CellGap * (BytesPerRow - 1)) + ColumnSlack;
    private const double DecodedColumnWidth = (DecodedCellWidth * BytesPerRow) + (CellGap * (BytesPerRow - 1)) + ColumnSlack;
    private static readonly FontFamily MonoFont = new("Cascadia Mono");
    private MessageByteInspection? _inspection;
    private string _gameName = string.Empty;

    public MessageByteInspectorWindow()
    {
        InitializeComponent();
        SystemBackdrop = new MicaBackdrop();
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1304, 780));
        WindowSizeLimits.SetMinimumSize(this, 1304, 780);
        WindowIcon.Apply(this);
        WindowTheme.Register(this);
        HighlightButton.IsChecked = true;
        BuildHexHeader();
        BuildLegend();
    }

    public void SetMessage(
        GameProfile gameProfile,
        MessageEntry? entry,
        MessageEncodingProfile encodingProfile)
    {
        _gameName = gameProfile.DisplayName;
        if (entry is null)
        {
            SetEmpty("No message selected.");
            return;
        }

        if (!MessageByteInspectorService.CanInspect(gameProfile.Kind))
        {
            SetEmpty($"{gameProfile.DisplayName} byte inspection is not supported yet.");
            return;
        }

        try
        {
            _inspection = MessageByteInspectorService.Inspect(gameProfile.Kind, entry, encodingProfile);
            TitleText.Text = $"Message {entry.Label()}";
            int tableByteCount = _inspection.Sections
                .Where(section => section.Kind != MessageByteSectionKind.MessageData)
                .Sum(section => section.Bytes.Count);
            SummaryText.Text = $"{gameProfile.DisplayName} - {entry.Text.Length} editor chars - {tableByteCount} table bytes - {_inspection.Bytes.Count} encoded bytes";
            RenderRows();
        }
        catch (Exception ex)
        {
            SetEmpty(UiOperationExceptionHandler.GetDisplayMessage("Message byte inspection failed", ex));
        }
    }

    public void SetEmpty(string status)
    {
        _inspection = null;
        TitleText.Text = "Message Byte Inspector";
        SummaryText.Text = status;
        RowsPanel.Children.Clear();
    }

    private void OnHighlightChanged(object sender, RoutedEventArgs e)
        => RenderRows();
}
