using HylianGrimoire.Codecs;
using HylianGrimoire.Glyphs;
using HylianGrimoire.Models;

namespace HylianGrimoire.Preview;

public interface IMessagePreviewWindow
{
    event EventHandler? PreviewClosed;

    void Activate();

    void Close();

    void SetEmpty();

    void SetMessage(
        MessageEntry entry,
        string editorText,
        IGlyphSource glyphSource,
        MessageEncodingProfile encodingProfile);
}
