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
    private readonly ISubjectRepository _subjects;
    private readonly INormRepository _norms;
    private readonly IAttributeRepository _attributes;
    private readonly INormMatcher _matcher;
    private readonly ISpecificityRanker _ranker;
    private readonly ITransformService _transforms;
    private readonly IAuditWriter _audit;

    public DisclosureEvaluator(
        ISubjectRepository subjects,
        INormRepository norms,
        IAttributeRepository attributes,
        INormMatcher matcher,
        ISpecificityRanker ranker,
        ITransformService transforms,
        IAuditWriter audit)
    {
        _subjects = subjects;
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
        var subject = await _subjects.FindAsync(request.SubjectId, cancellationToken);

        var result = subject is not null && subject.UserId == request.RequesterUserId
            ? await SelfAccessAsync(request, cancellationToken)
            : await GovernedAsync(request, cancellationToken);

        await _audit.RecordAsync(request, result, cancellationToken);

        return result;
    }

    /// <summary>
    /// A subject reading their own claims receives all of them, in the form they were stored.
    /// This precedes norm lookup deliberately: a right of access is not something the subject's
    /// own rules can narrow.
    /// </summary>
    private async Task<DisclosureResult> SelfAccessAsync(
        DisclosureRequest request,
        CancellationToken cancellationToken)
    {
        var claims = await _attributes.ListByKeyAsync(request.SubjectId, request.AttributeKey, cancellationToken);

        return DisclosureResult.SelfAccess(claims.Select(claim => claim.Value).ToList());
    }

    private async Task<DisclosureResult> GovernedAsync(
        DisclosureRequest request,
        CancellationToken cancellationToken)
    {
        var governing = await _norms.GetGoverningNormsAsync(request.SubjectId, request.AttributeKey, cancellationToken);
        var candidates = _matcher.Match(governing, request);
        var selection = _ranker.Select(candidates);

        return selection.Outcome switch
        {
            SelectionOutcome.NoMatch => DisclosureResult.Denied(DenyReasonCode.NoMatchingNorm),
            SelectionOutcome.Ambiguous => DisclosureResult.Denied(DenyReasonCode.AmbiguousNorms),
            SelectionOutcome.Selected => await ResolveAsync(selection.Norm!, request.Purpose, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(selection), selection.Outcome, "Unhandled selection.")
        };
    }

    /// <summary>
    /// Selection is already complete: the norm names the claim to disclose and the form to
    /// disclose it in. Only the purpose travels this far, and only to test it against the
    /// claim's collection limit, so matching cannot be revisited once it has been decided.
    /// </summary>
    private async Task<DisclosureResult> ResolveAsync(
        Norm norm,
        Purpose purpose,
        CancellationToken cancellationToken)
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

        // A claim collected for one purpose is not released for another. This is the only
        // point the engine refuses what the subject's own norm permits: a purpose limit is
        // undertaken at collection, and a later rule cannot dissolve it. The norm travels
        // with the refusal so the record shows which permission was overridden.
        if (claim.CollectedFor is not null && claim.CollectedFor != purpose)
        {
            return DisclosureResult.Denied(DenyReasonCode.PurposeIncompatible, norm);
        }

        var value = _transforms.Apply(norm.Transform, norm.TransformParameter, claim.Value);

        return DisclosureResult.Disclosed(norm, value);
    }
}
