using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace VanillaLauncher.Admin;

/// <summary>
/// Пароль Admin-режима: только соль + PBKDF2-хеш на диске, plaintext нигде не хранится
/// и не логируется. Файл живёт рядом с exe (там же, где appsettings.json) — это
/// build output, он не коммитится (bin/ в .gitignore).
///
/// "Привязка к ПК" из SPEC.md (опциональный пункт) не реализована — файл и так
/// живёт только на конкретной машине; отдельный крипто-механизм привязки посчитали
/// избыточным для приватного сервера на несколько друзей.
/// </summary>
public sealed class AdminAuthService
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;

    private readonly string _credentialsPath;

    public AdminAuthService(string credentialsPath)
    {
        _credentialsPath = credentialsPath;
    }

    public bool HasPassword() => File.Exists(_credentialsPath);

    public void SetPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Пароль не может быть пустым.", nameof(password));

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = ComputeHash(password, salt, Iterations);

        var record = new CredentialRecord
        {
            Salt = Convert.ToBase64String(salt),
            Hash = Convert.ToBase64String(hash),
            Iterations = Iterations
        };

        File.WriteAllText(_credentialsPath, JsonSerializer.Serialize(record));
    }

    public bool VerifyPassword(string password)
    {
        if (!File.Exists(_credentialsPath))
            return false;

        var record = JsonSerializer.Deserialize<CredentialRecord>(File.ReadAllText(_credentialsPath));
        if (record is null)
            return false;

        var salt = Convert.FromBase64String(record.Salt);
        var expectedHash = Convert.FromBase64String(record.Hash);
        var actualHash = ComputeHash(password, salt, record.Iterations);

        return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
    }

    private static byte[] ComputeHash(string password, byte[] salt, int iterations) =>
        Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, iterations, HashAlgorithmName.SHA256, HashSize);

    private sealed class CredentialRecord
    {
        public string Salt { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty;
        public int Iterations { get; set; }
    }
}
