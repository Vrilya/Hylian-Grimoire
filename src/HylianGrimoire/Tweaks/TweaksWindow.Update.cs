namespace HylianGrimoire.Tweaks;

public sealed partial class TweaksWindow
{
    private IDisposable BeginUpdate()
        => new UpdateScope(this);

    private sealed class UpdateScope : IDisposable
    {
        private TweaksWindow? _owner;

        public UpdateScope(TweaksWindow owner)
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
