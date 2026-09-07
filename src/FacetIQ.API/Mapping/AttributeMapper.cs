using FacetIQ.Contracts.Attributes;
using FacetIQ.Domain.Entities;
using FacetIQ.Domain.Enums;

namespace FacetIQ.API.Mapping;

public static class AttributeMapper
{
    /// <summary>
    /// The collection purpose is parsed rather than model-bound, as in the other two mappers.
    /// An absent purpose is no stated limit and succeeds; only an unrecognised one fails.
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
