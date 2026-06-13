using HylianGrimoire.Codecs;
using HylianGrimoire.Games;
using HylianGrimoire.Models;
using HylianGrimoire.O2r;
using HylianGrimoire.PromptEditor;
using HylianGrimoire.Rom;
using HylianGrimoire.Services;
using HylianGrimoire.TextTextures;
using HylianGrimoire.Textures;
using HylianGrimoire.TitleText;
using HylianGrimoire.Tweaks;

namespace HylianGrimoire.ToolWindows;

internal sealed class ToolWindowCoordinator
{
    private readonly Func<RomMessageData?> _getRomData;
    private readonly Func<MessageEncodingProfile> _createCurrentEncodingProfile;
    private readonly Func<List<MessageEntry>> _getCurrentEntriesForO2rModMaker;
    private readonly Func<IReadOnlyDictionary<int, List<MessageEntry>>> _getCurrentTextLanguagesForO2rModMaker;
    private readonly Action<string> _onO2rModMakerChanged;
    private readonly Action<TextureManagerChange> _onTextureManagerChanged;
    private readonly Action<string> _onTitleTextChanged;
    private readonly Action<string> _onPromptEditorChanged;
    private readonly Action<string> _onRomTweakChanged;
    private readonly Action<string> _onO2rModMakerOpenFailed;

    private TweaksWindow? _tweaksWindow;
    private TitleTextWindow? _titleTextWindow;
    private PromptEditorWindow? _promptEditorWindow;
    private TextureManagerWindow? _textureManagerWindow;
    private TextTextureEditorWindow? _textTextureEditorWindow;
    private O2rModMakerWindow? _o2rModMakerWindow;

    public ToolWindowCoordinator(
        Func<RomMessageData?> getRomData,
        Func<MessageEncodingProfile> createCurrentEncodingProfile,
        Func<List<MessageEntry>> getCurrentEntriesForO2rModMaker,
        Func<IReadOnlyDictionary<int, List<MessageEntry>>> getCurrentTextLanguagesForO2rModMaker,
        Action<string> onO2rModMakerChanged,
        Action<TextureManagerChange> onTextureManagerChanged,
        Action<string> onTitleTextChanged,
        Action<string> onPromptEditorChanged,
        Action<string> onRomTweakChanged,
        Action<string> onO2rModMakerOpenFailed)
    {
        _getRomData = getRomData;
        _createCurrentEncodingProfile = createCurrentEncodingProfile;
        _getCurrentEntriesForO2rModMaker = getCurrentEntriesForO2rModMaker;
        _getCurrentTextLanguagesForO2rModMaker = getCurrentTextLanguagesForO2rModMaker;
        _onO2rModMakerChanged = onO2rModMakerChanged;
        _onTextureManagerChanged = onTextureManagerChanged;
        _onTitleTextChanged = onTitleTextChanged;
        _onPromptEditorChanged = onPromptEditorChanged;
        _onRomTweakChanged = onRomTweakChanged;
        _onO2rModMakerOpenFailed = onO2rModMakerOpenFailed;
    }

    public void RefreshForLoadedDocument(
        ToolAvailability availability,
        GameProfile? activeGameProfile,
        RomMessageData? romData)
    {
        if (availability.CanUseTweaks)
        {
            _tweaksWindow?.SetRomData(romData);
        }
        else
        {
            CloseTweaksWindow();
        }

        if (availability.CanUseTitleText)
        {
            _titleTextWindow?.SetRomData(romData, romData?.ActiveMessageBankIndex ?? 0);
        }
        else
        {
            CloseTitleTextWindow();
        }

        if (availability.CanUsePromptEditor)
        {
            _promptEditorWindow?.SetRomData(romData);
        }
        else
        {
            ClosePromptEditorWindow();
        }

        if (availability.CanUseTextureManager)
        {
            _textureManagerWindow?.SetRomData(romData);
        }
        else
        {
            CloseTextureManagerWindow();
        }

        if (availability.CanUseTextTextureEditor)
        {
            _textTextureEditorWindow?.SetRomData(romData);
        }
        else
        {
            CloseTextTextureEditorWindow();
        }

        if (availability.CanUseO2rModMaker
            && O2rModPortProfileCatalog.TryGetProfile(activeGameProfile, romData?.Profile, out O2rModPortProfile o2rProfile))
        {
            _o2rModMakerWindow?.SetContext(o2rProfile, romData, _createCurrentEncodingProfile());
        }
        else
        {
            CloseO2rModMakerWindow();
        }
    }

