using System.ComponentModel.DataAnnotations;
using FacetIQ.API.Email;
using MailKit.Security;

namespace FacetIQ.API.Tests.Email;

public class SmtpOptionsTests
{
    [Fact]
    public void UnencryptedToLocalhost_IsValid()
    {
        var errors = Validate(Settings("localhost", SecureSocketOptions.None));

        Assert.Empty(errors);
    }

    [Fact]
    public void UnencryptedToRemoteHost_IsRejected()
    {
        var errors = Validate(Settings("smtp.example.test", SecureSocketOptions.None));

        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(SmtpOptions.Security)));
    }

    [Fact]
    public void EncryptedToRemoteHost_IsValid()
    {
        var errors = Validate(Settings("smtp.example.test", SecureSocketOptions.StartTls));

        Assert.Empty(errors);
    }

    [Fact]
    public void MissingHost_IsRejected()
    {
        var errors = Validate(Settings("", SecureSocketOptions.StartTls));

        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(SmtpOptions.Host)));
    }

    private static SmtpOptions Settings(string host, SecureSocketOptions security)
    {
        return new SmtpOptions
        {
            Host = host,
            Port = 587,
            Security = security,
            FromAddress = "no-reply@facetiq.test"
        };
    }

    private static List<ValidationResult> Validate(SmtpOptions options)
    {
        var results = new List<ValidationResult>();

        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);

        return results;
    }
}
