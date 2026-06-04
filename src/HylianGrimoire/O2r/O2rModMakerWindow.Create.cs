using HylianGrimoire.Models;
using HylianGrimoire.Textures;
using Microsoft.UI.Xaml;

namespace HylianGrimoire.O2r;

public sealed partial class O2rModMakerWindow
{
    private async void OnCreateMod(object sender, RoutedEventArgs e)
    {
        bool includeTextures = IncludeTexturesCheck.IsChecked == true;
        bool includeText = IncludeTextCheck.IsChecked == true;
        if (!includeTextures && !includeText)
        {
            await ShowErrorAsync("No resource type selected", "Enable text or textures before creating the mod.");
            return;
        }

        List<TextureDefinition> selectedTextures = includeTextures
            ? _textures
                .Where(texture => _selectedResources.Contains(_portProfile.GetTextureResourcePath(texture)))
                .ToList()
            : [];
        List<O2rTextResourceDefinition> selectedTextResources = includeText
            ? _textResources
                .Where(resource => _selectedTextResources.Contains(resource.ResourcePath))
                .ToList()
            : [];
        HashSet<string> selectedExistingTextureResources = includeTextures
            ? _existingEntries.Keys
                .Where(IsTextureResourcePath)
                .Where(_selectedResources.Contains)
                .Except(selectedTextures.Select(_portProfile.GetTextureResourcePath), StringComparer.Ordinal)
                .ToHashSet(StringComparer.Ordinal)
            : [];
        HashSet<string> selectedExistingTextResources = [];

        if (includeTextures && selectedTextures.Count > 0)
        {
            HashSet<string> overriddenPaths = _archiveTextureResources
                .Where(resource => resource.Status == O2rArchiveTextureStatus.DiffersFromRom)
                .Select(resource => resource.ResourcePath)
                .ToHashSet(StringComparer.Ordinal);
            List<string> textureConflicts = selectedTextures
                .Select(_portProfile.GetTextureResourcePath)
                .Where(overriddenPaths.Contains)
                .ToList();

            if (textureConflicts.Count > 0)
            {
                bool? overwrite = await AskOverwriteTextureOverridesAsync(textureConflicts);
                if (overwrite is null)
                {
                    return;
                }

                if (overwrite == false)
                {
                    selectedTextures = selectedTextures
                        .Where(texture => !textureConflicts.Contains(_portProfile.GetTextureResourcePath(texture)))
                        .ToList();
                    foreach (string resourcePath in textureConflicts)
                    {
                        selectedExistingTextureResources.Add(resourcePath);
                    }
                }
            }
        }

        List<MessageEntry> currentEntries = _getCurrentEntries();
        List<O2rTextPayload> textPayloads = BuildTextPayloads(selectedTextResources, currentEntries);

        if (includeText && textPayloads.Count > 0)
        {
            List<string> textConflicts = textPayloads
                .Where(payload => _existingEntries.TryGetValue(payload.ResourcePath, out byte[]? existing)
                    && !payload.Data.SequenceEqual(existing))
                .Select(payload => payload.ResourcePath)
                .ToList();

            if (textConflicts.Count > 0)
            {
                IReadOnlyList<string> textConflictNames = textConflicts
                    .Select(path => _textResources.FirstOrDefault(resource => resource.ResourcePath == path)?.DisplayName ?? path)
                    .ToList();
                bool? overwrite = await AskOverwriteTextResourcesAsync(textConflictNames);
                if (overwrite is null)
                {
                    return;
                }

                if (overwrite == false)
                {
                    textPayloads = textPayloads
                        .Where(payload => !textConflicts.Contains(payload.ResourcePath))
                        .ToList();
                    foreach (string resourcePath in textConflicts)
                    {
                        selectedExistingTextResources.Add(resourcePath);
                    }
                }
            }
        }

        if (selectedTextures.Count == 0 && selectedExistingTextureResources.Count == 0 && selectedTextResources.Count == 0 && selectedExistingTextResources.Count == 0)
        {
            await ShowErrorAsync("No resources selected", "Select at least one texture or text resource before creating the mod.");
            return;
        }

        string? path = await PickSaveO2rAsync();
        if (path is null)
        {
            return;
        }

        try
        {
            var progress = new Progress<int>(UpdateProgress);
            using IDisposable busy = ShowProgress("Creating .o2r");
            byte[]? rom = _romData?.DecompressedRom;
            await Task.Run(() => CreateO2r(
                path,
                rom,
                selectedTextures,
                textPayloads,
                selectedExistingTextureResources,
                selectedExistingTextResources,
                _existingEntries,
                _portProfile,
                replaceTextures: includeTextures,
                replaceText: includeText,
                progress));

            _existingModPath = path;
            _hasWorkspaceChanges = false;
            _existingEntries = O2rArchiveWriter.ReadEntries(path);
            _archiveTextureResources = BuildArchiveTextureResources(_existingEntries, _textures, _romData?.DecompressedRom);
            PopulateTextureTree();
            UpdateWorkspaceSummary();
            StatusText.Text = $"Created mod with {_selectedResources.Count} textures and {textPayloads.Count} text resources.";
            _onChanged($"Created {_portProfile.DisplayName} mod.");
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Failed to create .o2r", ex.Message);
        }
    }

    private static void CreateO2r(
        string outputPath,
        byte[]? rom,
        IReadOnlyList<TextureDefinition> textures,
        IReadOnlyList<O2rTextPayload> textPayloads,
        IReadOnlySet<string> selectedExistingTextureResources,
        IReadOnlySet<string> selectedExistingTextResources,
        IReadOnlyDictionary<string, byte[]> existingEntries,
        O2rModPortProfile portProfile,
        bool replaceTextures,
        bool replaceText,
        IProgress<int> progress)
    {
        var archive = new O2rArchiveWriter();
        foreach ((string resourcePath, byte[] data) in existingEntries)
        {
            bool isText = portProfile.IsTextResourcePath(resourcePath);
            bool isTexture = !isText;
            if (replaceText && isText)
            {
                if (!selectedExistingTextResources.Contains(resourcePath))
                {
                    continue;
                }
            }
            else if (replaceTextures && isTexture)
            {
                if (!selectedExistingTextureResources.Contains(resourcePath))
                {
                    continue;
                }
            }

            archive.Add(resourcePath, data);
        }

        int total = textures.Count + textPayloads.Count;
        int completed = 0;
        for (int i = 0; i < textures.Count; i++)
        {
            if (rom is null)
            {
                throw new InvalidOperationException("A ROM is required to create new texture resources.");
            }

            TextureDefinition texture = textures[i];
            byte[] raw = TextureRomService.ReadRaw(rom, texture);
            archive.Add(portProfile.GetTextureResourcePath(texture), O2rResourcePacker.PackTexture(texture, raw));
            progress.Report(GetPercent(++completed, total));
        }

        foreach (O2rTextPayload textPayload in textPayloads)
        {
            archive.Add(textPayload.ResourcePath, textPayload.Data);
            progress.Report(GetPercent(++completed, total));
        }

        archive.Write(outputPath);
    }
}
