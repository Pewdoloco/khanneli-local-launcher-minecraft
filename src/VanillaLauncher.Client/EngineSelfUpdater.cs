using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;

namespace VanillaLauncher.Client;

public sealed record EngineUpdateInfo(bool IsUpdateAvailable, string LatestVersion, string? DownloadUrl)
{
    public static EngineUpdateInfo UpToDate(string currentVersion) => new(false, currentVersion, null);
    public static EngineUpdateInfo Available(string latestVersion, string downloadUrl) => new(true, latestVersion, downloadUrl);
}

/// <summary>
/// Самообновление exe движка — отдельно от обновления модпака (UpdateService/Downloader):
/// там докачиваются файлы сборки в ProfileRoot, здесь целиком заменяется сам
/// VanillaLauncher.exe по релизам EngineGitHubOwner/EngineGitHubRepo (репозиторий движка,
/// не модпака — см. docs/TASK_PATH_AUTODETECT.md, "Модель распространения"). Полезно в первую
/// очередь игрокам: раньше единственный способ получить новый exe — админ вручную давал
/// файл; теперь любой, у кого настроен EngineGitHubOwner/EngineGitHubRepo, обновляется сам.
/// </summary>
public static class EngineSelfUpdater
{
    private const string TagPrefix = "engine-v";
    private const string AssetName = "VanillaLauncher.exe";

    public static async Task<EngineUpdateInfo> CheckForUpdateAsync(
        HttpClient http, string owner, string repo, string currentVersion, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{owner}/{repo}/releases/latest");
        request.Headers.UserAgent.ParseAdd("VanillaLauncher");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                $"GitHub API: не удалось проверить обновления движка ({(int)response.StatusCode} {response.StatusCode}): {body}");
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var tagName = root.GetProperty("tag_name").GetString() ?? string.Empty;
        if (!tagName.StartsWith(TagPrefix, StringComparison.OrdinalIgnoreCase))
            return EngineUpdateInfo.UpToDate(currentVersion);

        var latestVersionText = tagName[TagPrefix.Length..];
        if (!Version.TryParse(latestVersionText, out var latestVersion) || !Version.TryParse(currentVersion, out var current))
            return EngineUpdateInfo.UpToDate(currentVersion);

        if (latestVersion <= current)
            return EngineUpdateInfo.UpToDate(currentVersion);

        string? downloadUrl = null;
        if (root.TryGetProperty("assets", out var assets))
        {
            foreach (var asset in assets.EnumerateArray())
            {
                if (string.Equals(asset.GetProperty("name").GetString(), AssetName, StringComparison.OrdinalIgnoreCase))
                {
                    downloadUrl = asset.GetProperty("browser_download_url").GetString();
                    break;
                }
            }
        }

        // Тег новее, но нужного ассета в релизе нет (например, CI ещё не успел его залить,
        // или релиз собран вручную без exe) — сообщать об обновлении, которое нечем накатить,
        // хуже, чем промолчать.
        return downloadUrl is null ? EngineUpdateInfo.UpToDate(currentVersion) : EngineUpdateInfo.Available(latestVersionText, downloadUrl);
    }

    /// <summary>
    /// Скачивает новый exe, готовит и запускает маленький .bat, который подменит текущий exe
    /// после того, как этот процесс завершится, и перезапустит его. Windows не даёт переписать
    /// байты уже запущенного exe напрямую (файл занят загруженным образом процесса) — отсюда
    /// схема "скачали рядом → дождались выхода процесса по PID → move → restart". Сам процесс
    /// не завершает себя — это должна сделать вызывающая сторона (UI, см. MainWindow), сразу
    /// после успешного возврата отсюда: пока процесс жив, .bat крутится в цикле ожидания.
    /// appsettings.json/admin-auth.json рядом не трогаются — заменяется только сам exe.
    /// </summary>
    public static async Task PrepareAndLaunchUpdateAsync(HttpClient http, string downloadUrl, CancellationToken ct = default)
    {
        var exePath = Process.GetCurrentProcess().MainModule?.FileName
            ?? throw new InvalidOperationException("Не удалось определить путь к текущему exe.");
        var dir = Path.GetDirectoryName(exePath)!;
        var newExePath = Path.Combine(dir, "VanillaLauncher.update.exe");
        var scriptPath = Path.Combine(dir, "vlc-update.bat");

        var bytes = await http.GetByteArrayAsync(downloadUrl, ct);
        await File.WriteAllBytesAsync(newExePath, bytes, ct);

        var pid = Process.GetCurrentProcess().Id;
        var script = $"""
            @echo off
            :wait
            tasklist /FI "PID eq {pid}" 2>NUL | find "{pid}" >NUL
            if not errorlevel 1 (
                timeout /t 1 /nobreak >NUL
                goto wait
            )
            move /Y "{newExePath}" "{exePath}"
            start "" "{exePath}"
            del "%~f0"
            """;
        await File.WriteAllTextAsync(scriptPath, script, ct);

        Process.Start(new ProcessStartInfo
        {
            FileName = scriptPath,
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true
        });
    }
}
