using FacetIQ.Contracts.Norms;
using FacetIQ.Domain.Entities;
using FacetIQ.Domain.Enums;
using FacetIQ.Domain.Models;

namespace FacetIQ.API.Mapping;

public static class NormMapper
{
    /// <summary>
    /// Enums are parsed rather than model-bound, so an unrecognised value is refused at the
    /// boundary instead of arriving as a default. The action is derived from the transform and
    /// deny reason, as <see cref="DisclosureResult.Disclosed"/> derives its outcome.
    /// </summary>
    /// <param name="invalidMember">
    /// Which field was not recognised, so the refusal can name it. Three can fail here, unlike
    /// the other mappers.
    /// </param>
    public static bool TryToDomain(
        CreateNormRequest dto,
        Guid subjectId,
        out Norm norm,
        out string? invalidMember)
    {
        norm = null!;
        invalidMember = null;

        if (!TryParseOptional<Purpose>(dto.Purpose, out var purpose))
        {
            invalidMember = nameof(dto.Purpose);
            return false;
        }

        if (!TryParseOptional<TransformKind>(dto.Transform, out var transform))
        {
            invalidMember = nameof(dto.Transform);
            return false;
        }

        if (!TryParseOptional<DenyReasonCode>(dto.DenyReason, out var denyReason))
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

    /// <summary>An absent value is a wildcard and succeeds as null; only a present one can fail.</summary>
    private static bool TryParseOptional<T>(string? value, out T? parsed) where T : struct, Enum
    {
        if (value is null)
        {
            parsed = null;
            return true;
        }

        if (Enum.TryParse<T>(value, ignoreCase: true, out var result))
        {
            parsed = result;
            return true;
        }

        parsed = null;
        return false;
    }
}
