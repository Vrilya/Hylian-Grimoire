using HylianGrimoire.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HylianGrimoire.MessageInspector;

public sealed partial class MessageByteInspectorWindow
{
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
}
