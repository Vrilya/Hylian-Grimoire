using HylianGrimoire.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HylianGrimoire;

public sealed partial class MainWindow
{
    private void OnControlCodeFlyoutOpening(object sender, object e)
    {
        if (sender is not MenuFlyout flyout)
        {
            return;
        }

        BuildControlCodeFlyout(flyout);
    }

    private void BuildControlCodeFlyout(MenuFlyout flyout)
    {
        flyout.Items.Clear();
        AddEditorCommandItems(flyout);

        if (_session.CurrentIndex < 0)
        {
            flyout.Items.Add(new MenuFlyoutItem
            {
                Text = "Select a message first",
                IsEnabled = false,
            });
            return;
        }

        IReadOnlyList<MessageControlCodeGroup> groups = MessageControlCodeCatalog.GetGroups(CurrentGameProfile.Kind);
        if (groups.Count == 0)
        {
            flyout.Items.Add(new MenuFlyoutItem
            {
                Text = "No control codes available",
                IsEnabled = false,
            });
            return;
        }

        foreach (MessageControlCodeGroup group in groups)
        {
            var subItem = new MenuFlyoutSubItem
            {
                Text = group.Name,
            };

            foreach (MessageControlCodeEntry entry in group.Entries)
            {
                var item = new MenuFlyoutItem
                {
                    Text = entry.Label,
                    Tag = entry,
                };
                ToolTipService.SetToolTip(item, GetControlCodeToolTip(entry));
                item.Click += OnControlCodeMenuItemClick;
                subItem.Items.Add(item);
            }

            flyout.Items.Add(subItem);
        }
    }

    private void OnControlCodeMenuItemClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { Tag: MessageControlCodeEntry entry })
        {
            InsertControlCode(entry);
        }
    }

    private static string GetControlCodeToolTip(MessageControlCodeEntry entry)
    {
        string insertText = entry.InsertText == "\n" ? "Line break" : entry.InsertText;
        return string.IsNullOrWhiteSpace(entry.Description)
            ? insertText
            : $"{insertText}\n{entry.Description}";
    }
}
