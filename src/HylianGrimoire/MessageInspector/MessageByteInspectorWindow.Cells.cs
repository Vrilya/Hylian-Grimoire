using HylianGrimoire.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace HylianGrimoire.MessageInspector;

public sealed partial class MessageByteInspectorWindow
{
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
            MessageByteSegmentKind.RawByte => "Raw Byte",
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
            MessageByteSegmentKind.RawByte => Color.FromArgb(105, 244, 114, 182),
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
