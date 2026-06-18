using HylianGrimoire.Games;
using HylianGrimoire.Rom;
using HylianGrimoire.Textures;

namespace HylianGrimoire.TextTextures;

public static class FileSelectTextureCatalog
{
    public const int Height = 16;
    public const int MaxWidth = 144;

    private const string Group = "textures/title_static";

    public static readonly IReadOnlyList<FileSelectTextureSpec> Specs =
    [
        new("AreYouSure", "English", Group, "gFileSelAreYouSureENGTex", "Are you sure?", 128, FileSelectTexturePreset.ZeroX),
        new("AreYouSure2", "English", Group, "gFileSelAreYouSure2ENGTex", "Are you sure?", 128, FileSelectTexturePreset.ZeroX),
        new("CopyWhichFile", "English", Group, "gFileSelCopyWhichFileENGTex", "Copy which file?", 128, FileSelectTexturePreset.ZeroX),
        new("CopyToWhichFile", "English", Group, "gFileSelCopyToWhichFileENGTex", "Copy to which file?", 128, FileSelectTexturePreset.ZeroX),
        new("EraseWhichFile", "English", Group, "gFileSelEraseWhichFileENGTex", "Erase which file?", 128, FileSelectTexturePreset.ZeroX),
        new("FileCopied", "English", Group, "gFileSelFileCopiedENGTex", "File copied.", 128, FileSelectTexturePreset.ZeroX),
        new("FileEmpty", "English", Group, "gFileSelFileEmptyENGTex", "This is an empty file.", 128, FileSelectTexturePreset.FileEmpty),
        new("FileErased", "English", Group, "gFileSelFileErasedENGTex", "File erased.", 128, FileSelectTexturePreset.ZeroX),
        new("FileInUse", "English", Group, "gFileSelFileInUseENGTex", "This file is in use.", 128, FileSelectTexturePreset.FileEmpty),
        new("Name", "English", Group, "gFileSelNameENGTex", "Name?", 56, FileSelectTexturePreset.Name),
        new("NoEmptyFile", "English", Group, "gFileSelNoEmptyFileENGTex", "There is no empty file.", 128, FileSelectTexturePreset.FileEmpty),
        new("NoFileToCopy", "English", Group, "gFileSelNoFileToCopyENGTex", "No file to copy.", 128, FileSelectTexturePreset.FileEmpty),
        new("NoFileToErase", "English", Group, "gFileSelNoFileToEraseENGTex", "No file to erase.", 128, FileSelectTexturePreset.FileEmpty),
        new("OpenThisFile", "English", Group, "gFileSelOpenThisFileENGTex", "Open this file?", 128, FileSelectTexturePreset.ZeroX),
        new("Options", "English", Group, "gFileSelOptionsENGTex", "Options", 128, FileSelectTexturePreset.Options),
        new("PleaseSelectAFile", "English", Group, "gFileSelPleaseSelectAFileENGTex", "Please select a file.", 128, FileSelectTexturePreset.ZeroX),
        new("Controls", "English", Group, "gFileSelControlsENGTex", "A-Decide \u2022 B-Cancel", 144, FileSelectTexturePreset.Controls),
        new("AreYouSure2", "French", Group, "gFileSelAreYouSure2FRATex", "Etes-vous s\u00fbr?", 128, FileSelectTexturePreset.ZeroX),
        new("CopyWhichFile", "French", Group, "gFileSelCopyWhichFileFRATex", "Copier quel fichier?", 128, FileSelectTexturePreset.ZeroX),
        new("CopyToWhichFile", "French", Group, "gFileSelCopyToWhichFileFRATex", "Copier sur quel fichier?", 128, FileSelectTexturePreset.ZeroX),
        new("EraseWhichFile", "French", Group, "gFileSelEraseWhichFileFRATex", "Effacer quel fichier?", 128, FileSelectTexturePreset.ZeroX),
        new("FileCopied", "French", Group, "gFileSelFileCopiedFRATex", "Fichier copi\u00e9", 128, FileSelectTexturePreset.ZeroX),
        new("FileEmpty", "French", Group, "gFileSelFileEmptyFRATex", "Ce fichier est vide", 128, FileSelectTexturePreset.FileEmpty),
        new("FileErased", "French", Group, "gFileSelFileErasedFRATex", "Fichier effac\u00e9", 128, FileSelectTexturePreset.ZeroX),
        new("FileInUse", "French", Group, "gFileSelFileInUseFRATex", "Ce fichier est utilis\u00e9", 128, FileSelectTexturePreset.FileEmpty),
        new("Name", "French", Group, "gFileSelNameFRATex", "Nom?", 56, FileSelectTexturePreset.Name),
        new("NoEmptyFile", "French", Group, "gFileSelNoEmptyFileFRATex", "Aucun fichier vide", 128, FileSelectTexturePreset.FileEmpty),
        new("NoFileToCopy", "French", Group, "gFileSelNoFileToCopyFRATex", "Aucun fichier \u00e0 copier", 128, FileSelectTexturePreset.FileEmpty),
        new("NoFileToErase", "French", Group, "gFileSelNoFileToEraseFRATex", "Aucun fichier \u00e0 effacer", 128, FileSelectTexturePreset.FileEmpty),
        new("OpenThisFile", "French", Group, "gFileSelOpenThisFileFRATex", "Ouvrir ce fichier?", 128, FileSelectTexturePreset.ZeroX),
        new("PleaseSelectAFile", "French", Group, "gFileSelPleaseSelectAFileFRATex", "Veuillez choisir un fichier", 128, FileSelectTexturePreset.ZeroX),
        new("Controls", "French", Group, "gFileSelControlsFRATex", "A-Valider \u2022 B-Annuler", 144, FileSelectTexturePreset.Controls),
        new("AreYouSure2", "German", Group, "gFileSelAreYouSure2GERTex", "Sicher?", 128, FileSelectTexturePreset.ZeroX),
        new("CopyToWhichFile", "German", Group, "gFileSelCopyToWhichFileGERTex", "Wohin kopieren?", 128, FileSelectTexturePreset.ZeroX),
        new("FileCopied", "German", Group, "gFileSelFileCopiedGERTex", "Datei kopiert.", 128, FileSelectTexturePreset.ZeroX),
        new("FileEmpty", "German", Group, "gFileSelFileEmptyGERTex", "Datei ist leer !", 128, FileSelectTexturePreset.FileEmpty),
        new("FileErased", "German", Group, "gFileSelFileErasedGERTex", "Datei gel\u00f6scht.", 128, FileSelectTexturePreset.ZeroX),
        new("FileInUse", "German", Group, "gFileSelFileInUseGERTex", "Datei ist belegt !", 128, FileSelectTexturePreset.FileEmpty),
        new("Name", "German", Group, "gFileSelNameGERTex", "Name?", 56, FileSelectTexturePreset.Name),
        new("NoEmptyFile", "German", Group, "gFileSelNoEmptyFileGERTex", "Keine leere Datei !", 128, FileSelectTexturePreset.FileEmpty),
        new("NoFileToCopy", "German", Group, "gFileSelNoFileToCopyGERTex", "Keine Datei vorhanden.", 128, FileSelectTexturePreset.FileEmpty),
        new("NoFileToErase", "German", Group, "gFileSelNoFileToEraseGERTex", "Keine Datei vorhanden.", 128, FileSelectTexturePreset.FileEmpty),
        new("OpenThisFile", "German", Group, "gFileSelOpenThisFileGERTex", "Datei \u00f6ffnen?", 128, FileSelectTexturePreset.ZeroX),
        new("Options", "German", Group, "gFileSelOptionsGERTex", "Optionen", 128, FileSelectTexturePreset.Options),
        new("PleaseSelectAFile", "German", Group, "gFileSelPleaseSelectAFileGERTex", "Datei w\u00e4hlen.", 128, FileSelectTexturePreset.ZeroX),
        new("Controls", "German", Group, "gFileSelControlsGERTex", "A-Eingabe \u2022 B-Zur\u00fcck", 144, FileSelectTexturePreset.Controls),
    ];

