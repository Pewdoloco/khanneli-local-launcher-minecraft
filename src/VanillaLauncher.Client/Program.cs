using VanillaLauncher.Client;

// Этап 1: минимальный консольный прогон без UI.
var config = AppConfig.Load();
var manifestUrl = config.ManifestUrl;
var profileRoot = config.ProfileRoot;

using var http = new HttpClient();

Console.WriteLine("Проверка манифеста...");
var manifestService = new ManifestService(http);
var manifest = await manifestService.FetchAsync(manifestUrl);
Console.WriteLine($"Актуальная версия: {manifest.Version}, файлов в манифесте: {manifest.Files.Count}");

Console.WriteLine("Сверка локальных файлов...");
var updateService = new UpdateService(profileRoot);
var plan = await updateService.BuildPlanAsync(manifest);

var toDownload = plan.Where(p => p.Action == FileAction.NeedsDownload).ToList();
Console.WriteLine($"Требуют обновления: {toDownload.Count} из {plan.Count}");

if (toDownload.Count == 0)
{
    Console.WriteLine("Сборка актуальна.");
    return;
}

var downloader = new Downloader(http, profileRoot);
var progress = new Progress<DownloadProgress>(p =>
{
    var label = p.Stage == DownloadStage.Started ? "Скачивание" : "Готово";
    Console.WriteLine($"{label}: {p.FilePath} ({p.CompletedCount}/{p.TotalCount})");
});
await downloader.ApplyAsync(toDownload, progress);

Console.WriteLine("Обновление завершено.");
