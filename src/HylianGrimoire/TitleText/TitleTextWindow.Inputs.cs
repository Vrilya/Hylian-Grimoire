using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HylianGrimoire.TitleText;

public sealed partial class TitleTextWindow
{
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
        using (BeginUpdate())
        {
            textBox.Text = upper;
            textBox.SelectionStart = Math.Min(selectionStart, upper.Length);
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
        using (BeginUpdate())
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

    private void SetStatus(string message)
    {
        StatusText.Text = message;
        StatusText.Visibility = string.IsNullOrWhiteSpace(message) ? Visibility.Collapsed : Visibility.Visible;
    }
}
