using FacetIQ.Contracts.Attributes;
using FacetIQ.Domain.Entities;
using FacetIQ.Domain.Enums;

namespace FacetIQ.API.Mapping;

public static class AttributeMapper
{
    /// <summary>
    /// Builds the domain claim from the contract and the authenticated subject. The collection
    /// purpose is parsed rather than model-bound so an unrecognised value is refused at the
    /// boundary instead of arriving as a default, which is how the other two mappers treat theirs.
    ///
    /// An absent purpose is a claim collected under no stated limit, which is a different thing
    /// from an unrecognised one and succeeds.
    /// </summary>
    public static bool TryToDomain(
        CreateAttributeRequest dto,
        Guid subjectId,
        out SubjectAttribute attribute)
    {
        attribute = null!;

        Purpose? collectedFor = null;

        if (dto.CollectedFor is not null)
        {
            if (!Enum.TryParse<Purpose>(dto.CollectedFor, ignoreCase: true, out var parsed))
            {
                return false;
            }

            collectedFor = parsed;
        }

        attribute = new SubjectAttribute
        {
            Id = Guid.NewGuid(),
            SubjectId = subjectId,
            Key = dto.Key,
            Value = dto.Value,
            Label = dto.Label,
            CollectedFor = collectedFor
        };

        return true;
    }

    public static AttributeResponse ToContract(SubjectAttribute attribute) => new()
    {
        Id = attribute.Id,
        Key = attribute.Key,
        Value = attribute.Value,
        Label = attribute.Label,
        CollectedFor = attribute.CollectedFor?.ToString()
    };
}
