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
    private bool _updating;

    public TitleTextWindow(RomMessageData? romData, Action<string> onChanged)
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
            (TitleTextLine noController, TitleTextLine pressStart) = TitleTextService.Read(_romData.DecompressedRom, profile);
            ProfileText.Text = profile.DisplayName;
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
        int maxCharacters = sender == NoControllerTextBox ? 14 : 12;
        args.Cancel = !IsValidTitleTextInput(args.NewText, maxCharacters);
    }

    private void TryWriteCurrentTitleText()
    {
        if (_romData is null || _profile is null)
        {
            return;
        }

        try
        {
            TitleTextLine noController = GetNoControllerLine();
            TitleTextLine pressStart = GetPressStartLine();
            TitleTextService.Write(_romData.DecompressedRom, _profile, noController, pressStart);
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
            TitleTextService.Reset(_romData.DecompressedRom, _profile, kind);
            (TitleTextLine noController, TitleTextLine pressStart) = TitleTextService.Read(_romData.DecompressedRom, _profile);
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
                _romData.FontResources,
                GetNoControllerLine(),
                GetPressStartLine(),
                GuidesButton.IsChecked == true);
            PreviewImage.Source = new BitmapImage(imageUri);
        }
        catch (Exception ex)
        {
            ProfileText.Text = ex.Message;
        }
    }

    private TitleTextLine GetNoControllerLine()
    {
        return new TitleTextLine(
            TitleTextKind.NoController,
            NoControllerTextBox.Text,
            GetGapAfterIndex(NoControllerTextBox.Text, 1),
            ParseByte(NoControllerXBox.Text, "No controller X"),
            14);
    }

    private TitleTextLine GetPressStartLine()
    {
        return new TitleTextLine(
            TitleTextKind.PressStart,
            PressStartTextBox.Text,
            GetGapAfterIndex(PressStartTextBox.Text, 4),
            ParseByte(PressStartXBox.Text, "Press start X"),
            12);
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

    private static bool IsValidTitleTextInput(string text, int maxCharacters)
    {
        int spaces = 0;
        int visibleCharacters = 0;

        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];
            if (ch == ' ')
            {
                spaces++;
                if (spaces > 1 || i == 0)
                {
                    return false;
                }

                continue;
            }

            if (ch is not (>= 'A' and <= 'Z') and not (>= 'a' and <= 'z'))
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

    private void SetInputs(TitleTextLine noController, TitleTextLine pressStart)
    {
        _updating = true;
        try
        {
            NoControllerTextBox.Text = noController.Text;
            NoControllerXBox.Text = noController.X.ToString();
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
    }

    private void SetControlsEnabled(bool enabled)
    {
        NoControllerTextBox.IsEnabled = enabled;
        NoControllerXBox.IsEnabled = enabled;
        PressStartTextBox.IsEnabled = enabled;
        PressStartXBox.IsEnabled = enabled;
        GuidesButton.IsEnabled = enabled;
    }
}
