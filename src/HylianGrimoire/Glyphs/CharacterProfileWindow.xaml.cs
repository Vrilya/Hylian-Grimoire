using System.Collections.ObjectModel;
using System.Drawing;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using HylianGrimoire.Interop;
using HylianGrimoire.Rom;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace HylianGrimoire.Glyphs;

public sealed partial class CharacterProfileWindow : Window
{
    private readonly ObservableCollection<GlyphListItem> _glyphs = [];
    private readonly ObservableCollection<string> _profiles = [];
    private RomGlyphEditorSession? _romSession;
    private bool _updating;
    private byte? _selectedValue;

    public event EventHandler? GlyphDataChanged;

    public CharacterProfileWindow()
        : this(null)
    {
    }

    public CharacterProfileWindow(RomGlyphEditorSession? romSession)
    {
        _romSession = romSession;
        InitializeComponent();
        SystemBackdrop = new MicaBackdrop();
        Title = IsRomMode
            ? "Hylian Grimoire - ROM Glyph Manager"
            : "Hylian Grimoire - Glyph Manager";
        WindowHeadingText.Text = IsRomMode ? "ROM Glyphs" : "Glyphs";
        HelpText.Text = IsRomMode
            ? "Glyph image and width changes are written to the loaded ROM session. Glyph profiles control which keyboard characters map to each byte."
            : "Glyph profiles keep editor characters, preview images, and preview widths together. The byte value stays the same.";

        AttachRomSession(_romSession);

        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
        }

        AppWindow.Resize(new Windows.Graphics.SizeInt32(1100, 640));
        WindowSizeLimits.SetFixedWidth(this, 1100, 640);
        WindowIcon.Apply(this);
        WindowTheme.Register(this);
        Closed += (_, _) => DetachRomSession();

