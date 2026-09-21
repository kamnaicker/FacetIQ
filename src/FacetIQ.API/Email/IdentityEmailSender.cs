using FacetIQ.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MimeKit;

namespace FacetIQ.API.Email;

// Identity HTML-encodes what reaches the three IEmailSender methods. The registration methods
// build their own values instead: a six digit code and a base64url token, neither able to carry markup.
public sealed class IdentityEmailSender : IEmailSender<AppUser>
{
    private readonly IMailTransport _transport;
    private readonly RecipientThrottle _throttle;
    private readonly SmtpOptions _options;
    private readonly ILogger<IdentityEmailSender> _logger;

    public IdentityEmailSender(
        IMailTransport transport,
        RecipientThrottle throttle,
        IOptions<SmtpOptions> options,
        ILogger<IdentityEmailSender> logger)
    {
        _transport = transport;
        _throttle = throttle;
        _options = options.Value;
        _logger = logger;
    }

    public Task SendConfirmationLinkAsync(AppUser user, string email, string confirmationLink)
    {
        return SendAsync(
            email,
            ThrottleBucket.Account,
            "Confirm your email for FacetIQ",
            $"<p>Confirm your email by <a href=\"{confirmationLink}\">opening this link</a>.</p>");
    }

    public Task SendPasswordResetLinkAsync(AppUser user, string email, string resetLink)
    {
        return SendAsync(
            email,
            ThrottleBucket.Account,
            "Reset your FacetIQ password",
            $"<p>Reset your password by <a href=\"{resetLink}\">opening this link</a>.</p>");
    }

    public Task SendPasswordResetCodeAsync(AppUser user, string email, string resetCode)
    {
        return SendAsync(
            email,
            ThrottleBucket.Account,
            "Your FacetIQ password reset code",
            $"<p>Your password reset code is {resetCode}</p>");
    }

    /// <summary>
    /// Sends the code, with a cancellation link when one is given. A resend passes none, because only
    /// the first email's token is stored. Returns whether the message reached the transport.
    /// </summary>
    public Task<bool> SendRegistrationCodeAsync(string email, string code, string? cancellationUrl)
    {
        var cancellationParagraph = cancellationUrl is null
            ? string.Empty
            : $"<p>If you did not ask to register, <a href=\"{cancellationUrl}\">cancel it here</a>. "
              + "Nothing has been created yet.</p>";

        return SendAsync(
            email,
            ThrottleBucket.Registration,
            "Your FacetIQ registration code",
            $"<p>Your code is <strong>{code}</strong>. It is valid for 10 minutes.</p>" + cancellationParagraph);
    }

    /// <summary>Tells the holder of an address that someone tried to register it again.</summary>
    public Task SendExistingAccountNoticeAsync(string email)
    {
        return SendAsync(
            email,
            ThrottleBucket.Registration,
            "Someone tried to register your email for FacetIQ",
            "<p>An account already exists for this address. If that was you, sign in, or reset your "
            + "password if you have forgotten it. If it was not you, nothing was created or changed.</p>");
    }

    private async Task<bool> SendAsync(string email, ThrottleBucket bucket, string subject, string html)
    {
        // Callers pass an address that has already been parsed; TryParse keeps a bad one from throwing.
        if (!MailboxAddress.TryParse(email, out var recipient))
        {
            _logger.LogError("Email not sent: the recipient address could not be parsed.");

            return false;
        }

        // Skipped quietly: the callers answer the same either way, so nothing is revealed.
        if (!_throttle.TryAcquire(email, bucket))
        {
            _logger.LogWarning("Email not sent: the recipient has reached its sending limit.");

            return false;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(recipient);
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = html };

        // A mail failure must not fail the request that caused it. MailKit throws several unrelated
        // exception types for the same outcome, so all of them are caught.
        try
        {
            await _transport.SendAsync(message, CancellationToken.None);

            return true;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Email not sent: the transport failed.");

            return false;
        }
    }
}
