namespace VanillaLauncher.Client;

public enum DownloadStage { Started, Completed }

public sealed record DownloadProgress(string FilePath, int CompletedCount, int TotalCount, DownloadStage Stage);

public sealed class Downloader
{
    private readonly HttpClient _http;
    private readonly string _profileRoot;

    public Downloader(HttpClient http, string profileRoot)
    {
        _http = http;
        _profileRoot = profileRoot;
    }

    public async Task ApplyAsync(IEnumerable<FilePlanItem> plan, IProgress<DownloadProgress>? progress = null, CancellationToken ct = default)
    {
        var toDownload = plan.Where(p => p.Action == FileAction.NeedsDownload).ToList();
        var total = toDownload.Count;
        var completed = 0;

        foreach (var item in toDownload)
        {
            var targetPath = Path.Combine(_profileRoot, item.Entry.Path);
            var targetDir = Path.GetDirectoryName(targetPath)!;
            Directory.CreateDirectory(targetDir);

            var tempPath = targetPath + ".tmp";

            progress?.Report(new DownloadProgress(item.Entry.Path, completed, total, DownloadStage.Started));

            using var response = await _http.GetAsync(item.Entry.Url, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            await using (var fileStream = File.Create(tempPath))
            await using (var httpStream = await response.Content.ReadAsStreamAsync(ct))
            {
                await httpStream.CopyToAsync(fileStream, ct);
            }

            // Проверка хеша скачанного файла перед тем, как заменить рабочий
            var downloadedHash = await HashService.ComputeSha256Async(tempPath, ct);
            if (!HashService.Matches(downloadedHash, item.Entry.Sha256))
            {
                File.Delete(tempPath);
                throw new InvalidOperationException($"Хеш не совпал после скачивания: {item.Entry.Path}");
            }

            File.Move(tempPath, targetPath, overwrite: true);
            completed++;
            progress?.Report(new DownloadProgress(item.Entry.Path, completed, total, DownloadStage.Completed));
        }
    }
}
