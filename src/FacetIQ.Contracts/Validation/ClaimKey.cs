namespace FacetIQ.Contracts.Validation;

/// <summary>Claim keys are camelCase names, so spacing and markup cannot enter one.</summary>
public static class ClaimKey
{
    public const string Pattern = "^[a-z][a-zA-Z0-9]*$";

    public const string Message = "Must be a camelCase name such as dateOfBirth.";
}
