using VanillaLauncher.Client;
using Xunit;

namespace VanillaLauncher.Client.Tests;

public class GitHubRepoNameNormalizerTests
{
    [Theory]
    [InlineData("test-pack", "test-pack")]
    [InlineData("  test-pack  ", "test-pack")]
    [InlineData("https://github.com/Pewdoloco/test-pack.git", "test-pack")]
    [InlineData("https://github.com/Pewdoloco/test-pack", "test-pack")]
    [InlineData("https://github.com/Pewdoloco/test-pack/", "test-pack")]
    [InlineData("github.com/Pewdoloco/test-pack", "test-pack")]
    [InlineData("Pewdoloco/test-pack", "test-pack")]
    [InlineData("khanneli-local-launcher-minecraft.git", "khanneli-local-launcher-minecraft")]
    public void Normalize_ExtractsBareRepoName(string input, string expected)
    {
        Assert.Equal(expected, GitHubRepoNameNormalizer.Normalize(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_EmptyOrWhitespace_ReturnsNull(string? input)
    {
        Assert.Null(GitHubRepoNameNormalizer.Normalize(input));
    }
}
