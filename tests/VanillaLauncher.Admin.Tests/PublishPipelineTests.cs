using System.Net;
using VanillaLauncher.Admin;
using Xunit;

namespace VanillaLauncher.Admin.Tests;

/// <summary>
/// Полный пайплайн от начала до конца, но без реального Minecraft-сервера и
/// реального GitHub: ServerProcessController гоняет синтетический .bat (как в
/// ServerProcessControllerTests), GitHubReleaseClient бьёт в FakeHttpMessageHandler.
/// </summary>
public class PublishPipelineTests : IDisposable
{
    private readonly string _serverDir;
    private readonly string _buildSourceRoot;

    public PublishPipelineTests()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), "vlc-pipeline-tests-" + Guid.NewGuid());
        _serverDir = Path.Combine(baseDir, "server");
        _buildSourceRoot = Path.Combine(baseDir, "build");
        Directory.CreateDirectory(_serverDir);
        Directory.CreateDirectory(Path.Combine(_buildSourceRoot, "mods"));

        File.WriteAllText(Path.Combine(_buildSourceRoot, "mods", "a.jar"), "mod-a-content");
        File.WriteAllText(Path.Combine(_serverDir, "server.properties"), "level-name=world\r\n");

        Directory.CreateDirectory(Path.Combine(_serverDir, "world", "region"));
        File.WriteAllText(Path.Combine(_serverDir, "world", "level.dat"), "fake world");

        var batContent = string.Join("\r\n", new[]
        {
            "@echo off",
            "echo Server starting...",
            "echo Done (0.001s)! For help, type \"help\"",
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
        File.WriteAllText(Path.Combine(_serverDir, "start.bat"), batContent);
    }

    public void Dispose()
    {
        var baseDir = Path.GetDirectoryName(_serverDir)!;
        if (Directory.Exists(baseDir))
        {
            try { Directory.Delete(baseDir, recursive: true); } catch { /* фоновый процесс мог не успеть отпустить хендл */ }
        }
    }

    private static ReleasePublisher CreateFakePublisher(out FakeHttpMessageHandler fake)
    {
        fake = new FakeHttpMessageHandler(req =>
        {
            if (req.Method == HttpMethod.Post && req.RequestUri!.ToString().EndsWith("/releases"))
            {
                return FakeHttpMessageHandler.Json(HttpStatusCode.Created, """
                    {"id": 1, "upload_url": "https://uploads.github.com/repos/o/r/releases/1/assets{?name,label}", "html_url": "https://github.com/o/r/releases/tag/v1"}
                    """);
            }
            return FakeHttpMessageHandler.Json(HttpStatusCode.Created, """
                {"browser_download_url": "https://github.com/o/r/releases/download/v1/asset"}
                """);
        });
        var http = new HttpClient(fake);
        return new ReleasePublisher(new GitHubReleaseClient(http, "o", "r", "tok"));
    }

    [Fact]
    public async Task RunAsync_ServerAlreadyStopped_RunsFullPipelineAndLeavesServerStopped()
    {
        var controller = new ServerProcessController(_serverDir, "start.bat");
        var worldBackup = new WorldBackupService(_serverDir, Path.Combine(_serverDir, "backups"), maxBackupsToKeep: 5);
        var publisher = CreateFakePublisher(out var fake);
        var log = new System.Collections.Concurrent.ConcurrentQueue<string>();

        await new PublishPipeline().RunAsync(
            controller, worldBackup, "world", _serverDir, _buildSourceRoot,
            new[] { "mods" }, "v1", publisher, new Progress<string>(log.Enqueue),
            stopTimeout: TimeSpan.FromSeconds(5));

        // Пайплайн больше не запускает сервер сам по себе (см. класс-комментарий
        // PublishPipeline) — админ должен нажать "Запустить сервер" явно, когда готов.
        Assert.False(controller.IsRunning);
        Assert.True(File.Exists(Path.Combine(_serverDir, "mods", "a.jar"))); // серверные файлы синхронизированы
        Assert.True(Directory.GetFiles(Path.Combine(_serverDir, "backups"), "world_*.zip").Length == 1); // бэкап сделан
        Assert.True(fake.Requests.Count >= 3); // create release + хотя бы 1 asset + manifest.json
    }

    [Fact]
    public async Task RunAsync_ServerRunning_StopsFirstThenPublishesAndLeavesServerStopped()
    {
        var controller = new ServerProcessController(_serverDir, "start.bat");
        controller.Start();
        // дождаться реального старта, иначе StopAsync может ударить по ещё не готовому процессу
        await Task.Delay(500);

        var worldBackup = new WorldBackupService(_serverDir, Path.Combine(_serverDir, "backups"), maxBackupsToKeep: 5);
        var publisher = CreateFakePublisher(out _);
        var log = new System.Collections.Concurrent.ConcurrentQueue<string>();

        await new PublishPipeline().RunAsync(
            controller, worldBackup, "world", _serverDir, _buildSourceRoot,
            new[] { "mods" }, "v1", publisher, new Progress<string>(log.Enqueue),
            stopTimeout: TimeSpan.FromSeconds(5));

        Assert.Contains(log, l => l.Contains("остановка сервера"));
        Assert.False(controller.IsRunning); // не перезапускается пайплайном
    }

    [Fact]
    public async Task RunAsync_ServerDoesNotRespondToStop_AbortsBeforeSync()
    {
        // .bat, который игнорирует "stop" и никогда не завершается сам по себе
        var stubbornBat = string.Join("\r\n", new[]
        {
            "@echo off",
            "echo Server starting...",
            ":loop",
            "set /p CMD=",
            "goto :loop"
        });
        File.WriteAllText(Path.Combine(_serverDir, "stubborn.bat"), stubbornBat);

        var controller = new ServerProcessController(_serverDir, "stubborn.bat");
        controller.Start();
        await Task.Delay(500);

        var worldBackup = new WorldBackupService(_serverDir, Path.Combine(_serverDir, "backups"), maxBackupsToKeep: 5);
        var publisher = CreateFakePublisher(out var fake);
        var log = new System.Collections.Concurrent.ConcurrentQueue<string>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => new PublishPipeline().RunAsync(
            controller, worldBackup, "world", _serverDir, _buildSourceRoot,
            new[] { "mods" }, "v1", publisher, new Progress<string>(log.Enqueue),
            stopTimeout: TimeSpan.FromSeconds(2)));

        Assert.False(File.Exists(Path.Combine(_serverDir, "mods", "a.jar"))); // до синхронизации не дошло
        Assert.Empty(fake.Requests); // до публикации не дошло

        // stubborn.bat игнорирует "stop", штатно не остановится — убиваем по PID,
        // чтобы не оставлять зависший процесс после теста.
        if (controller.ProcessId is { } pid)
        {
            try
            {
                using var stray = System.Diagnostics.Process.GetProcessById(pid);
                stray.Kill(entireProcessTree: true);
            }
            catch (ArgumentException)
            {
                // процесс уже завершился сам — ничего убивать не нужно
            }
        }
    }

    [Fact]
    public async Task RunAsync_SmokeTestCrashes_AbortsBeforePublish_ButServerFilesAlreadySynced()
    {
        // Именно тот класс бага, ради которого добавлен смоук-тест: клиентский мод (либо
        // соврал в метаданных, либо забыт в ServerExcludeMods) роняет старт сервера сразу
        // после синхронизации новых файлов — до этого шага пайплайн об этом не узнавал.
        var crashingBat = string.Join("\r\n", new[]
        {
            "@echo off",
            "echo Server starting...",
            "echo [ERROR]: NoClassDefFoundError: some.client.OnlyMod",
            "exit /b 1"
        });
        File.WriteAllText(Path.Combine(_serverDir, "crashing.bat"), crashingBat);

        var controller = new ServerProcessController(_serverDir, "crashing.bat");
        var worldBackup = new WorldBackupService(_serverDir, Path.Combine(_serverDir, "backups"), maxBackupsToKeep: 5);
        var publisher = CreateFakePublisher(out var fake);
        var log = new System.Collections.Concurrent.ConcurrentQueue<string>();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => new PublishPipeline().RunAsync(
            controller, worldBackup, "world", _serverDir, _buildSourceRoot,
            new[] { "mods" }, "v1", publisher, new Progress<string>(log.Enqueue),
            stopTimeout: TimeSpan.FromSeconds(5), smokeTestTimeout: TimeSpan.FromSeconds(10)));

        Assert.Contains("NoClassDefFoundError", ex.Message);
        Assert.True(File.Exists(Path.Combine(_serverDir, "mods", "a.jar"))); // синхронизация уже прошла
        Assert.Empty(fake.Requests); // на GitHub ничего не загружено
        Assert.False(controller.IsRunning);
    }

    [Fact]
    public async Task RunAsync_SmokeTestCrashes_NamesSuspiciousJarInErrorMessage()
    {
        // Реальный сценарий инцидента: синхронизированный мод содержит ZIP-запись STORED с
        // флагом data descriptor — Forge/NeoForge падает на этом при сканировании mods, но не
        // логирует имя виновного файла. Пайплайн должен назвать его сам.
        File.WriteAllBytes(
            Path.Combine(_buildSourceRoot, "mods", "oculus-mc1.20.1-1.8.0.jar"),
            ZipTestHelper.BuildSingleEntryZip("assets/", generalPurposeFlag: 0x0008, compressionMethod: 0));

        var crashingBat = string.Join("\r\n", new[]
        {
            "@echo off",
            "echo Server starting...",
            "echo Exception in thread \"main\" java.util.zip.ZipException",
            "exit /b 1"
        });
        File.WriteAllText(Path.Combine(_serverDir, "crashing2.bat"), crashingBat);

        var controller = new ServerProcessController(_serverDir, "crashing2.bat");
        var worldBackup = new WorldBackupService(_serverDir, Path.Combine(_serverDir, "backups"), maxBackupsToKeep: 5);
        var publisher = CreateFakePublisher(out var fake);
        var log = new System.Collections.Concurrent.ConcurrentQueue<string>();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => new PublishPipeline().RunAsync(
            controller, worldBackup, "world", _serverDir, _buildSourceRoot,
            new[] { "mods" }, "v1", publisher, new Progress<string>(log.Enqueue),
            stopTimeout: TimeSpan.FromSeconds(5), smokeTestTimeout: TimeSpan.FromSeconds(10)));

        Assert.Contains("oculus-mc1.20.1-1.8.0.jar", ex.Message);
        Assert.Empty(fake.Requests);
    }

    [Fact]
    public async Task RunAsync_SmokeTestTimesOut_KillsHungProcessAndAbortsBeforePublish()
    {
        var hangingBat = string.Join("\r\n", new[]
        {
            "@echo off",
            "echo Server starting...",
            ":loop",
            "set /p CMD=",
            "goto :loop"
        });
        File.WriteAllText(Path.Combine(_serverDir, "hanging.bat"), hangingBat);

        var controller = new ServerProcessController(_serverDir, "hanging.bat");
        var worldBackup = new WorldBackupService(_serverDir, Path.Combine(_serverDir, "backups"), maxBackupsToKeep: 5);
        var publisher = CreateFakePublisher(out var fake);
        var log = new System.Collections.Concurrent.ConcurrentQueue<string>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => new PublishPipeline().RunAsync(
            controller, worldBackup, "world", _serverDir, _buildSourceRoot,
            new[] { "mods" }, "v1", publisher, new Progress<string>(log.Enqueue),
            stopTimeout: TimeSpan.FromSeconds(2), smokeTestTimeout: TimeSpan.FromMilliseconds(500)));

        Assert.Empty(fake.Requests);

        for (var i = 0; i < 20 && controller.IsRunning; i++)
            await Task.Delay(100);
        Assert.False(controller.IsRunning); // принудительно убит после таймаута
    }
}
