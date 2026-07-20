using System.Net;
using VanillaLauncher.Client;
using Xunit;

namespace VanillaLauncher.Client.Tests;

public class EngineSelfUpdaterTests
{
    private static string ReleaseJson(string tagName, params (string Name, string Url)[] assets)
    {
        var assetsJson = string.Join(",", assets.Select(a =>
            $$"""{ "name": "{{a.Name}}", "browser_download_url": "{{a.Url}}" }"""));

        return $$"""
            {
              "tag_name": "{{tagName}}",
              "assets": [{{assetsJson}}]
            }
            """;
    }

    [Fact]
    public async Task CheckForUpdateAsync_NewerTagWithAsset_ReturnsAvailable()
    {
        var json = ReleaseJson("engine-v1.2.0", ("VanillaLauncher.exe", "https://example.invalid/VanillaLauncher.exe"));
        var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(HttpStatusCode.OK, json));
        var http = new HttpClient(handler);

        var result = await EngineSelfUpdater.CheckForUpdateAsync(http, "owner", "repo", "1.0.0");

        Assert.True(result.IsUpdateAvailable);
        Assert.Equal("1.2.0", result.LatestVersion);
        Assert.Equal("https://example.invalid/VanillaLauncher.exe", result.DownloadUrl);
    }

    [Fact]
    public async Task CheckForUpdateAsync_SameVersion_ReturnsUpToDate()
    {
        var json = ReleaseJson("engine-v1.0.0", ("VanillaLauncher.exe", "https://example.invalid/VanillaLauncher.exe"));
        var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(HttpStatusCode.OK, json));
        var http = new HttpClient(handler);

        var result = await EngineSelfUpdater.CheckForUpdateAsync(http, "owner", "repo", "1.0.0");

        Assert.False(result.IsUpdateAvailable);
        Assert.Null(result.DownloadUrl);
    }

    [Fact]
    public async Task CheckForUpdateAsync_OlderTagThanCurrent_ReturnsUpToDate()
    {
        // Не должно происходить в норме (значит кто-то запускает более новую сборку, чем
        // опубликованный релиз), но не должно предлагать "откат" на более старую версию.
        var json = ReleaseJson("engine-v0.9.0", ("VanillaLauncher.exe", "https://example.invalid/VanillaLauncher.exe"));
        var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(HttpStatusCode.OK, json));
        var http = new HttpClient(handler);

        var result = await EngineSelfUpdater.CheckForUpdateAsync(http, "owner", "repo", "1.0.0");

        Assert.False(result.IsUpdateAvailable);
    }

    [Fact]
    public async Task CheckForUpdateAsync_TagWithoutEnginePrefix_ReturnsUpToDate()
    {
        var json = ReleaseJson("v2.0.0", ("VanillaLauncher.exe", "https://example.invalid/VanillaLauncher.exe"));
        var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(HttpStatusCode.OK, json));
        var http = new HttpClient(handler);

        var result = await EngineSelfUpdater.CheckForUpdateAsync(http, "owner", "repo", "1.0.0");

        Assert.False(result.IsUpdateAvailable);
    }

    [Fact]
    public async Task CheckForUpdateAsync_NewerTagWithoutMatchingAsset_ReturnsUpToDate()
    {
        // Тег новее, но нужного ассета в релизе нет - обновлять нечем, сообщать об этом
        // как о доступном обновлении было бы враньём (кнопка "Обновить" ничего не сделает).
        var json = ReleaseJson("engine-v1.2.0", ("SomethingElse.zip", "https://example.invalid/SomethingElse.zip"));
        var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(HttpStatusCode.OK, json));
        var http = new HttpClient(handler);

        var result = await EngineSelfUpdater.CheckForUpdateAsync(http, "owner", "repo", "1.0.0");

        Assert.False(result.IsUpdateAvailable);
    }

    [Fact]
    public async Task CheckForUpdateAsync_HttpError_Throws()
    {
        var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(HttpStatusCode.NotFound, "{}"));
        var http = new HttpClient(handler);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => EngineSelfUpdater.CheckForUpdateAsync(http, "owner", "repo", "1.0.0"));
    }

    [Fact]
    public async Task CheckForUpdateAsync_SendsUserAgentHeader()
    {
        HttpRequestMessage? capturedRequest = null;
        var json = ReleaseJson("engine-v1.0.0", ("VanillaLauncher.exe", "https://example.invalid/VanillaLauncher.exe"));
        var handler = new FakeHttpMessageHandler(req =>
        {
            capturedRequest = req;
            return FakeHttpMessageHandler.Json(HttpStatusCode.OK, json);
        });
        var http = new HttpClient(handler);

        await EngineSelfUpdater.CheckForUpdateAsync(http, "owner", "repo", "1.0.0");

        Assert.NotNull(capturedRequest);
        Assert.Equal("https://api.github.com/repos/owner/repo/releases/latest", capturedRequest!.RequestUri!.ToString());
        Assert.False(capturedRequest.Headers.UserAgent.Count == 0);
    }
}
