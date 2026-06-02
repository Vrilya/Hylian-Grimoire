using Microsoft.UI.Xaml;
using HylianGrimoire.Models;
using HylianGrimoire.PromptEditor;
using HylianGrimoire.Soh;
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

    private void OnOpenSohModMaker(object sender, RoutedEventArgs e)
    {
        if (!CanUseSohModMakerTool())
        {
            return;
        }

        try
        {
            if (_sohModMakerWindow is null)
            {
                var window = new SohModMakerWindow(
                    _session.RomData,
                    GetCurrentEntriesForSohModMaker,
                    GetCurrentTextLanguagesForSohModMaker,
                    CreateCurrentEncodingProfile(),
                    OnSohModMakerChanged);
                window.Closed += (_, _) => _sohModMakerWindow = null;
                _sohModMakerWindow = window;
            }
            else
            {
                _sohModMakerWindow.SetRomData(_session.RomData, CreateCurrentEncodingProfile());
            }

            _sohModMakerWindow.Activate();
        }
        catch (Exception ex)
        {
            _sohModMakerWindow = null;
            _ = ShowErrorAsync("Failed to open SoH Mod Maker", ex.Message);
        }
    }

    private List<MessageEntry> GetCurrentEntriesForSohModMaker()
    {
        CommitCurrent();
        return _session.Entries.ToList();
    }

    private IReadOnlyDictionary<int, List<MessageEntry>> GetCurrentTextLanguagesForSohModMaker()
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

    private void OnSohModMakerChanged(string status)
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

    private void CloseSohModMakerWindow()
    {
        _sohModMakerWindow?.Close();
        _sohModMakerWindow = null;
    }
}
