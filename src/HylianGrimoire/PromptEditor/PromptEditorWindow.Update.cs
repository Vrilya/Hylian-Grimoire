namespace HylianGrimoire.PromptEditor;

public sealed partial class PromptEditorWindow
{
    private IDisposable BeginUpdate()
        => new UpdateScope(this);

    private sealed class UpdateScope : IDisposable
    {
        private PromptEditorWindow? _owner;

        public UpdateScope(PromptEditorWindow owner)
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
