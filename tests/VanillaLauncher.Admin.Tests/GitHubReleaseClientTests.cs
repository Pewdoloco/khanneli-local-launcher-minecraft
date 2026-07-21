using System.Net;
using System.Text;
using VanillaLauncher.Admin;
using Xunit;

namespace VanillaLauncher.Admin.Tests;

public class GitHubReleaseClientTests
{
    [Fact]
    public async Task CreateReleaseAsync_SendsCorrectRequest_AndParsesResponse()
    {
        var fake = new FakeHttpMessageHandler(req => FakeHttpMessageHandler.Json(HttpStatusCode.Created, """
            {"id": 42, "upload_url": "https://uploads.github.com/repos/o/r/releases/42/assets{?name,label}", "html_url": "https://github.com/o/r/releases/tag/v1"}
            """));
        using var http = new HttpClient(fake);
        var client = new GitHubReleaseClient(http, "o", "r", "tok123");

        var release = await client.CreateReleaseAsync("v1", "v1");

        Assert.Equal(42, release.Id);
        Assert.Equal("https://uploads.github.com/repos/o/r/releases/42/assets{?name,label}", release.UploadUrlTemplate);
        Assert.Equal("https://github.com/o/r/releases/tag/v1", release.HtmlUrl);

        var (request, body) = Assert.Single(fake.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://api.github.com/repos/o/r/releases", request.RequestUri!.ToString());
        Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
        Assert.Equal("tok123", request.Headers.Authorization.Parameter);
        Assert.Contains("\"tag_name\":\"v1\"", body);
    }

    [Fact]
    public async Task CreateReleaseAsync_NonSuccessStatus_ThrowsWithBody()
    {
        var fake = new FakeHttpMessageHandler(_ =>
            FakeHttpMessageHandler.Json(HttpStatusCode.UnprocessableEntity, """{"message":"Validation Failed"}"""));
        using var http = new HttpClient(fake);
        var client = new GitHubReleaseClient(http, "o", "r", "tok");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.CreateReleaseAsync("v1", "v1"));
        Assert.Contains("Validation Failed", ex.Message);
        Assert.Contains("422", ex.Message);
    }

    [Fact]
    public async Task UploadAssetAsync_PostsToResolvedUploadUrl_WithNameAndContent()
    {
        var fake = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(HttpStatusCode.Created, """
            {"browser_download_url": "https://github.com/o/r/releases/download/v1/mods_a.jar"}
            """));
        using var http = new HttpClient(fake);
        var client = new GitHubReleaseClient(http, "o", "r", "tok");
        var release = new GitHubRelease(1, "https://uploads.github.com/repos/o/r/releases/1/assets{?name,label}", "https://github.com/o/r/releases/tag/v1");

        var downloadUrl = await client.UploadAssetAsync(release, "mods_a.jar", Encoding.UTF8.GetBytes("fake-jar-bytes"));

        var (request, body) = Assert.Single(fake.Requests);
        Assert.Equal("https://uploads.github.com/repos/o/r/releases/1/assets?name=mods_a.jar", request.RequestUri!.ToString());
        Assert.Equal("application/octet-stream", request.Content!.Headers.ContentType!.MediaType);
        Assert.Equal("fake-jar-bytes", body);
        Assert.Equal("https://github.com/o/r/releases/download/v1/mods_a.jar", downloadUrl);
    }

    /// <summary>
    /// GitHub переименовывает ассеты при сохранении (например, заменяет пробелы на
    /// точки в имени файла) — возвращаемый browser_download_url может отличаться от
    /// того, что мы предполагали бы по исходному assetName. Клиент должен отдавать
    /// именно то, что вернул GitHub, а не собирать ссылку сам.
    /// </summary>
    [Fact]
    public async Task UploadAssetAsync_GitHubRenamesAsset_ReturnsActualDownloadUrl()
    {
        var fake = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(HttpStatusCode.Created, """
            {"browser_download_url": "https://github.com/o/r/releases/download/v1/mods_Dangerous.Fabric.-.1.5.1.jar"}
            """));
        using var http = new HttpClient(fake);
        var client = new GitHubReleaseClient(http, "o", "r", "tok");
        var release = new GitHubRelease(1, "https://uploads.github.com/repos/o/r/releases/1/assets{?name,label}", "https://github.com/o/r/releases/tag/v1");

        var downloadUrl = await client.UploadAssetAsync(release, "mods_Dangerous Fabric - 1.5.1.jar", Encoding.UTF8.GetBytes("x"));

        Assert.Equal("https://github.com/o/r/releases/download/v1/mods_Dangerous.Fabric.-.1.5.1.jar", downloadUrl);
    }

    [Fact]
    public async Task UploadAssetAsync_NonSuccessStatus_Throws()
    {
        var fake = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(HttpStatusCode.Forbidden, """{"message":"rate limited"}"""));
        using var http = new HttpClient(fake);
        var client = new GitHubReleaseClient(http, "o", "r", "tok");
        var release = new GitHubRelease(1, "https://uploads.github.com/repos/o/r/releases/1/assets{?name,label}", "https://github.com/o/r/releases/tag/v1");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.UploadAssetAsync(release, "a.jar", Encoding.UTF8.GetBytes("x")));
        Assert.Contains("rate limited", ex.Message);
    }
}
