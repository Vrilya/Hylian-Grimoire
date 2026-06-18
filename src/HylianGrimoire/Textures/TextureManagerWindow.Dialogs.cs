using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HylianGrimoire.Textures;

public sealed partial class TextureManagerWindow
{
    private async Task ShowErrorAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = Content.XamlRoot,
        };

        await dialog.ShowAsync();
    }

    private async Task ShowOperationExceptionAsync(string title, Exception exception, string? recoveryMessage = null)
    {
        await UiOperationExceptionHandler.ShowAsync(title, exception, ShowErrorAsync, recoveryMessage);
    }

    private void SetLocalStatus(string message) => StatusText.Text = message;

    private IDisposable ShowProgress(string title)
    {
        ProgressTitleText.Text = title;
        ProgressPercentText.Text = "0%";
        ProgressBar.Value = 0;
        ProgressOverlay.Visibility = Visibility.Visible;
        return new ProgressScope(this);
    }

    private void UpdateProgress(int percent)
    {
        ProgressBar.Value = percent;
        ProgressPercentText.Text = $"{percent}%";
    }

    private static int GetPercent(int completed, int total)
        => total <= 0 ? 100 : Math.Clamp((int)Math.Round(completed * 100d / total), 0, 100);

    private sealed class ProgressScope(TextureManagerWindow owner) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            owner.ProgressOverlay.Visibility = Visibility.Collapsed;
            owner.ProgressBar.Value = 0;
            owner.ProgressPercentText.Text = "0%";
        }
    }
}
