using FacetIQ.Domain.Enums;

namespace FacetIQ.Domain.Models;

/// <summary>
/// One request for a subject's claim, described in the terms a norm can match against.
/// The requester's identity comes from the authenticated principal, never from the route,
/// so the same endpoint returns different representations to different callers.
/// </summary>
public sealed record DisclosureRequest(
    Guid SubjectId,
    string AttributeKey,
    string RequesterUserId,
    string? Relationship,
    Purpose Purpose,
    RequestChannel Channel);
