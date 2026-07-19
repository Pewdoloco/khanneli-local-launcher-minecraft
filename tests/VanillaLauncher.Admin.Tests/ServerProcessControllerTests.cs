using VanillaLauncher.Admin;
using Xunit;

namespace VanillaLauncher.Admin.Tests;

/// <summary>
/// Настоящий Minecraft-сервер здесь не поднимаем — вместо него синтетический .bat,
/// который так же блокируется на чтении команд из stdin и завершается "pause".
/// Это проверяет именно контракт обёртки (старт, лог, "stop" -> граceful exit),
/// а не сам сервер.
/// </summary>
public class ServerProcessControllerTests : IDisposable
{
    private readonly string _serverDir;

    public ServerProcessControllerTests()
    {
        _serverDir = Path.Combine(Path.GetTempPath(), "vlc-admin-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_serverDir);

        var batContent = string.Join("\r\n", new[]
        {
            "@echo off",
            "echo Server starting...",
            ":loop",
            "set /p CMD=",
            "if /I \"%CMD%\"==\"stop\" goto :stopping",
            "echo unknown command: %CMD%",
            "goto :loop",
            ":stopping",
            "echo Stopping server...",
            "echo Done.",
            "pause"
        });

        File.WriteAllText(Path.Combine(_serverDir, "fake_server.bat"), batContent);
    }

    public void Dispose()
    {
        if (Directory.Exists(_serverDir))
            Directory.Delete(_serverDir, recursive: true);
    }

    [Fact]
    public async Task Start_CapturesOutput_AndReportsRunning()
    {
        using var controller = new ServerProcessController(_serverDir, "fake_server.bat");
        var lines = new List<string>();
        controller.OutputReceived += line => { lock (lines) lines.Add(line); };

        controller.Start();

        Assert.True(await WaitUntilAsync(() => lines.Any(l => l.Contains("Server starting")), TimeSpan.FromSeconds(10)));
        Assert.True(controller.IsRunning);

        await controller.StopAsync(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task StopAsync_SendsStopCommand_AndProcessExitsGracefully()
    {
        using var controller = new ServerProcessController(_serverDir, "fake_server.bat");
        var lines = new List<string>();
        controller.OutputReceived += line => { lock (lines) lines.Add(line); };

        controller.Start();
        await WaitUntilAsync(() => lines.Any(l => l.Contains("Server starting")), TimeSpan.FromSeconds(10));

        var stoppedGracefully = await controller.StopAsync(TimeSpan.FromSeconds(10));

        Assert.True(stoppedGracefully);
        Assert.False(controller.IsRunning);
        lock (lines)
        {
            Assert.Contains(lines, l => l.Contains("Stopping server"));
            Assert.Contains(lines, l => l.Contains("Done."));
        }
    }

    [Fact]
    public void Start_MissingBatFile_Throws()
    {
        using var controller = new ServerProcessController(_serverDir, "does_not_exist.bat");
        Assert.Throws<FileNotFoundException>(() => controller.Start());
    }

    private static async Task<bool> WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
                return true;
            await Task.Delay(50);
        }
        return condition();
    }
}
