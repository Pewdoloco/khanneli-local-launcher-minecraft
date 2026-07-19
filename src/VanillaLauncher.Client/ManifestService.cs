using System.Text.Json;

namespace VanillaLauncher.Client;

public sealed class ManifestService
{
    private readonly HttpClient _http;

    public ManifestService(HttpClient http)
    {
        _http = http;
    }

    /// <summary>
    /// Скачивает manifest.json по прямой ссылке (asset публичного GitHub Release).
    /// Для приватного репозитория url должен вести на GitHub API asset endpoint,
    /// а в заголовках нужен Authorization: token/PAT — добавим на этапе, когда
    /// решится вопрос публичности репозитория.
    /// </summary>
    public async Task<Manifest> FetchAsync(string manifestUrl, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync(manifestUrl, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        var manifest = await JsonSerializer.DeserializeAsync<Manifest>(stream, cancellationToken: ct);

        if (manifest is null)
            throw new InvalidOperationException("Не удалось разобрать manifest.json");

        return manifest;
    }
}
