using System.Text;
using System.Text.Json;
using VanillaLauncher.Client;

namespace VanillaLauncher.Admin;

/// <summary>
/// Генерирует manifest.json из папки сборки и публикует его вместе со всеми файлами
/// в новый GitHub Release. Каждый публикуемый релиз самодостаточен — все файлы
/// перезаливаются заново (без переиспользования ассетов старых релизов), чтобы
/// манифест никогда не зависел от того, что старый релиз кто-то не удалит.
/// </summary>
public sealed class ReleasePublisher
{
    private readonly GitHubReleaseClient _client;

    public ReleasePublisher(GitHubReleaseClient client)
    {
        _client = client;
    }

    public async Task<Manifest> PublishAsync(
        string sourceRoot,
        IReadOnlyList<string> includeFolders,
        string version,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        progress?.Report($"Создание релиза {version}...");
        var release = await _client.CreateReleaseAsync(version, version, ct);

        var downloadBaseUrl = BuildAssetDownloadBaseUrl(release);

        // urlForPath даёт предварительную ссылку, пока файл ещё не загружен — она
        // перезаписывается ниже настоящим browser_download_url после аплоада, потому
        // что GitHub может переименовать ассет (например, заменить пробелы на точки),
        // и предсказанная заранее ссылка тогда не совпадёт с реальной (см.
        // GitHubReleaseClient.UploadAssetAsync).
        var manifest = await ManifestGenerator.GenerateAsync(
            sourceRoot,
            includeFolders,
            version,
            relPath => $"{downloadBaseUrl}/{Uri.EscapeDataString(ManifestGenerator.FlattenPathForAssetName(relPath))}",
            progress,
            ct);

        foreach (var entry in manifest.Files)
        {
            ct.ThrowIfCancellationRequested();
            var assetName = ManifestGenerator.FlattenPathForAssetName(entry.Path);
            progress?.Report($"Загрузка: {assetName}");
            var bytes = await File.ReadAllBytesAsync(Path.Combine(sourceRoot, entry.Path), ct);
            entry.Url = await _client.UploadAssetAsync(release, assetName, bytes, ct);
        }

        progress?.Report("Загрузка manifest.json...");
        var manifestJson = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
        await _client.UploadAssetAsync(release, "manifest.json", Encoding.UTF8.GetBytes(manifestJson), ct);

        progress?.Report($"Опубликовано: {release.HtmlUrl}");
        return manifest;
    }

    /// <summary>html_url вида ".../releases/tag/{tag}" -> download-URL ассетов ".../releases/download/{tag}".</summary>
    private static string BuildAssetDownloadBaseUrl(GitHubRelease release)
    {
        if (!release.HtmlUrl.Contains("/releases/tag/"))
            throw new InvalidOperationException($"Неожиданный формат html_url от GitHub API: {release.HtmlUrl}");

        return release.HtmlUrl.Replace("/releases/tag/", "/releases/download/");
    }
}
