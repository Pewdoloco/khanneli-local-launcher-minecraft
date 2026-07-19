using System.IO.Compression;
using VanillaLauncher.Admin;
using Xunit;

namespace VanillaLauncher.Admin.Tests;

public class WorldBackupServiceTests : IDisposable
{
    private readonly string _serverDir;
    private readonly string _backupsDir;

    public WorldBackupServiceTests()
    {
        _serverDir = Path.Combine(Path.GetTempPath(), "vlc-world-tests-" + Guid.NewGuid());
        _backupsDir = Path.Combine(_serverDir, "backups");
        Directory.CreateDirectory(_serverDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_serverDir))
            Directory.Delete(_serverDir, recursive: true);
    }

    private void CreateFakeWorld(string levelName, string marker)
    {
        var worldDir = Path.Combine(_serverDir, levelName);
        Directory.CreateDirectory(Path.Combine(worldDir, "region"));
        File.WriteAllText(Path.Combine(worldDir, "level.dat"), marker);
        File.WriteAllText(Path.Combine(worldDir, "region", "r.0.0.mca"), "fake chunk data");
    }

    [Fact]
    public void BackupWorld_NoWorldFolder_ReturnsNull()
    {
        var service = new WorldBackupService(_serverDir, _backupsDir, maxBackupsToKeep: 5);

        var result = service.BackupWorld("world");

        Assert.Null(result);
    }

    [Fact]
    public void BackupWorld_CreatesZipWithWorldContents()
    {
        CreateFakeWorld("world", "marker-content");
        var service = new WorldBackupService(_serverDir, _backupsDir, maxBackupsToKeep: 5);

        var backupPath = service.BackupWorld("world");

        Assert.NotNull(backupPath);
        Assert.True(File.Exists(backupPath));

        using var archive = ZipFile.OpenRead(backupPath);
        var levelEntry = archive.GetEntry("level.dat");
        Assert.NotNull(levelEntry);

        using var reader = new StreamReader(levelEntry.Open());
        Assert.Equal("marker-content", reader.ReadToEnd());

        // Оригинальная папка мира не тронута.
        Assert.True(Directory.Exists(Path.Combine(_serverDir, "world")));
    }

    [Fact]
    public void BackupWorld_RotatesOldBackupsBeyondLimit()
    {
        CreateFakeWorld("world", "v1");
        var service = new WorldBackupService(_serverDir, _backupsDir, maxBackupsToKeep: 2);

        service.BackupWorld("world");
        service.BackupWorld("world");
        service.BackupWorld("world");

        var remaining = Directory.GetFiles(_backupsDir, "world_*.zip");
        Assert.Equal(2, remaining.Length);
    }

    [Fact]
    public void RecreateWorld_BacksUpThenDeletesWorldFolder()
    {
        CreateFakeWorld("world", "before-recreate");
        var service = new WorldBackupService(_serverDir, _backupsDir, maxBackupsToKeep: 5);

        var backupPath = service.RecreateWorld("world");

        Assert.NotNull(backupPath);
        Assert.True(File.Exists(backupPath));
        Assert.False(Directory.Exists(Path.Combine(_serverDir, "world")));

        using var archive = ZipFile.OpenRead(backupPath);
        Assert.NotNull(archive.GetEntry("level.dat"));
    }

    [Fact]
    public void RecreateWorld_NoExistingWorld_ReturnsNullAndDoesNotThrow()
    {
        var service = new WorldBackupService(_serverDir, _backupsDir, maxBackupsToKeep: 5);

        var backupPath = service.RecreateWorld("world");

        Assert.Null(backupPath);
    }

    [Fact]
    public void Constructor_RejectsNonPositiveMaxBackups()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new WorldBackupService(_serverDir, _backupsDir, maxBackupsToKeep: 0));
    }
}
