using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HylianGrimoire.PromptEditor;

public sealed partial class PromptEditorWindow
{
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

        int index = FindLineIndex(selected.Kind);
        if (index < 0)
        {
            return;
        }

        ReplaceLine(index, selected with
        {
            IconX = (int)Math.Round(IconXBox.Value),
            TextX = (int)Math.Round(TextXBox.Value),
        });

        TryWrite();
        RefreshSelectedLineView();
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

        if (_profile is null)
        {
            return;
        }

        PromptEditorLanguage language = _profile.Languages[_languageKey];
        PromptEditorDefaults defaults = language.Defaults[selected.Kind];
        int index = FindLineIndex(selected.Kind);
        if (index < 0)
        {
            return;
        }

        ReplaceLine(index, selected with
        {
            IconX = defaults.IconX,
            TextX = defaults.TextX,
        });
        TryWrite();
        RefreshSelectedLineView();
    }

    private void OnResetAll(object sender, RoutedEventArgs e)
    {
        if (_profile is null)
        {
            return;
        }

        PromptEditorLanguage language = _profile.Languages[_languageKey];
        ReplaceLines(PromptEditorService.CreateDefaultLines(_profile, language));
        SelectLineIndex(0);
        TryWrite();
        RefreshSelectedLineView();
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
            SetStatus(string.Empty);
            _onChanged(PromptEditorService.IsPatchActive(_profile.Languages[_languageKey], _lines)
                ? "Updated prompt positions."
                : "Prompt patch removed.");
        }
        catch (Exception ex)
        {
            SetStatus(UiOperationExceptionHandler.GetDisplayMessage("Prompt editor write failed", ex));
        }
    }
}
