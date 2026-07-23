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

    [Theory]
    [InlineData("Pewdoloco", "Pewdoloco")]
    [InlineData("  Pewdoloco  ", "Pewdoloco")]
    [InlineData("https://github.com/Pewdoloco/test-pack", "Pewdoloco")]
    [InlineData("https://github.com/Pewdoloco/test-pack.git", "Pewdoloco")]
    [InlineData("https://github.com/Pewdoloco/test-pack/", "Pewdoloco")]
    [InlineData("github.com/Pewdoloco/test-pack", "Pewdoloco")]
    [InlineData("Pewdoloco/test-pack", "Pewdoloco")]
    public void NormalizeOwner_ExtractsOwnerNotRepo(string input, string expected)
    {
        // Тот же вставленный URL в поле Owner должен дать ВЛАДЕЛЬЦА (предпоследний сегмент),
        // а не имя репозитория (последний сегмент, см. Normalize выше) — баг, о котором
        // сообщил пользователь: полная ссылка в Owner давала неверное значение.
        Assert.Equal(expected, GitHubRepoNameNormalizer.NormalizeOwner(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeOwner_EmptyOrWhitespace_ReturnsNull(string? input)
    {
        Assert.Null(GitHubRepoNameNormalizer.NormalizeOwner(input));
    }
}
