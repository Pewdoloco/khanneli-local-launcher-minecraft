using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace VanillaLauncher.Admin;

public sealed record GitHubRelease(long Id, string UploadUrlTemplate, string HtmlUrl);

/// <summary>
/// Тонкий клиент GitHub REST API — только то, что нужно для публикации релиза:
/// создать релиз и загрузить в него ассеты. Токен передаётся вызывающей стороной
/// (см. GitHubTokenProvider) — этот класс его не читает из окружения сам, чтобы
/// оставаться тестируемым без реальных переменных окружения.
/// </summary>
public sealed class GitHubReleaseClient
{
    private readonly HttpClient _http;
    private readonly string _owner;
    private readonly string _repo;
    private readonly string _token;

    public GitHubReleaseClient(HttpClient http, string owner, string repo, string token)
    {
        _http = http;
        _owner = owner;
        _repo = repo;
        _token = token;
    }

    public async Task<GitHubRelease> CreateReleaseAsync(string tagName, string name, CancellationToken ct = default)
    {
        var payload = JsonSerializer.Serialize(new
        {
            tag_name = tagName,
            name,
            draft = false,
            prerelease = false
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.github.com/repos/{_owner}/{_repo}/releases");
        ApplyHeaders(request);
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        using var response = await _http.SendAsync(request, ct);
        await EnsureSuccessAsync(response, $"создание релиза {tagName}", ct);

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        return new GitHubRelease(
            root.GetProperty("id").GetInt64(),
            root.GetProperty("upload_url").GetString()!,
            root.GetProperty("html_url").GetString() ?? string.Empty);
    }

    public async Task UploadAssetAsync(GitHubRelease release, string assetName, byte[] content, CancellationToken ct = default)
    {
        // upload_url приходит в формате "https://uploads.github.com/.../assets{?name,label}" — берём часть до "{"
        var uploadUrl = release.UploadUrlTemplate.Split('{')[0];

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{uploadUrl}?name={Uri.EscapeDataString(assetName)}");
        ApplyHeaders(request);
        request.Content = new ByteArrayContent(content);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        using var response = await _http.SendAsync(request, ct);
        await EnsureSuccessAsync(response, $"загрузка ассета {assetName}", ct);
    }

    private void ApplyHeaders(HttpRequestMessage request)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.UserAgent.ParseAdd("VanillaLauncher-Admin");
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string action, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync(ct);
        throw new InvalidOperationException($"GitHub API: не удалось выполнить '{action}' ({(int)response.StatusCode} {response.StatusCode}): {body}");
    }
}
