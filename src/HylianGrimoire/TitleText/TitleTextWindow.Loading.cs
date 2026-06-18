namespace HylianGrimoire.TitleText;

public sealed partial class TitleTextWindow
{
    private void LoadFromRom()
    {
        using (BeginUpdate())
        {
            try
            {
                if (_romData is null)
                {
                    _profile = null;
                    ProfileText.Text = "No ROM loaded.";
                    SetStatus(string.Empty);
                    SetControlsEnabled(false);
                    ClearInputs();
                    PreviewImage.Source = null;
                    return;
                }

                if (!TitleTextService.TryGetProfile(_romData.Profile, out TitleTextPatchProfile? profile))
                {
                    _profile = null;
                    ProfileText.Text = _romData.Profile.Name;
                    SetStatus(string.Empty);
                    SetControlsEnabled(false);
                    ClearInputs();
                    PreviewImage.Source = null;
                    return;
                }

                _profile = profile;
                ProfileText.Text = TitleTextService.GetDisplayName(profile, _languageIndex);
                HelpText.Text = GetHelpText(profile);
                (TitleTextLine? noController, TitleTextLine pressStart) =
                    TitleTextService.Read(_romData.DecompressedRom, profile, _languageIndex);
                SetStatus(string.Empty);
                SetControlsEnabled(true);
                SetInputs(noController, pressStart);
            }
            catch (Exception ex)
            {
                SetStatus(UiOperationExceptionHandler.GetDisplayMessage("Title text load failed", ex));
                SetControlsEnabled(false);
            }
        }

        UpdatePreview();
    }

    private string GetHelpText(TitleTextPatchProfile profile)
    {
        int maxSpaces = TitleTextService.GetPressStartMaxSpaces(profile, _languageIndex);
        return maxSpaces == 1
            ? "Use one space to choose where the title-screen gap is drawn. The space is not written as a character."
            : $"Use up to {maxSpaces} spaces to choose where the title-screen gaps are drawn. The spaces are not written as characters.";
    }
}
