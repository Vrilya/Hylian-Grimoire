using HylianGrimoire.Games;
using HylianGrimoire.Models;
using Microsoft.UI.Xaml;

namespace HylianGrimoire;

public sealed partial class MainWindow
{
    private async void OnNewProject(object sender, RoutedEventArgs e)
    {
        if (!await ConfirmDiscardCurrentProjectAsync())
        {
            return;
        }

        GameProfile? profile = await PromptForNewProjectAsync();
        if (profile is null)
        {
            return;
        }

        ResetProjectSession(closeAuxiliaryWindows: true);
        _session.UseProject(profile);
        SetActiveCharacterProfileGame(profile.Kind);
        RefreshDocumentShell();
        SetStatus($"Created new {profile.DisplayName} project.");
    }

    private async void OnLoadDocument(object sender, RoutedEventArgs e)
    {
        if (!await ConfirmDiscardCurrentProjectAsync())
        {
            return;
        }

        string? path = await PickOpenFileAsync([".z64", ".bin", ".h"]);
        if (path is null)
        {
            return;
        }

        string extension = Path.GetExtension(path).ToLowerInvariant();
        switch (extension)
        {
            case ".z64":
                await LoadRomDocumentAsync(path);
                break;
            case ".bin":
                string? tblPath = await PickOpenFileAsync([".tbl"]);
                if (tblPath is not null)
                {
                    await LoadTableFilesDocumentAsync(path, tblPath);
                }

                break;
            case ".h":
                await LoadHeaderDocumentAsync(path);
                break;
            default:
                await ShowInfoAsync("Unsupported file", "Choose a .z64, .bin, or .h file.");
                break;
        }
    }

    private async void OnCloseProject(object sender, RoutedEventArgs e)
    {
        if (!HasActiveProject)
        {
            return;
        }

        if (!await ConfirmDiscardCurrentProjectAsync())
        {
            return;
        }

        ResetProjectSession(closeAuxiliaryWindows: true);
    }

    private async void OnSaveFiles(object sender, RoutedEventArgs e)
    {
        if (_session.Entries.Count == 0)
        {
            await ShowInfoAsync("Nothing to save", "No messages loaded.");
            return;
        }

        CommitCurrent();

        _ = _session.Kind switch
        {
            DocumentKind.DataFiles when _session.TablePath is not null && _session.BinaryPath is not null => await WriteFilesAsync(_session.TablePath, _session.BinaryPath) is not null,
            DocumentKind.Header when _session.HeaderPath is not null => await WriteHeaderAsync(_session.HeaderPath),
            DocumentKind.Rom when _session.RomPath is not null && _session.RomData is not null => await WriteRomAsync(_session.RomPath, _session.RomData),
            _ => await SaveCurrentFilesAsAsync(),
        };
    }

    private async void OnSaveAsFiles(object sender, RoutedEventArgs e)
    {
        if (_session.Entries.Count == 0)
        {
            await ShowInfoAsync("Nothing to save", "No messages loaded.");
            return;
        }

        CommitCurrent();
        _ = await SaveCurrentFilesAsAsync();
    }

    private async void OnSaveAsRom(object sender, RoutedEventArgs e)
    {
        if (_session.Entries.Count == 0)
        {
            await ShowInfoAsync("Nothing to save", "No messages loaded.");
            return;
        }

        if (_session.RomData is null)
        {
            await ShowInfoAsync("No ROM loaded", "Load a ROM before saving as ROM.");
            return;
        }

        CommitCurrent();

        string suggestedFileName = _session.RomPath is null
            ? "Hylian Grimoire.z64"
            : Path.GetFileName(_session.RomPath);
        string? path = await PickSaveFileAsync(".z64", suggestedFileName);
        if (path is null)
        {
            return;
        }

        bool? compress = await PromptForRomCompressionAsync();
        if (compress is null)
        {
            return;
        }

        if (await WriteRomAsync(path, _session.RomData, compress))
        {
            _session.MarkSavedAsRom(path);
            UpdateWindowTitle();
            UpdateLanguageMenuState();
            UpdateDiagnosticsContext();
        }
    }
}
