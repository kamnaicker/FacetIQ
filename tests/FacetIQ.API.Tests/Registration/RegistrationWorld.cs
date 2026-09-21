using System.Collections;
using System.Text.RegularExpressions;
using FacetIQ.API.Email;
using FacetIQ.API.Registration;
using FacetIQ.Data.Identity;
using FacetIQ.Domain.Abstractions.Repositories;
using FacetIQ.Domain.Entities;
using FacetIQ.Services.Subjects;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MimeKit;

namespace FacetIQ.API.Tests.Registration;

/// <summary>A RegistrationService wired to in-memory fakes, shared by the registration tests.</summary>
internal sealed class RegistrationWorld
{
    private static readonly Regex CodePattern = new("<strong>(\\d{6})</strong>", RegexOptions.Compiled);
    private static readonly Regex TokenPattern = new("token=([^\"&]+)", RegexOptions.Compiled);

    public FakeUserStore Users { get; }

    public FakePendingRegistrationStore Store { get; }

    public FakeSubjectRepository Subjects { get; }

    public RecordingMailTransport Transport { get; }

    public TestClock Clock { get; }

    public RegistrationService Service { get; }

    private readonly UserManager<AppUser> _userManager;

    /// <param name="clientBaseUrl">The client base URL used to build cancellation links.</param>
    /// <param name="shareClockWithThrottle">
    /// True puts the throttle on the test's clock so its one minute gap applies. By default it gets
    /// its own clock, so tests can send several mails to one address.
    /// </param>
    public RegistrationWorld(string clientBaseUrl = "http://localhost:5173", bool shareClockWithThrottle = false)
    {
        Store = new FakePendingRegistrationStore();
        Transport = new RecordingMailTransport();
        Clock = new TestClock();
        Users = new FakeUserStore();
        Subjects = new FakeSubjectRepository();

        var identityOptions = Options.Create(new IdentityOptions());

        _userManager = new UserManager<AppUser>(
            Users,
            identityOptions,
            new PasswordHasher<AppUser>(),
            [new UserValidator<AppUser>()],
            [new PasswordValidator<AppUser>()],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            services: null!,
            NullLogger<UserManager<AppUser>>.Instance);

        var throttleClock = shareClockWithThrottle ? (TimeProvider)Clock : new UnthrottledClock();
        var throttle = new RecipientThrottle(throttleClock);
        var smtpOptions = Options.Create(new SmtpOptions
        {
            Host = "localhost",
            FromAddress = "no-reply@facetiq.test"
        });

        var emailSender = new IdentityEmailSender(
            Transport,
            throttle,
            smtpOptions,
            NullLogger<IdentityEmailSender>.Instance);

        var clientOptions = Options.Create(new ClientOptions
        {
            BaseUrl = clientBaseUrl
        });

        var provisioner = new SubjectProvisioner(Subjects);

        Service = new RegistrationService(
            Store,
            _userManager,
            emailSender,
            Clock,
            clientOptions,
            provisioner,
            NullLogger<RegistrationService>.Instance);
    }

    public async Task CreateAccount(string email)
    {
        var user = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = email,
            Email = email
        };

