using FacetIQ.Domain.Entities;

namespace FacetIQ.Domain.Models;

/// <summary>Shared by matching and conflict detection so both score norms the same way.</summary>
public static class NormSpecificity
{
    /// <summary>Number of bound conditions, 0 to 2. Independent of the request.</summary>
    public static int Of(Norm norm) =>
        (norm.Relationship is null ? 0 : 1) +
        (norm.Purpose is null ? 0 : 1);
}
