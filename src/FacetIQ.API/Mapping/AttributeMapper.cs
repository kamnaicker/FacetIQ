using FacetIQ.Contracts.Attributes;
using FacetIQ.Domain.Entities;
using FacetIQ.Domain.Enums;

namespace FacetIQ.API.Mapping;

public static class AttributeMapper
{
    /// <summary>False when CollectedFor is present but not a recognised purpose.</summary>
    public static bool TryToDomain(
        CreateAttributeRequest dto,
        Guid subjectId,
        out SubjectAttribute attribute)
    {
        attribute = null!;

        if (!EnumValue.TryParseOptional<Purpose>(dto.CollectedFor, out var collectedFor))
        {
            return false;
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
