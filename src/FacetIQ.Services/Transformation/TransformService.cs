using System.Globalization;
using FacetIQ.Domain.Abstractions.Services;
using FacetIQ.Domain.Enums;

namespace FacetIQ.Services.Transformation;

/// <summary>Each transform returns something coarser that is still true of the subject.</summary>
public sealed class TransformService : ITransformService
{
    private const string RedactedValue = "[redacted]";
    private const int DefaultAgeThreshold = 18;

    private readonly TimeProvider _clock;

    public TransformService(TimeProvider clock) => _clock = clock;

    public bool TryApply(TransformKind kind, string? parameter, string value, out string result)
    {
        switch (kind)
        {
            case TransformKind.None:
                result = value;
                return true;

            case TransformKind.Redact:
                result = RedactedValue;
                return true;

            case TransformKind.Reformat:
                result = ToInitials(value);
                return true;

            case TransformKind.Generalise:
                return TryToAgeBand(value, parameter, out result);

            default:
                result = string.Empty;
                return false;
        }
    }

    // "Amara Nwosu" -> "A. N."
    private static string ToInitials(string value)
    {
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return string.Join(' ', parts.Select(part => $"{char.ToUpperInvariant(part[0])}."));
    }

    // "1994-03-11" with threshold 18 -> "over 18"
    private bool TryToAgeBand(string value, string? parameter, out string result)
    {
        result = string.Empty;

        if (!DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOfBirth))
        {
            return false;
        }

        var threshold = DefaultAgeThreshold;

        if (parameter is not null &&
            (!int.TryParse(parameter, NumberStyles.None, CultureInfo.InvariantCulture, out threshold) || threshold <= 0))
        {
            return false;
        }

        var today = DateOnly.FromDateTime(_clock.GetUtcNow().UtcDateTime);
        var age = today.Year - dateOfBirth.Year;

        if (dateOfBirth > today.AddYears(-age))
        {
            age--;
        }

        result = age >= threshold ? $"over {threshold}" : $"under {threshold}";
        return true;
    }
}
