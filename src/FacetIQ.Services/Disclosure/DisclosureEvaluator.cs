using FacetIQ.Domain.Abstractions.Repositories;
using FacetIQ.Domain.Abstractions.Services;
using FacetIQ.Domain.Entities;
using FacetIQ.Domain.Enums;
using FacetIQ.Domain.Models;

namespace FacetIQ.Services.Disclosure;

/// <summary>Runs the disclosure pipeline. Every outcome, including a refusal, writes one audit record.</summary>
public sealed class DisclosureEvaluator : IDisclosureEvaluator
{
    private readonly ISubjectRepository _subjects;
    private readonly INormRepository _norms;
    private readonly IStandingRepository _standings;
    private readonly IAttributeRepository _attributes;
    private readonly INormMatcher _matcher;
    private readonly ISpecificityRanker _ranker;
    private readonly ITransformService _transforms;
    private readonly IAuditWriter _audit;

    public DisclosureEvaluator(
        ISubjectRepository subjects,
        INormRepository norms,
        IStandingRepository standings,
        IAttributeRepository attributes,
        INormMatcher matcher,
        ISpecificityRanker ranker,
        ITransformService transforms,
        IAuditWriter audit)
    {
        _subjects = subjects;
        _norms = norms;
        _standings = standings;
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

    // Checked before norms: a subject's own rules cannot narrow their right of access.
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
        var standings = await ResolveStandingsAsync(request, cancellationToken);
        var candidates = _matcher.Match(governing, request, standings);
        var selection = _ranker.Select(candidates);

        return selection.Outcome switch
        {
            SelectionOutcome.NoMatch => DisclosureResult.Denied(DenyReasonCode.NoMatchingNorm),
            SelectionOutcome.Ambiguous => DisclosureResult.Denied(DenyReasonCode.AmbiguousNorms),
            SelectionOutcome.Selected => await ResolveAsync(selection.Norm!, request.Purpose, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(selection), selection.Outcome, "Unhandled selection.")
        };
    }

    // Relationships come from accepted standings only, never from the request.
    private async Task<IReadOnlySet<string>> ResolveStandingsAsync(
        DisclosureRequest request,
        CancellationToken cancellationToken)
    {
        var accepted = await _standings.GetAcceptedAsync(
            request.SubjectId,
            request.RequesterUserId,
            cancellationToken);

        return accepted
            .Select(standing => standing.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private async Task<DisclosureResult> ResolveAsync(
        Norm norm,
        Purpose purpose,
        CancellationToken cancellationToken)
    {
        // The stored reason is ignored so a rule cannot pass itself off as an engine refusal.
        if (norm.Action == ActionType.Deny)
        {
            return DisclosureResult.Denied(DenyReasonCode.RefusedByRule, norm);
        }

        var claim = await _attributes.FindAsync(norm.AttributeId, cancellationToken);

        // Unreachable while the foreign key holds; kept so a missing claim is refused, not thrown.
        if (claim is null)
        {
            return DisclosureResult.Denied(DenyReasonCode.ClaimUnavailable, norm);
        }

        // A collection purpose overrides a norm that would otherwise release the claim.
        if (claim.CollectedFor is not null && claim.CollectedFor != purpose)
        {
            return DisclosureResult.Denied(DenyReasonCode.PurposeIncompatible, norm);
        }

        if (!_transforms.TryApply(norm.Transform, norm.TransformParameter, claim.Value, out var value))
        {
            return DisclosureResult.Denied(DenyReasonCode.TransformFailed, norm);
        }

        return DisclosureResult.Disclosed(norm, value);
    }
}
