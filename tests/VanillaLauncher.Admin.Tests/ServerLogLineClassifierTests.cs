using VanillaLauncher.Admin;
using Xunit;

namespace VanillaLauncher.Admin.Tests;

public class ServerLogLineClassifierTests
{
    [Theory]
    [InlineData("[15:32:07] [Server thread/INFO]: Done (12.345s)! For help, type \"help\"", true)]
    [InlineData("Done (0.5s)! For help, type \"help\"", true)]
    [InlineData("Server starting...", false)]
    [InlineData("Done.", false)]
    public void IsSuccessLine_DetectsStandardReadyLine(string line, bool expected)
    {
        Assert.Equal(expected, ServerLogLineClassifier.IsSuccessLine(line));
    }

    [Theory]
    [InlineData("[15:32:07] [Server thread/ERROR]: something broke", true)]
    [InlineData("[15:32:07] [Server thread/FATAL]: something broke", true)]
    [InlineData("Exception in thread \"main\" java.lang.NoClassDefFoundError", true)]
    [InlineData("\tat some.package.Class.method(Class.java:10)", true)]
    [InlineData("Caused by: java.lang.RuntimeException", true)]
    [InlineData("Server starting...", false)]
    [InlineData("[15:32:07] [Server thread/INFO]: Done (12.345s)! For help, type \"help\"", false)]
    public void IsErrorLine_DetectsErrorIndicators(string line, bool expected)
    {
        Assert.Equal(expected, ServerLogLineClassifier.IsErrorLine(line));
    }
}
