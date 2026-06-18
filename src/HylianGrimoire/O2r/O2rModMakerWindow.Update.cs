namespace HylianGrimoire.O2r;

public sealed partial class O2rModMakerWindow
{
    private IDisposable BeginCheckUpdate()
        => new UpdateScope(this, UpdateScopeKind.Checks);

    private IDisposable BeginTextCheckUpdate()
        => new UpdateScope(this, UpdateScopeKind.TextChecks);

    private IDisposable BeginIncludeCheckUpdate()
        => new UpdateScope(this, UpdateScopeKind.IncludeChecks);

    private IDisposable BeginResourceViewUpdate()
        => new UpdateScope(this, UpdateScopeKind.ResourceView);

    private void IncrementUpdateDepth(UpdateScopeKind kind)
    {
        switch (kind)
        {
            case UpdateScopeKind.Checks:
                _checkUpdateDepth++;
                break;
            case UpdateScopeKind.TextChecks:
                _textCheckUpdateDepth++;
                break;
            case UpdateScopeKind.IncludeChecks:
                _includeCheckUpdateDepth++;
                break;
            case UpdateScopeKind.ResourceView:
                _resourceViewUpdateDepth++;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }

    private void DecrementUpdateDepth(UpdateScopeKind kind)
    {
        switch (kind)
        {
            case UpdateScopeKind.Checks:
                _checkUpdateDepth = Math.Max(0, _checkUpdateDepth - 1);
                break;
            case UpdateScopeKind.TextChecks:
                _textCheckUpdateDepth = Math.Max(0, _textCheckUpdateDepth - 1);
                break;
            case UpdateScopeKind.IncludeChecks:
                _includeCheckUpdateDepth = Math.Max(0, _includeCheckUpdateDepth - 1);
                break;
            case UpdateScopeKind.ResourceView:
                _resourceViewUpdateDepth = Math.Max(0, _resourceViewUpdateDepth - 1);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }

    private enum UpdateScopeKind
    {
        Checks,
        TextChecks,
        IncludeChecks,
        ResourceView,
    }

    private sealed class UpdateScope : IDisposable
    {
        private O2rModMakerWindow? _owner;
        private readonly UpdateScopeKind _kind;

        public UpdateScope(O2rModMakerWindow owner, UpdateScopeKind kind)
        {
            _owner = owner;
            _kind = kind;
            owner.IncrementUpdateDepth(kind);
        }

        public void Dispose()
        {
            if (_owner is null)
            {
                return;
            }

            _owner.DecrementUpdateDepth(_kind);
            _owner = null;
        }
    }
}
