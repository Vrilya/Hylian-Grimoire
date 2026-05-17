using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using HylianGrimoire.Interop;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace HylianGrimoire.Glyphs;

public sealed partial class GlyphOverrideWindow : Window
{
    private readonly ObservableCollection<GlyphListItem> _glyphs = [];
    private bool _updating;
    private byte? _selectedValue;

    public GlyphOverrideWindow()
    {
        InitializeComponent();
        SystemBackdrop = new MicaBackdrop();
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
        }

        AppWindow.Resize(new Windows.Graphics.SizeInt32(1100, 640));
        WindowSizeLimits.SetFixedWidth(this, 1100, 640);
        WindowIcon.Apply(this);
        WindowTheme.Register(this);

        GlyphList.ItemsSource = _glyphs;
        ReloadGlyphs();
        GlyphList.SelectedIndex = 0;
    }

    private void ReloadGlyphs()
    {
        byte? selected = _selectedValue;
        _glyphs.Clear();
        foreach (byte value in OotGlyphCatalog.GlyphValues)
        {
            OotGlyphInfo info = OotGlyphCatalog.GetGlyphInfo(value);
            _glyphs.Add(new GlyphListItem(info));
        }

        if (selected is byte selectedValue)
        {
            GlyphList.SelectedItem = _glyphs.FirstOrDefault(item => item.Value == selectedValue);
        }
    }

    private void OnGlyphSelected(object sender, SelectionChangedEventArgs e)
    {
        if (GlyphList.SelectedItem is not GlyphListItem item)
        {
            return;
        }

        _selectedValue = item.Value;
        ShowGlyph(item.Value);
    }

    private void ShowGlyph(byte value)
    {
        OotGlyphInfo info = OotGlyphCatalog.GetGlyphInfo(value);
        _updating = true;
        try
        {
            DetailTitle.Text = $"{info.Hex} Character";
            DefaultCharText.Text = $"{info.DefaultChar}";
            CurrentCharBox.Text = $"{info.CurrentChar}";
            DefaultWidthText.Text = $"{info.DefaultWidth:0.##}";
            CurrentWidthBox.Value = info.CurrentWidth;
            ImageStatusText.Text = info.HasImageOverride ? "Custom image" : "Default image";
            GlyphImage.Source = File.Exists(info.CurrentPath) ? new BitmapImage(new Uri(info.CurrentPath)) : null;
        }
        finally
        {
            _updating = false;
        }
    }

    private void OnCurrentCharChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating || _selectedValue is not byte value || CurrentCharBox.Text.Length == 0)
        {
            return;
        }

        OotGlyphInfo info = OotGlyphCatalog.GetGlyphInfo(value);
        if (CurrentCharBox.Text[0] == info.CurrentChar)
        {
            return;
        }

        GlyphOverrideStore.Current.SetDisplayChar(value, CurrentCharBox.Text[0]);
        RefreshSelected();
    }

    private void OnCurrentWidthChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_updating || _selectedValue is not byte value || double.IsNaN(sender.Value))
        {
            return;
        }

        double width = Math.Round(sender.Value, 2);
        OotGlyphInfo info = OotGlyphCatalog.GetGlyphInfo(value);
        if (Math.Abs(width - info.CurrentWidth) < 0.001)
        {
            return;
        }

        GlyphOverrideStore.Current.SetWidth(value, width);
        RefreshSelected();
    }

    private async void OnReplaceImage(object sender, RoutedEventArgs e)
    {
        if (_selectedValue is not byte value)
        {
            return;
        }

        string? path = await PickImageAsync();
        if (path is null)
        {
            return;
        }

        bool sameSize;
        try
        {
            sameSize = HasSameSize(path, OotGlyphCatalog.GetOriginalGlyphPath(value));
        }
        catch
        {
            await ShowDialogAsync("Invalid image", "The selected file could not be read as an image.");
            return;
        }

        if (!sameSize)
        {
            await ShowDialogAsync("Image size mismatch", "The replacement image must have the same pixel size as the original glyph.");
            return;
        }

        GlyphOverrideStore.Current.SetImage(value, path);
        RefreshSelected();
    }

    private void OnResetImage(object sender, RoutedEventArgs e)
    {
        if (_selectedValue is byte value)
        {
            GlyphOverrideStore.Current.ResetImage(value);
            RefreshSelected();
        }
    }

    private void OnResetWidth(object sender, RoutedEventArgs e)
    {
        if (_selectedValue is byte value)
        {
            GlyphOverrideStore.Current.ResetWidth(value);
            RefreshSelected();
        }
    }

    private void OnResetCharacter(object sender, RoutedEventArgs e)
    {
        if (_selectedValue is byte value)
        {
            GlyphOverrideStore.Current.ResetDisplayChar(value);
            RefreshSelected();
        }
    }

    private void RefreshSelected()
    {
        if (_selectedValue is not byte value)
        {
            return;
        }

        OotGlyphInfo info = OotGlyphCatalog.GetGlyphInfo(value);
        GlyphListItem? item = _glyphs.FirstOrDefault(item => item.Value == value);
        if (item is null)
        {
            ReloadGlyphs();
        }
        else
        {
            item.Update(info);
        }

        ShowGlyph(value);
    }

    private async Task<string?> PickImageAsync()
    {
        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            ViewMode = PickerViewMode.Thumbnail,
        };
        picker.FileTypeFilter.Add(".png");
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
        var file = await picker.PickSingleFileAsync();
        return file?.Path;
    }

    private async Task ShowDialogAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = Content.XamlRoot,
        };
        _ = await dialog.ShowAsync();
    }

    private static bool HasSameSize(string replacementPath, string originalPath)
    {
        using var replacement = new Bitmap(replacementPath);
        using var original = new Bitmap(originalPath);
        return replacement.Width == original.Width && replacement.Height == original.Height;
    }

    private sealed class GlyphListItem : INotifyPropertyChanged
    {
        private string _hex = string.Empty;
        private string _currentChar = string.Empty;
        private string _defaultChar = string.Empty;
        private string _currentWidth = string.Empty;
        private string _overrideLabel = string.Empty;

        public GlyphListItem(OotGlyphInfo info)
        {
            Value = info.Value;
            Update(info);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public byte Value { get; }

        public string Hex
        {
            get => _hex;
            private set => SetField(ref _hex, value);
        }

        public string CurrentChar
        {
            get => _currentChar;
            private set => SetField(ref _currentChar, value);
        }

        public string DefaultChar
        {
            get => _defaultChar;
            private set => SetField(ref _defaultChar, value);
        }

        public string CurrentWidth
        {
            get => _currentWidth;
            private set => SetField(ref _currentWidth, value);
        }

        public string OverrideLabel
        {
            get => _overrideLabel;
            private set => SetField(ref _overrideLabel, value);
        }

        public void Update(OotGlyphInfo info)
        {
            Hex = info.Hex;
            CurrentChar = info.CurrentChar.ToString();
            DefaultChar = $"Def {info.DefaultChar}";
            CurrentWidth = info.CurrentWidth.ToString("0.##");
            OverrideLabel = info.HasDisplayOverride || info.HasWidthOverride || info.HasImageOverride
                ? "Custom"
                : string.Empty;
        }

        private void SetField(ref string field, string value, [CallerMemberName] string? propertyName = null)
        {
            if (field == value)
            {
                return;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
