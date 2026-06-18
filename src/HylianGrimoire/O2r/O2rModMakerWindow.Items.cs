using System.ComponentModel;
using System.Runtime.CompilerServices;
using HylianGrimoire.Textures;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

namespace HylianGrimoire.O2r;

public sealed partial class O2rModMakerWindow
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

        public ArchiveTextureListItem(O2rArchiveTextureResource resource)
        {
            Resource = resource;
            ResourcePath = resource.ResourcePath;
            DisplayText = resource.DisplayText;
            DisplayBrush = resource.DisplayBrush;
        }

        public O2rArchiveTextureResource Resource { get; }

        public string ResourcePath { get; }

        public string DisplayText { get; }

        public Brush? DisplayBrush { get; }

        public bool IsChecked
        {
            get => _isChecked;
            set => SetField(ref _isChecked, value);
        }
    }

    public sealed class TextureFolderItem(string name, O2rModPortProfile portProfile) : NotifyItem
    {
        private bool _isChecked;

        public string Name { get; } = name;

        public SortedDictionary<string, TextureFolderItem> Folders { get; } = new(StringComparer.OrdinalIgnoreCase);

        public List<TextureDefinition> Textures { get; } = [];

        public List<O2rArchiveTextureResource> ArchiveTextures { get; } = [];

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
                folder = new TextureFolderItem(childName, portProfile);
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
                yield return portProfile.GetTextureResourcePath(texture);
            }

            foreach (O2rArchiveTextureResource texture in ArchiveTextures)
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

    public sealed record O2rArchiveTextureResource(string ResourcePath, O2rArchiveTextureStatus Status)
    {
        public string Name => Path.GetFileName(ResourcePath);

        public string DisplayText => Status switch
        {
            O2rArchiveTextureStatus.DiffersFromRom => $"{Name}  override",
            O2rArchiveTextureStatus.External => $"{Name}  external",
            _ => Name,
        };

        public string StatusText => Status switch
        {
            O2rArchiveTextureStatus.MatchesRom => "Matches loaded ROM.",
            O2rArchiveTextureStatus.DiffersFromRom => "Overrides loaded ROM.",
            O2rArchiveTextureStatus.External => "Not found in loaded ROM catalog.",
            _ => "Existing .o2r texture resource.",
        };

        public Brush? DisplayBrush => Status switch
        {
            O2rArchiveTextureStatus.DiffersFromRom => new SolidColorBrush(Colors.LightSalmon),
            O2rArchiveTextureStatus.External => new SolidColorBrush(Colors.LightSkyBlue),
            _ => null,
        };
    }

    private sealed record O2rTextPayload(string ResourcePath, byte[] Data);

    private enum ResourceViewMode
    {
        Rom,
        Mod,
    }

    public enum O2rArchiveTextureStatus
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

}
