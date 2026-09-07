using FacetIQ.Domain.Entities;
using FacetIQ.Domain.Models;

namespace FacetIQ.Domain.Abstractions.Services;

/// <summary>
/// Checks a norm against the ones a subject already holds, before it is persisted, so a tie is
/// refused while the subject is still writing it. Pure: detection depends only on its arguments.
/// </summary>
public interface IConflictDetector
{
    /// <param name="existing">
    /// The subject's own norms. Superseded revisions are ignored, so an edit does not conflict
    /// with what it replaces.
    /// </param>
    IReadOnlyList<NormConflict> Detect(Norm proposed, IReadOnlyList<Norm> existing);
}
