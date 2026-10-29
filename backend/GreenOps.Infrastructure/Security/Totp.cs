using System.Security.Cryptography;
using System.Text;

namespace GreenOps.Infrastructure.Security;

/// <summary>RFC 6238 TOTP (SHA-1, 6 hane, 30 sn) — Google/Microsoft Authenticator ile uyumlu.</summary>
public static class Totp
{
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
    private const int StepSeconds = 30;
    private const int Digits = 6;

    public static string GenerateSecret() => ToBase32(RandomNumberGenerator.GetBytes(20));

    /// <summary>Saat kaymasına karşı ±1 adım (±30 sn) toleransla doğrular.</summary>
    public static bool Verify(string base32Secret, string? code, DateTimeOffset? now = null)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != Digits || !code.All(char.IsDigit))
            return false;

        byte[] key;
        try { key = FromBase32(base32Secret); }
        catch (FormatException) { return false; }

        var counter = (now ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds() / StepSeconds;
        var expected = Encoding.ASCII.GetBytes(code);
        for (var offset = -1; offset <= 1; offset++)
        {
            var candidate = Encoding.ASCII.GetBytes(Compute(key, counter + offset));
            if (CryptographicOperations.FixedTimeEquals(candidate, expected))
                return true;
        }
        return false;
    }

    public static string Compute(byte[] key, long counter)
    {
        var counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian) Array.Reverse(counterBytes);

        var hash = HMACSHA1.HashData(key, counterBytes);
        var offset = hash[^1] & 0x0F;
        var binary = ((hash[offset] & 0x7F) << 24)
                     | (hash[offset + 1] << 16)
                     | (hash[offset + 2] << 8)
                     | hash[offset + 3];
        return (binary % (int)Math.Pow(10, Digits)).ToString().PadLeft(Digits, '0');
    }

    public static string ToBase32(byte[] data)
    {
        var sb = new StringBuilder();
        int buffer = 0, bits = 0;
        foreach (var b in data)
        {
            buffer = (buffer << 8) | b;
            bits += 8;
            while (bits >= 5)
            {
                sb.Append(Base32Alphabet[(buffer >> (bits - 5)) & 31]);
                bits -= 5;
            }
        }
        if (bits > 0)
            sb.Append(Base32Alphabet[(buffer << (5 - bits)) & 31]);
        return sb.ToString();
    }

    public static byte[] FromBase32(string input)
    {
        var clean = input.TrimEnd('=').Replace(" ", "").ToUpperInvariant();
        var bytes = new List<byte>();
        int buffer = 0, bits = 0;
        foreach (var c in clean)
        {
            var value = Base32Alphabet.IndexOf(c);
            if (value < 0) throw new FormatException("Geçersiz Base32 karakteri.");
            buffer = (buffer << 5) | value;
            bits += 5;
            if (bits >= 8)
            {
                bytes.Add((byte)((buffer >> (bits - 8)) & 0xFF));
                bits -= 8;
            }
        }
        return bytes.ToArray();
    }
}
