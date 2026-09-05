namespace FacetIQ.Domain.Enums;

/// <summary>
/// Who asserted a standing. An institution vouches for a requester the subject has never met;
/// a subject vouches for someone they already know. Neither can be the requester themselves.
/// </summary>
public enum IssuerKind
{
    Institution,
    Subject
}
