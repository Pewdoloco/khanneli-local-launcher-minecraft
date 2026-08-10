using VanillaLauncher.Admin;
using Xunit;

namespace VanillaLauncher.Admin.Tests;

public class ServerSmokeTestRunnerTests : IDisposable
{
    private readonly string _serverDir;

    public ServerSmokeTestRunnerTests()
    {
        _serverDir = Path.Combine(Path.GetTempPath(), "vlc-smoketest-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_serverDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_serverDir))
        {
            try { Directory.Delete(_serverDir, recursive: true); } catch { /* фоновый процесс мог не успеть отпустить хендл */ }
        }
    }

    private void WriteBat(string name, params string[] lines) =>
        File.WriteAllText(Path.Combine(_serverDir, name), string.Join("\r\n", lines));

    [Fact]
    public async Task RunAsync_ServerReachesDoneLine_ReturnsSuccessAndStopsServer()
    {
        WriteBat("ok.bat",
            "@echo off",
            "echo Server starting...",
            "echo Done (0.001s)! For help, type \"help\"",
            ":loop",
            "set /p CMD=",
            "if /I \"%CMD%\"==\"stop\" goto :stopping",
            "goto :loop",
            ":stopping",
            "echo Done.",
            "pause");

        var controller = new ServerProcessController(_serverDir, "ok.bat");
        var result = await new ServerSmokeTestRunner().RunAsync(
            controller, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5));

        Assert.Equal(ServerSmokeTestOutcome.Success, result.Outcome);
        Assert.False(controller.IsRunning);
    }

    [Fact]
    public async Task RunAsync_ProcessExitsBeforeDoneLine_ReturnsCrashedWithErrorLines()
    {
        WriteBat("crash.bat",
            "@echo off",
            "echo Server starting...",
            "echo [ERROR]: NoClassDefFoundError",
            "exit /b 1");

        var controller = new ServerProcessController(_serverDir, "crash.bat");
        var result = await new ServerSmokeTestRunner().RunAsync(
            controller, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5));

        Assert.Equal(ServerSmokeTestOutcome.Crashed, result.Outcome);
        Assert.Contains(result.ErrorLines, l => l.Contains("NoClassDefFoundError"));
    }

    [Fact]
    public async Task RunAsync_NeverReachesDoneLine_TimesOutAndKillsProcess()
    {
        WriteBat("hang.bat",
            "@echo off",
            "echo Server starting...",
            ":loop",
            "set /p CMD=",
            "goto :loop");

        var controller = new ServerProcessController(_serverDir, "hang.bat");
        var result = await new ServerSmokeTestRunner().RunAsync(
            controller, TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(5));

        Assert.Equal(ServerSmokeTestOutcome.TimedOut, result.Outcome);

        for (var i = 0; i < 20 && controller.IsRunning; i++)
            await Task.Delay(100);
        Assert.False(controller.IsRunning);
    }
}
