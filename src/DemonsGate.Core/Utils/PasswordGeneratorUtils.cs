using System.Security.Cryptography;
using System.Text;

namespace DemonsGate.Core.Utils;

/// <summary>
/// Utility class for generating secure passwords
/// </summary>
public static class PasswordGeneratorUtils
{
    private const string LowercaseChars = "abcdefghijklmnopqrstuvwxyz";
    private const string UppercaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string DigitChars = "0123456789";
    private const string SpecialChars = "!@#$%^&*()_-+=[]{}|;:,.<>?";

    /// <summary>
    /// Options for password generation
    /// </summary>
    public class PasswordOptions
    {
        public int Length { get; set; } = 16;
        public bool IncludeLowercase { get; set; } = true;
        public bool IncludeUppercase { get; set; } = true;
        public bool IncludeDigits { get; set; } = true;
        public bool IncludeSpecialChars { get; set; } = true;
        public bool RequireFromEachSet { get; set; } = true;
    }

    /// <summary>
    /// Generates a secure random password
    /// </summary>
    /// <param name="options">Password generation options</param>
    /// <returns>Generated password</returns>
    public static string GeneratePassword(PasswordOptions? options = null)
    {
        options ??= new PasswordOptions();

        if (options.Length < 1)
            throw new ArgumentException("Password length must be at least 1", nameof(options));

        var charSet = BuildCharacterSet(options);
        if (string.IsNullOrEmpty(charSet))
            throw new ArgumentException("At least one character type must be enabled", nameof(options));

        var password = new StringBuilder(options.Length);
        var requiredChars = new List<char>();

        // Add at least one character from each required set
        if (options.RequireFromEachSet)
        {
            if (options.IncludeLowercase)
                requiredChars.Add(GetRandomChar(LowercaseChars));
            if (options.IncludeUppercase)
                requiredChars.Add(GetRandomChar(UppercaseChars));
            if (options.IncludeDigits)
                requiredChars.Add(GetRandomChar(DigitChars));
            if (options.IncludeSpecialChars)
                requiredChars.Add(GetRandomChar(SpecialChars));
        }

        // Add remaining random characters
        var remainingLength = options.Length - requiredChars.Count;
        for (int i = 0; i < remainingLength; i++)
        {
            password.Append(GetRandomChar(charSet));
        }

        // Shuffle required chars and append
        Shuffle(requiredChars);
        foreach (var ch in requiredChars)
        {
            password.Append(ch);
        }

        // Final shuffle of the entire password
        return Shuffle(password.ToString());
    }

    /// <summary>
    /// Generates a simple password with default options
    /// </summary>
    /// <param name="length">Password length (default: 16)</param>
    /// <returns>Generated password</returns>
    public static string GeneratePassword(int length)
    {
        return GeneratePassword(new PasswordOptions { Length = length });
    }

    /// <summary>
    /// Generates a PIN (digits only)
    /// </summary>
    /// <param name="length">PIN length (default: 6)</param>
    /// <returns>Generated PIN</returns>
    public static string GeneratePin(int length = 6)
    {
        return GeneratePassword(new PasswordOptions
        {
            Length = length,
            IncludeLowercase = false,
            IncludeUppercase = false,
            IncludeDigits = true,
            IncludeSpecialChars = false,
            RequireFromEachSet = false
        });
    }

    /// <summary>
    /// Generates a passphrase-style password (lowercase and digits only, easier to type)
    /// </summary>
    /// <param name="length">Password length (default: 12)</param>
    /// <returns>Generated passphrase</returns>
    public static string GeneratePassphrase(int length = 12)
    {
        return GeneratePassword(new PasswordOptions
        {
            Length = length,
            IncludeLowercase = true,
            IncludeUppercase = false,
            IncludeDigits = true,
            IncludeSpecialChars = false,
            RequireFromEachSet = false
        });
    }

    /// <summary>
    /// Generates a highly secure password with all character types
    /// </summary>
    /// <param name="length">Password length (default: 32)</param>
    /// <returns>Generated secure password</returns>
    public static string GenerateSecurePassword(int length = 32)
    {
        return GeneratePassword(new PasswordOptions
        {
            Length = length,
            IncludeLowercase = true,
            IncludeUppercase = true,
            IncludeDigits = true,
            IncludeSpecialChars = true,
            RequireFromEachSet = true
        });
    }

    private static string BuildCharacterSet(PasswordOptions options)
    {
        var charSet = new StringBuilder();

        if (options.IncludeLowercase)
            charSet.Append(LowercaseChars);
        if (options.IncludeUppercase)
            charSet.Append(UppercaseChars);
        if (options.IncludeDigits)
            charSet.Append(DigitChars);
        if (options.IncludeSpecialChars)
            charSet.Append(SpecialChars);

        return charSet.ToString();
    }

    private static char GetRandomChar(string charSet)
    {
        var index = RandomNumberGenerator.GetInt32(0, charSet.Length);
        return charSet[index];
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static string Shuffle(string str)
    {
        var chars = str.ToCharArray().ToList();
        Shuffle(chars);
        return new string(chars.ToArray());
    }
}
