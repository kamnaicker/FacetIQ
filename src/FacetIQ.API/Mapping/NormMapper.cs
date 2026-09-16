using FacetIQ.Contracts.Norms;
using FacetIQ.Domain.Entities;
using FacetIQ.Domain.Enums;
using FacetIQ.Domain.Models;

namespace FacetIQ.API.Mapping;

public static class NormMapper
{
    /// <param name="invalidMember">The request field that failed to parse, for the error response.</param>
    public static bool TryToDomain(
        CreateNormRequest dto,
        Guid subjectId,
        out Norm norm,
        out string? invalidMember)
    {
        norm = null!;
        invalidMember = null;

        if (!EnumValue.TryParseOptional<Purpose>(dto.Purpose, out var purpose))
        {
            invalidMember = nameof(dto.Purpose);
            return false;
        }

        if (!EnumValue.TryParseOptional<TransformKind>(dto.Transform, out var transform))
        {
            invalidMember = nameof(dto.Transform);
            return false;
        }

        // Every other reason is the engine's to give.
        if (!EnumValue.TryParseOptional<DenyReasonCode>(dto.DenyReason, out var denyReason) ||
            denyReason is not (null or DenyReasonCode.RefusedByRule))
        {
            invalidMember = nameof(dto.DenyReason);
            return false;
        }

        norm = new Norm
        {
            Id = Guid.NewGuid(),
            Version = 1,
            SubjectId = subjectId,
            AttributeId = dto.AttributeId,
            Relationship = dto.Relationship,
            Purpose = purpose,
            Action = DeriveAction(transform, denyReason),
            Transform = transform ?? TransformKind.None,
            TransformParameter = dto.TransformParameter,
            DenyReason = denyReason,
            JustifyingPrinciple = dto.JustifyingPrinciple
        };

        return true;
    }

    public static NormResponse ToContract(Norm norm) => new()
    {
        Id = norm.Id,
        Version = norm.Version,
        AttributeId = norm.AttributeId,
        Relationship = norm.Relationship,
        Purpose = norm.Purpose?.ToString(),
        Action = norm.Action.ToString(),
        Transform = norm.Transform.ToString(),
        TransformParameter = norm.TransformParameter,
        DenyReason = norm.DenyReason?.ToString(),
        JustifyingPrinciple = norm.JustifyingPrinciple,
        Specificity = NormSpecificity.Of(norm)
    };

    public static NormConflictResponse ToContract(IReadOnlyList<NormConflict> conflicts) => new()
    {
        Collisions =
        [
            .. conflicts.Select(conflict => new NormCollision
            {
                Existing = ToContract(conflict.Existing),
                Specificity = conflict.Specificity,
                OverlappingRelationship = conflict.OverlappingRelationship,
                OverlappingPurpose = conflict.OverlappingPurpose?.ToString()
            })
        ]
    };

    private static ActionType DeriveAction(TransformKind? transform, DenyReasonCode? denyReason) =>
        denyReason is not null ? ActionType.Deny
            : transform is not null && transform != TransformKind.None ? ActionType.Transform
            : ActionType.Return;
}
