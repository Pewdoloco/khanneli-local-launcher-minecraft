using VanillaLauncher.Client;
using Xunit;

namespace VanillaLauncher.Client.Tests;

public class UpdateServiceTests : IDisposable
{
    private readonly string _profileRoot;

    public UpdateServiceTests()
    {
        _profileRoot = Path.Combine(Path.GetTempPath(), "vlc-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_profileRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_profileRoot))
            Directory.Delete(_profileRoot, recursive: true);
    }

    private async Task<ManifestFileEntry> WriteLocalFileAsync(string relativePath, string content)
    {
        var fullPath = Path.Combine(_profileRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(fullPath, content);

        var hash = await HashService.ComputeSha256Async(fullPath);
        return new ManifestFileEntry
        {
            Path = relativePath,
            Sha256 = hash,
            Size = new FileInfo(fullPath).Length,
            Url = $"https://example.invalid/{relativePath}"
        };
    }

    [Fact]
    public async Task BuildPlanAsync_FileMatchesHash_IsUpToDate()
    {
        var entry = await WriteLocalFileAsync("mods/example.jar", "mod-content");
        var manifest = new Manifest { Files = { entry } };

        var plan = await new UpdateService(_profileRoot).BuildPlanAsync(manifest);

        Assert.Equal(FileAction.UpToDate, Assert.Single(plan).Action);
    }

    [Fact]
    public async Task BuildPlanAsync_FileMissing_NeedsDownload()
    {
        var manifest = new Manifest
        {
            Files =
            {
                new ManifestFileEntry { Path = "mods/missing.jar", Sha256 = "deadbeef", Size = 1, Url = "https://example.invalid/missing.jar" }
            }
        };

        var plan = await new UpdateService(_profileRoot).BuildPlanAsync(manifest);

        Assert.Equal(FileAction.NeedsDownload, Assert.Single(plan).Action);
    }

    [Fact]
    public async Task BuildPlanAsync_FileContentChanged_NeedsDownload()
    {
        var entry = await WriteLocalFileAsync("config/settings.json", "original");
        // Simulate the manifest describing a newer version of the file than what's on disk.
        await File.WriteAllTextAsync(Path.Combine(_profileRoot, entry.Path), "modified locally");

        var manifest = new Manifest { Files = { entry } };

        var plan = await new UpdateService(_profileRoot).BuildPlanAsync(manifest);

        Assert.Equal(FileAction.NeedsDownload, Assert.Single(plan).Action);
    }

    [Fact]
    public async Task BuildPlanAsync_MixedState_ReturnsOneItemPerManifestEntry()
    {
        var upToDate = await WriteLocalFileAsync("mods/a.jar", "same");
        var stale = await WriteLocalFileAsync("mods/b.jar", "old");
        await File.WriteAllTextAsync(Path.Combine(_profileRoot, stale.Path), "new");
        var missing = new ManifestFileEntry { Path = "mods/c.jar", Sha256 = "deadbeef", Size = 1, Url = "https://example.invalid/c.jar" };

        var manifest = new Manifest { Files = { upToDate, stale, missing } };

        var plan = await new UpdateService(_profileRoot).BuildPlanAsync(manifest);

        Assert.Equal(3, plan.Count);
        Assert.Equal(FileAction.UpToDate, plan.Single(p => p.Entry.Path == "mods/a.jar").Action);
        Assert.Equal(FileAction.NeedsDownload, plan.Single(p => p.Entry.Path == "mods/b.jar").Action);
        Assert.Equal(FileAction.NeedsDownload, plan.Single(p => p.Entry.Path == "mods/c.jar").Action);
    }
}
