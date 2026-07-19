using VanillaLauncher.Client;
using Xunit;

namespace VanillaLauncher.Client.Tests;

public class HashServiceTests
{
    [Fact]
    public async Task ComputeSha256Async_ReturnsKnownHash_ForFixedContent()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "hello world");

            var hash = await HashService.ComputeSha256Async(tempFile);

            // sha256("hello world")
            Assert.Equal("b94d27b9934d3e08a52e52d7da7dabfac484efe37a5380ee9088f7ace2efcde9", hash);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ComputeSha256Async_DifferentContent_ProducesDifferentHash()
    {
        var fileA = Path.GetTempFileName();
        var fileB = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(fileA, "content A");
            await File.WriteAllTextAsync(fileB, "content B");

            var hashA = await HashService.ComputeSha256Async(fileA);
            var hashB = await HashService.ComputeSha256Async(fileB);

            Assert.NotEqual(hashA, hashB);
        }
        finally
        {
            File.Delete(fileA);
            File.Delete(fileB);
        }
    }

    [Theory]
    [InlineData("ABCDEF", "abcdef", true)]
    [InlineData("abcdef", "abcdef", true)]
    [InlineData("abcdef", "123456", false)]
    public void Matches_IsCaseInsensitive(string actual, string expected, bool shouldMatch)
    {
        Assert.Equal(shouldMatch, HashService.Matches(actual, expected));
    }
}