        // Through the UserManager, so the email is normalised as it would be for a real account.
        var result = await _userManager.CreateAsync(user);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException("The test fixture could not create the account.");
        }
    }

    /// <summary>The code most recently emailed, to any address, in this world.</summary>
    public string LastCode
    {
        get
        {
            var message = Transport.Sent.Last(sent => sent.Subject == "Your FacetIQ registration code");

            return ExtractCode(message);
        }
    }

    /// <summary>
    /// The live code for this registration, found among the emailed codes by matching the stored hash.
    /// </summary>
    public string CodeFor(Guid registrationId)
    {
        var pending = Store.Rows.Single(row => row.Id == registrationId);

        foreach (var message in Transport.Sent.Where(sent => sent.Subject == "Your FacetIQ registration code"))
        {
            var candidate = ExtractCode(message);

            if (VerificationCode.Matches(candidate, pending.CodeHash))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("No emailed code matches this registration's current hash.");
    }

    /// <summary>
    /// The cancellation token for this registration, found the same way as CodeFor. Resend emails
    /// carry no token and are skipped.
    /// </summary>
    public string CancellationTokenFor(Guid registrationId)
    {
        var pending = Store.Rows.Single(row => row.Id == registrationId);

        foreach (var message in Transport.Sent.Where(sent => sent.Subject == "Your FacetIQ registration code"))
        {
            var candidate = TryExtractToken(message);

            if (candidate is not null && VerificationCode.Matches(candidate, pending.CancellationTokenHash))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("No emailed token matches this registration's current hash.");
    }

    private static string ExtractCode(MimeMessage message)
    {
        var html = message.HtmlBody ?? string.Empty;
        var match = CodePattern.Match(html);

        if (!match.Success)
        {
            throw new InvalidOperationException("The message did not contain a registration code.");
        }

        return match.Groups[1].Value;
    }

    private static string? TryExtractToken(MimeMessage message)
    {
        var html = message.HtmlBody ?? string.Empty;
        var match = TokenPattern.Match(html);

        return match.Success ? match.Groups[1].Value : null;
    }

    // Moves only when a test calls Advance.
    public sealed class TestClock : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 9, 20, 0, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow()
        {
            return _now;
        }

        public void Advance(TimeSpan amount)
        {
            _now = _now + amount;
        }
    }

    // For the throttle only: each reading is past the last one, so its one minute gap never applies.
    private sealed class UnthrottledClock : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 9, 20, 0, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow()
        {
            var current = _now;
            _now = _now + TimeSpan.FromMinutes(2);

            return current;
        }
    }

    // Holds several users, found by id or normalised email. Enumerable so tests can inspect them.
    public sealed class FakeUserStore : IUserStore<AppUser>, IUserEmailStore<AppUser>, IEnumerable<AppUser>
    {
        private readonly List<AppUser> _users = [];

        public IPasswordHasher<AppUser> PasswordHasher { get; } = new PasswordHasher<AppUser>();

        public Task<string> GetUserIdAsync(AppUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Id);
        }

        public Task<string?> GetUserNameAsync(AppUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.UserName);
        }

        public Task SetUserNameAsync(AppUser user, string? userName, CancellationToken cancellationToken)
        {
            user.UserName = userName;

            return Task.CompletedTask;
        }

        public Task<string?> GetNormalizedUserNameAsync(AppUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.NormalizedUserName);
        }

        public Task SetNormalizedUserNameAsync(AppUser user, string? normalizedName, CancellationToken cancellationToken)
        {
            user.NormalizedUserName = normalizedName;

            return Task.CompletedTask;
        }

        public Task<IdentityResult> CreateAsync(AppUser user, CancellationToken cancellationToken)
        {
            _users.Add(user);

            return Task.FromResult(IdentityResult.Success);
        }

        public Task<IdentityResult> UpdateAsync(AppUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<IdentityResult> DeleteAsync(AppUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<AppUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_users.SingleOrDefault(user => user.Id == userId));
        }

        public Task<AppUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            return Task.FromResult(_users.SingleOrDefault(user => user.NormalizedUserName == normalizedUserName));
        }

        public Task SetEmailAsync(AppUser user, string? email, CancellationToken cancellationToken)
        {
            user.Email = email;

            return Task.CompletedTask;
        }

        public Task<string?> GetEmailAsync(AppUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Email);
        }

        public Task<bool> GetEmailConfirmedAsync(AppUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.EmailConfirmed);
        }

        public Task SetEmailConfirmedAsync(AppUser user, bool confirmed, CancellationToken cancellationToken)
        {
            user.EmailConfirmed = confirmed;

            return Task.CompletedTask;
        }

        public Task<AppUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        {
            return Task.FromResult(_users.SingleOrDefault(user => user.NormalizedEmail == normalizedEmail));
        }

        public Task<string?> GetNormalizedEmailAsync(AppUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.NormalizedEmail);
        }

        public Task SetNormalizedEmailAsync(AppUser user, string? normalizedEmail, CancellationToken cancellationToken)
        {
            user.NormalizedEmail = normalizedEmail;

            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }

        public IEnumerator<AppUser> GetEnumerator()
        {
            return _users.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public sealed class FakePendingRegistrationStore : IPendingRegistrationStore
    {
        public List<PendingRegistration> Rows { get; } = [];

        public Task AddAsync(PendingRegistration pending, CancellationToken cancellationToken)
        {
            Rows.Add(pending);

            return Task.CompletedTask;
        }

        public Task<PendingRegistration?> FindAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(Rows.SingleOrDefault(row => row.Id == id));
        }

        public Task<PendingRegistration?> FindByCancellationTokenAsync(string tokenHash, CancellationToken cancellationToken)
        {
            return Task.FromResult(Rows.SingleOrDefault(row => row.CancellationTokenHash == tokenHash));
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            Rows.RemoveAll(row => row.Id == id);

            return Task.CompletedTask;
        }

        public Task<int> RecordFailedAttemptAsync(Guid id, CancellationToken cancellationToken)
        {
            var pending = Rows.SingleOrDefault(row => row.Id == id);

            if (pending is null)
            {
                return Task.FromResult(0);
            }

            pending.Attempts += 1;

            return Task.FromResult(pending.Attempts);
        }

        public Task DeleteForEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        {
            Rows.RemoveAll(row => row.NormalizedEmail == normalizedEmail);

            return Task.CompletedTask;
        }

        public Task DeleteExpiredForEmailAsync(string normalizedEmail, DateTimeOffset now, CancellationToken cancellationToken)
        {
            Rows.RemoveAll(row => row.NormalizedEmail == normalizedEmail && row.ExpiresAt <= now);

            return Task.CompletedTask;
        }

        public Task<bool> TryClaimResendAsync(
            Guid id,
            DateTimeOffset now,
            DateTimeOffset sentNoLaterThan,
            int maxResends,
            CancellationToken cancellationToken)
        {
            var index = Rows.FindIndex(row => row.Id == id);

            if (index < 0)
            {
                return Task.FromResult(false);
            }

            var pending = Rows[index];

            if (pending.ExpiresAt <= now || pending.Resends >= maxResends || pending.LastSentAt > sentNoLaterThan)
            {
                return Task.FromResult(false);
            }

            Rows[index] = Replace(pending, row =>
            {
                row.Resends += 1;
                row.LastSentAt = now;
            });

            return Task.FromResult(true);
        }

        public Task ReleaseResendAsync(Guid id, DateTimeOffset previousLastSentAt, CancellationToken cancellationToken)
        {
            var index = Rows.FindIndex(row => row.Id == id);

            if (index >= 0)
            {
                Rows[index] = Replace(Rows[index], row =>
                {
                    row.Resends -= 1;
                    row.LastSentAt = previousLastSentAt;
                });
            }

            return Task.CompletedTask;
        }

        public Task ReplaceCodeAsync(Guid id, string codeHash, DateTimeOffset expiresAt, CancellationToken cancellationToken)
        {
            var index = Rows.FindIndex(row => row.Id == id);

            if (index >= 0)
            {
                Rows[index] = Replace(Rows[index], row =>
                {
                    row.CodeHash = codeHash;
                    row.Attempts = 0;
                    row.ExpiresAt = expiresAt;
                });
            }

            return Task.CompletedTask;
        }

        // A new instance rather than a mutation in place, so a caller holding an earlier read keeps
        // seeing the value from before this write, matching ExecuteUpdateAsync bypassing the tracked
        // entity in the real store.
        private static PendingRegistration Replace(PendingRegistration source, Action<PendingRegistration> mutate)
        {
            var copy = new PendingRegistration
            {
                Id = source.Id,
                Email = source.Email,
                NormalizedEmail = source.NormalizedEmail,
                PasswordHash = source.PasswordHash,
                CodeHash = source.CodeHash,
                CancellationTokenHash = source.CancellationTokenHash,
                Attempts = source.Attempts,
                Resends = source.Resends,
                CreatedAt = source.CreatedAt,
                ExpiresAt = source.ExpiresAt,
                LastSentAt = source.LastSentAt
            };

            mutate(copy);

            return copy;
        }
    }

    // FailNextAdd makes the next write throw, to simulate the database failing.
    public sealed class FakeSubjectRepository : ISubjectRepository
    {
        public List<Subject> Rows { get; } = [];

        public bool FailNextAdd { get; set; }

        public Task<Subject?> FindAsync(Guid subjectId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Rows.SingleOrDefault(subject => subject.Id == subjectId));
        }

        public Task<Subject?> FindByUserIdAsync(string userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Rows.SingleOrDefault(subject => subject.UserId == userId));
        }

        public Task<Subject> AddOrGetAsync(Subject subject, CancellationToken cancellationToken)
        {
            if (FailNextAdd)
            {
                throw new InvalidOperationException("The subject repository is unavailable.");
            }

            Rows.Add(subject);

            return Task.FromResult(subject);
        }
    }

    public sealed class RecordingMailTransport : IMailTransport
    {
        public List<MimeMessage> Sent { get; } = [];

        public bool FailNextSend { get; set; }

        public Task SendAsync(MimeMessage message, CancellationToken cancellationToken)
        {
            if (FailNextSend)
            {
                throw new InvalidOperationException("The mail transport is unavailable.");
            }

            Sent.Add(message);

            return Task.CompletedTask;
        }
    }
}
