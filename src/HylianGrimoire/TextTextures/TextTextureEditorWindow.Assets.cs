using System.Drawing;

namespace HylianGrimoire.TextTextures;

public sealed partial class TextTextureEditorWindow
{
    private const string ProjectFontFileName = "ProjectFont.ttc";
    private const string ProjectFontGeneralFamilyName = "ProjectFont";
    private const string ProjectFontTitleCardFamilyName = "ProjectFont";
    private const string ProjectFontExtraBoldFamilyName = "ProjectFont ExtraBold";

    private static readonly string ProjectFontRelativePath =
        Path.Combine("Assets", ProjectFontFileName);

    private static readonly string PauseHeaderTemplateRelativePath =
        Path.Combine("Assets", "Games", "Oot", "TextTextureTemplates", "PauseHeaders");

    private static readonly string EndTitleTemplateRelativePath =
        Path.Combine("Assets", "Games", "Oot", "TextTextureTemplates", "EndTitles");

    private static readonly string MiscAssetRelativePath =
        Path.Combine("Assets", "Misc");

    private bool TryFindRenderFonts(out TextTextureFont primaryFont, out TextTextureFont? secondaryFont, out string missingFontMessage)
    {
        primaryFont = TextTextureFont.FromPath(string.Empty);
        secondaryFont = null;
        missingFontMessage = string.Empty;

        if (!TryFindProjectFont(out string projectFontPath, out missingFontMessage))
        {
            return false;
        }

        TextTextureFont generalFont = new(projectFontPath, ProjectFontGeneralFamilyName, FontStyle.Bold);
        TextTextureFont titleCardFont = new(projectFontPath, ProjectFontTitleCardFamilyName, FontStyle.Regular);
        TextTextureFont extraBoldFont = new(projectFontPath, ProjectFontExtraBoldFamilyName);
        if (_selectedTextureKind is TextTextureKind.FileSelect)
        {
            bool isControls = IsFileSelectControlsTarget();
            primaryFont = titleCardFont;
            secondaryFont = isControls ? generalFont : null;
            return true;
        }

        if (UsesGameOverSettings())
        {
            primaryFont = IsContinuePlayingTarget() ? generalFont : extraBoldFont;
            return true;
        }

        if (_selectedTextureKind is TextTextureKind.BossTitleCards or TextTextureKind.PauseHeaders or TextTextureKind.MapPositionNames)
        {
            primaryFont = titleCardFont;
            if (_selectedTextureKind is TextTextureKind.PauseHeaders or TextTextureKind.MapPositionNames)
            {
                return true;
            }

            secondaryFont = extraBoldFont;
            return true;
        }

        primaryFont = generalFont;
        return true;
    }

    private static bool TryFindProjectFont(out string fontPath, out string missingFontMessage)
    {
        string? path = FindLocalPath(ProjectFontRelativePath);
        if (path is not null)
        {
            fontPath = path;
            missingFontMessage = string.Empty;
            return true;
        }

        fontPath = string.Empty;
        missingFontMessage = $@"Missing project font. Expected {ProjectFontRelativePath}.";
        return false;
    }

    private static bool TryFindPauseHeaderTemplateRoot(out string templateRoot, out string missingTemplateMessage)
    {
        string? path = FindLocalDirectory(PauseHeaderTemplateRelativePath);
        if (path is not null)
        {
            templateRoot = path;
            missingTemplateMessage = string.Empty;
            return true;
        }

        templateRoot = string.Empty;
        missingTemplateMessage = $@"Missing pause-header templates. Expected {PauseHeaderTemplateRelativePath}.";
        return false;
    }

    private static bool TryFindEndTitleTemplateAsset(string fileName, out string assetPath, out string missingAssetMessage)
    {
        string? path = FindLocalPath(Path.Combine(EndTitleTemplateRelativePath, fileName));
        if (path is not null)
        {
            assetPath = path;
            missingAssetMessage = string.Empty;
            return true;
        }

        assetPath = string.Empty;
        missingAssetMessage = $@"Missing end-title template asset. Expected {Path.Combine(EndTitleTemplateRelativePath, fileName)}.";
        return false;
    }

    private static bool TryFindMiscAsset(string fileName, out string assetPath, out string missingAssetMessage)
    {
        string? path = FindLocalPath(Path.Combine(MiscAssetRelativePath, fileName));
        if (path is not null)
        {
            assetPath = path;
            missingAssetMessage = string.Empty;
            return true;
        }

        assetPath = string.Empty;
        missingAssetMessage = $@"Missing misc asset. Expected {Path.Combine(MiscAssetRelativePath, fileName)}.";
        return false;
    }

    private static string? FindLocalPath(string relativePath)
        => FindExistingPath(relativePath, File.Exists);

    private static string? FindLocalDirectory(string relativePath)
        => FindExistingPath(relativePath, Directory.Exists);

    private static string? FindExistingPath(string relativePath, Predicate<string> exists)
    {
        var probes = new List<string>
        {
            AppContext.BaseDirectory,
            Environment.CurrentDirectory,
        };

        string? current = AppContext.BaseDirectory;
        for (int i = 0; i < 8 && current is not null; i++)
        {
            probes.Add(current);
            current = Directory.GetParent(current)?.FullName;
        }

        foreach (string probe in probes.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            string path = Path.GetFullPath(Path.Combine(probe, relativePath));
            if (exists(path))
            {
                return path;
            }
        }

        return null;
    }
}
