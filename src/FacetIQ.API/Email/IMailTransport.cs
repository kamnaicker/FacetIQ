using MimeKit;

namespace FacetIQ.API.Email;

public interface IMailTransport
{
    Task SendAsync(MimeMessage message, CancellationToken cancellationToken);
}
