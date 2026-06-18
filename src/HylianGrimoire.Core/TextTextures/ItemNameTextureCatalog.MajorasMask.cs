namespace HylianGrimoire.TextTextures;

public static partial class ItemNameTextureCatalog
{
    private const string MajorasMaskItemNameGroup = "archives/item_name_static";

    private static bool IsMajorasMaskItemNameGroup(string group)
        => string.Equals(group, MajorasMaskItemNameGroup, StringComparison.Ordinal);
}
