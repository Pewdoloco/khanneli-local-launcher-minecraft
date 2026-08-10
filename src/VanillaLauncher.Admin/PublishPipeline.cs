namespace VanillaLauncher.Admin;

/// <summary>
/// Полный пайплайн публикации обновления одной кнопкой:
/// бэкап мира -> остановка сервера -> синхронизация серверных файлов ->
/// тестовый запуск сервера (смоук-тест) -> генерация и публикация манифеста.
///
/// Сервер после публикации НЕ запускается обратно автоматически (было так раньше —
/// убрано по запросу: админ должен явно нажать "Запустить сервер" сам, когда готов,
/// а не обнаруживать постфактум, что сервер уже поднялся без его участия). Если
/// какой-то шаг падает, пайплайн не пытается восстановить предыдущее состояние — он
/// останавливается там, где упал, и оставляет сервер остановленным. Это осознанно
/// безопаснее, чем пытаться "докрутить" пайплайн дальше с неизвестным состоянием файлов.
///
/// Смоук-тест (см. <see cref="ServerSmokeTestRunner"/>) — ответ на реальный инцидент: клиентские
/// моды, просочившиеся на сервер (соврали в метаданных про environment или их забыли добавить
/// в ServerExcludeMods), роняли старт сервера уже ПОСЛЕ публикации, когда это обнаруживал либо
/// сам админ вручную, либо живые игроки. Теперь сервер синхронизированные файлы коротко
/// запускает сам сразу после синхронизации — если он не доходит до готовности, публикация на
/// GitHub не происходит вообще (manifest.json/ассеты не загружаются), а локально
/// синхронизированные файлы сервера остаются как есть для ручного разбора.
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
        TimeSpan? smokeTestTimeout = null,
        CancellationToken ct = default)
    {
        var effectiveStopTimeout = stopTimeout ?? TimeSpan.FromSeconds(60);

        progress.Report("Шаг 1/5: бэкап мира...");
        worldBackup.BackupWorld(levelName);

        if (serverController.IsRunning)
        {
            progress.Report("Шаг 2/5: остановка сервера...");
            var stopped = await serverController.StopAsync(effectiveStopTimeout, ct);
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

        progress.Report("Шаг 4/5: тестовый запуск сервера (проверка, что моды не роняют старт)...");
        var smokeTest = await new ServerSmokeTestRunner().RunAsync(
            serverController, smokeTestTimeout ?? TimeSpan.FromSeconds(120), effectiveStopTimeout, ct);

        if (smokeTest.Outcome != ServerSmokeTestOutcome.Success)
        {
            var reason = smokeTest.Outcome == ServerSmokeTestOutcome.TimedOut
                ? "сервер не дошёл до готовности за отведённое время"
                : "процесс сервера завершился раньше готовности (краш)";

            var culpritNotes = new List<string>();

            // Приоритет 1 — сам Forge/NeoForge помечает статус ERROR у конкретного мода в
            // отчёте о загрузке (таблица "ИмяФайла.jar |Имя |modid |версия |СТАТУС |..."):
            // самый надёжный источник, называет виновника напрямую, но появляется только если
            // сервер дошёл до этой стадии (после первичного сканирования папки mods).
            var failedMods = ForgeModStateTableParser.FindFailedModJars(smokeTest.Output);
            if (failedMods.Count > 0)
            {
                culpritNotes.Add(
                    "Forge/NeoForge сам пометил статус ERROR при загрузке: " +
                    $"{string.Join(", ", failedMods)}. Обычно значит, что это клиентский мод, " +
                    "которому не место на дедик-сервере — добавь имя файла в ServerExcludeMods " +
                    "(экран «Настройки») и убери сам jar из папки mods на сервере, затем " +
                    "опубликуй снова.");
            }

            // Приоритет 2 — известный, надёжно детектируемый (не эвристика) класс краша при
            // сканировании mods — ZIP-запись STORED с флагом data descriptor. Крашит РАНЬШЕ,
            // чем сервер успевает дойти до отчёта о загрузке модов (см. выше), поэтому это
            // отдельная, непересекающаяся проверка, а не альтернатива первой.
            var suspiciousJars = JarDataDescriptorScanner.FindSuspiciousJars(Path.Combine(serverDirectory, "mods"));
            if (suspiciousJars.Count > 0)
            {
                culpritNotes.Add(
                    "Похоже на ZIP-дефект в файле(ах), из-за которого Forge/NeoForge падает при " +
                    "сканировании mods (сам файл не обязательно повреждён, так его просто собрал " +
                    $"автор мода): {string.Join(", ", suspiciousJars)}.");
            }

            var culpritNote = culpritNotes.Count > 0 ? string.Join(" ", culpritNotes) + " " : "";

            var details = smokeTest.ErrorLines.Count > 0
                ? "Похожие на ошибку строки из вывода:\n" + string.Join("\n", smokeTest.ErrorLines)
                : "Строк, похожих на ошибку, не найдено — смотри полный лог в консоли.";

            throw new InvalidOperationException(
                $"Тестовый запуск сервера не удался ({reason}). Публикация прервана, на GitHub " +
                $"ничего не загружено. {culpritNote}Синхронизированные файлы сервера остались как " +
                $"есть, проверь их вручную. {details}");
        }

        progress.Report("Шаг 5/5: генерация и публикация манифеста...");
        await publisher.PublishAsync(buildSourceRoot, includeFolders, version, progress, ct);

        progress.Report(
            "Публикация завершена. Тестовый запуск прошёл успешно, сервер снова остановлен — " +
            "запусти его вручную кнопкой «Запустить сервер», когда будешь готов.");
    }
}
