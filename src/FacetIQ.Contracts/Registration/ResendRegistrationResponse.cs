namespace FacetIQ.Contracts.Registration;

/// <summary>Whether the resend was sent, and how it affects what the client may try next.</summary>
public sealed record ResendRegistrationResponse
{
    public required bool Sent { get; init; }

    public required int RetryAfterSeconds { get; init; }

    public required int ResendsLeft { get; init; }
}
