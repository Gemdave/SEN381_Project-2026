using System.Security.Cryptography;
using CivicConnect.Application.Abstractions;

namespace CivicConnect.Infrastructure.Security;

/// <summary>
/// CR-003. PBKDF2 with HMAC-SHA256, a random 16 byte salt per password and a 32 byte
/// key, from the .NET base library (no extra package to patch or verify). The stored
/// form is "v1.iterations.salt.key", so the cost can be raised later and old hashes
/// still verify. 600,000 iterations is the OWASP guidance for PBKDF2-HMAC-SHA256.
/// The comparison is constant time.
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    public const int DefaultIterations = 600_000;
    public const int MinimumIterations = 100_000;

    private const string Version = "v1";
    private const int SaltBytes = 16;
    private const int KeyBytes = 32;
    private const int MaxAcceptedIterations = 5_000_000;

    private static readonly byte[] DummySalt = new byte[SaltBytes];
    private readonly int iterations;

    public Pbkdf2PasswordHasher(int iterations = DefaultIterations)
        => this.iterations = Math.Max(iterations, MinimumIterations);

    public string Hash(string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, KeyBytes);
        return $"{Version}.{iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
    }

    public bool Verify(string? password, string? hash)
    {
        password ??= string.Empty;

        if (!TryParse(hash, out var cost, out var salt, out var expected))
        {
            // Same work as a real check, so the response time does not say whether the account exists.
            _ = Rfc2898DeriveBytes.Pbkdf2(password, DummySalt, iterations, HashAlgorithmName.SHA256, KeyBytes);
            return false;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, cost, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static bool TryParse(string? hash, out int cost, out byte[] salt, out byte[] key)
    {
        cost = 0;
        salt = [];
        key = [];

        var parts = hash?.Split('.');
        if (parts is not { Length: 4 } || parts[0] != Version
            || !int.TryParse(parts[1], out cost) || cost < 1 || cost > MaxAcceptedIterations)
        {
            return false;
        }

        try
        {
            salt = Convert.FromBase64String(parts[2]);
            key = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        return salt.Length > 0 && key.Length > 0;
    }
}
