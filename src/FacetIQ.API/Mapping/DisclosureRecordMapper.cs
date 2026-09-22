using FacetIQ.Contracts.Disclosure;
using FacetIQ.Domain.Entities;

namespace FacetIQ.API.Mapping;

public static class DisclosureRecordMapper
{
    /// <param name="requester">The requester's address, or null when their account no longer exists.</param>
    public static DisclosureRecordResponse ToContract(AuditRecord record, string? requester, bool isSelf)
    {
        return new DisclosureRecordResponse
        {
            Id = record.Id,
            Timestamp = record.Timestamp,
            Requester = requester,
            IsSelf = isSelf,
            AttributeKey = record.RequestedAttributeKey,
            Purpose = record.Purpose.ToString(),
            Outcome = record.Outcome.ToString(),
            DenyReason = record.DenyReason?.ToString(),
            Transform = record.Transform?.ToString(),
            TransformParameter = record.TransformParameter,
            JustifyingPrinciple = record.JustifyingPrinciple
        };
    }
}
