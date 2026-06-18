using HylianGrimoire.Diagnostics;
using Xunit;

namespace HylianGrimoire.Tests;

public sealed class DiagnosticLogServiceTests
{
    [Fact]
    public void WriteEvent_WritesRuntimeContextAndException()
    {
        using var scope = new TestDirectory();
        var service = new DiagnosticLogService(
            new DiagnosticAppInfo("Hylian Grimoire", "3.0.4"),
            scope.Path,
            retainedLogFiles: 5);

        string logPath = service.WriteEvent(
            "Handled exception",
            new InvalidOperationException("preview failed"),
            new Dictionary<string, string?>
            {
                ["Document kind"] = "Rom",
                ["Current message ID"] = "0x1234",
                ["Empty"] = null,
            },
            "while rendering preview");

        string text = File.ReadAllText(logPath);
        Assert.Contains("Hylian Grimoire diagnostic log", text);
        Assert.Contains("Event: Handled exception", text);
        Assert.Contains("App version: 3.0.4", text);
        Assert.Contains("Document kind: Rom", text);
        Assert.Contains("Current message ID: 0x1234", text);
        Assert.Contains("Empty: (none)", text);
        Assert.Contains("while rendering preview", text);
        Assert.Contains("System.InvalidOperationException: preview failed", text);
    }

    [Fact]
    public void WriteEvent_PrunesOldLogs()
    {
        using var scope = new TestDirectory();
        string oldLog1 = System.IO.Path.Combine(scope.Path, "old-1.log");
        string oldLog2 = System.IO.Path.Combine(scope.Path, "old-2.log");
        Directory.CreateDirectory(scope.Path);
        File.WriteAllText(oldLog1, "old");
        File.WriteAllText(oldLog2, "old");
        File.SetLastWriteTimeUtc(oldLog1, DateTime.UtcNow.AddMinutes(-10));
        File.SetLastWriteTimeUtc(oldLog2, DateTime.UtcNow.AddMinutes(-9));

        var service = new DiagnosticLogService(
            new DiagnosticAppInfo("Hylian Grimoire", "3.0.4"),
            scope.Path,
            retainedLogFiles: 2);

        string newLog = service.WriteEvent("new event", null);

        string[] logs = Directory.GetFiles(scope.Path, "*.log");
        Assert.Equal(2, logs.Length);
        Assert.Contains(newLog, logs);
        Assert.DoesNotContain(oldLog1, logs);
    }

    private sealed class TestDirectory : IDisposable
    {
        public TestDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "HylianGrimoireTests",
                Guid.NewGuid().ToString("N"));
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
