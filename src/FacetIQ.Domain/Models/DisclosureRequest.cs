using FacetIQ.Domain.Enums;

namespace FacetIQ.Domain.Models;

/// <summary>
/// One request for a subject's claim, described in the terms a norm can match against.
/// The requester's identity comes from the authenticated principal, never from the route,
/// so the same endpoint returns different representations to different callers.
///
/// Purpose is the only condition a caller states. Relationship is absent by design: it is
/// resolved from standings the system holds, so no field here can place a requester in a
/// context the subject wrote a rule about.
/// </summary>
public sealed record DisclosureRequest(
    Guid SubjectId,
    string AttributeKey,
    string RequesterUserId,
    Purpose Purpose,
    RequestChannel Channel);
