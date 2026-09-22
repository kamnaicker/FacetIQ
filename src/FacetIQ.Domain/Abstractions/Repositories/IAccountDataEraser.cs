namespace FacetIQ.Domain.Abstractions.Repositories;

/// <summary>
/// Removes the profile, claims, rules and standings belonging to an account. The disclosure log is
/// left as written: it holds no values, and without the profile its ids identify nobody.
/// </summary>
public interface IAccountDataEraser
{
    Task EraseAsync(string userId, CancellationToken cancellationToken);
}
