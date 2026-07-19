namespace VanillaLauncher.Admin;

/// <summary>
/// Полный пайплайн публикации обновления одной кнопкой:
/// бэкап мира -> остановка сервера -> синхронизация серверных файлов ->
/// генерация и публикация манифеста -> запуск сервера.
///
/// Если какой-то шаг падает, пайплайн не пытается восстановить предыдущее
/// состояние и не запускает сервер обратно — он останавливается там, где
/// упал, и оставляет сервер остановленным. Это осознанно безопаснее, чем
/// пытаться "докрутить" пайплайн дальше с неизвестным состоянием файлов.
/// </summary>
public sealed class PublishPipeline
{
    public async Task RunAsync(
        ServerProcessController serverController,
        WorldBackupService worldBackup,
        string levelName,
        string serverDirectory,
        string buildSourceRoot,
        IReadOnlyList<string> includeFolders,
        string version,
        ReleasePublisher publisher,
        IProgress<string> progress,
        IReadOnlyList<string>? serverExcludeFileNames = null,
        TimeSpan? stopTimeout = null,
        CancellationToken ct = default)
    {
        progress.Report("Шаг 1/5: бэкап мира...");
        worldBackup.BackupWorld(levelName);

        if (serverController.IsRunning)
        {
            progress.Report("Шаг 2/5: остановка сервера...");
            var stopped = await serverController.StopAsync(stopTimeout ?? TimeSpan.FromSeconds(60), ct);
            if (!stopped)
                throw new InvalidOperationException(
                    "Сервер не остановился штатно — публикация прервана, сервер оставлен как есть.");
        }
        else
        {
            progress.Report("Шаг 2/5: сервер уже остановлен.");
        }

        progress.Report("Шаг 3/5: синхронизация серверных файлов...");
        await ServerFileSync.MirrorAsync(buildSourceRoot, serverDirectory, includeFolders, serverExcludeFileNames, progress, ct);

        progress.Report("Шаг 4/5: генерация и публикация манифеста...");
        await publisher.PublishAsync(buildSourceRoot, includeFolders, version, progress, ct);

        progress.Report("Шаг 5/5: запуск сервера...");
        serverController.Start();

        progress.Report("Публикация завершена.");
    }
}
