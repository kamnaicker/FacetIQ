using FacetIQ.Domain.Abstractions.Repositories;
using FacetIQ.Domain.Abstractions.Services;
using FacetIQ.Domain.Entities;
using FacetIQ.Domain.Models;

namespace FacetIQ.Services.Auditing;

public sealed class AuditWriter : IAuditWriter
{
    private readonly IAuditRecordRepository _records;
    private readonly TimeProvider _clock;

    public AuditWriter(IAuditRecordRepository records, TimeProvider clock)
    {
        _records = records;
        _clock = clock;
    }

    public Task RecordAsync(
        DisclosureRequest request,
        DisclosureResult result,
        CancellationToken cancellationToken)
    {
        // The released value is not copied here. The norm revision and the transform applied
        // are enough to derive it, so the record stays verifiable without duplicating the
        // subject's data into a second store.
        var record = new AuditRecord
        {
            Id = Guid.NewGuid(),
            Timestamp = _clock.GetUtcNow(),
            RequesterUserId = request.RequesterUserId,
            SubjectId = request.SubjectId,
            RequestedAttributeKey = request.AttributeKey,
            Purpose = request.Purpose,
            Channel = request.Channel,
            Outcome = result.Outcome,
            DenyReason = result.DenyReason,
            NormId = result.Norm?.Id,
            NormVersion = result.Norm?.Version,
            Transform = result.Norm?.Transform,
            TransformParameter = result.Norm?.TransformParameter,
            JustifyingPrinciple = result.Norm?.JustifyingPrinciple
        };

        return _records.AddAsync(record, cancellationToken);
    }
}
