using System.Text.RegularExpressions;
using Platform.Domain.Common;

namespace Platform.Domain.ValueObjects;

/// <summary>
/// URL-friendly slug value object.
/// </summary>
public sealed record Slug
{
    private static readonly Regex SlugPattern = new(
        @"^[a-z0-9]+(?:-[a-z0-9]+)*$",
        RegexOptions.Compiled);

    public string Value { get; }

    private Slug(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Create slug from string (validates format).
    /// </summary>
    public static Slug From(string value)
    {
        Guard.Against.NullOrWhiteSpace(value, nameof(value));

        value = value.Trim().ToLowerInvariant();

        if (!SlugPattern.IsMatch(value))
            throw new DomainException(
                $"Invalid slug format: '{value}'. Slug must contain only lowercase letters, numbers, and hyphens.");

        return new Slug(value);
    }

    /// <summary>
    /// Generate slug from title.
    /// </summary>
    public static Slug FromTitle(string title)
    {
        Guard.Against.NullOrWhiteSpace(title, nameof(title));

        var slug = title.Trim().ToLowerInvariant();

        // Replace Turkish characters
        slug = slug
            .Replace("ç", "c")
            .Replace("ğ", "g")
            .Replace("ı", "i")
            .Replace("ö", "o")
            .Replace("ş", "s")
            .Replace("ü", "u");

        // Replace spaces and special chars with hyphen
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"[\s_]+", "-");
        slug = Regex.Replace(slug, @"-+", "-");
        slug = slug.Trim('-');

        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainException("Could not generate valid slug from title.");

        return new Slug(slug);
    }

    /// <summary>
    /// Implicit conversion to string.
    /// </summary>
    public static implicit operator string(Slug slug) => slug.Value;

    public override string ToString() => Value;
}
