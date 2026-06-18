using HylianGrimoire.Rom;

namespace HylianGrimoire.Glyphs;

public sealed partial class CharacterProfileWindow
{
    public void SetRomSession(RomGlyphEditorSession? romSession)
    {
        if (ReferenceEquals(_romSession, romSession))
        {
            return;
        }

        DetachRomSession();
        _romSession = romSession;
        AttachRomSession(_romSession);
        RefreshWindowMode();
        RefreshProfileAndGlyphViews();
    }

    private void AttachRomSession(RomGlyphEditorSession? romSession)
    {
        if (romSession is null)
        {
            return;
        }

        romSession.Changed += OnRomSessionChanged;
        _characterProfileRuntime.SetCustomGlyphsAvailable(romSession.HasLoadedCustomGlyphOrWidth());
    }

    private void DetachRomSession()
    {
        if (_romSession is not null)
        {
            _romSession.Changed -= OnRomSessionChanged;
        }
    }

    private void OnRomSessionChanged(object? sender, EventArgs e)
    {
        GlyphDataChanged?.Invoke(this, EventArgs.Empty);
    }
}
