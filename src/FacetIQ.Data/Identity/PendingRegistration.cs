namespace FacetIQ.Data.Identity;

/// <summary>
/// A registration waiting for its code. An abandoned one expires as a row rather than remaining as
/// an account nobody can use.
/// </summary>
public sealed class PendingRegistration
{
    public Guid Id { get; set; }

    /// <summary>The address as typed. The account is created with it.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Normalised through UserManager.NormalizeEmail, so lookups match Identity's own.</summary>
    public string NormalizedEmail { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string CodeHash { get; set; } = string.Empty;

    public string CancellationTokenHash { get; set; } = string.Empty;

    public int Attempts { get; set; }

    public int Resends { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>When the current code was sent, first send or resend. The resend cooldown runs from this.</summary>
    public DateTimeOffset LastSentAt { get; set; }
}
