using VanillaLauncher.Admin;
using Xunit;

namespace VanillaLauncher.Admin.Tests;

public class ServerPropertiesReaderTests : IDisposable
{
    private readonly string _dir;

    public ServerPropertiesReaderTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "vlc-props-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void GetLevelName_NoFile_ReturnsDefault()
    {
        Assert.Equal("world", ServerPropertiesReader.GetLevelName(_dir));
    }

    [Fact]
    public void GetLevelName_ReadsCustomValue()
    {
        File.WriteAllText(Path.Combine(_dir, "server.properties"), "motd=hi\r\nlevel-name=my_custom_world\r\ngamemode=survival\r\n");

        Assert.Equal("my_custom_world", ServerPropertiesReader.GetLevelName(_dir));
    }

    [Fact]
    public void GetLevelName_EmptyValue_ReturnsDefault()
    {
        File.WriteAllText(Path.Combine(_dir, "server.properties"), "level-name=\r\n");

        Assert.Equal("world", ServerPropertiesReader.GetLevelName(_dir));
    }
}
