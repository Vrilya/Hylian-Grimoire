namespace HylianGrimoire.TextTextures;

public sealed partial class TextTextureEditorWindow
{
    private void SetEndTitleControlsVisible(bool isOcarinaEndTitle, bool isPresentedByEndTitle, bool isLegendEndTitle)
    {
        SetVisible(EndTitleComponentPositionGrid, isOcarinaEndTitle);
        SetVisible(LegendShowRegisteredCheck, isLegendEndTitle);
        SetVisible(EndTitleBoldCheck, isPresentedByEndTitle);
        SetVisible(LegendRegisteredXBox, isLegendEndTitle);
    }

    private void SetEndTitleControlsEnabled(bool enabled)
    {
        EndTitlePrefixXBox.IsEnabled = enabled;
        EndTitleTmXBox.IsEnabled = enabled;
        EndTitleSuffixXBox.IsEnabled = enabled;
        EndTitleBoldCheck.IsEnabled = enabled;
        LegendShowRegisteredCheck.IsEnabled = enabled;
    }

    private void SetEndTitleControlsFromSettings()
    {
        ConfigureStandardHorizontalPositionBox(_endTitleSettings.XNudge);
        CenterCheck.IsChecked = _endTitleSettings.Center;
        EndTitlePrefixXBox.Value = _endTitleSettings.PrefixX;
        EndTitleTmXBox.Value = _endTitleSettings.TmX;
        EndTitleSuffixXBox.Value = _endTitleSettings.SuffixX;
        EndTitleBoldCheck.IsChecked = _endTitleSettings.PresentedByBold;
        LegendShowRegisteredCheck.IsChecked = _endTitleSettings.LegendShowRegistered;
        LegendRegisteredXBox.Value = _endTitleSettings.LegendRegisteredXNudge;
    }

    private EndTitleTextureRenderSettings ReadEndTitleSettings()
    {
        bool usesCenter = UsesCenterControl();
        bool center = usesCenter ? CenterCheck.IsChecked == true : _endTitleSettings.Center;
        bool isLegend = IsLegendEndTitleTarget();
        return _endTitleSettings with
        {
            Center = center,
            XNudge = usesCenter && center ? DefaultHorizontalPosition : ReadInt(XNudgeBox, DefaultHorizontalPosition),
            PrefixX = ReadDouble(EndTitlePrefixXBox, _endTitleSettings.PrefixX),
            TmX = ReadDouble(EndTitleTmXBox, _endTitleSettings.TmX),
            SuffixX = ReadDouble(EndTitleSuffixXBox, _endTitleSettings.SuffixX),
            LegendRegisteredXNudge = isLegend ? ReadDouble(LegendRegisteredXBox, _endTitleSettings.LegendRegisteredXNudge) : _endTitleSettings.LegendRegisteredXNudge,
            LegendShowRegistered = isLegend ? LegendShowRegisteredCheck.IsChecked == true : _endTitleSettings.LegendShowRegistered,
            PresentedByBold = EndTitleBoldCheck.IsChecked == true,
        };
    }

    private void UpdateEndTitlePositionControlState(bool hasTarget)
        => SetEnabledWithOpacity(LegendRegisteredXBox, hasTarget && LegendShowRegisteredCheck.IsChecked == true);

    private bool IsOcarinaEndTitleTarget()
    {
        if (_selectedTextureKind != TextTextureKind.EndTitles)
        {
            return false;
        }

        if (GetSelectedTarget()?.Texture is null)
        {
            return true;
        }

        return GetSelectedEndTitleStyle() == EndTitleTextureStyle.OcarinaOfTime;
    }

    private bool IsPresentedByEndTitleTarget()
        => GetSelectedEndTitleStyle() == EndTitleTextureStyle.PresentedBy;

    private bool IsLegendEndTitleTarget()
        => GetSelectedEndTitleStyle() == EndTitleTextureStyle.LegendOfZelda;

    private bool UsesCenterControl()
        => _selectedTextureKind != TextTextureKind.BossTitleCards
            && !IsFixedPositionEndTitleTarget();

    private bool IsFixedPositionEndTitleTarget()
    {
        if (_selectedTextureKind != TextTextureKind.EndTitles)
        {
            return false;
        }

        EndTitleTextureStyle? style = GetSelectedEndTitleStyle();
        return style is EndTitleTextureStyle.OcarinaOfTime or EndTitleTextureStyle.LegendOfZelda;
    }

    private EndTitleTextureStyle? GetSelectedEndTitleStyle()
    {
        if (_selectedTextureKind != TextTextureKind.EndTitles)
        {
            return null;
        }

        if (GetSelectedTarget()?.Texture is not { } texture
            || !EndTitleTextureCatalog.IsEndTitleTexture(texture))
        {
            return null;
        }

        return EndTitleTextureCatalog.GetSpec(texture).Style;
    }
}
