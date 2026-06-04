using Microsoft.UI.Xaml;
using HylianGrimoire.Models;
using HylianGrimoire.O2r;
using HylianGrimoire.PromptEditor;
using HylianGrimoire.Textures;

namespace HylianGrimoire;

public sealed partial class MainWindow
{
    private void OnOpenTweaks(object sender, RoutedEventArgs e)
    {
        if (!CanUseTweaksTool())
        {
            return;
        }

        if (_tweaksWindow is null)
        {
            _tweaksWindow = new Tweaks.TweaksWindow(_session.RomData, OnRomTweakChanged);
            _tweaksWindow.Closed += (_, _) => _tweaksWindow = null;
        }
        else
        {
            _tweaksWindow.SetRomData(_session.RomData);
        }

        _tweaksWindow.Activate();
    }

    private void OnOpenTitleText(object sender, RoutedEventArgs e)
    {
        if (!CanUseTitleTextTool())
        {
            return;
        }

        if (_titleTextWindow is null)
        {
            _titleTextWindow = new TitleText.TitleTextWindow(_session.RomData, _session.RomData?.ActiveMessageBankIndex ?? 0, OnTitleTextChanged);
            _titleTextWindow.Closed += (_, _) => _titleTextWindow = null;
        }
        else
        {
            _titleTextWindow.SetRomData(_session.RomData, _session.RomData?.ActiveMessageBankIndex ?? 0);
        }

        _titleTextWindow.Activate();
    }

    private void OnOpenPromptEditor(object sender, RoutedEventArgs e)
    {
        if (!CanUsePromptEditorTool())
        {
            return;
        }

        if (_promptEditorWindow is null)
        {
            _promptEditorWindow = new PromptEditorWindow(_session.RomData, OnPromptEditorChanged);
            _promptEditorWindow.Closed += (_, _) => _promptEditorWindow = null;
        }
        else
        {
            _promptEditorWindow.SetRomData(_session.RomData);
        }

        _promptEditorWindow.Activate();
    }

    private void OnOpenTextureManager(object sender, RoutedEventArgs e)
    {
        if (!CanUseTextureManagerTool())
        {
            return;
        }

        if (_textureManagerWindow is null)
        {
            _textureManagerWindow = new TextureManagerWindow(_session.RomData, OnTextureManagerChanged);
            _textureManagerWindow.Closed += (_, _) => _textureManagerWindow = null;
        }
        else
        {
            _textureManagerWindow.SetRomData(_session.RomData);
        }

        _textureManagerWindow.Activate();
    }

    private void OnOpenO2rModMaker(object sender, RoutedEventArgs e)
    {
        if (!CanUseO2rModMakerTool())
        {
            return;
        }

        try
        {
            O2rModPortProfile portProfile = O2rModPortProfileCatalog.GetProfile(CurrentGameProfile, _session.RomData?.Profile);
            if (_o2rModMakerWindow is null)
            {
                var window = new O2rModMakerWindow(
                    portProfile,
                    _session.RomData,
                    GetCurrentEntriesForO2rModMaker,
                    GetCurrentTextLanguagesForO2rModMaker,
                    CreateCurrentEncodingProfile(),
                    OnO2rModMakerChanged);
                window.Closed += (_, _) => _o2rModMakerWindow = null;
                _o2rModMakerWindow = window;
            }
            else
            {
                _o2rModMakerWindow.SetContext(portProfile, _session.RomData, CreateCurrentEncodingProfile());
            }

            _o2rModMakerWindow.Activate();
        }
        catch (Exception ex)
        {
            _o2rModMakerWindow = null;
            _ = ShowErrorAsync("Failed to open O2R Mod Maker", ex.Message);
        }
    }

    private List<MessageEntry> GetCurrentEntriesForO2rModMaker()
    {
        CommitCurrent();
        return _session.Entries.ToList();
    }

    private IReadOnlyDictionary<int, List<MessageEntry>> GetCurrentTextLanguagesForO2rModMaker()
    {
        CommitCurrent();

        if (_session.Kind == DocumentKind.Header && _session.HeaderLanguageEntries is not null)
        {
            CommitHeaderLanguageChanges();
            return _session.HeaderLanguageEntries.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.ToList());
        }

        return new Dictionary<int, List<MessageEntry>>
        {
            [0] = _session.Entries.ToList(),
        };
    }

    private void OnO2rModMakerChanged(string status)
    {
        SetStatus(status);
    }

    private void OnTextureManagerChanged(string status)
    {
        MarkRomBankDirty();
        SetStatus(status);
    }

    private void OnTitleTextChanged(string status)
    {
        MarkRomBankDirty();
        SetStatus(status);
    }

    private void OnPromptEditorChanged(string status)
    {
        MarkRomBankDirty();
        SetStatus(status);
    }

    private void OnRomTweakChanged(string status)
    {
        MarkRomBankDirty();
        _promptEditorWindow?.SetRomData(_session.RomData);
        SetStatus(status);
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

    private void CloseO2rModMakerWindow()
    {
        _o2rModMakerWindow?.Close();
        _o2rModMakerWindow = null;
    }
}
