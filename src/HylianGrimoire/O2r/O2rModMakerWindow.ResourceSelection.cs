using HylianGrimoire.Textures;

namespace HylianGrimoire.O2r;

public sealed partial class O2rModMakerWindow
{
    private void RefreshSelectedDetails()
    {
        if (GetSelectedTextureItem() is { } textureItem)
        {
            RefreshSelectedTextureDetails(textureItem);
            return;
        }

        if (GetSelectedArchiveTextureItem() is { } archiveItem)
        {
            UpdateArchiveTexturePreview(archiveItem.Resource);
            return;
        }

        ClearSelectedTextureDetails();
    }

    private void RefreshSelectedTextureDetails(TextureListItem item)
    {
        if (_romData is null)
        {
            ClearPreview();
            string resourcePath = _portProfile.GetTextureResourcePath(item.Texture);
            DetailsText.Text =
                $"{resourcePath}\n" +
                "Texture preview requires a loaded ROM.\n" +
                (_selectedResources.Contains(resourcePath) ? "Included in mod." : "Not included.");
            return;
        }

        try
        {
            UpdatePreviewImage(item.Texture);
            int byteLength = TextureCodec.GetByteLength(item.Texture.Width, item.Texture.Height, item.Texture.Format);
            string resourcePath = _portProfile.GetTextureResourcePath(item.Texture);
            DetailsText.Text =
                $"{resourcePath}\n" +
                $"0x{item.Texture.RomAddress:X}  {item.Texture.Width}x{item.Texture.Height}  {item.Texture.Format}  {byteLength} bytes\n" +
                (_selectedResources.Contains(resourcePath) ? "Included in mod." : "Not included.");
        }
        catch (Exception ex)
        {
            ClearPreview();
            DetailsText.Text = UiOperationExceptionHandler.GetDisplayMessage("O2R texture preview failed", ex);
        }
    }

    private void ClearSelectedTextureDetails()
    {
        ClearPreview();
        DetailsText.Text = GetTextureResourceSummary();
    }

    private string GetTextureResourceSummary()
    {
        return _resourceViewMode == ResourceViewMode.Rom
            ? $"{_textures.Count} ROM textures available."
            : $"{_selectedResources.Count} .o2r texture resources.";
    }

    private TextureListItem? GetSelectedTextureItem()
        => TextureTree.SelectedNode?.Content as TextureListItem;

    private ArchiveTextureListItem? GetSelectedArchiveTextureItem()
        => TextureTree.SelectedNode?.Content as ArchiveTextureListItem;
}
