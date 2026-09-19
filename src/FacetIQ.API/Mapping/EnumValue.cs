namespace FacetIQ.API.Mapping;

/// <summary>
/// Accepts a member name only, ignoring case. Enum.TryParse also accepts numbers, padding and
/// comma-joined names ("Regulatory,Clinical" parses as Social).
/// </summary>
internal static class EnumValue
{
    public static bool TryParse<T>(string value, out T parsed) where T : struct, Enum
    {
        var name = Enum.GetNames<T>()
            .FirstOrDefault(member => string.Equals(member, value, StringComparison.OrdinalIgnoreCase));

        parsed = name is null ? default : Enum.Parse<T>(name);

        return name is not null;
    }

    /// <summary>Null succeeds as null, since an absent condition is a wildcard.</summary>
    public static bool TryParseOptional<T>(string? value, out T? parsed) where T : struct, Enum
    {
        parsed = null;

        if (value is null)
        {
            return true;
        }

        if (!TryParse<T>(value, out var result))
        {
            return false;
        }

        parsed = result;

        return true;
    }
}