        ProfileCombo.ItemsSource = _profiles;
        GlyphList.ItemsSource = _glyphs;
        ReloadProfiles();
        ReloadGlyphs();
        GlyphList.SelectedIndex = 0;
    }

    public bool IsRomMode => _romSession is not null;

    public void SetRomSession(RomGlyphEditorSession? romSession)
    {
        if (ReferenceEquals(_romSession, romSession))
        {
            return;
        }

        DetachRomSession();
        _romSession = romSession;
        AttachRomSession(_romSession);
        RefreshWindowMode();
        ReloadProfiles();
        ReloadGlyphs();
        if (_selectedValue is byte value)
        {
            ShowGlyph(value);
        }
    }

    private void AttachRomSession(RomGlyphEditorSession? romSession)
    {
        if (romSession is null)
        {
            return;
        }

        romSession.Changed += OnRomSessionChanged;
        CharacterProfileStore.Current.SetCustomGlyphsAvailable(romSession.HasLoadedCustomGlyphOrWidth());
    }

    private void DetachRomSession()
    {
        if (_romSession is not null)
        {
            _romSession.Changed -= OnRomSessionChanged;
        }
    }

    private void OnRomSessionChanged(object? sender, EventArgs e)
    {
        GlyphDataChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RefreshWindowMode()
    {
        Title = IsRomMode
            ? "Hylian Grimoire - ROM Glyph Manager"
            : "Hylian Grimoire - Glyph Manager";
        WindowHeadingText.Text = IsRomMode ? "ROM Glyphs" : "Glyphs";
        HelpText.Text = IsRomMode
            ? "Glyph image and width changes are written to the loaded ROM session. Glyph profiles control which keyboard characters map to each byte."
            : "Glyph profiles keep editor characters, preview images, and preview widths together. The byte value stays the same.";
    }

    private void ReloadGlyphs()
    {
        byte? selected = _selectedValue;
        _glyphs.Clear();
        foreach (byte value in OotGlyphCatalog.GlyphValues)
        {
            OotGlyphInfo info = GetGlyphInfo(value);
            _glyphs.Add(new GlyphListItem(info));
        }

        if (selected is byte selectedValue)
        {
            GlyphList.SelectedItem = _glyphs.FirstOrDefault(item => item.Value == selectedValue);
        }
    }

    private void ReloadProfiles()
    {
        _updating = true;
        try
        {
            _profiles.Clear();
            foreach (string profileName in CharacterProfileStore.Current.ProfileNames)
            {
                _profiles.Add(profileName);
            }

            ProfileCombo.SelectedItem = CharacterProfileStore.Current.SelectedProfileName;
            UpdateProfileControlState();
        }
        finally
        {
            _updating = false;
        }
    }

    private void UpdateProfileControlState()
    {
        CurrentCharBox.IsEnabled = CharacterProfileStore.Current.CanEditSelectedProfile;
        CurrentWidthBox.IsEnabled = CharacterProfileStore.Current.CanEditSelectedProfile || IsRomMode;
        GlyphImageButton.IsEnabled = CharacterProfileStore.Current.CanEditSelectedProfile || IsRomMode;
        ReplaceImageButton.IsEnabled = CharacterProfileStore.Current.CanEditSelectedProfile || IsRomMode;
        ResetImageButton.IsEnabled = CharacterProfileStore.Current.CanEditSelectedProfile || IsRomMode;
        ResetWidthButton.IsEnabled = CharacterProfileStore.Current.CanEditSelectedProfile || IsRomMode;
        ResetCharacterButton.IsEnabled = CharacterProfileStore.Current.CanEditSelectedProfile;
        DeleteProfileButton.IsEnabled = CharacterProfileStore.Current.CanDeleteSelectedProfile;
    }

    private void OnGlyphSelected(object sender, SelectionChangedEventArgs e)
    {
        if (GlyphList.SelectedItem is not GlyphListItem item)
        {
            return;
        }

        _selectedValue = item.Value;
        ShowGlyph(item.Value);
    }

    private void ShowGlyph(byte value)
    {
        OotGlyphInfo info = GetGlyphInfo(value);
        _updating = true;
        try
        {
            DetailTitle.Text = $"{info.Hex} Glyph";
            DefaultCharText.Text = $"{info.DefaultChar}";
            CurrentCharBox.Text = $"{info.CurrentChar}";
            DefaultWidthText.Text = $"{info.DefaultWidth:0.##}";
            CurrentWidthBox.Value = info.CurrentWidth;
            ImageStatusText.Text = IsRomMode
                ? (info.HasImageOverride ? "Custom" : string.Empty)
                : (info.HasImageOverride ? "Custom image" : "Default image");
            GlyphImage.Source = File.Exists(info.CurrentPath) ? new BitmapImage(new Uri(info.CurrentPath)) : null;
            UpdateProfileControlState();
        }
        finally
        {
            _updating = false;
        }
    }

    private void OnCurrentCharChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating || _selectedValue is not byte value || CurrentCharBox.Text.Length == 0)
        {
            return;
        }

        OotGlyphInfo info = GetGlyphInfo(value);
        if (CurrentCharBox.Text[0] == info.CurrentChar)
        {
            return;
        }

        CharacterProfileStore.Current.SetDisplayChar(value, CurrentCharBox.Text[0]);
        RefreshSelected();
    }

    private void OnProfileSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updating || ProfileCombo.SelectedItem is not string profileName)
        {
            return;
        }

        if (IsRomMode && profileName == CharacterProfileStore.DefaultProfileName)
        {
            _romSession?.ResetAllToDefault();
        }
        else if (IsRomMode && profileName == CharacterProfileStore.CustomGlyphsProfileName)
        {
            _romSession?.RestoreLoadedRomGlyphs();
        }

        CharacterProfileStore.Current.SelectProfile(profileName);
        if (IsRomMode
            && profileName != CharacterProfileStore.DefaultProfileName
            && profileName != CharacterProfileStore.CustomGlyphsProfileName)
        {
            _romSession?.ApplySelectedCharacterProfile();
        }

        ReloadProfiles();
        ReloadGlyphs();
        if (_selectedValue is byte value)
        {
            ShowGlyph(value);
        }
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

        bool created = CharacterProfileStore.Current.CreateProfile(profileNameBox.Text);
        if (!created)
        {
            await ShowDialogAsync("Profile not added", "Choose a unique profile name.");
            return;
        }

        _romSession?.CaptureIntoSelectedCharacterProfile();
        ReloadProfiles();
        ReloadGlyphs();
        if (_selectedValue is byte value)
        {
            ShowGlyph(value);
        }
    }

    private async void OnDeleteProfile(object sender, RoutedEventArgs e)
    {
        if (!CharacterProfileStore.Current.CanDeleteSelectedProfile)
        {
            return;
        }

        var dialog = new ContentDialog
        {
            Title = "Delete character profile",
            Content = $"Delete {CharacterProfileStore.Current.SelectedProfileName}?",
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

        CharacterProfileStore.Current.DeleteSelectedProfile();
        if (IsRomMode)
        {
            _romSession?.ResetAllToDefault();
        }

        ReloadProfiles();
        ReloadGlyphs();
        if (_selectedValue is byte value)
        {
            ShowGlyph(value);
        }
    }

    private void OnCurrentWidthChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_updating || _selectedValue is not byte value || double.IsNaN(sender.Value))
        {
            return;
        }

        double width = Math.Round(sender.Value, 2);
        OotGlyphInfo info = GetGlyphInfo(value);
        if (Math.Abs(width - info.CurrentWidth) < 0.001)
        {
            return;
        }

        if (_romSession is null)
        {
            CharacterProfileStore.Current.SetWidth(value, width);
        }
        else
        {
            _romSession.SetWidth(value, width);
            if (CharacterProfileStore.Current.CanEditSelectedProfile)
            {
                CharacterProfileStore.Current.SetWidth(value, width, info.DefaultWidth);
            }
        }

        RefreshSelected();
    }

    private async void OnReplaceImage(object sender, RoutedEventArgs e)
    {
        if (_selectedValue is not byte value)
        {
            return;
        }

        string? path = await PickImageAsync();
        if (path is null)
        {
            return;
        }

        bool sameSize;
        try
        {
            sameSize = HasSameSize(path, OotGlyphCatalog.GetOriginalGlyphPath(value));
        }
        catch
        {
            await ShowDialogAsync("Invalid image", "The selected file could not be read as an image.");
            return;
        }

        if (!sameSize)
        {
            await ShowDialogAsync("Image size mismatch", "The replacement image must have the same pixel size as the original glyph.");
            return;
        }

        if (_romSession is null)
        {
            CharacterProfileStore.Current.SetImage(value, path);
        }
        else
        {
            _romSession.SetImage(value, path);
            if (CharacterProfileStore.Current.CanEditSelectedProfile)
            {
                CharacterProfileStore.Current.SetImage(value, path);
            }
        }

        RefreshSelected();
    }

    private void OnResetImage(object sender, RoutedEventArgs e)
    {
        if (_selectedValue is byte value)
        {
            if (_romSession is null)
            {
                CharacterProfileStore.Current.ResetImage(value);
            }
            else
            {
                _romSession.ResetImage(value);
                if (CharacterProfileStore.Current.CanEditSelectedProfile)
                {
                    CharacterProfileStore.Current.ResetImage(value);
                }
            }

            RefreshSelected();
        }
    }

    private void OnResetWidth(object sender, RoutedEventArgs e)
    {
        if (_selectedValue is byte value)
        {
            if (_romSession is null)
            {
                CharacterProfileStore.Current.ResetWidth(value);
            }
            else
            {
                _romSession.ResetWidth(value);
                if (CharacterProfileStore.Current.CanEditSelectedProfile)
                {
                    CharacterProfileStore.Current.ResetWidth(value);
                }
            }

            RefreshSelected();
        }
    }

    private void OnResetCharacter(object sender, RoutedEventArgs e)
    {
        if (_selectedValue is byte value)
        {
            CharacterProfileStore.Current.ResetDisplayChar(value);
            RefreshSelected();
        }
    }

    private void RefreshSelected()
    {
        if (_selectedValue is not byte value)
        {
            return;
        }

        OotGlyphInfo info = GetGlyphInfo(value);
        GlyphListItem? item = _glyphs.FirstOrDefault(item => item.Value == value);
        if (item is null)
        {
            ReloadGlyphs();
        }
        else
        {
            item.Update(info);
        }

        ShowGlyph(value);
    }

    private OotGlyphInfo GetGlyphInfo(byte value)
    {
        return _romSession?.GetGlyphInfo(value) ?? OotGlyphCatalog.GetGlyphInfo(value);
    }

    private async Task<string?> PickImageAsync()
    {
        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            ViewMode = PickerViewMode.Thumbnail,
        };
        picker.FileTypeFilter.Add(".png");
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
        var file = await picker.PickSingleFileAsync();
        return file?.Path;
    }

    private async Task ShowDialogAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = Content.XamlRoot,
        };
        _ = await dialog.ShowAsync();
    }

    private static bool HasSameSize(string replacementPath, string originalPath)
    {
        using var replacement = new Bitmap(replacementPath);
        using var original = new Bitmap(originalPath);
        return replacement.Width == original.Width && replacement.Height == original.Height;
    }

}
