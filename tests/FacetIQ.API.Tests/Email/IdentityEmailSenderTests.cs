using FacetIQ.API.Email;
using FacetIQ.Data.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MimeKit;

namespace FacetIQ.API.Tests.Email;

public class IdentityEmailSenderTests
{
    // Identity passes the link already HTML-encoded.
    private const string Link = "http://localhost:5197/confirmEmail?userId=riya&amp;code=abc";

    [Fact]
    public async Task ConfirmationLink_IsSentToTheAddress()
    {
        var transport = new RecordingMailTransport();

        await Sender(transport).SendConfirmationLinkAsync(new AppUser(), "riya@example.test", Link);

        var message = Assert.Single(transport.Sent);
        Assert.Equal("riya@example.test", Assert.Single(message.To.Mailboxes).Address);
        Assert.Equal("no-reply@facetiq.test", Assert.Single(message.From.Mailboxes).Address);
        Assert.Contains($"href=\"{Link}\"", message.HtmlBody);
    }

    [Fact]
    public async Task PasswordResetCode_IsInTheBody()
    {
        var transport = new RecordingMailTransport();

        await Sender(transport).SendPasswordResetCodeAsync(new AppUser(), "riya@example.test", "reset-code-123");

        var message = Assert.Single(transport.Sent);
        Assert.Contains("reset-code-123", message.HtmlBody);
    }

    [Fact]
    public async Task RepeatedEmailToTheSameAddress_IsNotSent()
    {
        var transport = new RecordingMailTransport();
        var sender = Sender(transport);

        await sender.SendConfirmationLinkAsync(new AppUser(), "riya@example.test", Link);
        await sender.SendConfirmationLinkAsync(new AppUser(), "riya@example.test", Link);

        Assert.Single(transport.Sent);
    }

    // Identity creates the account and then asks for the mail, so a transport failure here would
    // report failure for an account that now exists. Seen live on 20 September: Gmail refused the
    // credentials, registration returned 500, and the unconfirmed account blocked a second attempt.
    [Fact]
    public async Task TransportFailure_DoesNotReachTheCaller()
    {
        var sender = Sender(new FailingMailTransport(), NullLogger<IdentityEmailSender>.Instance);

        await sender.SendConfirmationLinkAsync(new AppUser(), "riya@example.test", Link);
    }

    [Fact]
    public async Task TransportFailure_IsLogged()
    {
        var logger = new RecordingLogger<IdentityEmailSender>();

        await Sender(new FailingMailTransport(), logger)
            .SendConfirmationLinkAsync(new AppUser(), "riya@example.test", Link);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
    }

    private static IdentityEmailSender Sender(IMailTransport transport)
    {
        return Sender(transport, NullLogger<IdentityEmailSender>.Instance);
    }

    private static IdentityEmailSender Sender(IMailTransport transport, ILogger<IdentityEmailSender> logger)
    {
        var options = Options.Create(new SmtpOptions
        {
            Host = "localhost",
            FromAddress = "no-reply@facetiq.test"
        });

        return new IdentityEmailSender(
            transport,
            new RecipientThrottle(TimeProvider.System),
            options,
            logger);
    }

    private sealed class FailingMailTransport : IMailTransport
    {
        public Task SendAsync(MimeMessage message, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("The SMTP server rejected the credentials.");
        }
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }
    }

    private sealed class RecordingMailTransport : IMailTransport
    {
        public List<MimeMessage> Sent { get; } = [];

        public Task SendAsync(MimeMessage message, CancellationToken cancellationToken)
        {
            Sent.Add(message);

            return Task.CompletedTask;
        }
    }
}
