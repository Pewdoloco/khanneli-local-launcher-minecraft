namespace VanillaLauncher.Admin;

/// <summary>
/// Полный пайплайн публикации обновления одной кнопкой:
/// бэкап мира -> остановка сервера -> синхронизация серверных файлов ->
/// генерация и публикация манифеста.
///
/// Сервер после публикации НЕ запускается обратно автоматически (было так раньше —
/// убрано по запросу: админ должен явно нажать "Запустить сервер" сам, когда готов,
/// а не обнаруживать постфактум, что сервер уже поднялся без его участия). Если
/// какой-то шаг падает, пайплайн не пытается восстановить предыдущее состояние — он
/// останавливается там, где упал, и оставляет сервер остановленным. Это осознанно
/// безопаснее, чем пытаться "докрутить" пайплайн дальше с неизвестным состоянием файлов.
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
        progress.Report("Шаг 1/4: бэкап мира...");
        worldBackup.BackupWorld(levelName);

        if (serverController.IsRunning)
        {
            progress.Report("Шаг 2/4: остановка сервера...");
            var stopped = await serverController.StopAsync(stopTimeout ?? TimeSpan.FromSeconds(60), ct);
            if (!stopped)
                throw new InvalidOperationException(
                    "Сервер не остановился штатно — публикация прервана, сервер оставлен как есть.");
        }
        else
        {
            progress.Report("Шаг 2/4: сервер уже остановлен.");
        }

        progress.Report("Шаг 3/4: синхронизация серверных файлов...");
        await ServerFileSync.MirrorAsync(buildSourceRoot, serverDirectory, includeFolders, serverExcludeFileNames, progress, ct);

        progress.Report("Шаг 4/4: генерация и публикация манифеста...");
        await publisher.PublishAsync(buildSourceRoot, includeFolders, version, progress, ct);

        progress.Report("Публикация завершена. Сервер остановлен — запусти его вручную кнопкой «Запустить сервер», когда будешь готов.");
    }
}
