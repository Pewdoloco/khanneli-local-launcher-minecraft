using System.Net;
using VanillaLauncher.Client;
using Xunit;

namespace VanillaLauncher.Client.Tests;

public class GitHubApiErrorTranslatorTests
{
    [Fact]
    public void NotFound_HintsAtWrongRepoNameOrUrl()
    {
        var hint = GitHubApiErrorTranslator.TryGetHint(HttpStatusCode.NotFound, """{"message":"Not Found"}""");
        Assert.NotNull(hint);
        Assert.Contains("GitHubRepo", hint);
    }

    [Fact]
    public void Forbidden_ResourceNotAccessible_HintsAtTokenPermissions()
    {
        var hint = GitHubApiErrorTranslator.TryGetHint(HttpStatusCode.Forbidden,
            """{"message":"Resource not accessible by personal access token"}""");
        Assert.NotNull(hint);
        Assert.Contains("VANILLALAUNCHER_GITHUB_TOKEN", hint);
    }

    [Fact]
    public void Forbidden_OtherReason_ReturnsNull()
    {
        var hint = GitHubApiErrorTranslator.TryGetHint(HttpStatusCode.Forbidden, """{"message":"rate limit exceeded"}""");
        Assert.Null(hint);
    }

    [Fact]
    public void UnprocessableEntity_RepositoryEmpty_HintsAtInitialCommit()
    {
        var hint = GitHubApiErrorTranslator.TryGetHint(HttpStatusCode.UnprocessableEntity,
            """{"message":"Validation Failed","errors":[{"resource":"Release","code":"custom","message":"Repository is empty."}]}""");
        Assert.NotNull(hint);
        Assert.Contains("README", hint);
    }

    [Fact]
    public void UnprocessableEntity_AlreadyExists_HintsAtChangingVersion()
    {
        var hint = GitHubApiErrorTranslator.TryGetHint(HttpStatusCode.UnprocessableEntity,
            """{"errors":[{"code":"already_exists","field":"tag_name"}]}""");
        Assert.NotNull(hint);
        Assert.Contains("Версия релиза", hint);
    }

    [Fact]
    public void Unauthorized_HintsAtTokenValidity()
    {
        var hint = GitHubApiErrorTranslator.TryGetHint(HttpStatusCode.Unauthorized, """{"message":"Bad credentials"}""");
        Assert.NotNull(hint);
        Assert.Contains("VANILLALAUNCHER_GITHUB_TOKEN", hint);
    }

    [Fact]
    public void UnrecognizedError_ReturnsNull()
    {
        var hint = GitHubApiErrorTranslator.TryGetHint(HttpStatusCode.InternalServerError, """{"message":"oops"}""");
        Assert.Null(hint);
    }
}
