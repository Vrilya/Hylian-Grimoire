using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Globalization.NumberFormatting;

namespace HylianGrimoire.TextTextures;

public sealed partial class TextTextureEditorWindow
{
    private const int HorizontalPositionMinimum = -128;
    private const int HorizontalPositionMaximum = 128;
    private const int DefaultHorizontalPosition = 0;
    private const double DisabledControlOpacity = 0.55;

    private const string PositionHeader = "X position";

    private void UpdateTextControlVisibility()
    {
        bool isBoss = _selectedTextureKind == TextTextureKind.BossTitleCards;
        bool isOcarinaEndTitle = IsOcarinaEndTitleTarget();
        bool isPresentedByEndTitle = IsPresentedByEndTitleTarget();
        bool isLegendEndTitle = IsLegendEndTitleTarget();
        bool isPause = _selectedTextureKind == TextTextureKind.PauseHeaders;
        bool isItem = _selectedTextureKind == TextTextureKind.ItemNames;
        bool isPrompt = _selectedTextureKind == TextTextureKind.PausePrompts;
        bool isChoicePrompt = isPrompt && IsChoicePromptTarget();
        bool isDungeonMapName = _selectedTextureKind == TextTextureKind.DungeonMapNames;
        bool isFileSelect = _selectedTextureKind == TextTextureKind.FileSelect;
        bool isPlace = _selectedTextureKind == TextTextureKind.PlaceTitleCards;
        bool isGameOver = _selectedTextureKind == TextTextureKind.GameOver;

        PrimaryTextBox.Header = isOcarinaEndTitle ? "Title" : "Text";
        SetVisible(PrimaryTextBox, !isBoss);
        SetVisible(PositionSettingsGrid, !isBoss);
        SetVisible(CenterCheck, UsesCenterControl());

        SetItemControlsVisible(isItem);
        SetCompactTextControlsVisible(isChoicePrompt || isDungeonMapName || isFileSelect, isChoicePrompt || isFileSelect);
        SetEndTitleControlsVisible(isOcarinaEndTitle, isPresentedByEndTitle, isLegendEndTitle);
        SetPlaceTitleCardControlsVisible(isPlace);
        SetBossTitleCardControlsVisible(isBoss);
        SetGameOverControlsVisible(isGameOver);
        SetPauseHeaderControlsVisible(isPause);

        Grid.SetColumnSpan(XNudgeBox, isItem || isChoicePrompt || isDungeonMapName || isFileSelect || isPlace || isPause || isLegendEndTitle || isGameOver ? 1 : 2);
    }

    private void UpdateEnabledState()
    {
        bool hasTarget = _romData is not null && GetSelectedTarget() is not null;
        PrimaryTextBox.IsEnabled = hasTarget;
        TargetCombo.IsEnabled = hasTarget;
        SavePngButton.IsEnabled = hasTarget;
        ApplyButton.IsEnabled = hasTarget;
        CenterCheck.IsEnabled = hasTarget;

        SetItemControlsEnabled(hasTarget);
        SetCompactTextControlsEnabled(hasTarget);
        SetEndTitleControlsEnabled(hasTarget);
        SetPlaceTitleCardControlsEnabled(hasTarget);
        SetBossTitleCardControlsEnabled(hasTarget);
        SetGameOverControlsEnabled(hasTarget);
        SetPauseHeaderControlsEnabled(hasTarget);
        UpdatePositionControlState(hasTarget);
    }

    private void UpdateRenderSettings()
    {
        if (_updatingControls)
        {
            return;
        }

        switch (_selectedTextureKind)
        {
            case TextTextureKind.ItemNames:
                _itemSettings = ReadItemSettings();
                break;
            case TextTextureKind.PausePrompts:
                _promptSettings = ReadPromptSettings();
                break;
            case TextTextureKind.DungeonMapNames:
                ReadDungeonMapNameSettings();
                break;
            case TextTextureKind.FileSelect:
                ReadFileSelectSettings();
                break;
            case TextTextureKind.EndTitles:
                _endTitleSettings = ReadEndTitleSettings();
                break;
            case TextTextureKind.PlaceTitleCards:
                _placeSettings = ReadPlaceSettings();
                break;
            case TextTextureKind.BossTitleCards:
                _bossSettings = ReadBossSettings();
                break;
            case TextTextureKind.GameOver:
                ReadGameOverSettings();
                break;
            case TextTextureKind.PauseHeaders:
                _pauseSettings = ReadPauseSettings();
                _showPauseOriginalColors = PauseOriginalColorsCheck.IsChecked == true;
                break;
        }

        UpdatePositionControlState();
        RefreshPreview();
    }

