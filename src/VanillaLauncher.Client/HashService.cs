using System.Security.Cryptography;

namespace VanillaLauncher.Client;

public static class HashService
{
    public static async Task<string> ComputeSha256Async(string filePath, CancellationToken ct = default)
    {
        await using var stream = File.OpenRead(filePath);
        var hashBytes = await SHA256.HashDataAsync(stream, ct);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public static bool Matches(string actualHash, string expectedHash) =>
        string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase);
}
