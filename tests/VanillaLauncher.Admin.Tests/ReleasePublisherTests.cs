using System.Net;
using VanillaLauncher.Admin;
using Xunit;

namespace VanillaLauncher.Admin.Tests;

public class ReleasePublisherTests : IDisposable
{
    private readonly string _sourceRoot;

    public ReleasePublisherTests()
    {
        _sourceRoot = Path.Combine(Path.GetTempPath(), "vlc-publish-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(Path.Combine(_sourceRoot, "mods"));
        Directory.CreateDirectory(Path.Combine(_sourceRoot, "config", "sub"));
        File.WriteAllText(Path.Combine(_sourceRoot, "mods", "a.jar"), "mod-a-content");
        File.WriteAllText(Path.Combine(_sourceRoot, "config", "sub", "c.json"), "config-c-content");
    }

    public void Dispose()
    {
        if (Directory.Exists(_sourceRoot))
            Directory.Delete(_sourceRoot, recursive: true);
    }

    [Fact]
    public async Task PublishAsync_CreatesReleaseUploadsAllFilesAndManifest_WithFlatAssetNames()
    {
        var fake = new FakeHttpMessageHandler(req =>
        {
            if (req.RequestUri!.ToString() == "https://api.github.com/repos/o/r/releases")
            {
                return FakeHttpMessageHandler.Json(HttpStatusCode.Created, """
                    {"id": 7, "upload_url": "https://uploads.github.com/repos/o/r/releases/7/assets{?name,label}", "html_url": "https://github.com/o/r/releases/tag/v1"}
                    """);
            }

            // Реальный GitHub может переименовать assetName при сохранении, но для этого
            // теста имена простые (без пробелов) — считаем, что он вернул тот же assetName,
            // как и было бы с настоящим GitHub. См. UploadAssetAsync_GitHubRenamesAsset_*
            // в GitHubReleaseClientTests для случая, когда имена расходятся.
            var name = Uri.UnescapeDataString(req.RequestUri!.Query.TrimStart('?').Split('=')[1]);
            return FakeHttpMessageHandler.Json(HttpStatusCode.Created,
                $$"""{"browser_download_url": "https://github.com/o/r/releases/download/v1/{{name}}"}""");
        });
        using var http = new HttpClient(fake);
        var client = new GitHubReleaseClient(http, "o", "r", "tok");
        var publisher = new ReleasePublisher(client);

        var manifest = await publisher.PublishAsync(_sourceRoot, new[] { "mods", "config" }, "v1");

        // 1 create release + 2 file assets + 1 manifest.json = 4 запроса
        Assert.Equal(4, fake.Requests.Count);

        var uploadRequests = fake.Requests.Skip(1).Select(r => r.Request.RequestUri!.ToString()).ToList();
        Assert.Contains(uploadRequests, u => u.Contains("name=mods_a.jar"));
        Assert.Contains(uploadRequests, u => u.Contains("name=config_sub_c.json"));
        Assert.Contains(uploadRequests, u => u.Contains("name=manifest.json"));

        Assert.Equal(2, manifest.Files.Count);
        var modEntry = manifest.Files.Single(f => f.Path == "mods/a.jar");
        Assert.Equal("https://github.com/o/r/releases/download/v1/mods_a.jar", modEntry.Url);
    }

    [Fact]
    public async Task PublishAsync_ReleaseCreationFails_DoesNotUploadAnything()
    {
        var fake = new FakeHttpMessageHandler(_ =>
            FakeHttpMessageHandler.Json(HttpStatusCode.UnprocessableEntity, """{"message":"tag already exists"}"""));
        using var http = new HttpClient(fake);
        var client = new GitHubReleaseClient(http, "o", "r", "tok");
        var publisher = new ReleasePublisher(client);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => publisher.PublishAsync(_sourceRoot, new[] { "mods", "config" }, "v1"));

        Assert.Single(fake.Requests); // только попытка создать релиз, до аплоадов не дошло
    }
}
