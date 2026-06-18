using HylianGrimoire.Textures;

namespace HylianGrimoire.TextTextures;

public sealed partial class TextTextureEditorWindow
{
    private static readonly TextTextureKindDescriptor[] TextureKinds =
    [
        new(
            TextTextureKind.ItemNames,
            "Item Names",
            SmallTexturePreviewScale,
            "item_texture",
            profile => ItemNameTextureCatalog.TryGetTargets(profile, out IReadOnlyList<TextureDefinition>? textures)
                ? textures.Select(texture => new TextTextureTargetItem(texture, TextTextureKind.ItemNames)).ToArray()
                : [],
            target => target.TargetKey.Contains(PreferredItemNameTextureFragment, StringComparison.Ordinal)),
        new(
            TextTextureKind.MapNames,
            "Map Names",
            SmallTexturePreviewScale,
            "map_name",
            profile => MapNameTextureCatalog.TryGetTargets(profile, out IReadOnlyList<TextureDefinition>? textures)
                ? textures.Select(texture => new TextTextureTargetItem(texture, TextTextureKind.MapNames)).ToArray()
                : [],
            target => string.Equals(target.TargetKey, PreferredMapNameTextureName, StringComparison.Ordinal)),
        new(
            TextTextureKind.MapPositionNames,
            "Map Position Names",
            SmallTexturePreviewScale,
            "map_position_name",
            profile => MapPositionNameTextureCatalog.TryGetTargets(profile, out IReadOnlyList<TextureDefinition>? textures)
                ? textures.Select(texture => new TextTextureTargetItem(texture, TextTextureKind.MapPositionNames)).ToArray()
                : [],
            target => string.Equals(target.TargetKey, PreferredMapPositionNameTextureName, StringComparison.Ordinal)),
        new(
            TextTextureKind.PausePrompts,
            "Pause Prompts",
            SmallTexturePreviewScale,
            "pause_prompt",
            profile => PausePromptTextureCatalog.TryGetTargets(profile, out IReadOnlyList<TextureDefinition>? textures)
                ? textures.Select(texture => new TextTextureTargetItem(texture, TextTextureKind.PausePrompts)).ToArray()
                : [],
            target => string.Equals(target.TargetKey, PreferredPausePromptTextureName, StringComparison.Ordinal)),
        new(
            TextTextureKind.DungeonMapNames,
            "Dungeon Map Names",
            SmallTexturePreviewScale,
            "dungeon_map_name",
            profile => DungeonMapNameTextureCatalog.TryGetTargets(profile, out IReadOnlyList<TextureDefinition>? textures)
                ? textures.Select(texture => new TextTextureTargetItem(texture, TextTextureKind.DungeonMapNames)).ToArray()
                : [],
            target => string.Equals(target.TargetKey, PreferredDungeonMapNameTextureName, StringComparison.Ordinal)),
        new(
            TextTextureKind.FileSelect,
            "File Select",
            SmallTexturePreviewScale,
            "file_select",
            profile => FileSelectTextureCatalog.TryGetTargets(profile, out IReadOnlyList<TextureDefinition>? textures)
                ? textures.Select(texture => new TextTextureTargetItem(texture, TextTextureKind.FileSelect)).ToArray()
                : [],
            target => string.Equals(target.TargetKey, PreferredFileSelectTextureName, StringComparison.Ordinal)),
        new(
            TextTextureKind.PlaceTitleCards,
            "Place Title Cards",
            PlaceTitleCardPreviewScale,
            "place_titlecard",
            profile => PlaceTitleCardTextureCatalog.TryGetTargets(profile, out IReadOnlyList<TextureDefinition>? textures)
                ? textures.Select(texture => new TextTextureTargetItem(texture, TextTextureKind.PlaceTitleCards)).ToArray()
                : [],
            target => target.TargetKey.Contains(PreferredPlaceTitleCardTextureFragment, StringComparison.Ordinal)),
        new(
            TextTextureKind.BossTitleCards,
            "Boss Title Cards",
            BossTitleCardPreviewScale,
            "boss_titlecard",
            profile => BossTitleCardTextureCatalog.TryGetTargets(profile, out IReadOnlyList<TextureDefinition>? textures)
                ? textures.Select(texture => new TextTextureTargetItem(texture, TextTextureKind.BossTitleCards)).ToArray()
                : [],
            target => target.TargetKey.Contains(PreferredBossTitleCardTextureFragment, StringComparison.Ordinal)),
        new(
            TextTextureKind.GameOver,
            "Game Over",
            GameOverPreviewScale,
            "game_over",
            profile => GameOverTextureCatalog.TryGetTargets(profile, out IReadOnlyList<GameOverTextureTarget>? targets)
                ? targets.Select(target => new TextTextureTargetItem(target)).ToArray()
                : [],
            target => target.GameOverTarget?.Spec.Key == PreferredGameOverKey),
        new(
            TextTextureKind.PauseHeaders,
            "Pause Headers",
            PauseHeaderPreviewScale,
            "pause_header",
            profile => PauseHeaderTextureCatalog.TryGetTargets(profile, out IReadOnlyList<PauseHeaderTextureTarget>? targets)
                ? targets.Select(target => new TextTextureTargetItem(target)).ToArray()
                : [],
            target => target.PauseHeaderTarget?.Spec.Key == PreferredPauseHeaderKey),
        new(
            TextTextureKind.EndTitles,
            "End Titles",
            SmallTexturePreviewScale,
            "end_title",
            profile => EndTitleTextureCatalog.TryGetTargets(profile, out IReadOnlyList<TextureDefinition>? textures)
                ? textures.Select(texture => new TextTextureTargetItem(texture, TextTextureKind.EndTitles)).ToArray()
                : [],
            target => string.Equals(target.TargetKey, PreferredEndTitleTextureName, StringComparison.Ordinal)),
        ..CreateMajorasMaskTextureKindDescriptors(),
    ];

