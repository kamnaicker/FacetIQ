using Microsoft.AspNetCore.Identity;

namespace FacetIQ.API.Registration;

public enum StartOutcome
{
    CodeSent,
    AlreadyRegistered,
    Invalid
}

public sealed record StartResult(StartOutcome Outcome, Guid RegistrationId, IReadOnlyList<IdentityError> Errors);

public enum ConfirmOutcome
{
    Created,
    Invalid,
    AlreadyRegistered,

    // Account creation failed for a reason other than a duplicate.
    Failed
}

public sealed record ResendResult(bool Sent, int RetryAfterSeconds, int ResendsLeft);
