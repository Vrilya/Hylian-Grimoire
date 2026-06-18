namespace HylianGrimoire.Glyphs;

public sealed partial class CharacterProfileWindow
{
    private IDisposable BeginUpdate()
        => new UpdateScope(this);

    private sealed class UpdateScope : IDisposable
    {
        private CharacterProfileWindow? _owner;

        public UpdateScope(CharacterProfileWindow owner)
        {
            _owner = owner;
            owner._updateDepth++;
        }

        public void Dispose()
        {
            if (_owner is null)
            {
                return;
            }

            _owner._updateDepth = Math.Max(0, _owner._updateDepth - 1);
            _owner = null;
        }
    }
}
