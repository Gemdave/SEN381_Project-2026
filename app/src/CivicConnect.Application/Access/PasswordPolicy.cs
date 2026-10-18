using CivicConnect.Domain.Access;

namespace CivicConnect.Application.Access;
/*CR-003. Length is what protects a password, so the rule is a minimum length and 
a short list of obvious choices rather than forced symbols (NIST SP 800-63B). 
It is not a breached-password check; that is listed as future work.*/
public static class PasswordPolicy
{
    public const int MinLength = 12;
    public const int MaxLength = 128;

    private static readonly HashSet<string> Obvious = new(StringComparer.OrdinalIgnoreCase)
    {
        "password1234", "password12345", "passwordpassword", "123456789012", "1234567890123",
        "qwertyuiopas", "qwerty123456", "letmein123456", "welcome12345", "civicconnect", "civicconnect1"
    };

    /*-----Returns the reason the password is refused, or null when it is acceptable.-----*/
    public static string? Check(string? password, string? email)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < MinLength)
        {
            return $"Use at least {MinLength} characters.";
        }
        if (password.Length > MaxLength)
        {
            return $"Use at most {MaxLength} characters.";
        }
        var normalizedEmail = AppUser.NormalizeEmail(email);
        if (normalizedEmail.Length > 0 && string.Equals(password, normalizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            return "Do not use your email address as your password.";
        }

        if (Obvious.Contains(password) || password.Distinct().Count() < 4)
        {
            return "That password is too easy to guess. Choose something less obvious.";
        }

        return null;
    }
}
