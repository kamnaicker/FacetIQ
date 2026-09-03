using FacetIQ.Domain.Abstractions.Repositories;
using FacetIQ.Domain.Abstractions.Services;
using FacetIQ.Domain.Entities;
using FacetIQ.Domain.Enums;
using FacetIQ.Domain.Models;

namespace FacetIQ.Services.Disclosure;

/// <summary>
/// Orders the disclosure pipeline. Every path through this class ends in exactly one audit
/// record, including the paths that refuse.
/// </summary>
public sealed class DisclosureEvaluator : IDisclosureEvaluator
{
    private readonly INormRepository _norms;
    private readonly IAttributeRepository _attributes;
    private readonly INormMatcher _matcher;
    private readonly ISpecificityRanker _ranker;
    private readonly ITransformService _transforms;
    private readonly IAuditWriter _audit;

    public DisclosureEvaluator(
        INormRepository norms,
        IAttributeRepository attributes,
        INormMatcher matcher,
        ISpecificityRanker ranker,
        ITransformService transforms,
        IAuditWriter audit)
    {
        _norms = norms;
        _attributes = attributes;
        _matcher = matcher;
        _ranker = ranker;
        _transforms = transforms;
        _audit = audit;
    }

    public async Task<DisclosureResult> EvaluateAsync(
        DisclosureRequest request,
        CancellationToken cancellationToken)
    {
        var governing = await _norms.GetGoverningNormsAsync(request.SubjectId, request.AttributeKey, cancellationToken);
        var candidates = _matcher.Match(governing, request);
        var selection = _ranker.Select(candidates);

        var result = selection.Outcome switch
        {
            SelectionOutcome.NoMatch => DisclosureResult.Denied(DenyReasonCode.NoMatchingNorm),
            SelectionOutcome.Ambiguous => DisclosureResult.Denied(DenyReasonCode.AmbiguousNorms),
            SelectionOutcome.Selected => await ResolveAsync(selection.Norm!, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(selection), selection.Outcome, "Unhandled selection.")
        };

        await _audit.RecordAsync(request, result, cancellationToken);

        return result;
    }

    /// <summary>
    /// Selection is already complete: the norm names the claim to disclose and the form to
    /// disclose it in. Nothing below this point consults the request, so context cannot be
    /// revisited once it has been decided.
    /// </summary>
    private async Task<DisclosureResult> ResolveAsync(Norm norm, CancellationToken cancellationToken)
    {
        if (norm.Action == ActionType.Deny)
        {
            return DisclosureResult.Denied(norm.DenyReason ?? DenyReasonCode.PurposeIncompatible, norm);
        }

        var claim = await _attributes.FindAsync(norm.AttributeId, cancellationToken);

        if (claim is null)
        {
            return DisclosureResult.Denied(DenyReasonCode.ClaimUnavailable, norm);
        }

        var value = _transforms.Apply(norm.Transform, norm.TransformParameter, claim.Value);

        return DisclosureResult.Disclosed(norm, value);
    }
}
