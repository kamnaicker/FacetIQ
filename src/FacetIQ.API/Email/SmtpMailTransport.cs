using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

namespace FacetIQ.API.Email;

public sealed class SmtpMailTransport : IMailTransport
{
    private readonly SmtpOptions _options;

    public SmtpMailTransport(IOptions<SmtpOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendAsync(MimeMessage message, CancellationToken cancellationToken)
    {
        using var client = new SmtpClient();

        await client.ConnectAsync(_options.Host, _options.Port, _options.Security, cancellationToken);

        // Mailpit accepts mail without signing in.
        if (!string.IsNullOrEmpty(_options.Username))
        {
            await client.AuthenticateAsync(_options.Username, _options.Password ?? string.Empty, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
