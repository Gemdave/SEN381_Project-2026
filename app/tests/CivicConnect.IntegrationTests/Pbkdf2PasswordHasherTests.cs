using CivicConnect.Infrastructure.Security;
using Xunit;

namespace CivicConnect.IntegrationTests;

/// <summary>CR-003: the password hash. No database is needed.</summary>
[Trait("TestCase", "CR-003")]
public class Pbkdf2PasswordHasherTests
{
    private static readonly Pbkdf2PasswordHasher Hasher = new(Pbkdf2PasswordHasher.MinimumIterations);

    [Fact]
    public void A_password_verifies_against_its_own_hash_and_nothing_else()
    {
        var hash = Hasher.Hash("correct horse battery");

        Assert.True(Hasher.Verify("correct horse battery", hash));
        Assert.False(Hasher.Verify("correct horse batterz", hash));
        Assert.False(Hasher.Verify("", hash));
        Assert.False(Hasher.Verify(null, hash));
    }

    [Fact]
    public void The_hash_never_contains_the_password_and_is_salted()
    {
        var first = Hasher.Hash("correct horse battery");
        var second = Hasher.Hash("correct horse battery");

        Assert.DoesNotContain("correct horse battery", first);
        Assert.StartsWith("v1." + Pbkdf2PasswordHasher.MinimumIterations + ".", first);
        Assert.NotEqual(first, second);
        Assert.True(Hasher.Verify("correct horse battery", second));
    }

    [Fact]
    public void A_hash_made_at_a_different_cost_still_verifies()
    {
        var older = new Pbkdf2PasswordHasher(Pbkdf2PasswordHasher.MinimumIterations + 50_000).Hash("correct horse battery");

        Assert.True(Hasher.Verify("correct horse battery", older));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("plain text")]
    [InlineData("v1.100000.not-base64.not-base64")]
    [InlineData("v2.100000.AAAA.AAAA")]
    [InlineData("v1.0.AAAA.AAAA")]
    [InlineData("v1.999999999.AAAA.AAAA")]
    [InlineData("v1.100000.AAAA")]
    public void A_missing_or_malformed_hash_never_verifies_and_never_throws(string? hash)
        => Assert.False(Hasher.Verify("correct horse battery", hash));

    [Fact]
    public void The_cost_has_a_floor()
        => Assert.StartsWith("v1." + Pbkdf2PasswordHasher.MinimumIterations + ".", new Pbkdf2PasswordHasher(1).Hash("x"));
}
