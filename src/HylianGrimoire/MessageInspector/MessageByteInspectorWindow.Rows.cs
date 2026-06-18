using HylianGrimoire.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HylianGrimoire.MessageInspector;

public sealed partial class MessageByteInspectorWindow
{
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
}
