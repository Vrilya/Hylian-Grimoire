using HylianGrimoire.Games;
using HylianGrimoire.MessageInspector;
using HylianGrimoire.Models;
using HylianGrimoire.Services;
using Microsoft.UI.Xaml;

namespace HylianGrimoire;

public sealed partial class MainWindow
{
    private MessageByteInspectorWindow? _messageByteInspectorWindow;

    private void OnOpenMessageByteInspector(object sender, RoutedEventArgs e)
    {
        if (!CanUseMessageByteInspectorTool())
        {
            return;
        }

        if (_messageByteInspectorWindow is null)
        {
            _messageByteInspectorWindow = new MessageByteInspectorWindow();
            _messageByteInspectorWindow.Closed += (_, _) => _messageByteInspectorWindow = null;
        }

        RefreshMessageByteInspector();
        _messageByteInspectorWindow.Activate();
    }

    private void RefreshMessageByteInspector()
    {
        if (_messageByteInspectorWindow is null)
        {
            return;
        }

        if (ActiveGameProfile is not GameProfile profile)
        {
            _messageByteInspectorWindow.SetEmpty("No project is loaded.");
            return;
        }

        _messageByteInspectorWindow.SetMessage(
            profile,
            CreateCurrentInspectorEntrySnapshot(),
            CreateCurrentEncodingProfile());
    }

    private MessageEntry? CreateCurrentInspectorEntrySnapshot()
    {
        if (_session.CurrentIndex < 0 || _session.CurrentIndex >= _session.Entries.Count)
        {
            return null;
        }

        MessageEntry snapshot = _session.Entries[_session.CurrentIndex].CreateSnapshot();
        string editorText = CurrentGameProfile.EditorTextSyntax.FromDisplay(GetEditorText());
        if (CurrentGameProfile.EditorTextSyntax.TryNormalizeEditorText(editorText, out string normalized))
        {
            editorText = normalized;
        }

        snapshot.Text = editorText;
        return snapshot;
    }

    private bool CanUseMessageByteInspectorTool()
        => ActiveGameProfile is GameProfile profile
            && MessageByteInspectorService.CanInspect(profile.Kind)
            && _session.Entries.Count > 0;
}
