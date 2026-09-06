using FacetIQ.Domain.Entities;

namespace FacetIQ.Domain.Models;

/// <summary>A norm that matched a request, paired with the specificity it matched at.</summary>
public sealed record NormCandidate(Norm Norm, int Specificity);
