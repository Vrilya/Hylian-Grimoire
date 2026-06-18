namespace HylianGrimoire.PromptEditor;

public sealed partial class PromptEditorWindow
{
    private void LoadFromRom()
    {
        using IDisposable update = BeginUpdate();
        try
        {
            ReplaceLines([]);
            if (_romData is null)
            {
                _profile = null;
                ProfileText.Text = "No ROM loaded.";
                SetStatus(string.Empty);
                SetControlsEnabled(false);
                PreviewImage.Source = null;
                return;
            }

            if (!PromptEditorProfileCatalog.TryGetProfile(_romData.Profile, out PromptEditorProfile? profile))
            {
                _profile = null;
                ProfileText.Text = _romData.Profile.Name;
                SetStatus(string.Empty);
                SetControlsEnabled(false);
                PreviewImage.Source = null;
                return;
            }

            _profile = profile;
            _languageKey = PromptEditorProfileCatalog.GetDefaultLanguageKey(_romData.Profile, _romData.ActiveMessageBankIndex);
            ProfileText.Text = GetProfileText(profile, _languageKey);
            ReadCurrentLines();
            SetStatus(string.Empty);
            SelectLineIndex(0);
            SetControlsEnabled(true);
        }
        catch (Exception ex)
        {
            SetStatus(UiOperationExceptionHandler.GetDisplayMessage("Prompt editor load failed", ex));
            SetControlsEnabled(false);
        }

        RefreshSelectedLineView();
    }

    private void ReadCurrentLines()
    {
        if (_romData is null || _profile is null)
        {
            return;
        }

        ReplaceLines(PromptEditorService.Read(_romData.DecompressedRom, _profile, _languageKey));
    }

    private static string GetProfileText(PromptEditorProfile profile, string languageKey)
    {
        string languageLabel = profile.Languages.TryGetValue(languageKey, out PromptEditorLanguage? language)
            ? language.Label
            : languageKey;
        return $"{profile.DisplayName} - {languageLabel}";
    }
}
