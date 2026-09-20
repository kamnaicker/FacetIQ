using System.Globalization;
using System.Net.Mail;
using FacetIQ.Domain.Abstractions.Services;
using PhoneNumbers;

namespace FacetIQ.Services.Validation;

/// <summary>Checks the kinds with a fixed format. Names, pronouns, employers and addresses stay free text.</summary>
public sealed class ClaimValueValidator : IClaimValueValidator
{
    private const int OldestAge = 150;

    private readonly TimeProvider _clock;
    private readonly PhoneNumberUtil _phones = PhoneNumberUtil.GetInstance();

    public ClaimValueValidator(TimeProvider clock)
    {
        _clock = clock;
    }

    public bool IsValid(string key, string value, out string problem)
    {
        problem = key switch
        {
            "dateOfBirth" => DateOfBirthProblem(value),
            "email" => EmailProblem(value),
            "phone" => PhoneProblem(value),
            _ => string.Empty
        };

        return problem.Length == 0;
    }

    private string DateOfBirthProblem(string value)
    {
        if (!DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return "Must be a date written as yyyy-MM-dd.";
        }

        var today = DateOnly.FromDateTime(_clock.GetUtcNow().UtcDateTime);

        if (date > today || date < today.AddYears(-OldestAge))
        {
            return "Must be a date within the last 150 years.";
        }

        return string.Empty;
    }

    // Exact match, so a display name or surrounding text is refused rather than trimmed away.
    private static string EmailProblem(string value)
    {
        if (MailAddress.TryCreate(value, out var address) && address.Address == value)
        {
            return string.Empty;
        }

        return "Must be an email address.";
    }

    // E.164 only, so one number has one spelling and exact comparison works.
    private string PhoneProblem(string value)
    {
        const string problem = "Must be a valid number with its country code, such as +27821234567.";

        PhoneNumber number;

        try
        {
            number = _phones.Parse(value, null);
        }
        catch (NumberParseException)
        {
            return problem;
        }

        if (_phones.IsValidNumber(number) && _phones.Format(number, PhoneNumberFormat.E164) == value)
        {
            return string.Empty;
        }

        return problem;
    }
}