    private const string PreferredItemNameTextureFragment = "BoleroOfFire";
    private const string PreferredMapNameTextureName = "gHyruleFieldPointNameENGTex";
    private const string PreferredMapPositionNameTextureName = "gHyruleFieldPositionNameENGTex";
    private const string PreferredPausePromptTextureName = "gPauseToSelectItemENGTex";
    private const string PreferredDungeonMapNameTextureName = "gPauseBotWTitleENGTex";
    private const string PreferredFileSelectTextureName = "gFileSelAreYouSureENGTex";
    private const string PreferredEndTitleTextureName = "sOcarinaOfTimeTex";
    private const string PreferredPlaceTitleCardTextureFragment = "ForestTemple";
    private const string PreferredBossTitleCardTextureFragment = "Volvagia";
    private const string PreferredGameOverKey = "GameOver";
    private const string PreferredPauseHeaderKey = "Equipment";

    private void RefreshTextureKindOptions()
    {
        using IDisposable controlUpdate = BeginControlUpdate();
        _availableTextureKinds = GetAvailableTextureKinds();
        TextureKindCombo.ItemsSource = null;
        TextureKindCombo.ItemsSource = _availableTextureKinds;
        TextureKindCombo.IsEnabled = _availableTextureKinds.Count > 0;

        TextTextureKindDescriptor? selected = _availableTextureKinds.FirstOrDefault(item => item.Kind == _selectedTextureKind)
            ?? _availableTextureKinds.FirstOrDefault();
        if (selected is null)
        {
            TextureKindCombo.SelectedItem = null;
            return;
        }

        _selectedTextureKind = selected.Kind;
        TextureKindCombo.SelectedItem = selected;
    }

    private IReadOnlyList<TextTextureKindDescriptor> GetAvailableTextureKinds()
    {
        if (_romData is null)
        {
            return TextureKinds;
        }

        return TextureKinds
            .Where(kind => IsTextureKindSupportedByProfile(kind, _romData.Profile))
            .Where(kind => kind.HasTargets(_romData.Profile))
            .ToArray();
    }

    private void LoadTargets()
    {
        using IDisposable controlUpdate = BeginControlUpdate();
        TargetCombo.ItemsSource = null;
        TargetCombo.SelectedItem = null;
        if (_romData is null)
        {
            ProfileText.Text = "Load a supported ROM.";
            TargetCombo.ItemsSource = Array.Empty<TextTextureTargetItem>();
            ClearTextInputs();
            return;
        }

        IReadOnlyList<TextTextureTargetItem> items = SelectedTextureKind.GetTargets(_romData.Profile);
        if (items.Count == 0)
        {
            ProfileText.Text = $"No {GetTextureKindLabel()} targets for {_romData.Profile.Name}.";
            TargetCombo.ItemsSource = Array.Empty<TextTextureTargetItem>();
            ClearTextInputs();
            return;
        }

        ProfileText.Text = _romData.Profile.Name;
        TargetCombo.ItemsSource = items;
        TextTextureTargetItem selected = items.FirstOrDefault(SelectedTextureKind.IsPreferredTarget) ?? items[0];
        TargetCombo.SelectedItem = selected;
        SetTextFromTarget(selected);
        UpdateTextControlVisibility();
        SetPreviewSlotSize();
    }

    private TextTextureKindDescriptor SelectedTextureKind
        => TextureKinds.First(item => item.Kind == _selectedTextureKind);

    private TextTextureTargetItem? GetSelectedTarget()
        => TargetCombo.SelectedItem as TextTextureTargetItem;

    private void SetTextFromTarget(TextTextureTargetItem item)
    {
        if (_selectedTextureKind == TextTextureKind.BossTitleCards)
        {
            BossTopTextBox.Text = item.DefaultTopText;
            BossBottomTextBox.Text = item.DefaultBottomText;
            return;
        }

        if (_selectedTextureKind == TextTextureKind.EndTitles)
        {
            if (item.Texture is { } texture && EndTitleTextureCatalog.GetSpec(texture).Style == EndTitleTextureStyle.OcarinaOfTime)
            {
                PrimaryTextBox.Text = EndTitleTextParts.Parse(item.DefaultText).Title;
                return;
            }

            PrimaryTextBox.Text = item.DefaultText;
            return;
        }

        if (_selectedTextureKind == TextTextureKind.MapPositionNames)
        {
            string[] lines = item.DefaultText
                .ReplaceLineEndings("\n")
                .Split('\n', StringSplitOptions.TrimEntries);
            MapPositionLine1TextBox.Text = lines.ElementAtOrDefault(0) ?? string.Empty;
            MapPositionLine2TextBox.Text = lines.ElementAtOrDefault(1) ?? string.Empty;
            PrimaryTextBox.Text = item.DefaultText;
            return;
        }

        PrimaryTextBox.Text = item.DefaultText;
    }

    private void ClearTextInputs()
    {
        PrimaryTextBox.Text = string.Empty;
        MapPositionLine1TextBox.Text = string.Empty;
        MapPositionLine2TextBox.Text = string.Empty;
        BossTopTextBox.Text = string.Empty;
        BossBottomTextBox.Text = string.Empty;
    }

    private string GetTextureKindLabel()
        => SelectedTextureKind.Label;
}
