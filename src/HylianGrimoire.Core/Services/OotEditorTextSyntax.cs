namespace HylianGrimoire.Services;

using HylianGrimoire.Codecs;

public sealed class OotEditorTextSyntax : IEditorTextSyntax
{
    public static OotEditorTextSyntax Instance { get; } = new();

    private OotEditorTextSyntax()
    {
    }

    public string ToDisplay(string editorText) => MessageTextSyntax.ToDisplay(editorText);

    public string FromDisplay(string displayText) => MessageTextSyntax.FromDisplay(displayText);

    public bool TryNormalizeEditorText(string editorText, out string normalized)
        => MessageTextSyntax.TryNormalizeEditorText(editorText, out normalized);
}
