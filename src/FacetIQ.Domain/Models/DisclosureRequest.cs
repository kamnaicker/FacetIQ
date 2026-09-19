using FacetIQ.Domain.Enums;

namespace FacetIQ.Domain.Models;

/// <summary>
/// A request for a subject's claim. The requester comes from the token. There is no relationship
/// field: relationships are resolved from standings.
/// </summary>
public sealed record DisclosureRequest(
    Guid SubjectId,
    string AttributeKey,
    string RequesterUserId,
    Purpose Purpose,
    RequestChannel Channel);