    private void SetControlsFromCurrentSettings()
    {
        _updatingControls = true;
        try
        {
            CenterCheck.Content = "Center";
            switch (_selectedTextureKind)
            {
                case TextTextureKind.ItemNames:
                    SetItemControlsFromSettings();
                    break;
                case TextTextureKind.PausePrompts:
                    SetPromptControlsFromSettings();
                    break;
                case TextTextureKind.DungeonMapNames:
                    SetDungeonMapNameControlsFromSettings();
                    break;
                case TextTextureKind.FileSelect:
                    SetFileSelectControlsFromSettings();
                    break;
                case TextTextureKind.EndTitles:
                    SetEndTitleControlsFromSettings();
                    break;
                case TextTextureKind.PlaceTitleCards:
                    SetPlaceTitleCardControlsFromSettings();
                    break;
                case TextTextureKind.BossTitleCards:
                    SetBossTitleCardControlsFromSettings();
                    break;
                case TextTextureKind.GameOver:
                    SetGameOverControlsFromSettings();
                    break;
                case TextTextureKind.PauseHeaders:
                    SetPauseHeaderControlsFromSettings();
                    break;
            }
        }
        finally
        {
            _updatingControls = false;
        }

        UpdatePositionControlState();
    }

    private void ConfigureStandardHorizontalPositionBox(double value)
        => ConfigureHorizontalPositionBox(HorizontalPositionMinimum, HorizontalPositionMaximum, PositionHeader, value);

    private void ConfigureHorizontalPositionBox(
        double minimum,
        double maximum,
        string header,
        double value,
        double smallChange = 1,
        double largeChange = 10)
    {
        XNudgeBox.Minimum = minimum;
        XNudgeBox.Maximum = maximum;
        XNudgeBox.Header = header;
        XNudgeBox.SmallChange = smallChange;
        XNudgeBox.LargeChange = largeChange;
        ConfigureNumberFormatter(XNudgeBox, smallChange < 1 ? 1 : 0);
        XNudgeBox.Value = value;
    }

    private void UpdatePositionControlState(bool? hasTargetOverride = null)
    {
        bool hasTarget = hasTargetOverride ?? (_romData is not null && GetSelectedTarget() is not null);
        SetEnabledWithOpacity(XNudgeBox, hasTarget && (!UsesCenterControl() || CenterCheck.IsChecked != true));
        UpdateItemPositionControlState(hasTarget);
        UpdateCompactTextPositionControlState(hasTarget);
        UpdateEndTitlePositionControlState(hasTarget);
        UpdatePlaceTitleCardPositionControlState(hasTarget);
        UpdateBossTitleCardPositionControlState(hasTarget);
        UpdateGameOverPositionControlState(hasTarget);
        UpdatePauseHeaderPositionControlState(hasTarget);
    }

    private static int ReadInt(NumberBox box, int fallback)
        => double.IsNaN(box.Value) ? fallback : (int)Math.Round(box.Value);

    private static double ReadDouble(NumberBox box, double fallback)
        => double.IsNaN(box.Value) ? fallback : box.Value;

    private static void ConfigureNumberFormatter(NumberBox box, int fractionDigits)
    {
        double increment = Math.Pow(10, -(int)fractionDigits);
        box.NumberFormatter = new DecimalFormatter
        {
            FractionDigits = fractionDigits,
            NumberRounder = new IncrementNumberRounder
            {
                Increment = increment,
                RoundingAlgorithm = RoundingAlgorithm.RoundHalfUp,
            },
        };
    }

    private static void SetVisible(UIElement element, bool visible)
        => element.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

    private static void SetEnabledWithOpacity(Control control, bool enabled)
    {
        control.IsEnabled = enabled;
        control.Opacity = enabled ? 1 : DisabledControlOpacity;
    }
}
