using VanillaLauncher.Client;
using Xunit;

namespace VanillaLauncher.Client.Tests;

public class AppConfigTests : IDisposable
{
    private readonly string _dir;

    public AppConfigTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "vlc-appconfig-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    private void WriteAppSettings(string json) =>
        File.WriteAllText(Path.Combine(_dir, "appsettings.json"), json);

    [Fact]
    public void Load_MissingExternalFile_FallsBackToEmbeddedDefault()
    {
        // appsettings.json может отсутствовать рядом с exe (забыли приложить к релизу,
        // как случилось с 26.1.2-b1) — Load не должен падать, а взять встроенный дефолт.
        // Встроенный дефолт — по-настоящему пустой шаблон "движка" (appsettings.default.json,
        // без значений конкретного модпака, см. docs/TASK_PATH_AUTODETECT.md), поэтому
        // результат — валидный, но неадаптированный конфиг (IsConfigured == false), а не
        // готовые к работе значения.
        var config = AppConfig.Load(_dir);

        Assert.False(config.IsConfigured);
        Assert.Equal(string.Empty, config.ManifestUrl);
    }

    [Fact]
    public void Load_MissingExternalFile_ThenSave_CreatesLocalFile()
    {
        var config = AppConfig.Load(_dir);
        config.ProfileRoot = "D:\\friend-picked-this";
        config.Save();

        Assert.True(File.Exists(Path.Combine(_dir, "appsettings.json")));
        var reloaded = AppConfig.Load(_dir);
        Assert.Equal("D:\\friend-picked-this", reloaded.ProfileRoot);
    }

    [Fact]
    public void Load_MissingManifestUrl_DoesNotThrow_ButIsNotConfigured()
    {
        // Пустой/частичный ManifestUrl/GitHubOwner/GitHubRepo — валидное состояние
        // "движок ещё не адаптирован под модпак", а не ошибка чтения файла (см.
        // AppConfig.IsConfigured, docs/TASK_PATH_AUTODETECT.md).
        WriteAppSettings("""{ "ProfileRoot": "C:\\somewhere" }""");

        var config = AppConfig.Load(_dir);

        Assert.False(config.IsConfigured);
        Assert.Equal("C:\\somewhere", config.ProfileRoot);
    }

    [Fact]
    public void IsConfigured_RequiresManifestUrlAndGitHubOwnerAndRepo()
    {
        var config = new AppConfig();
        Assert.False(config.IsConfigured);

        config.ManifestUrl = "https://example.invalid/manifest.json";
        Assert.False(config.IsConfigured);

        config.GitHubOwner = "Owner";
        Assert.False(config.IsConfigured);

        config.GitHubRepo = "Repo";
        Assert.True(config.IsConfigured);
    }

    [Fact]
    public void Load_ValidFile_ReturnsConfig()
    {
        WriteAppSettings("""{ "ManifestUrl": "https://example.invalid/manifest.json", "ProfileRoot": "C:\\somewhere" }""");

        var config = AppConfig.Load(_dir);

        Assert.Equal("https://example.invalid/manifest.json", config.ManifestUrl);
        Assert.Equal("C:\\somewhere", config.ProfileRoot);
    }

    [Fact]
    public void Save_WritesBackToOriginalFile_PreservingOtherFields()
    {
        WriteAppSettings("""{ "ManifestUrl": "https://example.invalid/manifest.json", "ProfileRoot": "C:\\old", "MaxBackupsToKeep": 7 }""");
        var config = AppConfig.Load(_dir);

        config.ProfileRoot = "D:\\new-path";
        config.Save();

        var reloaded = AppConfig.Load(_dir);
        Assert.Equal("D:\\new-path", reloaded.ProfileRoot);
        Assert.Equal(7, reloaded.MaxBackupsToKeep); // не потерялось при перезаписи
    }

    [Fact]
    public void Load_MissingGuideFields_FallsBackToGenericDefaults()
    {
        // Гайды - generic-дефолт движка (см. DefaultGuides), не значение конкретного
        // модпака - поэтому, в отличие от ManifestUrl/ProfileRoot, JSON без этих полей
        // должен получить непустой, осмысленный текст, а не пустую строку.
        WriteAppSettings("""{ "ManifestUrl": "https://example.invalid/manifest.json", "ProfileRoot": "C:\\somewhere" }""");

        var config = AppConfig.Load(_dir);

        Assert.False(string.IsNullOrWhiteSpace(config.ClientGuideShort));
        Assert.False(string.IsNullOrWhiteSpace(config.ClientGuideFull));
        Assert.False(string.IsNullOrWhiteSpace(config.AdminGuideShort));
        Assert.False(string.IsNullOrWhiteSpace(config.AdminGuideFull));
    }

    [Fact]
    public void Save_PreservesCustomGuideText()
    {
        WriteAppSettings("""{ "ManifestUrl": "https://example.invalid/manifest.json", "ProfileRoot": "C:\\old" }""");
        var config = AppConfig.Load(_dir);

        config.ClientGuideShort = "Кастомная краткая инструкция клиента.";
        config.AdminGuideFull = "Кастомная полная инструкция админа.";
        config.Save();

        var reloaded = AppConfig.Load(_dir);
        Assert.Equal("Кастомная краткая инструкция клиента.", reloaded.ClientGuideShort);
        Assert.Equal("Кастомная полная инструкция админа.", reloaded.AdminGuideFull);
    }

    [Fact]
    public void Save_WithoutLoad_Throws()
    {
        var config = new AppConfig { ManifestUrl = "https://x", ProfileRoot = "C:\\x" };

        Assert.Throws<InvalidOperationException>(() => config.Save());
    }

    [Fact]
    public void Save_PreservesAutoDetectFields()
    {
        WriteAppSettings("""{ "ManifestUrl": "https://example.invalid/manifest.json", "ProfileRoot": "C:\\old" }""");
        var config = AppConfig.Load(_dir);

        config.ClientFolderName = "VanillaScary";
        config.ClientSearchRoots = new List<string> { "D:\\Games\\curseforge\\Instances" };
        config.ServerFolderName = "Server VS";
        config.Save();

        var reloaded = AppConfig.Load(_dir);
        Assert.Equal("VanillaScary", reloaded.ClientFolderName);
        Assert.Equal(new List<string> { "D:\\Games\\curseforge\\Instances" }, reloaded.ClientSearchRoots);
        Assert.Equal("Server VS", reloaded.ServerFolderName);
    }
}
