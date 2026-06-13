using HylianGrimoire.Codecs;
using HylianGrimoire.Games;
using HylianGrimoire.Interop;
using HylianGrimoire.Models;
using HylianGrimoire.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

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

    private void BuildLegend()
    {
        LegendPanel.Children.Clear();
        foreach (MessageByteSegmentKind kind in Enum.GetValues<MessageByteSegmentKind>())
        {
            LegendPanel.Children.Add(CreateLegendItem(kind));
        }
    }

    private UIElement CreateLegendItem(MessageByteSegmentKind kind)
    {
        var swatch = new Border
        {
            Width = 12,
            Height = 12,
            CornerRadius = new CornerRadius(3),
            Background = GetKindBrush(kind, highlighted: true),
            BorderBrush = GetCellBorderBrush(),
            BorderThickness = new Thickness(1),
            VerticalAlignment = VerticalAlignment.Center,
        };

        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 5,
            Children =
            {
                swatch,
                new TextBlock
                {
                    Text = GetKindLabel(kind),
                    FontSize = 12,
                    Opacity = 0.82,
                    VerticalAlignment = VerticalAlignment.Center,
                },
            },
        };
    }

    private void BuildHexHeader()
    {
        HexHeaderPanel.Children.Clear();
        for (int index = 0; index < BytesPerRow; index++)
        {
            HexHeaderPanel.Children.Add(CreateHexHeaderCell(index));
        }
    }

    private static UIElement CreateHexHeaderCell(int index)
        => new Border
        {
            Width = HexCellWidth,
            Height = 20,
            Margin = GetByteCellMargin(index),
            Child = new TextBlock
            {
                Text = $"{index:X2}",
                FontFamily = MonoFont,
                FontSize = 13,
                Opacity = 0.82,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };

    private void RenderRows()
    {
        RowsPanel.Children.Clear();
        if (_inspection is null)
        {
            return;
        }

        foreach (MessageByteSection section in _inspection.Sections)
        {
            RowsPanel.Children.Add(CreateSectionHeader(section));
            for (int rowOffset = 0; rowOffset < section.Bytes.Count; rowOffset += BytesPerRow)
            {
                RowsPanel.Children.Add(CreateRow(section, rowOffset));
            }
        }
    }

    private static UIElement CreateSectionHeader(MessageByteSection section)
    {
        var row = new Grid
        {
            ColumnSpacing = 12,
            Margin = new Thickness(0, section.Kind == MessageByteSectionKind.MessageData ? 8 : 0, 0, 0),
        };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(OffsetColumnWidth) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(HexColumnWidth) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(DecodedColumnWidth) });

        var title = new TextBlock
        {
            Text = section.Title,
            FontFamily = MonoFont,
            FontSize = 12,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Opacity = 0.72,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(title, 1);
        row.Children.Add(title);

        return row;
    }

    private UIElement CreateRow(MessageByteSection section, int rowOffset)
    {
        var row = new Grid
        {
            ColumnSpacing = 12,
            MinHeight = 28,
        };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(OffsetColumnWidth) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(HexColumnWidth) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(DecodedColumnWidth) });

        row.Children.Add(new TextBlock
        {
            Text = $"0x{rowOffset:x4}",
            FontFamily = MonoFont,
            VerticalAlignment = VerticalAlignment.Center,
        });

        var hexPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 0,
        };
        Grid.SetColumn(hexPanel, 1);
        row.Children.Add(hexPanel);

        var textPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 0,
        };
        Grid.SetColumn(textPanel, 2);
        row.Children.Add(textPanel);

        int rowEnd = Math.Min(rowOffset + BytesPerRow, section.Bytes.Count);
        bool showDecodedText = section.Kind == MessageByteSectionKind.MessageData;
        for (int offset = rowOffset; offset < rowEnd; offset++)
        {
            byte value = section.Bytes[offset];
            MessageByteSegment? segment = FindSegment(section, offset);
            hexPanel.Children.Add(CreateByteCell(section, offset, value, segment, asTextPreview: false));
            if (showDecodedText)
            {
                textPanel.Children.Add(CreateByteCell(section, offset, value, segment, asTextPreview: true));
            }
        }

        return row;
    }

    private Border CreateByteCell(MessageByteSection section, int offset, byte value, MessageByteSegment? segment, bool asTextPreview)
    {
        int cellIndex = offset % BytesPerRow;
        var content = new TextBlock
        {
            Text = asTextPreview ? ToTextPreview(value) : $"{value:X2}",
            FontFamily = MonoFont,
            FontSize = 13,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var cell = new Border
        {
            Width = asTextPreview ? DecodedCellWidth : HexCellWidth,
            Height = CellHeight,
            Margin = GetByteCellMargin(cellIndex),
            CornerRadius = new CornerRadius(3),
            Background = segment is null
                ? GetNeutralCellBrush()
                : GetKindBrush(segment.Kind, HighlightButton.IsChecked == true),
            BorderBrush = GetCellBorderBrush(),
            BorderThickness = new Thickness(1),
            Child = content,
        };

        ToolTipService.SetToolTip(cell, CreateToolTip(section, offset, value, segment));
        return cell;
    }

    private static Thickness GetByteCellMargin(int cellIndex)
        => new(0, 0, cellIndex == BytesPerRow - 1 ? 0 : CellGap, 0);

    private static MessageByteSegment? FindSegment(MessageByteSection section, int offset)
        => section.Segments.FirstOrDefault(segment => segment.Contains(offset));

    private string CreateToolTip(MessageByteSection section, int offset, byte value, MessageByteSegment? segment)
    {
        if (segment is null)
        {
            return $"Section: {section.Title}\nOffset: 0x{offset:x4}\nByte: 0x{value:X2}\nGame: {_gameName}";
        }

        string syntax = string.IsNullOrWhiteSpace(segment.EditorSyntax) ? string.Empty : $"\nEditor syntax: {segment.EditorSyntax}";
        return $"Section: {section.Title}\nOffset: 0x{offset:x4}\nByte: 0x{value:X2}\nKind: {GetKindLabel(segment.Kind)}\n{segment.Label}{syntax}\n{segment.Description}";
    }

    private static string ToTextPreview(byte value)
        => value is >= 0x20 and <= 0x7e ? ((char)value).ToString() : ".";

    private static string GetKindLabel(MessageByteSegmentKind kind)
        => kind switch
        {
            MessageByteSegmentKind.TableField => "Table Field",
            MessageByteSegmentKind.HeaderField => "Header Field",
            MessageByteSegmentKind.Text => "Text",
            MessageByteSegmentKind.LineBreak => "Line Break",
            MessageByteSegmentKind.ControlCode => "Control Code",
            MessageByteSegmentKind.Parameter => "Parameter",
            MessageByteSegmentKind.Terminator => "End",
            MessageByteSegmentKind.Padding => "Padding",
            _ => kind.ToString(),
        };

    private static SolidColorBrush GetKindBrush(MessageByteSegmentKind kind, bool highlighted)
    {
        if (!highlighted)
        {
            return GetNeutralCellBrush();
        }

        Color color = kind switch
        {
            MessageByteSegmentKind.TableField => Color.FromArgb(90, 125, 211, 252),
            MessageByteSegmentKind.HeaderField => Color.FromArgb(90, 45, 212, 191),
            MessageByteSegmentKind.Text => Color.FromArgb(90, 52, 211, 153),
            MessageByteSegmentKind.LineBreak => Color.FromArgb(110, 245, 158, 11),
            MessageByteSegmentKind.ControlCode => Color.FromArgb(100, 96, 165, 250),
            MessageByteSegmentKind.Parameter => Color.FromArgb(105, 192, 132, 252),
            MessageByteSegmentKind.Terminator => Color.FromArgb(110, 248, 113, 113),
            MessageByteSegmentKind.Padding => Color.FromArgb(90, 148, 163, 184),
            _ => Color.FromArgb(60, 148, 163, 184),
        };
        return new SolidColorBrush(color);
    }

    private static SolidColorBrush GetNeutralCellBrush()
        => new(Color.FromArgb(30, 255, 255, 255));

    private static SolidColorBrush GetCellBorderBrush()
        => new(Color.FromArgb(55, 255, 255, 255));
}
