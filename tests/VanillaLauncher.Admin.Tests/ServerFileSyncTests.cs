using VanillaLauncher.Admin;
using Xunit;

namespace VanillaLauncher.Admin.Tests;

public class ServerFileSyncTests : IDisposable
{
    private readonly string _sourceRoot;
    private readonly string _serverRoot;

    public ServerFileSyncTests()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), "vlc-filesync-tests-" + Guid.NewGuid());
        _sourceRoot = Path.Combine(baseDir, "source");
        _serverRoot = Path.Combine(baseDir, "server");
        Directory.CreateDirectory(_sourceRoot);
        Directory.CreateDirectory(_serverRoot);
    }

    public void Dispose()
    {
        var baseDir = Path.GetDirectoryName(_sourceRoot)!;
        if (Directory.Exists(baseDir))
            Directory.Delete(baseDir, recursive: true);
    }

    private void WriteSource(string relativePath, string content)
    {
        var full = Path.Combine(_sourceRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
    }

    private void WriteServer(string relativePath, string content)
    {
        var full = Path.Combine(_serverRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
    }

    [Fact]
    public async Task MirrorAsync_CopiesNewFiles()
    {
        WriteSource("mods/a.jar", "content-a");

        await ServerFileSync.MirrorAsync(_sourceRoot, _serverRoot, new[] { "mods" });

        Assert.Equal("content-a", File.ReadAllText(Path.Combine(_serverRoot, "mods", "a.jar")));
    }

    [Fact]
    public async Task MirrorAsync_RemovesStaleFilesNotInSource()
    {
        WriteSource("mods/a.jar", "content-a");
        WriteServer("mods/old.jar", "stale content");

        await ServerFileSync.MirrorAsync(_sourceRoot, _serverRoot, new[] { "mods" });

        Assert.False(File.Exists(Path.Combine(_serverRoot, "mods", "old.jar")));
        Assert.True(File.Exists(Path.Combine(_serverRoot, "mods", "a.jar")));
    }

    [Fact]
    public async Task MirrorAsync_SkipsCopyWhenContentIdentical()
    {
        WriteSource("mods/a.jar", "same-content");
        WriteServer("mods/a.jar", "same-content");
        var targetPath = Path.Combine(_serverRoot, "mods", "a.jar");
        var beforeWrite = File.GetLastWriteTimeUtc(targetPath);

        await Task.Delay(50); // разница во времени должна быть измерима, если файл перезаписан
        await ServerFileSync.MirrorAsync(_sourceRoot, _serverRoot, new[] { "mods" });

        Assert.Equal(beforeWrite, File.GetLastWriteTimeUtc(targetPath));
    }

    [Fact]
    public async Task MirrorAsync_OverwritesChangedFiles()
    {
        WriteSource("mods/a.jar", "new-content");
        WriteServer("mods/a.jar", "old-content");

        await ServerFileSync.MirrorAsync(_sourceRoot, _serverRoot, new[] { "mods" });

        Assert.Equal("new-content", File.ReadAllText(Path.Combine(_serverRoot, "mods", "a.jar")));
    }

    [Fact]
    public async Task MirrorAsync_MissingSourceFolder_IsSkippedWithoutError()
    {
        await ServerFileSync.MirrorAsync(_sourceRoot, _serverRoot, new[] { "does-not-exist" });
        // не должно бросить исключение
    }

    [Fact]
    public async Task MirrorAsync_ExcludedFile_IsNotCopied()
    {
        WriteSource("mods/sodium.jar", "client-only-mod");
        WriteSource("mods/serverlogic.jar", "shared-mod");

        await ServerFileSync.MirrorAsync(_sourceRoot, _serverRoot, new[] { "mods" }, new[] { "sodium.jar" });

        Assert.False(File.Exists(Path.Combine(_serverRoot, "mods", "sodium.jar")));
        Assert.True(File.Exists(Path.Combine(_serverRoot, "mods", "serverlogic.jar")));
    }

    [Fact]
    public async Task MirrorAsync_ExcludedFile_RemovedFromServerIfAlreadyThere()
    {
        WriteSource("mods/sodium.jar", "client-only-mod");
        WriteServer("mods/sodium.jar", "stale copy from before exclusion existed");

        await ServerFileSync.MirrorAsync(_sourceRoot, _serverRoot, new[] { "mods" }, new[] { "sodium.jar" });

        Assert.False(File.Exists(Path.Combine(_serverRoot, "mods", "sodium.jar")));
    }

    [Fact]
    public async Task MirrorAsync_ExcludeIsCaseInsensitive()
    {
        WriteSource("mods/Sodium.JAR", "client-only-mod");

        await ServerFileSync.MirrorAsync(_sourceRoot, _serverRoot, new[] { "mods" }, new[] { "sodium.jar" });

        Assert.False(File.Exists(Path.Combine(_serverRoot, "mods", "Sodium.JAR")));
    }
}
