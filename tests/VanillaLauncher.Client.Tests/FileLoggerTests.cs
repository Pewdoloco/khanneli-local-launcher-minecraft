using VanillaLauncher.Client;
using Xunit;

namespace VanillaLauncher.Client.Tests;

public class FileLoggerTests : IDisposable
{
    private readonly string _dir;

    public FileLoggerTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "vlc-filelogger-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
        }
    }

    [Fact]
    public void Constructor_CreatesLogsDirectoryAndFile()
    {
        using var logger = new FileLogger(_dir);

        Assert.NotNull(logger.FilePath);
        Assert.True(File.Exists(logger.FilePath));
        Assert.Equal(Path.Combine(_dir, "logs"), Path.GetDirectoryName(logger.FilePath));
    }

    [Fact]
    public void Log_WritesLineToFile()
    {
        string filePath;
        using (var logger = new FileLogger(_dir))
        {
            logger.Log("hello world");
            filePath = logger.FilePath!;
        }

        var content = File.ReadAllText(filePath);
        Assert.Contains("hello world", content);
    }

    [Fact]
    public void Constructor_KeepsAtMostTwentyLogFiles()
    {
        var logsDir = Path.Combine(_dir, "logs");
        Directory.CreateDirectory(logsDir);
        for (var i = 0; i < 25; i++)
        {
            var path = Path.Combine(logsDir, $"launcher-existing-{i}.log");
            File.WriteAllText(path, "old");
            File.SetCreationTimeUtc(path, DateTime.UtcNow.AddMinutes(-i)); // больше i -> старее
        }

        using var logger = new FileLogger(_dir); // создаёт ещё один файл + чистит старые (см. FileLogger.MaxFilesToKeep = 20)

        var remaining = Directory.GetFiles(logsDir, "launcher-*.log");
        Assert.True(remaining.Length <= 20, $"Expected at most 20 log files, found {remaining.Length}");
    }

    [Fact]
    public void Constructor_BaseDirectoryIsActuallyAFile_DoesNotThrow_FilePathIsNull()
    {
        var blockerPath = Path.Combine(_dir, "blocker-file");
        File.WriteAllText(blockerPath, "not a directory");

        var logger = new FileLogger(blockerPath);

        Assert.Null(logger.FilePath);
        logger.Log("should not throw"); // no-op, не должно бросать
        logger.Dispose();
    }
}
