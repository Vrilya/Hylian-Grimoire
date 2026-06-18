namespace HylianGrimoire.TextTextures;

public static partial class ItemNameTextureCatalog
{
    private const string OcarinaItemNameGroup = "textures/item_name_static";

    private static bool IsOcarinaItemNameGroup(string group)
        => string.Equals(group, OcarinaItemNameGroup, StringComparison.Ordinal);
}
