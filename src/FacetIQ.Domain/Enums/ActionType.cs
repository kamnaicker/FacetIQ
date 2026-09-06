namespace FacetIQ.Domain.Enums;

/// <summary>
/// The outcome of evaluating a disclosure request. Unlike an access-control verdict,
/// Transform is a first-class result: the decision is the representation returned.
/// </summary>
public enum ActionType
{
    Return,
    Transform,
    Deny
}
