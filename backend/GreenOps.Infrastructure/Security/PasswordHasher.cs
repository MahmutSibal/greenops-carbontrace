using System.Security.Cryptography;
using System.Text;

namespace GreenOps.Infrastructure.Security;

public static class PasswordHasher
{
    public static string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    /// <summary>BCrypt hash'i doğrular; eski SHA256 hash'ler için de çalışır (needsRehash = true).</summary>
    public static bool Verify(string password, string hash, out bool needsRehash)
    {
        needsRehash = false;
        if (string.IsNullOrEmpty(hash))
            return false;

        if (hash.StartsWith("$2"))
            return BCrypt.Net.BCrypt.Verify(password, hash);

        var legacy = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(password)));
        var ok = CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(legacy), Encoding.UTF8.GetBytes(hash));
        needsRehash = ok;
        return ok;
    }
}

public static class PasswordPolicy
{
    public const int MinLength = 8;

    /// <summary>Kuralı sağlamıyorsa hata mesajı, sağlıyorsa null döner.</summary>
    public static string? Validate(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < MinLength)
            return $"Şifre en az {MinLength} karakter olmalıdır.";
        if (password.Length > 128)
            return "Şifre en fazla 128 karakter olabilir.";
        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            return "Şifre en az bir harf ve bir rakam içermelidir.";
        return null;
    }
}

public static class TokenHasher
{
    /// <summary>Gizli token'ları (şifre sıfırlama, API anahtarı) DB'de düz metin yerine SHA-256 hex olarak saklar.</summary>
    public static string Sha256(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    public static string NewUrlSafeToken(int bytes = 32)
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(bytes))
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');
}
