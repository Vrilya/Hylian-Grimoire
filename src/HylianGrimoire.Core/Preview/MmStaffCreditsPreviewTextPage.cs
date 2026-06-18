using HylianGrimoire.Codecs;

namespace HylianGrimoire.Preview;

public static class MmStaffCreditsPreviewTextPage
{
    public static IReadOnlyList<IReadOnlyList<OotPreviewToken>> FromEditorTextPages(
        string editorText,
        MessageEncodingProfile encodingProfile)
    {
        return OotPreviewTextPage.FromMessageTokensPages(
            MessageTextSyntax.FromEditorText(RemoveNonVisualStaffTags(editorText)),
            encodingProfile);
    }

    private static string RemoveNonVisualStaffTags(string editorText)
    {
        return editorText.Replace("[persistent]", string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
