using FacetIQ.API.Email;
using FacetIQ.Data.Identity;
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

    private static IdentityEmailSender Sender(IMailTransport transport)
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
            NullLogger<IdentityEmailSender>.Instance);
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
