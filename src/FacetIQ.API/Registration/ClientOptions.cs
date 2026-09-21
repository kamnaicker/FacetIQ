using System.ComponentModel.DataAnnotations;

namespace FacetIQ.API.Registration;

public sealed class ClientOptions
{
    public const string Section = "Client";

    // Validated at startup, since a missing or relative value would put a dead link in every email.
    [Required]
    [Url]
    public string BaseUrl { get; set; } = string.Empty;
}
