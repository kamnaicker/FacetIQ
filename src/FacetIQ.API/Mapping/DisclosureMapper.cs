using FacetIQ.Contracts.Disclosure;
using FacetIQ.Domain.Enums;
using FacetIQ.Domain.Models;

namespace FacetIQ.API.Mapping;

public static class DisclosureMapper
{
    /// <summary>
    /// Builds the domain request from the contract and the authenticated caller. Purpose is
    /// parsed rather than bound so an unrecognised value is rejected at the boundary instead
    /// of arriving at the engine as a default.
    /// </summary>
    public static bool TryToDomain(
        DisclosureRequestDto dto,
        string requesterUserId,
        RequestChannel channel,
        out DisclosureRequest request)
    {
        if (!Enum.TryParse<Purpose>(dto.Purpose, ignoreCase: true, out var purpose))
        {
            request = null!;
            return false;
        }

        request = new DisclosureRequest(
            dto.SubjectId,
            dto.AttributeKey,
            requesterUserId,
            dto.Relationship,
            purpose,
            channel);

        return true;
    }

    public static DisclosureResponseDto ToContract(DisclosureResult result) => new()
    {
        Outcome = result.Outcome.ToString(),
        Value = result.Value,
        DenyReason = result.DenyReason?.ToString(),
        JustifyingPrinciple = result.Norm?.JustifyingPrinciple
    };
}
