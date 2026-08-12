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

    [Fact]
    public void GetServerIp_NoFile_ReturnsNull()
    {
        Assert.Null(ServerPropertiesReader.GetServerIp(_dir));
    }

    [Fact]
    public void GetServerIp_EmptyValue_ReturnsNull()
    {
        // Ванильный дефолт — сервер слушает на всех интерфейсах, не значит "не настроено".
        File.WriteAllText(Path.Combine(_dir, "server.properties"), "server-ip=\r\n");

        Assert.Null(ServerPropertiesReader.GetServerIp(_dir));
    }

    [Fact]
    public void GetServerIp_ReadsCustomValue()
    {
        File.WriteAllText(Path.Combine(_dir, "server.properties"), "server-ip=100.64.0.5\r\n");

        Assert.Equal("100.64.0.5", ServerPropertiesReader.GetServerIp(_dir));
    }

    [Fact]
    public void GetServerPort_NoFile_ReturnsNull()
    {
        Assert.Null(ServerPropertiesReader.GetServerPort(_dir));
    }

    [Fact]
    public void GetServerPort_ReadsCustomValue()
    {
        File.WriteAllText(Path.Combine(_dir, "server.properties"), "server-port=25599\r\n");

        Assert.Equal(25599, ServerPropertiesReader.GetServerPort(_dir));
    }

    [Fact]
    public void GetServerPort_NotANumber_ReturnsNull()
    {
        File.WriteAllText(Path.Combine(_dir, "server.properties"), "server-port=abc\r\n");

        Assert.Null(ServerPropertiesReader.GetServerPort(_dir));
    }

    [Fact]
    public void SetServerAddress_UpdatesExistingKeys_PreservesOtherLines()
    {
        var path = Path.Combine(_dir, "server.properties");
        File.WriteAllText(path, "#comment\r\nmotd=hi\r\nserver-ip=\r\nserver-port=25565\r\ngamemode=survival\r\n");

        ServerPropertiesReader.SetServerAddress(_dir, "100.64.0.5", 25599);

        var lines = File.ReadAllLines(path);
        Assert.Equal(new[] { "#comment", "motd=hi", "server-ip=100.64.0.5", "server-port=25599", "gamemode=survival" }, lines);
    }

    [Fact]
    public void SetServerAddress_KeysMissing_AppendsThem()
    {
        var path = Path.Combine(_dir, "server.properties");
        File.WriteAllText(path, "motd=hi\r\n");

        ServerPropertiesReader.SetServerAddress(_dir, "100.64.0.5", 25599);

        Assert.Equal("100.64.0.5", ServerPropertiesReader.GetServerIp(_dir));
        Assert.Equal(25599, ServerPropertiesReader.GetServerPort(_dir));
        Assert.Contains("motd=hi", File.ReadAllLines(path));
    }

    [Fact]
    public void SetServerAddress_NullIp_WritesEmptyValue()
    {
        var path = Path.Combine(_dir, "server.properties");
        File.WriteAllText(path, "server-ip=100.64.0.5\r\nserver-port=25565\r\n");

        ServerPropertiesReader.SetServerAddress(_dir, null, 25565);

        Assert.Null(ServerPropertiesReader.GetServerIp(_dir));
    }
}
