namespace HylianGrimoire.Services;

public interface IEditorTextSyntax
{
    string ToDisplay(string editorText);

    string FromDisplay(string displayText);

    bool TryNormalizeEditorText(string editorText, out string normalized);
}
