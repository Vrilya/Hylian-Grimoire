using HylianGrimoire.Interop;
using HylianGrimoire.Rom;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;

namespace HylianGrimoire.TitleText;

public sealed partial class TitleTextWindow : Window
{
    private readonly Action<string> _onChanged;
    private RomMessageData? _romData;
    private TitleTextPatchProfile? _profile;
    private int _languageIndex;
    private bool _updating;

    public TitleTextWindow(RomMessageData? romData, int languageIndex, Action<string> onChanged)
    {
        InitializeComponent();
        _onChanged = onChanged;
        SystemBackdrop = new MicaBackdrop();
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.IsResizable = false;
        }

        AppWindow.Resize(new Windows.Graphics.SizeInt32(1389, 950));
        WindowTheme.Register(this);
        WindowIcon.Apply(this);
        WindowSizeLimits.SetFixedSize(this, 1389, 950);
        SetRomData(romData, languageIndex);
    }

    public void SetRomData(RomMessageData? romData, int languageIndex)
    {
        _romData = romData;
        _languageIndex = languageIndex;
        LoadFromRom();
    }

    private void LoadFromRom()
    {
        _updating = true;
        try
        {
            if (_romData is null)
            {
                _profile = null;
                ProfileText.Text = "No ROM loaded.";
                SetControlsEnabled(false);
                ClearInputs();
                PreviewImage.Source = null;
                return;
            }

            if (!TitleTextService.TryGetProfile(_romData.Profile, out TitleTextPatchProfile? profile))
            {
                _profile = null;
                ProfileText.Text = _romData.Profile.Name;
                SetControlsEnabled(false);
                ClearInputs();
                PreviewImage.Source = null;
                return;
            }

            _profile = profile;
            (TitleTextLine? noController, TitleTextLine pressStart) =
                TitleTextService.Read(_romData.DecompressedRom, profile, _languageIndex);
            ProfileText.Text = TitleTextService.GetDisplayName(profile, _languageIndex);
            HelpText.Text = GetHelpText(profile);
            SetControlsEnabled(true);
            SetInputs(noController, pressStart);
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

        UpdatePreview();
    }

    private void OnPreviewInputChanged(object sender, RoutedEventArgs e)
    {
        if (_updating)
        {
            return;
        }

        if (sender is TextBox textBox)
        {
            NormalizeTitleTextBox(textBox);
        }

        if (!ReferenceEquals(sender, GuidesButton))
        {
            TryWriteCurrentTitleText();
        }

        UpdatePreview();
    }

    private void OnTitleTextBeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
    {
        int maxCharacters = sender == NoControllerTextBox
            ? _profile?.NoController?.MaxCharacters ?? 14
            : _profile is null
                ? 12
                : TitleTextService.GetPressStartMaxCharacters(_profile, _languageIndex);
        int maxSpaces = sender == NoControllerTextBox || _profile is null
            ? 1
            : TitleTextService.GetPressStartMaxSpaces(_profile, _languageIndex);
        bool allowUWithDiaeresis = sender != NoControllerTextBox &&
            _profile is not null &&
            TitleTextService.AllowsLocalizedUDiaeresis(_profile, _languageIndex);
        args.Cancel = !IsValidTitleTextInput(args.NewText, maxCharacters, maxSpaces, allowUWithDiaeresis);
    }

    private void TryWriteCurrentTitleText()
    {
        if (_romData is null || _profile is null)
        {
            return;
        }

        try
        {
            TitleTextLine? noController = GetNoControllerLine();
            TitleTextLine pressStart = GetPressStartLine();
            TitleTextService.Write(_romData.DecompressedRom, _profile, noController, pressStart, _languageIndex);
            _onChanged("Title text edited.");
        }
        catch (Exception ex)
        {
            ProfileText.Text = ex.Message;
        }
    }

    private void OnResetNoController(object sender, RoutedEventArgs e)
    {
        ResetLine(TitleTextKind.NoController);
    }

    private void OnResetPressStart(object sender, RoutedEventArgs e)
    {
        ResetLine(TitleTextKind.PressStart);
    }

    private void ResetLine(TitleTextKind kind)
    {
        if (_romData is null || _profile is null)
        {
            return;
        }

        try
        {
            TitleTextService.Reset(_romData.DecompressedRom, _profile, kind, _languageIndex);
            (TitleTextLine? noController, TitleTextLine pressStart) =
                TitleTextService.Read(_romData.DecompressedRom, _profile, _languageIndex);
            SetInputs(noController, pressStart);
            string message = kind == TitleTextKind.NoController
                ? "No controller text reset."
                : "Press start text reset.";
            _onChanged(message);
            UpdatePreview();
        }
        catch (Exception ex)
        {
            ProfileText.Text = ex.Message;
        }
    }

    private void UpdatePreview()
    {
        if (_romData is null || _profile is null)
        {
            return;
        }

        try
        {
            Uri imageUri = TitleTextPreviewRenderer.Render(
                _romData.DecompressedRom,
                _profile,
                _romData.FontResources,
                GetNoControllerLine(),
                GetPressStartLine(),
                GuidesButton.IsChecked == true,
                _languageIndex);
            PreviewImage.Source = new BitmapImage(imageUri);
        }
        catch (Exception ex)
        {
            ProfileText.Text = ex.Message;
        }
    }

    private TitleTextLine? GetNoControllerLine()
    {
        if (_profile?.NoController is null)
        {
            return null;
        }

        return new TitleTextLine(
            TitleTextKind.NoController,
            NoControllerTextBox.Text,
            GetGapAfterIndex(NoControllerTextBox.Text, _profile.NoController.DefaultGapAfterIndex),
            ParseByte(NoControllerXBox.Text, "No controller X"),
            _profile.NoController.MaxCharacters);
    }

    private TitleTextLine GetPressStartLine()
    {
        TitleTextPatchProfile loadedProfile = _profile
            ?? throw new InvalidOperationException("No title text profile is loaded.");
        int maxCharacters = TitleTextService.GetPressStartMaxCharacters(loadedProfile, _languageIndex);

        return new TitleTextLine(
            TitleTextKind.PressStart,
            PressStartTextBox.Text,
            GetGapAfterIndex(PressStartTextBox.Text, loadedProfile.PressStart.DefaultGapAfterIndex),
            ParseByte(PressStartXBox.Text, "Press start X"),
            maxCharacters);
    }

    private static int GetGapAfterIndex(string text, int defaultGapAfterIndex)
    {
        text = text.Trim().ToUpperInvariant();
        int gapIndex = text.IndexOf(' ');
        if (gapIndex > 0)
        {
            return gapIndex - 1;
        }

        int visibleCharacters = text.Count(ch => ch is >= 'A' and <= 'Z');
        return visibleCharacters > 0 ? visibleCharacters - 1 : defaultGapAfterIndex;
    }

    private static bool IsValidTitleTextInput(string text, int maxCharacters, int maxSpaces, bool allowUWithDiaeresis)
    {
        int spaces = 0;
        int visibleCharacters = 0;

        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];
            if (ch == ' ')
            {
                spaces++;
                if (spaces > maxSpaces || i == 0)
                {
                    return false;
                }

                continue;
            }

            if (ch is not (>= 'A' and <= 'Z') and not (>= 'a' and <= 'z') &&
                !(allowUWithDiaeresis && (ch == 'Ü' || ch == 'ü')))
            {
                return false;
            }

            visibleCharacters++;
            if (visibleCharacters > maxCharacters)
            {
                return false;
            }
        }

        return true;
    }

    private void NormalizeTitleTextBox(TextBox textBox)
    {
        string upper = textBox.Text.ToUpperInvariant();
        if (textBox.Text == upper)
        {
            return;
        }

        int selectionStart = textBox.SelectionStart;
        _updating = true;
        try
        {
            textBox.Text = upper;
            textBox.SelectionStart = Math.Min(selectionStart, upper.Length);
        }
        finally
        {
            _updating = false;
        }
    }

    private static int ParseByte(string text, string label)
    {
        if (!int.TryParse(text, out int value) || value is < 0 or > 255)
        {
            throw new InvalidDataException($"{label} must be a number from 0 to 255.");
        }

        return value;
    }

    private void SetInputs(TitleTextLine? noController, TitleTextLine pressStart)
    {
        _updating = true;
        try
        {
            NoControllerPanel.Visibility = noController is null ? Visibility.Collapsed : Visibility.Visible;
            if (noController is not null)
            {
                NoControllerTextBox.Text = noController.Text;
                NoControllerXBox.Text = noController.X.ToString();
            }
            else
            {
                NoControllerTextBox.Text = string.Empty;
                NoControllerXBox.Text = string.Empty;
            }

            PressStartTextBox.Text = pressStart.Text;
            PressStartXBox.Text = pressStart.X.ToString();
        }
        finally
        {
            _updating = false;
        }
    }

    private void ClearInputs()
    {
        NoControllerTextBox.Text = string.Empty;
        NoControllerXBox.Text = string.Empty;
        PressStartTextBox.Text = string.Empty;
        PressStartXBox.Text = string.Empty;
        NoControllerPanel.Visibility = Visibility.Visible;
    }

    private void SetControlsEnabled(bool enabled)
    {
        bool noControllerEnabled = enabled && _profile?.NoController is not null;
        NoControllerTextBox.IsEnabled = noControllerEnabled;
        NoControllerXBox.IsEnabled = noControllerEnabled;
        PressStartTextBox.IsEnabled = enabled;
        PressStartXBox.IsEnabled = enabled;
        GuidesButton.IsEnabled = enabled;
    }

    private string GetHelpText(TitleTextPatchProfile profile)
    {
        int maxSpaces = TitleTextService.GetPressStartMaxSpaces(profile, _languageIndex);
        return maxSpaces == 1
            ? "Use one space to choose where the title-screen gap is drawn. The space is not written as a character."
            : $"Use up to {maxSpaces} spaces to choose where the title-screen gaps are drawn. The spaces are not written as characters.";
    }
}
