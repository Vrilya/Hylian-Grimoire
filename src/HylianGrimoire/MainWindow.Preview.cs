using Microsoft.UI.Xaml;
using HylianGrimoire.Codecs;
using HylianGrimoire.Games;
using HylianGrimoire.Glyphs;
using HylianGrimoire.Preview;

namespace HylianGrimoire;

public sealed partial class MainWindow
{
    private void OnTogglePreview(object sender, RoutedEventArgs e)
    {
        if (_previewWindow is not null)
        {
            _previewWindow.Close();
            return;
        }

        OpenPreviewWindow();
    }

    private void OpenPreviewWindow()
    {
        if (_previewWindow is null)
        {
            GameProfile? profile = ActiveGameProfile;
            if (profile is null || !profile.Capabilities.SupportsMessagePreview)
            {
                PreviewToggle.IsChecked = false;
                SetStatus(profile is null
                    ? "Create or load a project before opening preview."
                    : $"{profile.DisplayName} message preview is not available yet.");
                return;
            }

            _previewWindow = MessagePreviewWindowFactory.Create(profile);
            PreviewToggle.IsChecked = true;
            _previewWindow.PreviewClosed += (_, _) =>
            {
                _previewWindow = null;
                PreviewToggle.IsChecked = false;
            };
        }

        UpdatePreview();
        _previewWindow.Activate();
    }

    private void UpdatePreview()
    {
        if (_previewWindow is null)
        {
            return;
        }

        if (_session.CurrentIndex < 0 || _session.CurrentIndex >= _session.Entries.Count)
        {
            _previewWindow.SetEmpty();
            return;
        }

        var entry = _session.Entries[_session.CurrentIndex];
        try
        {
            CharacterProfileSnapshot snapshot = CreateCharacterProfileSnapshot(CurrentGameProfile);
            IGlyphSource glyphSource = _characterProfileRuntime.CreateGlyphSource(CurrentGameProfile, _session.RomData, snapshot);
            MessageEncodingProfile encodingProfile = CreateEncodingProfile(CurrentGameProfile, snapshot);

            _previewWindow.SetMessage(
                entry,
                CurrentGameProfile.EditorTextSyntax.FromDisplay(GetEditorText()),
                glyphSource,
                encodingProfile);
        }
        catch (InvalidDataException ex)
        {
            SetStatus($"Preview not updated: {ex.Message}");
        }
    }
}
