using HylianGrimoire.Rom;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;

namespace HylianGrimoire;

public sealed partial class MainWindow
{
    private IDisposable ShowProgress(string status, string progressText)
    {
        string previousStatus = StatusText.Text;
        SetStatus(progressText);
        ProgressTitleText.Text = status;
        ProgressPercentText.Text = "0%";
        ProgressModalBar.Value = 0;
        ProgressOverlay.Visibility = Visibility.Visible;
        AutomationProperties.SetName(ProgressModalBar, progressText);
        return new BusyScope(this, previousStatus);
    }

    private void UpdateBusyProgress(RomFileOperationProgress progress)
    {
        int percent = (int)Math.Round(progress.Percent);
        ProgressModalBar.Value = percent;
        ProgressPercentText.Text = $"{percent}%";
    }

    private sealed class BusyScope : IDisposable
    {
        private readonly MainWindow _owner;
        private readonly string _previousStatus;
        private bool _disposed;

        public BusyScope(MainWindow owner, string previousStatus)
        {
            _owner = owner;
            _previousStatus = previousStatus;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _owner.ProgressOverlay.Visibility = Visibility.Collapsed;
            _owner.ProgressModalBar.Value = 0;
            _owner.ProgressPercentText.Text = "0%";
            _owner.SetStatus(_previousStatus);
        }
    }
}
