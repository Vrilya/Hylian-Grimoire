using Microsoft.UI.Xaml;
using HylianGrimoire.Codecs;
using HylianGrimoire.Glyphs;
using HylianGrimoire.Models;
using HylianGrimoire.Rom;
using HylianGrimoire.Services;

namespace HylianGrimoire;

public sealed partial class MainWindow
{
    private void OnOpenCharacterProfiles(object sender, RoutedEventArgs e)
    {
        if (!CanUseGlyphTools())
        {
            return;
        }

        if (_characterProfileWindow is null)
        {
            _characterProfileWindow = new Glyphs.CharacterProfileWindow(_characterProfileRuntime, CreateRomGlyphSession());
            _characterProfileWindow.GlyphDataChanged += OnGlyphWindowDataChanged;
            _characterProfileWindow.Closed += (_, _) => _characterProfileWindow = null;
        }
        else
        {
            _characterProfileWindow.SetRomSession(CreateRomGlyphSession());
        }

        _characterProfileWindow.Activate();
    }

    private async void OnOpenGlyphRemap(object sender, RoutedEventArgs e)
    {
        if (!CanUseGlyphTools() || _session.Entries.Count == 0)
        {
            return;
        }

        CommitCurrent();
        MessageEncodingProfile encodingProfile = CreateCurrentEncodingProfile();
        IGlyphSource glyphSource = _characterProfileRuntime.CreateGlyphSource(CurrentGameProfile, _session.RomData);

        _glyphRemapWindow?.Close();
        _glyphRemapWindow = new GlyphRemapWindow(
            _session.Entries,
            glyphSource,
            encodingProfile,
            ApplyGlyphRemap);
        _glyphRemapWindow.Closed += (_, _) => _glyphRemapWindow = null;
        _glyphRemapWindow.Activate();
    }

    private int ApplyGlyphRemap(byte source, byte target)
    {
        CommitCurrent();
        int replacements = MessageGlyphRemapper.Replace(_session.Entries, source, target, CreateCurrentEncodingProfile());
        if (replacements == 0)
        {
            SetStatus($"No 0x{source:X2} glyphs found in the loaded script.");
            return 0;
        }

        MarkDirty();
        PopulateList();
        if (_session.CurrentIndex >= 0 && _session.CurrentIndex < _session.Entries.Count)
        {
            ShowEntry(_session.CurrentIndex);
        }

        UpdatePreview();
        SetStatus($"Remapped {replacements} glyphs from 0x{source:X2} to 0x{target:X2}.");
        return replacements;
    }

    private void OnGlyphWindowDataChanged(object? sender, EventArgs e)
    {
        ApplyRomMutation();
        UpdatePreview();
    }

    private void RemapEditorTextForCharacterProfileChange(CharacterProfileSelectionChangedEventArgs args)
    {
        if (!CanUseGlyphTools())
        {
            _activeCharacterProfileName = args.SelectedProfileName;
            return;
        }

        if (string.Equals(_activeCharacterProfileName, args.SelectedProfileName, StringComparison.Ordinal))
        {
            return;
        }

        CommitCurrent();
        bool wasDirty = _session.HasUnsavedChanges;
        foreach (MessageEntry entry in _session.Entries)
        {
            entry.Text = _characterProfileRuntime.RemapEditorText(entry.Text, args);
        }

        _activeCharacterProfileName = args.SelectedProfileName;
        PopulateList();
        if (_session.CurrentIndex >= 0 && _session.CurrentIndex < _session.Entries.Count)
        {
            ShowEntry(_session.CurrentIndex);
        }

        if (wasDirty)
        {
            _session.ForceDirty();
        }
        else
        {
            MarkClean();
        }
    }

    private RomGlyphEditorSession? CreateRomGlyphSession()
    {
        RomMessageData? romData = _session.RomData;
        return GetToolAvailability().CanUseRomGlyphEditor && romData is not null
            ? new RomGlyphEditorSession(
                romData.DecompressedRom,
                romData.FontResources,
                romData.Profile.FontBaseline,
                romData.Profile.Game)
            : null;
    }

}
