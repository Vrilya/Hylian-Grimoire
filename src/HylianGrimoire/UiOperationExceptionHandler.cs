using System.Runtime.InteropServices;

namespace HylianGrimoire;

internal static class UiOperationExceptionHandler
{
    public static async Task ShowAsync(
        string title,
        Exception exception,
        Func<string, string, Task> showErrorAsync,
        string? recoveryMessage = null,
        Action<string>? setStatus = null,
        string? statusMessage = null)
    {
        await showErrorAsync(title, GetDisplayMessage(title, exception, recoveryMessage));

        if (!string.IsNullOrWhiteSpace(statusMessage))
        {
            setStatus?.Invoke(statusMessage);
        }
    }

    public static string GetDisplayMessage(
        string title,
        Exception exception,
        string? recoveryMessage = null)
    {
        if (!IsRecoverableOperationError(exception))
        {
            AppDiagnostics.LogHandledException(title, exception);
        }

        return BuildOperationErrorMessage(exception.Message, recoveryMessage);
    }

    private static bool IsRecoverableOperationError(Exception exception)
        => exception is InvalidDataException
            or IOException
            or UnauthorizedAccessException
            or ArgumentException
            or FormatException
            or NotSupportedException
            or ExternalException;

    private static string BuildOperationErrorMessage(string message, string? recoveryMessage)
        => string.IsNullOrWhiteSpace(recoveryMessage)
            ? message
            : $"{message}{Environment.NewLine}{Environment.NewLine}{recoveryMessage}";
}
