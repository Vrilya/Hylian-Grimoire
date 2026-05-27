using System.ComponentModel;
using System.Runtime.CompilerServices;
using HylianGrimoire.Textures;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

namespace HylianGrimoire.Soh;

public sealed partial class SohModMakerWindow
{
    public sealed class TextureListItem(TextureDefinition texture) : NotifyItem
    {
        private bool _isChecked;

        public TextureDefinition Texture { get; } = texture;

        public string DisplayText => $"{Texture.Name}  {Texture.Width}x{Texture.Height}  {Texture.Format}";

        public Brush? DisplayBrush => null;

        public bool IsChecked
        {
            get => _isChecked;
            set => SetField(ref _isChecked, value);
        }
    }

    public sealed class ArchiveTextureListItem : NotifyItem
    {
        private bool _isChecked;

        public ArchiveTextureListItem(SohArchiveTextureResource resource)
        {
            Resource = resource;
            ResourcePath = resource.ResourcePath;
            DisplayText = resource.DisplayText;
            DisplayBrush = resource.DisplayBrush;
        }

        public SohArchiveTextureResource Resource { get; }

        public string ResourcePath { get; }

        public string DisplayText { get; }

        public Brush? DisplayBrush { get; }

        public bool IsChecked
        {
            get => _isChecked;
            set => SetField(ref _isChecked, value);
        }
    }

    public sealed class TextureFolderItem(string name) : NotifyItem
    {
        private bool _isChecked;

        public string Name { get; } = name;

        public SortedDictionary<string, TextureFolderItem> Folders { get; } = new(StringComparer.OrdinalIgnoreCase);

        public List<TextureDefinition> Textures { get; } = [];

        public List<SohArchiveTextureResource> ArchiveTextures { get; } = [];

        public int TotalCount { get; private set; }

        public bool IsPopulated { get; set; }

        public bool IsChecked
        {
            get => _isChecked;
            set => SetField(ref _isChecked, value);
        }

        public string DisplayText => $"{Name} ({TotalCount})";

        public Brush? DisplayBrush => null;

        public TextureFolderItem GetOrAddFolder(string childName)
        {
            if (!Folders.TryGetValue(childName, out TextureFolderItem? folder))
            {
                folder = new TextureFolderItem(childName);
                Folders.Add(childName, folder);
            }

            return folder;
        }

        public IEnumerable<TextureDefinition> GetAllTextures()
        {
            foreach (TextureDefinition texture in Textures)
            {
                yield return texture;
            }

            foreach (TextureFolderItem folder in Folders.Values)
            {
                foreach (TextureDefinition texture in folder.GetAllTextures())
                {
                    yield return texture;
                }
            }
        }

        public IEnumerable<string> GetAllResourcePaths()
        {
            foreach (TextureDefinition texture in Textures)
            {
                yield return SohResourcePacker.GetTextureResourcePath(texture);
            }

            foreach (SohArchiveTextureResource texture in ArchiveTextures)
            {
                yield return texture.ResourcePath;
            }

            foreach (TextureFolderItem folder in Folders.Values)
            {
                foreach (string resourcePath in folder.GetAllResourcePaths())
                {
                    yield return resourcePath;
                }
            }
        }

        public int UpdateTotalCount()
        {
            TotalCount = Textures.Count + ArchiveTextures.Count + Folders.Values.Sum(folder => folder.UpdateTotalCount());
            return TotalCount;
        }
    }

    public sealed record SohArchiveTextureResource(string ResourcePath, SohArchiveTextureStatus Status)
    {
        public string Name => Path.GetFileName(ResourcePath);

        public string DisplayText => Status switch
        {
            SohArchiveTextureStatus.DiffersFromRom => $"{Name}  override",
            SohArchiveTextureStatus.External => $"{Name}  external",
            _ => Name,
        };

        public string StatusText => Status switch
        {
            SohArchiveTextureStatus.MatchesRom => "Matches loaded ROM.",
            SohArchiveTextureStatus.DiffersFromRom => "Overrides loaded ROM.",
            SohArchiveTextureStatus.External => "Not found in loaded ROM catalog.",
            _ => "Existing .o2r texture resource.",
        };

        public Brush? DisplayBrush => Status switch
        {
            SohArchiveTextureStatus.DiffersFromRom => new SolidColorBrush(Colors.LightSalmon),
            SohArchiveTextureStatus.External => new SolidColorBrush(Colors.LightSkyBlue),
            _ => null,
        };
    }

    public sealed record SohTextResourceItem(
        string DisplayName,
        string ResourcePath,
        SohTextResourceKind Kind,
        int BankIndex);

    private sealed record SohTextPayload(string ResourcePath, byte[] Data);

    public enum SohTextResourceKind
    {
        CurrentDocument,
        MessageBank,
        Credits,
    }

    private enum ResourceViewMode
    {
        Rom,
        Mod,
    }

    public enum SohArchiveTextureStatus
    {
        InArchive,
        MatchesRom,
        DiffersFromRom,
        External,
    }

    public abstract class NotifyItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    private sealed class ProgressScope(SohModMakerWindow owner) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            owner.ProgressOverlay.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            owner.ProgressBar.Value = 0;
            owner.ProgressPercentText.Text = "0%";
        }
    }
}
