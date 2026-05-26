using HylianGrimoire.Interop;
using HylianGrimoire.Rom;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace HylianGrimoire.PromptEditor;

public sealed partial class PromptEditorWindow : Window
{
    private readonly Action<string> _onChanged;
    private RomMessageData? _romData;
    private PromptEditorProfile? _profile;
    private readonly List<PromptEditorLine> _lines = [];
    private string _languageKey = "eng";
    private bool _updating;

    public PromptEditorWindow(RomMessageData? romData, Action<string> onChanged)
    {
        InitializeComponent();
        _onChanged = onChanged;
        SystemBackdrop = new MicaBackdrop();
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.IsResizable = false;
        }

        var windowSize = new Windows.Graphics.SizeInt32(1085, 820);
        AppWindow.Resize(windowSize);
        WindowSizeLimits.SetMinimumSize(this, windowSize.Width, windowSize.Height);
        WindowIcon.Apply(this);
        AppWindow.TitleBar.ResetToDefault();
        WindowTheme.Register(this);
        SetRomData(romData);
    }

    public void SetRomData(RomMessageData? romData)
    {
        _romData = romData;
        LoadFromRom();
    }

    private void LoadFromRom()
    {
        _updating = true;
        try
        {
            _lines.Clear();
            PromptList.ItemsSource = null;
            if (_romData is null)
            {
                _profile = null;
                ProfileText.Text = "No ROM loaded.";
                SetControlsEnabled(false);
                PreviewImage.Source = null;
                return;
            }

            if (!PromptEditorProfileCatalog.TryGetProfile(_romData.Profile, out PromptEditorProfile? profile))
            {
                _profile = null;
                ProfileText.Text = _romData.Profile.Name;
                SetControlsEnabled(false);
                PreviewImage.Source = null;
                return;
            }

            _profile = profile;
            _languageKey = PromptEditorProfileCatalog.GetDefaultLanguageKey(_romData.Profile, _romData.ActiveMessageBankIndex);
            ProfileText.Text = GetProfileText(profile, _languageKey);
            ReadCurrentLines();
            PromptList.ItemsSource = _lines;
            PromptList.SelectedIndex = 0;
            SetControlsEnabled(true);
        }
        catch (Exception ex)
        {
            ProfileText.Text = ex.Message;
            SetControlsEnabled(false);
        }
        finally
        {
            _updating = false;
        }

        ShowSelectedLine();
        UpdatePreview();
    }

    private void ReadCurrentLines()
    {
        if (_romData is null || _profile is null)
        {
            return;
        }

        _lines.Clear();
        _lines.AddRange(PromptEditorService.Read(_romData.DecompressedRom, _profile, _languageKey));
    }

    private void OnPromptSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updating)
        {
            return;
        }

        ShowSelectedLine();
        UpdatePreview();
    }

    private void OnCoordinateChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_updating || PromptList.SelectedItem is not PromptEditorLine selected)
        {
            return;
        }

        if (double.IsNaN(IconXBox.Value) || double.IsNaN(TextXBox.Value))
        {
            return;
        }

        int index = _lines.FindIndex(line => line.Kind == selected.Kind);
        if (index < 0)
        {
            return;
        }

        _lines[index] = selected with
        {
            IconX = (int)Math.Round(IconXBox.Value),
            TextX = (int)Math.Round(TextXBox.Value),
        };

        TryWrite();
        UpdatePreview();
    }

    private void OnPreviewInputChanged(object sender, RoutedEventArgs e)
    {
        if (!_updating)
        {
            UpdatePreview();
        }
    }

    private void OnResetSelected(object sender, RoutedEventArgs e)
    {
        if (PromptList.SelectedItem is not PromptEditorLine selected)
        {
            return;
        }

        PromptEditorLanguage language = PromptEditorProfileCatalog.Languages[_languageKey];
        PromptEditorDefaults defaults = language.Defaults[selected.Kind];
        int index = _lines.FindIndex(line => line.Kind == selected.Kind);
        _lines[index] = selected with
        {
            IconX = defaults.IconX,
            TextX = defaults.TextX,
        };
        RefreshListSelection(index);
        TryWrite();
        ShowSelectedLine();
        UpdatePreview();
    }

    private void OnResetAll(object sender, RoutedEventArgs e)
    {
        PromptEditorLanguage language = PromptEditorProfileCatalog.Languages[_languageKey];
        _lines.Clear();
        _lines.AddRange(PromptEditorService.CreateDefaultLines(language));
        RefreshListSelection(0);
        TryWrite();
        ShowSelectedLine();
        UpdatePreview();
    }

    private void TryWrite()
    {
        if (_romData is null || _profile is null)
        {
            return;
        }

        try
        {
            PromptEditorService.Write(_romData.DecompressedRom, _profile, _languageKey, _lines);
            _onChanged(PromptEditorService.IsPatchActive(PromptEditorProfileCatalog.Languages[_languageKey], _lines)
                ? "Updated prompt positions."
                : "Prompt patch removed.");
        }
        catch (Exception ex)
        {
            ProfileText.Text = ex.Message;
        }
    }

    private static string GetProfileText(PromptEditorProfile profile, string languageKey)
    {
        string languageLabel = PromptEditorProfileCatalog.Languages.TryGetValue(languageKey, out PromptEditorLanguage? language)
            ? language.Label
            : languageKey;
        return $"{profile.DisplayName} - {languageLabel}";
    }

    private void ShowSelectedLine()
    {
        _updating = true;
        try
        {
            if (PromptList.SelectedItem is not PromptEditorLine selected)
            {
                SelectedPromptText.Text = "Select a prompt";
                IconXBox.Value = double.NaN;
                TextXBox.Value = double.NaN;
                return;
            }

            SelectedPromptText.Text = selected.Label;
            IconXBox.Value = selected.IconX;
            TextXBox.Value = selected.TextX;
        }
        finally
        {
            _updating = false;
        }
    }

    private void RefreshListSelection(int index)
    {
        PromptList.ItemsSource = null;
        PromptList.ItemsSource = _lines;
        PromptList.SelectedIndex = Math.Clamp(index, 0, _lines.Count - 1);
    }

    private void UpdatePreview()
    {
        if (_romData is null || _profile is null || _lines.Count == 0)
        {
            return;
        }

        try
        {
            PromptEditorKind selected = PromptList.SelectedItem is PromptEditorLine line
                ? line.Kind
                : _lines[0].Kind;
            Uri imageUri = PromptEditorPreviewRenderer.Render(
                _romData.DecompressedRom,
                _profile,
                _languageKey,
                _lines,
                selected,
                GuidesButton.IsChecked == true,
                FramesButton.IsChecked == true);
            PreviewImage.Source = new BitmapImage(imageUri);
        }
        catch (Exception ex)
        {
            ProfileText.Text = ex.Message;
        }
    }

    private void SetControlsEnabled(bool enabled)
    {
        PromptList.IsEnabled = enabled;
        IconXBox.IsEnabled = enabled;
        TextXBox.IsEnabled = enabled;
        FramesButton.IsEnabled = enabled;
        GuidesButton.IsEnabled = enabled;
    }
}
