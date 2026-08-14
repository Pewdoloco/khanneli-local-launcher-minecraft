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
    [InlineData("echo [ERROR]: NoClassDefFoundError: some.client.OnlyMod", true)]
    [InlineData("Exception in thread \"main\" java.lang.NoClassDefFoundError", true)]
    [InlineData("\tat some.package.Class.method(Class.java:10)", true)]
    [InlineData("Caused by: java.lang.RuntimeException", true)]
    [InlineData("VoidFog-1.20.1-2.0.23.jar |VoidFog |voidfog |1.20.1-2.0.23 |ERROR |Manifest: NOSIGNATURE", true)]
    [InlineData("java.util.concurrent.ExecutionException: java.lang.AbstractMethodError: Receiver class apoli.util.PowerRestrictedCraftingRecipe does not define or inherit an implementation of the resolved method", true)]
    [InlineData("java.lang.NoClassDefFoundError: net/minecraft/client/MouseHandler", true)]
    [InlineData("Server starting...", false)]
    [InlineData("[15:32:07] [Server thread/INFO]: Done (12.345s)! For help, type \"help\"", false)]
    public void IsErrorLine_DetectsErrorIndicators(string line, bool expected)
    {
        Assert.Equal(expected, ServerLogLineClassifier.IsErrorLine(line));
    }

    [Theory]
    [InlineData(
        "[18:41:29] [main/WARN] [mixin/]: Error loading class: net/minecraft/client/MouseHandler (java.lang.RuntimeException: Attempted to load class net/minecraft/client/MouseHandler for invalid dist DEDICATED_SERVER)")]
    [InlineData("[main/WARN] [some.Logger/]: something about an Exception, but this is just a warning")]
    [InlineData("[main/INFO] [some.Logger/]: totally normal info line")]
    public void IsErrorLine_IgnoresWarnAndInfoLinesEvenWhenTheyMentionExceptions(string line)
    {
        // Реальный баг из живого прогона: старая проверка ловила голое Contains("Exception"),
        // из-за чего WARN-строки, которые просто УПОМИНАЮТ исключение в своём тексте (не сами
        // являются ошибкой), попадали в панель "Ошибки". Пользователь явно попросил — только
        // реальные ошибки, предупреждения не нужны.
        Assert.False(ServerLogLineClassifier.IsErrorLine(line));
    }
}
