using FacetIQ.API.Email;
using FacetIQ.Data.Identity;
using FacetIQ.Services.Subjects;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MimeKit;

namespace FacetIQ.API.Registration;

/// <summary>Registration by one-time code. No account exists until a code is redeemed.</summary>
public sealed class RegistrationService
{
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(30);
    private const int MaxResends = 3;

    private readonly IPendingRegistrationStore _store;
    private readonly UserManager<AppUser> _users;
    private readonly IdentityEmailSender _emailSender;
    private readonly TimeProvider _clock;
    private readonly ClientOptions _clientOptions;
    private readonly SubjectProvisioner _provisioner;
    private readonly ILogger<RegistrationService> _logger;

    public RegistrationService(
        IPendingRegistrationStore store,
        UserManager<AppUser> users,
        IdentityEmailSender emailSender,
        TimeProvider clock,
        IOptions<ClientOptions> clientOptions,
        SubjectProvisioner provisioner,
        ILogger<RegistrationService> logger)
    {
        _store = store;
        _users = users;
        _emailSender = emailSender;
        _clock = clock;
        _clientOptions = clientOptions.Value;
        _provisioner = provisioner;
        _logger = logger;
    }

    public async Task<StartResult> StartAsync(string email, string password, CancellationToken cancellationToken)
    {
        if (!TryParseAddress(email, out var parsedEmail))
        {
            // Refused before anything is written, so variants of one address cannot each become a
            // separate row and throttle key.
            return new StartResult(StartOutcome.Invalid, Guid.Empty, [new IdentityError
            {
                Code = "InvalidEmail",
                Description = "The email address is not valid."
            }]);
        }

        var normalisedEmail = _users.NormalizeEmail(parsedEmail) ?? string.Empty;
        var now = _clock.GetUtcNow();

        // There is no background job, so expired rows are cleared when the address is next used.
        await _store.DeleteExpiredForEmailAsync(normalisedEmail, now, cancellationToken);

        var existingUser = await _users.FindByEmailAsync(normalisedEmail);

        if (existingUser is not null)
        {
            // Nothing is written. The holder of the address is told instead.
            await _emailSender.SendExistingAccountNoticeAsync(parsedEmail);

            return new StartResult(StartOutcome.AlreadyRegistered, Guid.Empty, []);
        }

        // Never stored. Identity's validators need a user to validate against.
        var transientUser = new AppUser
        {
            Email = parsedEmail,
            UserName = parsedEmail
        };

        var errors = new List<IdentityError>();

        foreach (var validator in _users.UserValidators)
        {
            // Refuses an address Identity cannot use as a user name, such as one with an apostrophe,
            // before a code is sent rather than after it is entered.
            var validation = await validator.ValidateAsync(_users, transientUser);

            if (!validation.Succeeded)
            {
                errors.AddRange(validation.Errors);
            }
        }

        foreach (var validator in _users.PasswordValidators)
        {
            var validation = await validator.ValidateAsync(_users, transientUser, password);

            if (!validation.Succeeded)
            {
                errors.AddRange(validation.Errors);
            }
        }

        if (errors.Count > 0)
        {
            return new StartResult(StartOutcome.Invalid, Guid.Empty, errors);
        }

        var passwordHash = _users.PasswordHasher.HashPassword(transientUser, password);
        var code = VerificationCode.Generate();
        var cancellationTokenValue = VerificationCode.GenerateCancellationToken();

        var pending = new PendingRegistration
        {
            Id = Guid.NewGuid(),
            // As typed, since the account is created from it. Lookups use NormalizedEmail.
            Email = parsedEmail,
            NormalizedEmail = normalisedEmail,
            PasswordHash = passwordHash,
            CodeHash = VerificationCode.Hash(code),
            CancellationTokenHash = VerificationCode.Hash(cancellationTokenValue),
            CreatedAt = now,
            ExpiresAt = now + CodeLifetime,
            LastSentAt = now
        };

        await _store.AddAsync(pending, cancellationToken);

        // A configured trailing slash would otherwise double up with the path.
        var baseUrl = _clientOptions.BaseUrl.TrimEnd('/');
        var cancellationUrl = $"{baseUrl}/registration/cancel?token={cancellationTokenValue}";

        await _emailSender.SendRegistrationCodeAsync(parsedEmail, code, cancellationUrl);

        return new StartResult(StartOutcome.CodeSent, pending.Id, []);
    }

