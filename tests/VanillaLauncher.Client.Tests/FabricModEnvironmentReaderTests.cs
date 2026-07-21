using System.IO.Compression;
using VanillaLauncher.Client;
using Xunit;

namespace VanillaLauncher.Client.Tests;

public class FabricModEnvironmentReaderTests : IDisposable
{
    private readonly string _dir;

    public FabricModEnvironmentReaderTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "vlc-fabricenv-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    private string CreateJar(string fileName, string? fabricModJson)
    {
        var path = Path.Combine(_dir, fileName);
        using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            if (fabricModJson is not null)
            {
                var entry = archive.CreateEntry("fabric.mod.json");
                using var writer = new StreamWriter(entry.Open());
                writer.Write(fabricModJson);
            }
        }
        return path;
    }

    [Fact]
    public void TryGetEnvironment_ClientOnly_ReturnsClient()
    {
        var jar = CreateJar("client-only.jar", """{"environment": "client"}""");
        Assert.Equal("client", FabricModEnvironmentReader.TryGetEnvironment(jar));
        Assert.True(FabricModEnvironmentReader.IsClientOnly(jar));
    }

    [Fact]
    public void TryGetEnvironment_Universal_ReturnsStarNotClientOnly()
    {
        var jar = CreateJar("universal.jar", """{"environment": "*"}""");
        Assert.Equal("*", FabricModEnvironmentReader.TryGetEnvironment(jar));
        Assert.False(FabricModEnvironmentReader.IsClientOnly(jar));
    }

    [Fact]
    public void TryGetEnvironment_MissingField_ReturnsNull()
    {
        var jar = CreateJar("no-env-field.jar", """{"id": "somemod"}""");
        Assert.Null(FabricModEnvironmentReader.TryGetEnvironment(jar));
        Assert.False(FabricModEnvironmentReader.IsClientOnly(jar));
    }

    [Fact]
    public void TryGetEnvironment_MissingFabricModJson_ReturnsNull()
    {
        var jar = CreateJar("not-fabric.jar", fabricModJson: null);
        Assert.Null(FabricModEnvironmentReader.TryGetEnvironment(jar));
    }

    [Fact]
    public void TryGetEnvironment_NotAZipFile_ReturnsNullInsteadOfThrowing()
    {
        var path = Path.Combine(_dir, "corrupt.jar");
        File.WriteAllText(path, "not actually a zip");
        Assert.Null(FabricModEnvironmentReader.TryGetEnvironment(path));
    }
}
