namespace HylianGrimoire.Services;

using System.Text.RegularExpressions;
using HylianGrimoire.Codecs;
using HylianGrimoire.Codecs.MajorasMask;

public sealed partial class MmEditorTextSyntax : IEditorTextSyntax
{
    [GeneratedRegex(@"\n?(\[(?:break|continue|breakdelay:[^\]]*)\])\n?")]
    private static partial Regex PageControlTagFull();

    [GeneratedRegex(@"(\[break2\])\n?")]
    private static partial Regex Break2TagTrailing();

    public static MmEditorTextSyntax Instance { get; } = new();

    private MmEditorTextSyntax()
    {
    }

    public string ToDisplay(string editorText)
    {
        string display = PageControlTagFull().Replace(editorText, "\n$1\n");
        return Break2TagTrailing().Replace(display, "$1\n");
    }

    public string FromDisplay(string displayText)
    {
        string editorText = PageControlTagFull().Replace(displayText, "$1");
        return Break2TagTrailing().Replace(editorText, "$1");
    }

    public bool TryNormalizeEditorText(string editorText, out string normalized)
    {
        try
        {
            byte[] encoded = MmMessageTextCodec.Encode(editorText, MessageEncodingProfile.MajorasMask);
            normalized = MmMessageTextCodec.Decode(
                encoded,
                0,
                encoded.Length,
                MessageEncodingProfile.MajorasMask);
            return !string.Equals(editorText, normalized, StringComparison.Ordinal);
        }
        catch (InvalidDataException)
        {
            normalized = editorText;
            return false;
        }
    }
}
