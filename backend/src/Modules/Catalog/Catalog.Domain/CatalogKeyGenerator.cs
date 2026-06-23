using System.Text;
using SharedKernel;

namespace Catalog.Domain;

/// <summary>Özellik ve varyant anahtarlarını ad/etiketten ortak biçimde üretir.</summary>
internal static class CatalogKeyGenerator
{
    public const int MaxLength = 200;

    public static Result<string> GenerateFromName(string name)
    {
        var normalized = name.ToLowerInvariant()
            .Replace('ı', 'i')
            .Replace('ş', 's')
            .Replace('ğ', 'g')
            .Replace('ü', 'u')
            .Replace('ö', 'o')
            .Replace('ç', 'c');

        var builder = new StringBuilder(normalized.Length);
        var pendingSeparator = false;

        foreach (var ch in normalized)
        {
            if (char.IsAsciiLetterOrDigit(ch))
            {
                if (pendingSeparator && builder.Length > 0)
                {
                    builder.Append('_');
                }

                builder.Append(ch);
                pendingSeparator = false;
                continue;
            }

            if (!pendingSeparator && builder.Length > 0)
            {
                pendingSeparator = true;
            }
        }

        if (builder.Length == 0)
        {
            return Result.Failure<string>(Error.Validation("Key is required."));
        }

        if (builder.Length > MaxLength)
        {
            return Result.Failure<string>(Error.Validation($"Key must be at most {MaxLength} characters."));
        }

        return Result.Success(builder.ToString().ToUpperInvariant());
    }

    public static Result<string> ValidateExplicit(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<string>(Error.Validation("Key is required."));
        }

        var trimmed = value.Trim();
        if (trimmed.Length > MaxLength)
        {
            return Result.Failure<string>(Error.Validation($"Key must be at most {MaxLength} characters."));
        }

        return Result.Success(trimmed);
    }
}
