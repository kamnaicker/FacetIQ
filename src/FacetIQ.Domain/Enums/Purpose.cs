namespace FacetIQ.Domain.Enums;

/// <summary>
/// Why the requester is asking. This is the transmission principle of the information
/// flow, and one of the three conditions a norm may bind.
/// </summary>
public enum Purpose
{
    Unspecified,
    Identification,
    Regulatory,
    Clinical,
    Social,
    Religious
}
