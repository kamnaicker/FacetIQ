using FacetIQ.Contracts.Disclosure;
using FacetIQ.Domain.Enums;
using FacetIQ.Domain.Models;

namespace FacetIQ.API.Mapping;

public static class DisclosureMapper
{
    /// <summary>False when the purpose is not a recognised name.</summary>
    public static bool TryToDomain(
        DisclosureRequestDto dto,
        Guid subjectId,
        string requesterUserId,
        RequestChannel channel,
        out DisclosureRequest request)
    {
        if (!EnumValue.TryParse<Purpose>(dto.Purpose, out var purpose))
        {
            request = null!;
            return false;
        }

        request = new DisclosureRequest(
            subjectId,
            dto.AttributeKey,
            requesterUserId,
            purpose,
            channel);

        return true;
    }

    public static DisclosureResponseDto ToContract(DisclosureResult result) => new()
    {
        Outcome = result.Outcome.ToString(),
        Value = result.Value,
        Values = result.Values,
        DenyReason = result.DenyReason?.ToString(),
        JustifyingPrinciple = result.Norm?.JustifyingPrinciple
    };
}
