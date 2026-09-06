namespace FacetIQ.Domain.Enums;

/// <summary>
/// How the request reached the system. Recorded for audit; not a norm condition.
/// </summary>
public enum RequestChannel
{
    Api,
    Web
}
