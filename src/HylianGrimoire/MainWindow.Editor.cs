using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using HylianGrimoire.Codecs;
using HylianGrimoire.Glyphs;
using HylianGrimoire.Models;
using HylianGrimoire.PromptEditor;
using HylianGrimoire.Rom;
using HylianGrimoire.Services;
using HylianGrimoire.TitleText;

namespace HylianGrimoire;

public sealed partial class MainWindow
{
    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating || _currentIdx < 0)
        {
            return;
        }

        string editorText = GetEditorText();
        string raw = MessageTextSyntax.FromDisplay(editorText);
        if (!string.Equals(_entries[_currentIdx].Text, raw, StringComparison.Ordinal))
        {
            _entries[_currentIdx].Text = raw;
            MarkDirty();
        }

        string normalized = MessageTextSyntax.ToDisplay(raw);
        if (normalized != editorText)
        {
            _updating = true;
            try
            {
                int caret = TextEditor.SelectionStart;
                TextEditor.Text = normalized;
                TextEditor.SelectionStart = Math.Min(caret, TextEditor.Text.Length);
            }
            finally
            {
                _updating = false;
            }
        }

        RefreshListItem(_currentIdx);
        UpdatePreview();
    }

    private void OnTypeChange(object sender, SelectionChangedEventArgs e)
    {
        if (_updating || _currentIdx < 0)
        {
            return;
        }

        if (TypeCombo.SelectedItem is MessageTypeItem item)
        {
            if (_entries[_currentIdx].Type == item.Value)
            {
                return;
            }

            _entries[_currentIdx].Type = item.Value;
            MarkDirty();
            UpdatePreview();
        }
    }

    private void OnPosChange(object sender, SelectionChangedEventArgs e)
    {
        if (_updating || _currentIdx < 0)
        {
            return;
        }

        if (_entries[_currentIdx].Position != PositionCombo.SelectedIndex)
        {
            _entries[_currentIdx].Position = PositionCombo.SelectedIndex;
            MarkDirty();
        }
    }

    private void OnWordWrapToggled(object sender, RoutedEventArgs e)
    {
        bool wrap = WordWrapSwitch.IsOn;
        TextEditor.TextWrapping = wrap ? TextWrapping.Wrap : TextWrapping.NoWrap;
        ScrollViewer.SetHorizontalScrollBarVisibility(
            TextEditor,
            wrap ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto);
    }

    private void OnEditorFontSizeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (EditorFontSizeBox.SelectedItem is ComboBoxItem item
            && item.Content is string text
            && double.TryParse(text, out double fontSize))
        {
            TextEditor.FontSize = fontSize;
        }
    }

    private void OnOpenCharacterProfiles(object sender, RoutedEventArgs e)
    {
        if (_characterProfileWindow is null)
        {
            _characterProfileWindow = new Glyphs.CharacterProfileWindow(CreateRomGlyphSession());
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
        if (_entries.Count == 0)
        {
            return;
        }

        CommitCurrent();
        IOotGlyphSource glyphSource = _romData is null
            ? OotGlyphSources.ActiveProfile
            : new RomGlyphSource(_romData.DecompressedRom, _romData.FontResources);

        _glyphRemapWindow?.Close();
        _glyphRemapWindow = new GlyphRemapWindow(_entries, glyphSource, ApplyGlyphRemap);
        _glyphRemapWindow.Closed += (_, _) => _glyphRemapWindow = null;
        _glyphRemapWindow.Activate();
    }

    private async void OnOpenFontOrder(object sender, RoutedEventArgs e)
    {
        MessageEntry? entry = FontOrderService.FindEntry(_entries, _romData);
        if (entry is null)
        {
            return;
        }

        var editor = new TextBox
        {
            AcceptsReturn = true,
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Cascadia Mono"),
            MinWidth = 520,
            MinHeight = 220,
            TextWrapping = TextWrapping.NoWrap,
        };
        editor.Text = FontOrderService.GetEditorText(entry, _romData);
        ScrollViewer.SetHorizontalScrollBarVisibility(editor, ScrollBarVisibility.Auto);
        ScrollViewer.SetVerticalScrollBarVisibility(editor, ScrollBarVisibility.Auto);

        var resetStandardButton = new Button
        {
            Content = "Reset to standard",
        };
        resetStandardButton.Click += (_, _) =>
        {
            editor.Text = FontOrderCodec.GetStandardEditorText();
        };

        var resetLoadedButton = new Button
        {
            Content = "Reset to loaded",
        };
        resetLoadedButton.Click += (_, _) =>
        {
            editor.Text = FontOrderService.GetLoadedEditorText(entry, _romData);
        };

        var resetButtons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
        };
        resetButtons.Children.Add(resetLoadedButton);
        resetButtons.Children.Add(resetStandardButton);

        var content = new StackPanel
        {
            Spacing = 12,
        };
        content.Children.Add(editor);
        content.Children.Add(resetButtons);

        var dialog = new ContentDialog
        {
            Title = "Font order (0xFFFC)",
            Content = content,
            PrimaryButtonText = "Apply",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = Content.XamlRoot,
        };

        ContentDialogResult result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ApplyFontOrderTextAsync(entry, editor.Text);
        }
    }

    private void OnOpenTweaks(object sender, RoutedEventArgs e)
    {
        if (!CanUseTweaksTool())
        {
            return;
        }

        if (_tweaksWindow is null)
        {
            _tweaksWindow = new Tweaks.TweaksWindow(_romData, OnRomTweakChanged);
            _tweaksWindow.Closed += (_, _) => _tweaksWindow = null;
        }
        else
        {
            _tweaksWindow.SetRomData(_romData);
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
            _titleTextWindow = new TitleText.TitleTextWindow(_romData, OnTitleTextChanged);
            _titleTextWindow.Closed += (_, _) => _titleTextWindow = null;
        }
        else
        {
            _titleTextWindow.SetRomData(_romData);
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
            _promptEditorWindow = new PromptEditorWindow(_romData, OnPromptEditorChanged);
            _promptEditorWindow.Closed += (_, _) => _promptEditorWindow = null;
        }
        else
        {
            _promptEditorWindow.SetRomData(_romData);
        }

        _promptEditorWindow.Activate();
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
        SetStatus(status);
    }

    private int ApplyGlyphRemap(byte source, byte target)
    {
        CommitCurrent();
        int replacements = MessageGlyphRemapper.Replace(_entries, source, target);
        if (replacements == 0)
        {
            SetStatus($"No 0x{source:X2} glyphs found in the loaded script.");
            return 0;
        }

        MarkDirty();
        PopulateList();
        if (_currentIdx >= 0 && _currentIdx < _entries.Count)
        {
            ShowEntry(_currentIdx);
        }

        UpdatePreview();
        SetStatus($"Remapped {replacements} glyphs from 0x{source:X2} to 0x{target:X2}.");
        return replacements;
    }

    private void OnGlyphWindowDataChanged(object? sender, EventArgs e)
    {
        MarkDirty();
        UpdatePreview();
    }

    private void RemapEditorTextForCharacterProfileChange(CharacterProfileSelectionChangedEventArgs args)
    {
        if (string.Equals(_activeCharacterProfileName, args.SelectedProfileName, StringComparison.Ordinal))
        {
            return;
        }

        CommitCurrent();
        bool wasDirty = _hasUnsavedChanges;
        foreach (MessageEntry entry in _entries)
        {
            entry.Text = CharacterProfileStore.Current.RemapEditorText(entry.Text, args.PreviousProfile, args.SelectedProfileName);
        }

        _activeCharacterProfileName = args.SelectedProfileName;
        PopulateList();
        if (_currentIdx >= 0 && _currentIdx < _entries.Count)
        {
            ShowEntry(_currentIdx);
        }

        if (wasDirty)
        {
            _hasUnsavedChanges = true;
        }
        else
        {
            MarkClean();
        }
    }

    private RomGlyphEditorSession? CreateRomGlyphSession()
    {
        return _romData is null
            ? null
            : new RomGlyphEditorSession(
                _romData.DecompressedRom,
                _romData.FontResources,
                _romData.Profile.FontBaseline);
    }

    private void RefreshAuxiliaryWindowsForLoadedDocument()
    {
        _characterProfileWindow?.SetRomSession(CreateRomGlyphSession());
        UpdateRomToolState();

        if (CanUseTweaksTool())
        {
            _tweaksWindow?.SetRomData(_romData);
        }
        else
        {
            CloseTweaksWindow();
        }

        if (CanUseTitleTextTool())
        {
            _titleTextWindow?.SetRomData(_romData);
        }
        else
        {
            CloseTitleTextWindow();
        }

        if (CanUsePromptEditorTool())
        {
            _promptEditorWindow?.SetRomData(_romData);
        }
        else
        {
            ClosePromptEditorWindow();
        }
    }

    private void UpdateRomToolState()
    {
        SaveMenu.IsEnabled = _documentKind != DocumentKind.None || _entries.Count > 0;
        SaveAsRomItem.IsEnabled = _romData is not null;
        RemapGlyphBytesItem.IsEnabled = _entries.Count > 0;
        FontOrderToolItem.IsEnabled = CanUseFontOrderTool();
        ImportHeaderIntoRomItem.IsEnabled = _romData is not null;
        TitleTextToolItem.IsEnabled = CanUseTitleTextTool();
        PromptEditorToolItem.IsEnabled = CanUsePromptEditorTool();
        TweaksToolItem.IsEnabled = CanUseTweaksTool();
    }

    private bool CanUseTweaksTool() => _romData?.Profile.IsRetail == true;

    private bool CanUseFontOrderTool()
        => FontOrderService.CanEdit(_entries, _romData);

    private async Task ApplyFontOrderTextAsync(MessageEntry entry, string editorText)
    {
        FontOrderUpdateResult result = FontOrderService.ApplyEditorText(entry, editorText);
        if (result.ErrorMessage is not null)
        {
            await ShowErrorAsync("Invalid font order", result.ErrorMessage);
            return;
        }

        if (!result.Changed)
        {
            return;
        }

        MarkDirty();
        PopulateList();
        SetStatus("Updated font order (0xFFFC).");
    }

    private bool CanUseTitleTextTool() =>
        _romData is not null
        && _romData.Profile.IsRetail
        && TitleTextService.TryGetProfile(_romData.Profile, out _);

    private bool CanUsePromptEditorTool() =>
        _romData is not null
        && _romData.Profile.IsRetail
        && PromptEditorProfileCatalog.TryGetProfile(_romData.Profile, out _);

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

    private void SetStatus(string message) => StatusText.Text = message;

    private string GetEditorText() => TextEditor.Text.Replace("\r\n", "\n").Replace('\r', '\n');
}
