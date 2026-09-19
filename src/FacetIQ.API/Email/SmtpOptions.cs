using System.ComponentModel.DataAnnotations;
using MailKit.Security;

namespace FacetIQ.API.Email;

public sealed class SmtpOptions : IValidatableObject
{
    public const string Section = "Smtp";

    [Required]
    public string Host { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; set; } = 587;

    public SecureSocketOptions Security { get; set; } = SecureSocketOptions.StartTls;

    public string? Username { get; set; }

    public string? Password { get; set; }

    [Required]
    [EmailAddress]
    public string FromAddress { get; set; } = string.Empty;

    public string FromName { get; set; } = "FacetIQ";

    // Credentials and confirmation links must not travel unencrypted over a network.
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Security == SecureSocketOptions.None && Host != "localhost")
        {
            yield return new ValidationResult(
                "Unencrypted SMTP is only allowed to localhost.",
                [nameof(Security)]);
        }
    }
}
