using System.Diagnostics;
using HylianGrimoire.Diagnostics;
using Microsoft.UI.Xaml;

namespace HylianGrimoire;

internal static class AppDiagnostics
{
    private static readonly object Sync = new();
    private static readonly Dictionary<string, string?> Context = new(StringComparer.Ordinal);
    private static bool _initialized;
    private static DiagnosticLogService? _logService;

    public static string LogDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        AppMetadata.DisplayName,
        "Logs");

    public static void Initialize(Application app)
    {
        lock (Sync)
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            _logService = new DiagnosticLogService(
                new DiagnosticAppInfo(AppMetadata.DisplayName, AppMetadata.DisplayVersion),
                LogDirectory);
            Context["Log directory"] = LogDirectory;
        }

        app.UnhandledException += OnApplicationUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledDomainException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    public static void UpdateStatus(string status)
    {
        SetContextValue("Last status", status);
    }

    public static void UpdateDocumentContext(
        string documentKind,
        string? game,
        string? documentPath,
        string? romProfile,
        string? romFormat,
        string? romSection,
        string? activeLanguage,
        string? characterProfile,
        int entryCount,
        int? currentEntryIndex,
        string? currentMessageId,
        bool hasUnsavedChanges)
    {
        lock (Sync)
        {
            Context["Document kind"] = documentKind;
            Context["Game"] = game;
            Context["Document path"] = documentPath;
            Context["ROM profile"] = romProfile;
            Context["ROM format"] = romFormat;
            Context["ROM section"] = romSection;
            Context["Active language"] = activeLanguage;
            Context["Character profile"] = characterProfile;
            Context["Entry count"] = entryCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Context["Current entry index"] = currentEntryIndex?.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Context["Current message ID"] = currentMessageId;
            Context["Has unsaved changes"] = hasUnsavedChanges ? "true" : "false";
        }
    }

    public static string? LogHandledException(string title, Exception exception)
    {
        return TryWriteEvent($"Handled exception - {title}", exception);
    }

    public static void OpenLogDirectory()
    {
        Directory.CreateDirectory(LogDirectory);
        Process.Start(new ProcessStartInfo
        {
            FileName = LogDirectory,
            UseShellExecute = true,
        });
    }

    private static void OnApplicationUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs args)
    {
        string details = $"Message: {args.Message}{Environment.NewLine}Handled before logger: {args.Handled}";
        TryWriteEvent("WinUI unhandled exception", args.Exception, details);
    }

    private static void OnUnhandledDomainException(object sender, System.UnhandledExceptionEventArgs args)
    {
        string details = $"Runtime terminating: {args.IsTerminating}";
        if (args.ExceptionObject is Exception exception)
        {
            TryWriteEvent("AppDomain unhandled exception", exception, details);
            return;
        }

        TryWriteEvent("AppDomain unhandled exception", null, $"{details}{Environment.NewLine}{args.ExceptionObject}");
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        TryWriteEvent("Unobserved task exception", args.Exception);
        args.SetObserved();
    }

    private static string? TryWriteEvent(string eventName, Exception? exception, string? details = null)
    {
        try
        {
            DiagnosticLogService? service;
            Dictionary<string, string?> context;
            lock (Sync)
            {
                service = _logService;
                context = new Dictionary<string, string?>(Context, StringComparer.Ordinal);
            }

            return service?.WriteEvent(eventName, exception, context, details);
        }
        catch
        {
            return null;
        }
    }

    private static void SetContextValue(string key, string? value)
    {
        lock (Sync)
        {
            Context[key] = value;
        }
    }
}
