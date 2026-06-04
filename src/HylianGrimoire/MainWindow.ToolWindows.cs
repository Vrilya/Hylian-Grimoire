using Microsoft.UI.Xaml;
using HylianGrimoire.Models;
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

        _toolWindows.OpenTweaks(_session.RomData);
    }

    private void OnOpenTitleText(object sender, RoutedEventArgs e)
    {
        if (!CanUseTitleTextTool())
        {
            return;
        }

        _toolWindows.OpenTitleText(_session.RomData);
    }

    private void OnOpenPromptEditor(object sender, RoutedEventArgs e)
    {
        if (!CanUsePromptEditorTool())
        {
            return;
        }

        _toolWindows.OpenPromptEditor(_session.RomData);
    }

    private void OnOpenTextureManager(object sender, RoutedEventArgs e)
    {
        if (!CanUseTextureManagerTool())
        {
            return;
        }

        _toolWindows.OpenTextureManager(_session.RomData);
    }

    private void OnOpenO2rModMaker(object sender, RoutedEventArgs e)
    {
        if (!CanUseO2rModMakerTool())
        {
            return;
        }

        _toolWindows.OpenO2rModMaker(CurrentGameProfile, _session.RomData);
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

    private void OnTextureManagerChanged(TextureManagerChange change)
    {
        if (change.MutatedRom)
        {
            ApplyRomMutation(change.Status);
            return;
        }

        SetStatus(change.Status);
    }

    private void OnTitleTextChanged(string status)
        => ApplyRomMutation(status);

    private void OnPromptEditorChanged(string status)
        => ApplyRomMutation(status);

    private void OnRomTweakChanged(string status)
        => ApplyRomMutation(status);

    private void OnO2rModMakerOpenFailed(string message)
    {
        _ = ShowErrorAsync("Failed to open O2R Mod Maker", message);
    }
}
