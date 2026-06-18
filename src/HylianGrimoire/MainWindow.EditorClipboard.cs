using System.Runtime.InteropServices;
using Windows.ApplicationModel.DataTransfer;

namespace HylianGrimoire;

public sealed partial class MainWindow
{
    private bool TryCopyTextToClipboard(string text)
    {
        try
        {
            var package = new DataPackage();
            package.SetText(text);
            Clipboard.SetContent(package);
            return true;
        }
        catch (Exception ex) when (IsClipboardException(ex))
        {
            SetClipboardFailureStatus("Clipboard copy failed", ex);
            return false;
        }
    }

    private static bool ClipboardContainsText()
    {
        try
        {
            return Clipboard.GetContent().Contains(StandardDataFormats.Text);
        }
        catch (Exception ex) when (IsClipboardException(ex))
        {
            return false;
        }
    }

    private void SetClipboardFailureStatus(string title, Exception exception)
        => SetStatus($"{title}: {UiOperationExceptionHandler.GetDisplayMessage(title, exception)}");

    private static bool IsClipboardException(Exception exception)
        => exception is COMException
            or UnauthorizedAccessException;
}