    public async Task<ConfirmOutcome> ConfirmAsync(
        Guid registrationId,
        string code,
        string password,
        CancellationToken cancellationToken)
    {
        var pending = await _store.FindAsync(registrationId, cancellationToken);

        if (pending is null)
        {
            return ConfirmOutcome.Invalid;
        }

        var now = _clock.GetUtcNow();

        if (pending.ExpiresAt <= now || pending.Attempts >= 5)
        {
            await _store.DeleteAsync(pending.Id, cancellationToken);

            return ConfirmOutcome.Invalid;
        }

        var codeMatches = VerificationCode.Matches(code, pending.CodeHash);

        // Run unconditionally: skipping it when the code is already wrong would make a slow
        // response reveal that the code was right.
        var passwordCheckUser = new AppUser { Email = pending.Email };
        var passwordVerification = _users.PasswordHasher.VerifyHashedPassword(
            passwordCheckUser,
            pending.PasswordHash,
            password);
        var passwordMatches = passwordVerification is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;

        if (!codeMatches || !passwordMatches)
        {
            var attempts = await _store.RecordFailedAttemptAsync(pending.Id, cancellationToken);

            if (attempts >= 5)
            {
                await _store.DeleteAsync(pending.Id, cancellationToken);
            }

            return ConfirmOutcome.Invalid;
        }

        // Another attempt may have claimed the address since this one started.
        var existingUser = await _users.FindByEmailAsync(pending.NormalizedEmail);

        if (existingUser is not null)
        {
            await _store.DeleteForEmailAsync(pending.NormalizedEmail, cancellationToken);

            return ConfirmOutcome.AlreadyRegistered;
        }

        var user = new AppUser
        {
            UserName = pending.Email,
            Email = pending.Email,
            EmailConfirmed = true,
            PasswordHash = pending.PasswordHash,
        };

        // The stored hash is used as is: the password was validated at start and its plaintext is gone.
        var created = await _users.CreateAsync(user);

        if (!created.Succeeded)
        {
            if (created.Errors.Any(error => error.Code is "DuplicateEmail" or "DuplicateUserName"))
            {
                // Claimed between the check above and this call.
                await _store.DeleteForEmailAsync(pending.NormalizedEmail, cancellationToken);

                return ConfirmOutcome.AlreadyRegistered;
            }

            // StartAsync already ran the same validators, so any other failure is unexpected and is
            // reported as one rather than as a duplicate. The pending row is kept.
            _logger.LogError(
                "Account creation failed for a reason other than a duplicate: {Errors}",
                string.Join(", ", created.Errors.Select(error => error.Code)));

            return ConfirmOutcome.Failed;
        }

        // The account exists from here, so its pending rows go whether or not provisioning succeeds.
        await _store.DeleteForEmailAsync(pending.NormalizedEmail, cancellationToken);

        try
        {
            await _provisioner.EnsureAsync(user.Id, cancellationToken);
        }
        catch (Exception exception)
        {
            // POST /subject provisions on first use, so this must not fail the confirmation.
            _logger.LogError(exception, "Subject provisioning failed after account creation for user {UserId}.", user.Id);
        }

        return ConfirmOutcome.Created;
    }

    public async Task<ResendResult> ResendAsync(Guid registrationId, CancellationToken cancellationToken)
    {
        var pending = await _store.FindAsync(registrationId, cancellationToken);
        var now = _clock.GetUtcNow();

        if (pending is null || pending.ExpiresAt <= now || pending.Resends >= MaxResends)
        {
            return new ResendResult(false, 0, 0);
        }

        // Claims the slot atomically before sending, so two concurrent resends for the same row
        // cannot both pass the cooldown and the cap.
        var claimed = await _store.TryClaimResendAsync(
            pending.Id,
            now,
            now - ResendCooldown,
            MaxResends,
            cancellationToken);

        if (!claimed)
        {
            // Another caller's claim already moved LastSentAt, so the numbers reported come from a
            // fresh read rather than the one taken above.
            var current = await _store.FindAsync(pending.Id, cancellationToken);

            if (current is null)
            {
                return new ResendResult(false, 0, 0);
            }

            var sinceLastSent = now - current.LastSentAt;
            var retryAfterSeconds = sinceLastSent < ResendCooldown
                ? (int)Math.Ceiling((ResendCooldown - sinceLastSent).TotalSeconds)
                : 0;

            return new ResendResult(false, retryAfterSeconds, Math.Max(0, MaxResends - current.Resends));
        }

        var code = VerificationCode.Generate();

        // The cancellation token is kept, so the link in the first email still works.
        var delivered = await _emailSender.SendRegistrationCodeAsync(pending.Email, code, cancellationUrl: null);

        if (!delivered)
        {
            // Release the claimed slot, since nothing was sent under it.
            await _store.ReleaseResendAsync(pending.Id, pending.LastSentAt, cancellationToken);

            return new ResendResult(false, 0, MaxResends - pending.Resends);
        }

        await _store.ReplaceCodeAsync(pending.Id, VerificationCode.Hash(code), now + CodeLifetime, cancellationToken);

        return new ResendResult(true, (int)ResendCooldown.TotalSeconds, MaxResends - (pending.Resends + 1));
    }

    public async Task CancelAsync(string token, CancellationToken cancellationToken)
    {
        var tokenHash = VerificationCode.Hash(token);
        var pending = await _store.FindByCancellationTokenAsync(tokenHash, cancellationToken);

        if (pending is null)
        {
            return;
        }

        // Every attempt for the address, not only the one this token came from.
        await _store.DeleteForEmailAsync(pending.NormalizedEmail, cancellationToken);
    }

    // MimeKit also accepts display names, angle brackets and padding. Anything that does not
    // round-trip to the input exactly is refused rather than narrowed to the address inside it.
    private static bool TryParseAddress(string email, out string address)
    {
        address = string.Empty;

        if (!MailboxAddress.TryParse(email, out var mailbox))
        {
            return false;
        }

        if (!string.Equals(mailbox.Address, email, StringComparison.Ordinal))
        {
            return false;
        }

        address = mailbox.Address;

        return true;
    }
}