    public void OpenTweaks(RomMessageData? romData)
    {
        if (_tweaksWindow is null)
        {
            _tweaksWindow = new TweaksWindow(romData, OnRomTweakChanged);
            _tweaksWindow.Closed += (_, _) => _tweaksWindow = null;
        }
        else
        {
            _tweaksWindow.SetRomData(romData);
        }

        _tweaksWindow.Activate();
    }

    public void OpenTitleText(RomMessageData? romData)
    {
        int languageIndex = romData?.ActiveMessageBankIndex ?? 0;
        if (_titleTextWindow is null)
        {
            _titleTextWindow = new TitleTextWindow(romData, languageIndex, _onTitleTextChanged);
            _titleTextWindow.Closed += (_, _) => _titleTextWindow = null;
        }
        else
        {
            _titleTextWindow.SetRomData(romData, languageIndex);
        }

        _titleTextWindow.Activate();
    }

    public void OpenPromptEditor(RomMessageData? romData)
    {
        if (_promptEditorWindow is null)
        {
            _promptEditorWindow = new PromptEditorWindow(romData, _onPromptEditorChanged);
            _promptEditorWindow.Closed += (_, _) => _promptEditorWindow = null;
        }
        else
        {
            _promptEditorWindow.SetRomData(romData);
        }

        _promptEditorWindow.Activate();
    }

    public void OpenTextureManager(RomMessageData? romData)
    {
        if (_textureManagerWindow is null)
        {
            _textureManagerWindow = new TextureManagerWindow(romData, _onTextureManagerChanged);
            _textureManagerWindow.Closed += (_, _) => _textureManagerWindow = null;
        }
        else
        {
            _textureManagerWindow.SetRomData(romData);
        }

        _textureManagerWindow.Activate();
    }

    public void OpenTextTextureEditor(RomMessageData? romData)
    {
        if (_textTextureEditorWindow is null)
        {
            _textTextureEditorWindow = new TextTextureEditorWindow(romData, _onTextureManagerChanged);
            _textTextureEditorWindow.Closed += (_, _) => _textTextureEditorWindow = null;
        }
        else
        {
            _textTextureEditorWindow.SetRomData(romData);
        }

        _textTextureEditorWindow.Activate();
    }

    public void OpenO2rModMaker(GameProfile currentGameProfile, RomMessageData? romData)
    {
        try
        {
            O2rModPortProfile portProfile = O2rModPortProfileCatalog.GetProfile(currentGameProfile, romData?.Profile);
            MessageEncodingProfile encodingProfile = _createCurrentEncodingProfile();
            if (_o2rModMakerWindow is null)
            {
                var window = new O2rModMakerWindow(
                    portProfile,
                    romData,
                    _getCurrentEntriesForO2rModMaker,
                    _getCurrentTextLanguagesForO2rModMaker,
                    encodingProfile,
                    _onO2rModMakerChanged);
                window.Closed += (_, _) => _o2rModMakerWindow = null;
                _o2rModMakerWindow = window;
            }
            else
            {
                _o2rModMakerWindow.SetContext(portProfile, romData, encodingProfile);
            }

            _o2rModMakerWindow.Activate();
        }
        catch (Exception ex)
        {
            _o2rModMakerWindow = null;
            _onO2rModMakerOpenFailed(UiOperationExceptionHandler.GetDisplayMessage("Failed to open O2R Mod Maker", ex));
        }
    }

    public void CloseAll()
    {
        CloseTweaksWindow();
        CloseTitleTextWindow();
        ClosePromptEditorWindow();
        CloseTextureManagerWindow();
        CloseTextTextureEditorWindow();
        CloseO2rModMakerWindow();
    }

    private void OnRomTweakChanged(string status)
    {
        _onRomTweakChanged(status);
        _promptEditorWindow?.SetRomData(_getRomData());
    }

    private void CloseTweaksWindow()
    {
        _tweaksWindow?.Close();
        _tweaksWindow = null;
    }

    private void CloseTitleTextWindow()
    {
        _titleTextWindow?.Close();
        _titleTextWindow = null;
    }

    private void ClosePromptEditorWindow()
    {
        _promptEditorWindow?.Close();
        _promptEditorWindow = null;
    }

    private void CloseTextureManagerWindow()
    {
        _textureManagerWindow?.Close();
        _textureManagerWindow = null;
    }

    private void CloseTextTextureEditorWindow()
    {
        _textTextureEditorWindow?.Close();
        _textTextureEditorWindow = null;
    }

    private void CloseO2rModMakerWindow()
    {
        _o2rModMakerWindow?.Close();
        _o2rModMakerWindow = null;
    }
}
