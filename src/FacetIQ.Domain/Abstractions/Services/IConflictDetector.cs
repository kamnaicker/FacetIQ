using FacetIQ.Domain.Entities;
using FacetIQ.Domain.Models;

namespace FacetIQ.Domain.Abstractions.Services;

/// <summary>
/// Checks a norm against the ones a subject already holds, before it is persisted, so a tie is
/// refused while the subject is still writing it rather than surfacing as an ambiguity refusal
/// against some request months later. Pure: detection depends only on its arguments.
/// </summary>
public interface IConflictDetector
{
    /// <param name="existing">
    /// The subject's own norms. Superseded revisions are ignored: a norm that can no longer
    /// govern a request cannot collide over one, so an edit does not conflict with what it
    /// replaces.
    /// </param>
    /// <returns>Every norm the proposed one collides with, or empty when it is safe to store.</returns>
    IReadOnlyList<NormConflict> Detect(Norm proposed, IReadOnlyList<Norm> existing);
}
