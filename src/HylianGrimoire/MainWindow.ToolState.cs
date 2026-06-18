using HylianGrimoire.Games;
using HylianGrimoire.Models;
using HylianGrimoire.O2r;
using HylianGrimoire.Services;

namespace HylianGrimoire;

public sealed partial class MainWindow
{
    private void RefreshAuxiliaryWindowsForLoadedDocument()
    {
        ToolAvailability availability = GetToolAvailability();
        bool hasActiveProject = availability.HasActiveProject;
        EditorSurface.IsHitTestVisible = hasActiveProject;
        EditorSurface.Opacity = hasActiveProject ? 1 : 0.62;
        EditorOptionsPanel.IsHitTestVisible = hasActiveProject;
        EditorOptionsPanel.Opacity = hasActiveProject ? 1 : 0.62;

        if (ActiveGameProfile is GameProfile profile)
        {
            TypeCombo.ItemsSource = profile.MessageTypes.Items;
            PositionCombo.ItemsSource = profile.MessagePositions.Items;
        }
        else
        {
            TypeCombo.ItemsSource = Array.Empty<MessageTypeItem>();
            PositionCombo.ItemsSource = Array.Empty<MessagePositionItem>();
        }

        _characterProfileWindow?.SetRomSession(CreateRomGlyphSession());
        UpdateRomToolState(availability);
        RefreshMessageByteInspector();

        if (!availability.CanUseMessagePreview)
        {
            _previewWindow?.Close();
        }

        _toolWindows.RefreshForLoadedDocument(availability, ActiveGameProfile, _session.RomData);
    }

    private void UpdateRomToolState()
        => UpdateRomToolState(GetToolAvailability());

    private void UpdateRomToolState(ToolAvailability availability)
    {
        CloseProjectItem.IsEnabled = availability.HasActiveProject;
        SaveMenu.IsEnabled = availability.CanSaveDocument;
        SaveAsRomItem.IsEnabled = availability.CanSaveLoadedRom;
        ExportHeaderItem.IsEnabled = availability.CanExportHeader;
        GlyphProfileMenu.IsEnabled = availability.CanUseGlyphTools;
        GlyphManagerButton.IsEnabled = availability.CanUseGlyphTools;
        RemapGlyphBytesItem.IsEnabled = availability.CanRemapGlyphBytes;
        PreviewToggle.IsEnabled = availability.CanUseMessagePreview;
        FontOrderToolItem.IsEnabled = availability.CanUseFontOrder;
        ImportHeaderIntoRomItem.IsEnabled = availability.CanImportHeaderIntoRom;
        TitleTextToolItem.IsEnabled = availability.CanUseTitleText;
        PromptEditorToolItem.IsEnabled = availability.CanUsePromptEditor;
        TextureManagerToolItem.IsEnabled = availability.CanUseTextureManager;
        TextTextureEditorToolItem.IsEnabled = availability.CanUseTextTextureEditor;
        O2rModMakerToolItem.IsEnabled = availability.CanUseO2rModMaker;
        O2rModMakerToolItem.Text = availability.CanUseO2rModMaker
            && O2rModPortProfileCatalog.TryGetProfile(ActiveGameProfile, _session.RomData?.Profile, out O2rModPortProfile menuO2rProfile)
                ? menuO2rProfile.ToolTitle
                : "O2R Mod Maker";
        TweaksToolItem.IsEnabled = availability.CanUseTweaks;
        MessageByteInspectorToolItem.IsEnabled = CanUseMessageByteInspectorTool();
    }

    private ToolAvailability GetToolAvailability() =>
        ToolAvailabilityService.Build(ActiveGameProfile, _session.Kind, _session.Entries, _session.RomData);

    private bool CanUseCHeaders() => GetToolAvailability().CanUseCHeaders;

    private bool CanUseGlyphTools() => GetToolAvailability().CanUseGlyphTools;

    private bool CanUseTweaksTool() => GetToolAvailability().CanUseTweaks;

    private bool CanUseTitleTextTool() => GetToolAvailability().CanUseTitleText;

    private bool CanUsePromptEditorTool() => GetToolAvailability().CanUsePromptEditor;

    private bool CanUseTextureManagerTool() => GetToolAvailability().CanUseTextureManager;

    private bool CanUseTextTextureEditor() => GetToolAvailability().CanUseTextTextureEditor;

    private bool CanUseO2rModMakerTool() => GetToolAvailability().CanUseO2rModMaker;
}
