using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HylianGrimoire.Models;

public sealed class MessageItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public MessageItem(MessageEntry entry, int index)
    {
        Entry = entry;
        Index = index;
    }

    public MessageEntry Entry { get; }
    public int Index { get; }
    public string Label => Entry.Label();

    public void Refresh() => OnPropertyChanged(nameof(Label));

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
