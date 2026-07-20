using VanillaLauncher.Client;
using Xunit;

namespace VanillaLauncher.Client.Tests;

public class PathAutoDetectServiceTests : IDisposable
{
    private readonly string _dir;

    public PathAutoDetectServiceTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "vlc-pathautodetect-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void TryFind_ExactNameMatch_ReturnsPath()
    {
        var expected = Directory.CreateDirectory(Path.Combine(_dir, "VanillaScary")).FullName;

        var result = PathAutoDetectService.TryFind("VanillaScary", new[] { _dir });

        Assert.Equal(expected, result);
    }

    [Fact]
    public void TryFind_CaseInsensitiveMatch_ReturnsPath()
    {
        var expected = Directory.CreateDirectory(Path.Combine(_dir, "VanillaScary")).FullName;

        var result = PathAutoDetectService.TryFind("vanillascary", new[] { _dir });

        Assert.Equal(expected, result);
    }

    [Fact]
    public void TryFind_NoMatch_ReturnsNull()
    {
        Directory.CreateDirectory(Path.Combine(_dir, "SomethingElse"));

        var result = PathAutoDetectService.TryFind("VanillaScary", new[] { _dir });

        Assert.Null(result);
    }

    [Fact]
    public void TryFind_SearchesRootsInOrder_ReturnsFirstMatch()
    {
        var dir2 = Path.Combine(_dir, "root2");
        Directory.CreateDirectory(dir2);
        var expected = Directory.CreateDirectory(Path.Combine(dir2, "VanillaScary")).FullName;

        var result = PathAutoDetectService.TryFind("VanillaScary", new[] { _dir, dir2 });

        Assert.Equal(expected, result);
    }

    [Fact]
    public void TryFind_MissingRoot_SkipsAndContinues()
    {
        var missingRoot = Path.Combine(_dir, "does-not-exist");
        var expected = Directory.CreateDirectory(Path.Combine(_dir, "VanillaScary")).FullName;

        var result = PathAutoDetectService.TryFind("VanillaScary", new[] { missingRoot, _dir });

        Assert.Equal(expected, result);
    }

    [Fact]
    public void TryFind_EmptyFolderName_ReturnsNull()
    {
        Directory.CreateDirectory(Path.Combine(_dir, "VanillaScary"));

        var result = PathAutoDetectService.TryFind("", new[] { _dir });

        Assert.Null(result);
    }

    [Fact]
    public void TryFind_NullFolderName_ReturnsNull()
    {
        var result = PathAutoDetectService.TryFind(null, new[] { _dir });

        Assert.Null(result);
    }

    [Fact]
    public void TryFind_DoesNotRecurseIntoSubdirectories()
    {
        var nested = Path.Combine(_dir, "outer", "VanillaScary");
        Directory.CreateDirectory(nested);

        var result = PathAutoDetectService.TryFind("VanillaScary", new[] { _dir });

        Assert.Null(result);
    }

    [Fact]
    public void TryFind_ExpandsEnvironmentVariableInRoot()
    {
        const string varName = "VLC_TEST_ROOT_" + nameof(TryFind_ExpandsEnvironmentVariableInRoot);
        Environment.SetEnvironmentVariable(varName, _dir);
        try
        {
            var expected = Directory.CreateDirectory(Path.Combine(_dir, "VanillaScary")).FullName;

            var result = PathAutoDetectService.TryFind("VanillaScary", new[] { $"%{varName}%" });

            Assert.Equal(expected, result);
        }
        finally
        {
            Environment.SetEnvironmentVariable(varName, null);
        }
    }

    [Fact]
    public void TryFind_ExpandsEnvironmentVariableInsideLongerPath()
    {
        const string varName = "VLC_TEST_ROOT_" + nameof(TryFind_ExpandsEnvironmentVariableInsideLongerPath);
        Environment.SetEnvironmentVariable(varName, _dir);
        try
        {
            var instancesRoot = Directory.CreateDirectory(Path.Combine(_dir, "curseforge", "Instances")).FullName;
            var expected = Directory.CreateDirectory(Path.Combine(instancesRoot, "VanillaScary")).FullName;

            var result = PathAutoDetectService.TryFind("VanillaScary", new[] { $"%{varName}%\\curseforge\\Instances" });

            Assert.Equal(expected, result);
        }
        finally
        {
            Environment.SetEnvironmentVariable(varName, null);
        }
    }

    [Fact]
    public void TryFind_UnknownEnvironmentVariable_LeftLiteral_SkipsRootWithoutThrowing()
    {
        // Environment.ExpandEnvironmentVariables не бросает на нераспознанном "%имя%" — просто
        // оставляет строку как есть, которая затем не существует на диске и тихо пропускается.
        var expected = Directory.CreateDirectory(Path.Combine(_dir, "VanillaScary")).FullName;

        var result = PathAutoDetectService.TryFind(
            "VanillaScary",
            new[] { "%VLC_DOES_NOT_EXIST_AS_ENV_VAR%", _dir });

        Assert.Equal(expected, result);
    }
}
