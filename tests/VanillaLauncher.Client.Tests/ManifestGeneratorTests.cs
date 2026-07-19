using VanillaLauncher.Client;
using Xunit;

namespace VanillaLauncher.Client.Tests;

public class ManifestGeneratorTests : IDisposable
{
    private readonly string _sourceRoot;

    public ManifestGeneratorTests()
    {
        _sourceRoot = Path.Combine(Path.GetTempPath(), "vlc-manifestgen-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(Path.Combine(_sourceRoot, "mods"));
        Directory.CreateDirectory(Path.Combine(_sourceRoot, "config", "sub"));
        Directory.CreateDirectory(Path.Combine(_sourceRoot, "saves")); // не должно попасть в манифест

        File.WriteAllText(Path.Combine(_sourceRoot, "mods", "a.jar"), "mod-a");
        File.WriteAllText(Path.Combine(_sourceRoot, "config", "sub", "c.json"), "config-c");
        File.WriteAllText(Path.Combine(_sourceRoot, "saves", "world.dat"), "should-be-ignored");
    }

    public void Dispose()
    {
        if (Directory.Exists(_sourceRoot))
            Directory.Delete(_sourceRoot, recursive: true);
    }

    [Fact]
    public async Task GenerateAsync_OnlyIncludesListedFolders()
    {
        var manifest = await ManifestGenerator.GenerateAsync(
            _sourceRoot, new[] { "mods", "config" }, "v1", path => $"https://example.invalid/{path}");

        Assert.Equal(2, manifest.Files.Count);
        Assert.Contains(manifest.Files, f => f.Path == "mods/a.jar");
        Assert.Contains(manifest.Files, f => f.Path == "config/sub/c.json");
        Assert.DoesNotContain(manifest.Files, f => f.Path.Contains("saves"));
    }

    [Fact]
    public async Task GenerateAsync_ComputesCorrectHashAndUsesUrlCallback()
    {
        var manifest = await ManifestGenerator.GenerateAsync(
            _sourceRoot, new[] { "mods" }, "v1", path => $"flat:{ManifestGenerator.FlattenPathForAssetName(path)}");

        var entry = Assert.Single(manifest.Files);
        Assert.Equal("mods/a.jar", entry.Path);
        Assert.Equal(await HashService.ComputeSha256Async(Path.Combine(_sourceRoot, "mods", "a.jar")), entry.Sha256);
        Assert.Equal("flat:mods_a.jar", entry.Url);
    }

    [Fact]
    public async Task GenerateAsync_MissingFolder_IsSkippedWithoutError()
    {
        var manifest = await ManifestGenerator.GenerateAsync(
            _sourceRoot, new[] { "mods", "does-not-exist" }, "v1", path => path);

        Assert.Single(manifest.Files);
    }

    [Theory]
    [InlineData("mods/a.jar", "mods_a.jar")]
    [InlineData("config/sub/c.json", "config_sub_c.json")]
    [InlineData("plain.txt", "plain.txt")]
    public void FlattenPathForAssetName_ReplacesSlashes(string input, string expected)
    {
        Assert.Equal(expected, ManifestGenerator.FlattenPathForAssetName(input));
    }
}
