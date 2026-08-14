using FacetIQ.Domain.Enums;

namespace FacetIQ.Domain.Entities;

public class Norm
{
    SubjectAttribute Attribute { get; set; } = null!;
    int Channel { get; set; }
    int Purpose { get; set; }
    int Relationship { get; set; }
    ActionType ActionType { get; set; }
    TransformKind TransformKind{ get; set; }
    DenyReasonCode DenyReason { get; set; }
    string JustifyingPrinciple { get; set; } = null!;

}
