using HylianGrimoire.Rom;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HylianGrimoire.Glyphs;

public sealed partial class CharacterProfileWindow
{
    private void ReloadProfiles()
    {
        using IDisposable update = BeginUpdate();
        _profiles.Clear();
        foreach (string profileName in _characterProfileRuntime.ProfileNames)
        {
            _profiles.Add(profileName);
        }

        ProfileCombo.SelectedItem = _characterProfileRuntime.SelectedProfileName;
        UpdateProfileControlState();
    }

    private void UpdateProfileControlState()
    {
        CurrentCharBox.IsEnabled = _characterProfileRuntime.CanEditSelectedProfile;
        CurrentWidthBox.IsEnabled = _characterProfileRuntime.CanEditSelectedProfile || IsRomMode;
        GlyphImageButton.IsEnabled = _characterProfileRuntime.CanEditSelectedProfile || IsRomMode;
        ReplaceImageButton.IsEnabled = _characterProfileRuntime.CanEditSelectedProfile || IsRomMode;
        ResetImageButton.IsEnabled = _characterProfileRuntime.CanEditSelectedProfile || IsRomMode;
        ResetWidthButton.IsEnabled = _characterProfileRuntime.CanEditSelectedProfile || IsRomMode;
        ResetCharacterButton.IsEnabled = _characterProfileRuntime.CanEditSelectedProfile;
        DeleteProfileButton.IsEnabled = _characterProfileRuntime.CanDeleteSelectedProfile;
    }

    private void OnProfileSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updating || ProfileCombo.SelectedItem is not string profileName)
        {
            return;
        }

        SelectCharacterProfile(profileName);
        RefreshProfileAndGlyphViews();
    }

    private void SelectCharacterProfile(string profileName)
    {
        RomGlyphEditorSession? romSession = _romSession;
        if (romSession is not null)
        {
            if (profileName == CharacterProfileStore.DefaultProfileName)
            {
                romSession.ResetAllToDefault();
            }
            else if (profileName == CharacterProfileStore.CustomGlyphsProfileName)
            {
                romSession.RestoreLoadedRomGlyphs();
            }
        }

        _characterProfileRuntime.SelectProfile(profileName);

        if (romSession is not null && IsStoredCharacterProfile(profileName))
        {
            romSession.ApplyCharacterProfile(CreateCharacterProfileSnapshot());
        }
    }

    private static bool IsStoredCharacterProfile(string profileName)
    {
        return profileName != CharacterProfileStore.DefaultProfileName
            && profileName != CharacterProfileStore.CustomGlyphsProfileName;
    }

    private async void OnAddProfile(object sender, RoutedEventArgs e)
    {
        var profileNameBox = new TextBox
        {
            Header = "Profile name",
            PlaceholderText = "New profile",
        };

        var dialog = new ContentDialog
        {
            Title = "Add character profile",
            Content = profileNameBox,
            PrimaryButtonText = "Add",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = Content.XamlRoot,
        };

        ContentDialogResult result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
        {
            return;
        }

        bool created = _characterProfileRuntime.CreateProfile(profileNameBox.Text);
        if (!created)
        {
            await ShowDialogAsync("Profile not added", "Choose a unique profile name.");
            return;
        }

        CaptureRomGlyphsIntoSelectedCharacterProfile();
        RefreshProfileAndGlyphViews();
    }

    private async void OnDeleteProfile(object sender, RoutedEventArgs e)
    {
        if (!_characterProfileRuntime.CanDeleteSelectedProfile)
        {
            return;
        }

        var dialog = new ContentDialog
        {
            Title = "Delete character profile",
            Content = $"Delete {_characterProfileRuntime.SelectedProfileName}?",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = Content.XamlRoot,
        };

        ContentDialogResult result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
        {
            return;
        }

        _characterProfileRuntime.DeleteSelectedProfile();
        if (IsRomMode)
        {
            _romSession?.ResetAllToDefault();
        }

        RefreshProfileAndGlyphViews();
    }

    private void CaptureRomGlyphsIntoSelectedCharacterProfile()
    {
        if (_romSession is null)
        {
            return;
        }

        CharacterProfileSnapshot snapshot = CreateCharacterProfileSnapshot();
        foreach (byte value in GameGlyphCatalog.GetGlyphValues(GlyphGameKind))
        {
            GlyphInfo info = _romSession.GetGlyphInfo(value, snapshot);
            if (info.HasWidthOverride)
            {
                _characterProfileRuntime.SetWidth(value, info.CurrentWidth, info.DefaultWidth);
            }

            if (info.HasImageOverride)
            {
                _characterProfileRuntime.SetImage(value, info.CurrentPath);
            }
        }
    }
}
