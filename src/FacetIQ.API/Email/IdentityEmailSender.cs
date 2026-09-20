using FacetIQ.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MimeKit;

namespace FacetIQ.API.Email;

// Identity HTML-encodes links and codes before they reach this class.
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
            "Confirm your email for FacetIQ",
            $"<p>Confirm your email by <a href=\"{confirmationLink}\">opening this link</a>.</p>");
    }

    public Task SendPasswordResetLinkAsync(AppUser user, string email, string resetLink)
    {
        return SendAsync(
            email,
            "Reset your FacetIQ password",
            $"<p>Reset your password by <a href=\"{resetLink}\">opening this link</a>.</p>");
    }

    public Task SendPasswordResetCodeAsync(AppUser user, string email, string resetCode)
    {
        return SendAsync(
            email,
            "Your FacetIQ password reset code",
            $"<p>Your password reset code is {resetCode}</p>");
    }

    private async Task SendAsync(string email, string subject, string html)
    {
        // Skipped quietly: the Identity endpoints return 200 either way, so nothing is revealed.
        // A refused attempt still counts against the address, which leaves four more today and a
        // minute's wait, enough for the account to ask for the mail again.
        if (!_throttle.TryAcquire(email))
        {
            _logger.LogWarning("Email not sent: the recipient has reached its sending limit.");

            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = html };

        // Identity creates the account and then asks for the mail, so a failure here would report
        // failure for an account that exists, and the leftover row would block a second attempt.
        // The account is left unconfirmed instead, which is the state resendConfirmationEmail
        // expects. Every transport fault is caught, since MailKit raises several unrelated types
        // for one outcome: the mail did not go.
        try
        {
            await _transport.SendAsync(message, CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Email not sent: the transport failed.");
        }
    }
}
