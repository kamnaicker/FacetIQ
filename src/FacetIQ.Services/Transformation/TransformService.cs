using System.Globalization;
using FacetIQ.Domain.Abstractions.Services;
using FacetIQ.Domain.Enums;

namespace FacetIQ.Services.Transformation;

/// <summary>
/// Produces a coarser representation of a claim. Each transform returns a value that is
/// still true of the subject; none of them substitutes information the subject did not give.
/// </summary>
public sealed class TransformService : ITransformService
{
    private const string RedactedValue = "[redacted]";

    public string Apply(TransformKind kind, string? parameter, string value) => kind switch
    {
        TransformKind.None => value,
        TransformKind.Redact => RedactedValue,
        TransformKind.Reformat => ToInitials(value),
        TransformKind.Generalise => ToAgeBand(value, parameter),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported transform.")
    };

    /// <summary>"Amara Nwosu" becomes "A. N." -- enough to confirm a match, not to identify.</summary>
    private static string ToInitials(string value)
    {
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return string.Join(' ', parts.Select(part => $"{char.ToUpperInvariant(part[0])}."));
    }

    /// <summary>
    /// A date of birth becomes a threshold statement such as "over 18". The returned value
    /// was never stored, which is the step selective disclosure alone cannot take.
    /// </summary>
    private static string ToAgeBand(string value, string? parameter)
    {
        if (!DateOnly.TryParse(value, CultureInfo.InvariantCulture, out var dateOfBirth))
        {
            throw new InvalidOperationException("Generalise expects a date value.");
        }

        var threshold = int.Parse(parameter ?? "18", CultureInfo.InvariantCulture);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - dateOfBirth.Year;

        if (dateOfBirth > today.AddYears(-age))
        {
            age--;
        }

        return age >= threshold ? $"over {threshold}" : $"under {threshold}";
    }
}