    private static readonly IReadOnlyDictionary<string, FileSelectTextureSpec> SpecsByName = Specs
        .ToDictionary(spec => spec.TextureName, StringComparer.Ordinal);

    public static bool TryGetTargets(RomVersionProfile profile, out IReadOnlyList<TextureDefinition> textures)
    {
        if (profile.Game != GameKind.OcarinaOfTime || !TextureCatalog.TryGetTextures(profile, out IReadOnlyList<TextureDefinition>? catalog))
        {
            textures = [];
            return false;
        }

        Dictionary<string, TextureDefinition> byName = catalog
            .Where(IsFileSelectTexture)
            .ToDictionary(texture => texture.Name, StringComparer.Ordinal);

        var result = new List<TextureDefinition>(Specs.Count);
        foreach (FileSelectTextureSpec spec in Specs)
        {
            if (byName.TryGetValue(spec.TextureName, out TextureDefinition? texture))
            {
                result.Add(texture);
            }
        }

        textures = result;
        return textures.Count > 0;
    }

    public static IReadOnlyList<TextureDefinition> GetTargets(RomVersionProfile profile)
        => TryGetTargets(profile, out IReadOnlyList<TextureDefinition>? textures)
            ? textures
            : throw new NotSupportedException($"File Select texture catalog is not available for {profile.Name}.");

    public static bool IsFileSelectTexture(TextureDefinition texture)
        => SpecsByName.TryGetValue(texture.Name, out FileSelectTextureSpec? spec)
            && string.Equals(texture.Group, spec.Group, StringComparison.Ordinal)
            && texture.Width == spec.Width
            && texture.Height == Height
            && texture.Format == TextureFormat.IA8;

    public static bool IsControlsTexture(TextureDefinition texture)
        => GetPreset(texture) == FileSelectTexturePreset.Controls;

    public static FileSelectTexturePreset GetPreset(TextureDefinition texture)
        => SpecsByName.TryGetValue(texture.Name, out FileSelectTextureSpec? spec) && IsFileSelectTexture(texture)
            ? spec.Preset
            : FileSelectTexturePreset.Default;

    public static string GetDisplayText(TextureDefinition texture)
        => SpecsByName.TryGetValue(texture.Name, out FileSelectTextureSpec? spec)
            ? spec.SampleText
            : texture.Name;

    public static string GetLanguage(TextureDefinition texture)
        => SpecsByName.TryGetValue(texture.Name, out FileSelectTextureSpec? spec)
            ? spec.Language
            : "Unknown";

}

public enum FileSelectTexturePreset
{
    Default,
    ZeroX,
    FileEmpty,
    Name,
    Options,
    Controls,
}

public sealed record FileSelectTextureSpec(
    string Key,
    string Language,
    string Group,
    string TextureName,
    string SampleText,
    int Width,
    FileSelectTexturePreset Preset);
