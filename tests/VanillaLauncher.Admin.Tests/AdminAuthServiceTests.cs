using VanillaLauncher.Admin;
using Xunit;

namespace VanillaLauncher.Admin.Tests;

public class AdminAuthServiceTests : IDisposable
{
    private readonly string _credentialsPath;

    public AdminAuthServiceTests()
    {
        _credentialsPath = Path.Combine(Path.GetTempPath(), "vlc-auth-tests-" + Guid.NewGuid() + ".json");
    }

    public void Dispose()
    {
        if (File.Exists(_credentialsPath))
            File.Delete(_credentialsPath);
    }

    [Fact]
    public void HasPassword_FalseBeforeSet_TrueAfterSet()
    {
        var service = new AdminAuthService(_credentialsPath);

        Assert.False(service.HasPassword());
        service.SetPassword("hunter2");
        Assert.True(service.HasPassword());
    }

    [Fact]
    public void VerifyPassword_NoPasswordSet_ReturnsFalse()
    {
        var service = new AdminAuthService(_credentialsPath);

        Assert.False(service.VerifyPassword("anything"));
    }

    [Fact]
    public void VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        var service = new AdminAuthService(_credentialsPath);
        service.SetPassword("correct-horse-battery-staple");

        Assert.True(service.VerifyPassword("correct-horse-battery-staple"));
    }

    [Fact]
    public void VerifyPassword_WrongPassword_ReturnsFalse()
    {
        var service = new AdminAuthService(_credentialsPath);
        service.SetPassword("correct-horse-battery-staple");

        Assert.False(service.VerifyPassword("wrong-password"));
    }

    [Fact]
    public void VerifyPassword_PersistsAcrossInstances()
    {
        new AdminAuthService(_credentialsPath).SetPassword("my-secret");

        var freshInstance = new AdminAuthService(_credentialsPath);
        Assert.True(freshInstance.VerifyPassword("my-secret"));
    }

    [Fact]
    public void SetPassword_EmptyPassword_Throws()
    {
        var service = new AdminAuthService(_credentialsPath);

        Assert.Throws<ArgumentException>(() => service.SetPassword(""));
    }

    [Fact]
    public void CredentialsFile_NeverContainsPlaintextPassword()
    {
        var service = new AdminAuthService(_credentialsPath);
        service.SetPassword("super-secret-plaintext-marker");

        var fileContent = File.ReadAllText(_credentialsPath);

        Assert.DoesNotContain("super-secret-plaintext-marker", fileContent);
    }

    [Fact]
    public void SetPassword_TwoCallsProduceDifferentSalts()
    {
        var pathA = Path.Combine(Path.GetTempPath(), "vlc-auth-a-" + Guid.NewGuid() + ".json");
        var pathB = Path.Combine(Path.GetTempPath(), "vlc-auth-b-" + Guid.NewGuid() + ".json");

        try
        {
            new AdminAuthService(pathA).SetPassword("same-password");
            new AdminAuthService(pathB).SetPassword("same-password");

            // Разные соли -> разное содержимое файла даже для одинакового пароля
            Assert.NotEqual(File.ReadAllText(pathA), File.ReadAllText(pathB));
        }
        finally
        {
            File.Delete(pathA);
            File.Delete(pathB);
        }
    }
}
