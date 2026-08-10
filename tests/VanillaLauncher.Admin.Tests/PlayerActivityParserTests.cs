using VanillaLauncher.Admin;
using Xunit;

namespace VanillaLauncher.Admin.Tests;

public class PlayerActivityParserTests
{
    [Theory]
    [InlineData("[15:32:07] [Server thread/INFO]: Steve joined the game", "Steve")]
    [InlineData("[15:32:07] [Server thread/INFO]: Alex_2 joined the game", "Alex_2")]
    [InlineData("Notch joined the game", "Notch")]
    public void TryParseJoin_ExtractsName(string line, string expectedName)
    {
        Assert.Equal(expectedName, PlayerActivityParser.TryParseJoin(line));
    }

    [Theory]
    [InlineData("[15:34:10] [Server thread/INFO]: Steve left the game", "Steve")]
    [InlineData("Notch left the game", "Notch")]
    public void TryParseLeave_ExtractsName(string line, string expectedName)
    {
        Assert.Equal(expectedName, PlayerActivityParser.TryParseLeave(line));
    }

    [Theory]
    [InlineData("[15:32:07] [Server thread/INFO]: Done (12.345s)! For help, type \"help\"")]
    [InlineData("[15:33:00] [Server thread/INFO]: <Steve> hello everyone")]
    [InlineData("")]
    public void TryParseJoin_ReturnsNullForNonMatchingLines(string line)
    {
        Assert.Null(PlayerActivityParser.TryParseJoin(line));
        Assert.Null(PlayerActivityParser.TryParseLeave(line));
    }
}
