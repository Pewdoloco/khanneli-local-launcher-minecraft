using VanillaLauncher.Admin;
using Xunit;

namespace VanillaLauncher.Admin.Tests;

public class ForgeModStateTableParserTests
{
    [Fact]
    public void FindFailedModJars_ExtractsJarWithErrorStatus()
    {
        var lines = new[]
        {
            "\t\tVoidFog-1.20.1-2.0.23.jar                         |VoidFog                       |voidfog                       |1.20.1-2.0.23       |ERROR     |Manifest: NOSIGNATURE",
        };

        var result = ForgeModStateTableParser.FindFailedModJars(lines);

        Assert.Equal(new[] { "VoidFog-1.20.1-2.0.23.jar" }, result);
    }

    [Fact]
    public void FindFailedModJars_IgnoresRowsWithoutErrorStatus()
    {
        var lines = new[]
        {
            "\t\tSomeMod-1.0.0.jar                                  |SomeMod                       |somemod                       |1.0.0               |DONE      |Manifest: NOSIGNATURE",
        };

        var result = ForgeModStateTableParser.FindFailedModJars(lines);

        Assert.Empty(result);
    }

    [Fact]
    public void FindFailedModJars_IgnoresErrorSubstringOutsideTableFormat()
    {
        // "ERROR" встречается сплошь и рядом в обычных лог-строках — не таблица загрузки модов,
        // не должно давать ложных срабатываний.
        var lines = new[]
        {
            "[main/ERROR] [some.Logger/]: something went wrong, but this isn't the mod table",
        };

        var result = ForgeModStateTableParser.FindFailedModJars(lines);

        Assert.Empty(result);
    }

    [Fact]
    public void FindFailedModJars_ReturnsDistinctJarsAcrossMultipleMatchingLines()
    {
        var lines = new[]
        {
            "VoidFog-1.20.1-2.0.23.jar|VoidFog|voidfog|1.20.1-2.0.23|ERROR|Manifest: NOSIGNATURE",
            "VoidFog-1.20.1-2.0.23.jar|VoidFog|voidfog|1.20.1-2.0.23|ERROR|Manifest: NOSIGNATURE",
            "OtherMod-2.0.0.jar|OtherMod|othermod|2.0.0|ERROR|Manifest: NOSIGNATURE",
            "GoodMod-1.0.0.jar|GoodMod|goodmod|1.0.0|DONE|Manifest: NOSIGNATURE",
        };

        var result = ForgeModStateTableParser.FindFailedModJars(lines);

        Assert.Equal(2, result.Count);
        Assert.Contains("VoidFog-1.20.1-2.0.23.jar", result);
        Assert.Contains("OtherMod-2.0.0.jar", result);
    }

    [Fact]
    public void FindFailedModJars_IgnoresFirstColumnNotEndingInJar()
    {
        var lines = new[]
        {
            "not-a-jar-file|Something|somemodid|1.0.0|ERROR|Manifest: NOSIGNATURE",
        };

        var result = ForgeModStateTableParser.FindFailedModJars(lines);

        Assert.Empty(result);
    }

    [Fact]
    public void FindFailedModJars_EmptyInput_ReturnsEmpty()
    {
        var result = ForgeModStateTableParser.FindFailedModJars(Array.Empty<string>());

        Assert.Empty(result);
    }
}
