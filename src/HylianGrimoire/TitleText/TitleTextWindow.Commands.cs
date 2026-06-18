using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HylianGrimoire.TitleText;

public sealed partial class TitleTextWindow
{
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
            SetStatus(string.Empty);
            _onChanged("Title text edited.");
        }
        catch (Exception ex)
        {
            SetStatus(UiOperationExceptionHandler.GetDisplayMessage("Title text write failed", ex));
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
            SetStatus(string.Empty);
            _onChanged(message);
            UpdatePreview();
        }
        catch (Exception ex)
        {
            SetStatus(UiOperationExceptionHandler.GetDisplayMessage("Title text reset failed", ex));
        }
    }
}
