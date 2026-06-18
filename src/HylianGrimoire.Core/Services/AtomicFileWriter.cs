using System.Runtime.ExceptionServices;
using System.Text;

namespace HylianGrimoire.Services;

public sealed record AtomicFileWrite(string Path, byte[] Contents);

public static class AtomicFileWriter
{
    private const string TempMarker = ".hylian-write-";

    public static void WriteAllBytes(string path, byte[] contents)
    {
        WriteAllBytesBatch([new AtomicFileWrite(path, contents)]);
    }

    public static void WriteAllText(string path, string contents)
    {
        WriteAllText(path, contents, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    public static void WriteAllText(string path, string contents, Encoding encoding)
    {
        WriteAllBytes(path, encoding.GetBytes(contents));
    }

    public static void WriteAllBytesBatch(IReadOnlyList<AtomicFileWrite> writes)
    {
        if (writes.Count == 0)
        {
            return;
        }

        List<PendingWrite> pendingWrites = writes.Select(CreatePendingWrite).ToList();
        var committedWrites = new List<PendingWrite>();

        try
        {
            foreach (PendingWrite pendingWrite in pendingWrites)
            {
                WriteTempFile(pendingWrite.TempPath, pendingWrite.Contents);
            }

            foreach (PendingWrite pendingWrite in pendingWrites)
            {
                Commit(pendingWrite);
                committedWrites.Add(pendingWrite);
            }
        }
        catch (Exception ex)
        {
            ExceptionDispatchInfo captured = ExceptionDispatchInfo.Capture(ex);
            RollBack(committedWrites);
            captured.Throw();
        }
        finally
        {
            foreach (PendingWrite pendingWrite in pendingWrites)
            {
                TryDeleteFile(pendingWrite.TempPath);
                TryDeleteFile(pendingWrite.BackupPath);
            }
        }
    }

    private static PendingWrite CreatePendingWrite(AtomicFileWrite write)
    {
        string destinationPath = Path.GetFullPath(write.Path);
        string directory = Path.GetDirectoryName(destinationPath)
            ?? throw new InvalidOperationException($"Could not determine the destination folder for {destinationPath}.");
        Directory.CreateDirectory(directory);

        string fileName = Path.GetFileName(destinationPath);
        string token = Guid.NewGuid().ToString("N");
        return new PendingWrite(
            destinationPath,
            Path.Combine(directory, $"{fileName}{TempMarker}{token}.tmp"),
            Path.Combine(directory, $"{fileName}{TempMarker}{token}.bak"),
            write.Contents);
    }

    private static void WriteTempFile(string tempPath, byte[] contents)
    {
        using var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        stream.Write(contents, 0, contents.Length);
        stream.Flush(flushToDisk: true);
    }

    private static void Commit(PendingWrite pendingWrite)
    {
        if (File.Exists(pendingWrite.DestinationPath))
        {
            File.Replace(pendingWrite.TempPath, pendingWrite.DestinationPath, pendingWrite.BackupPath, ignoreMetadataErrors: true);
            return;
        }

        File.Move(pendingWrite.TempPath, pendingWrite.DestinationPath);
    }

    private static void RollBack(List<PendingWrite> committedWrites)
    {
        for (int i = committedWrites.Count - 1; i >= 0; i--)
        {
            PendingWrite committedWrite = committedWrites[i];
            if (File.Exists(committedWrite.BackupPath))
            {
                RestoreBackup(committedWrite);
            }
            else
            {
                TryDeleteFile(committedWrite.DestinationPath);
            }
        }
    }

    private static void RestoreBackup(PendingWrite committedWrite)
    {
        try
        {
            if (File.Exists(committedWrite.DestinationPath))
            {
                File.Replace(committedWrite.BackupPath, committedWrite.DestinationPath, null, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(committedWrite.BackupPath, committedWrite.DestinationPath);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private sealed record PendingWrite(string DestinationPath, string TempPath, string BackupPath, byte[] Contents);
}
