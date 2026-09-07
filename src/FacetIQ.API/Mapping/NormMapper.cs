using FacetIQ.Contracts.Norms;
using FacetIQ.Domain.Entities;
using FacetIQ.Domain.Enums;
using FacetIQ.Domain.Models;

namespace FacetIQ.API.Mapping;

public static class NormMapper
{
    /// <summary>
    /// Builds the domain norm from the contract and the authenticated subject. Enums are parsed
    /// rather than model-bound so an unrecognised value is refused at the boundary instead of
    /// arriving as a default, which is how DisclosureMapper already treats purpose.
    ///
    /// The action is derived from the transform and the deny reason rather than accepted from the
    /// caller, on the same reasoning as <see cref="DisclosureResult.Disclosed"/>: stated
    /// separately it could contradict them, and there is no sensible way to resolve that.
    /// </summary>
    /// <param name="invalidMember">
    /// Which field was not recognised, so the refusal can name it. Three fields here can fail,
    /// unlike the other two mappers where only one can, which is why this one has to report it.
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

    /// <summary>
    /// An absent value is a wildcard and parses successfully to null. Only a value that is
    /// present and unrecognised is a failure.
    /// </summary>
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
