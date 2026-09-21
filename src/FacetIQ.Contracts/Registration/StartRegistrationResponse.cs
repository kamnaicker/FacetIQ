namespace FacetIQ.Contracts.Registration;

/// <summary>Identifies the pending attempt, for confirming or resending.</summary>
public sealed record StartRegistrationResponse
{
    public required Guid RegistrationId { get; init; }
}
