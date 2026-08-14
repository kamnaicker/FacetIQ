using FacetIQ.Domain.Enums;
using System.Threading.Channels;

namespace FacetIQ.Domain.Entities;

public class AuditRecord
{
    DateTime Timestamp { get; set; }
    Subject SubjectId { get; set; } = null!;
    Attribute RequestedAttribute { get; set; } = null!;
    RequestChannel Channel { get; set; }
    Purpose Purpose { get; set; }

}
