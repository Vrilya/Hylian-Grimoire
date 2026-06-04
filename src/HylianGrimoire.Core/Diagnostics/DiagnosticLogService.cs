using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace HylianGrimoire.Diagnostics;

public sealed class DiagnosticLogService
{
    private readonly object _sync = new();
    private readonly DiagnosticAppInfo _appInfo;

    public DiagnosticLogService(
        DiagnosticAppInfo appInfo,
        string logDirectory,
        int retainedLogFiles = 30)
    {
        ArgumentNullException.ThrowIfNull(appInfo);
        ArgumentException.ThrowIfNullOrWhiteSpace(logDirectory);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(retainedLogFiles);

        _appInfo = appInfo;
        LogDirectory = logDirectory;
        RetainedLogFiles = retainedLogFiles;
    }

    public string LogDirectory { get; }

    public int RetainedLogFiles { get; }

    public string WriteEvent(
        string eventName,
        Exception? exception,
        IReadOnlyDictionary<string, string?>? context = null,
        string? details = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);

        lock (_sync)
        {
            Directory.CreateDirectory(LogDirectory);

            string path = CreateLogPath(eventName);
            File.WriteAllText(
                path,
                BuildLogText(eventName, exception, context, details),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            PruneLogs();
            return path;
        }
    }

    private string CreateLogPath(string eventName)
    {
        string stamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);
        string safeEventName = SanitizeFileName(eventName);
        string path = Path.Combine(LogDirectory, $"{stamp}-{safeEventName}.log");
        int suffix = 2;
        while (File.Exists(path))
        {
            path = Path.Combine(LogDirectory, $"{stamp}-{safeEventName}-{suffix}.log");
            suffix++;
        }

        return path;
    }

    private string BuildLogText(
        string eventName,
        Exception? exception,
        IReadOnlyDictionary<string, string?>? context,
        string? details)
    {
        var text = new StringBuilder();
        text.AppendLine($"{_appInfo.DisplayName} diagnostic log");
        text.AppendLine($"Timestamp UTC: {DateTimeOffset.UtcNow:O}");
        text.AppendLine($"Event: {eventName}");
        text.AppendLine($"App version: {_appInfo.DisplayVersion}");
        text.AppendLine($"Process path: {Environment.ProcessPath ?? "(unknown)"}");
        text.AppendLine($"Process ID: {Environment.ProcessId}");
        text.AppendLine($"OS: {RuntimeInformation.OSDescription}");
        text.AppendLine($".NET: {RuntimeInformation.FrameworkDescription}");
        text.AppendLine($"Architecture: {RuntimeInformation.ProcessArchitecture}");

        if (!string.IsNullOrWhiteSpace(details))
        {
            text.AppendLine();
            text.AppendLine("Details:");
            text.AppendLine(details);
        }

        if (context is not null && context.Count > 0)
        {
            text.AppendLine();
            text.AppendLine("Context:");
            foreach (KeyValuePair<string, string?> pair in context.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                text.Append("  ");
                text.Append(pair.Key);
                text.Append(": ");
                text.AppendLine(FormatContextValue(pair.Value));
            }
        }

        if (exception is not null)
        {
            text.AppendLine();
            text.AppendLine("Exception:");
            text.AppendLine(exception.ToString());
        }

        return text.ToString();
    }

    private void PruneLogs()
    {
        try
        {
            var directory = new DirectoryInfo(LogDirectory);
            FileInfo[] oldLogs = directory
                .EnumerateFiles("*.log")
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .ThenByDescending(file => file.Name, StringComparer.Ordinal)
                .Skip(RetainedLogFiles)
                .ToArray();

            foreach (FileInfo log in oldLogs)
            {
                try
                {
                    log.Delete();
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }
        catch (DirectoryNotFoundException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (IOException)
        {
        }
    }

    private static string SanitizeFileName(string value)
    {
        var result = new StringBuilder(value.Length);
        char[] invalidChars = Path.GetInvalidFileNameChars();
        foreach (char ch in value)
        {
            if (invalidChars.Contains(ch))
            {
                result.Append('-');
            }
            else if (char.IsWhiteSpace(ch))
            {
                result.Append('-');
            }
            else
            {
                result.Append(char.ToLowerInvariant(ch));
            }
        }

        string sanitized = result.ToString().Trim('-');
        return sanitized.Length == 0 ? "event" : sanitized;
    }

    private static string FormatContextValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "(none)";
        }

        return value.Replace("\r", "\\r", StringComparison.Ordinal).Replace("\n", "\\n", StringComparison.Ordinal);
    }
}
