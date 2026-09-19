using System.Security.Claims;
using FacetIQ.Contracts.Disclosure;
using FacetIQ.Domain.Abstractions.Repositories;
using FacetIQ.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace FacetIQ.API.Controllers;

[ApiController]
[Route("[controller]")]
public class HistoryController : ControllerBase
{
    private const int PageSize = 100;

    private readonly ISubjectRepository _subjects;
    private readonly IAuditRecordRepository _records;
    private readonly IUserDirectory _users;

    public HistoryController(
        ISubjectRepository subjects,
        IAuditRecordRepository records,
        IUserDirectory users)
    {
        _subjects = subjects;
        _records = records;
        _users = users;
    }

    /// <summary>The latest decisions about the caller, refusals included, newest first.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DisclosureRecordResponse>>> Get(
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is null)
        {
            return Unauthorized();
        }

        var subject = await _subjects.FindByUserIdAsync(userId, cancellationToken);

        if (subject is null)
        {
            return Forbid();
        }

        var records = await _records.ListForSubjectAsync(subject.Id, PageSize, cancellationToken);

        // Cached per requester and run sequentially, as the DbContext cannot query concurrently.
        var addresses = new Dictionary<string, string?>();
        var responses = new List<DisclosureRecordResponse>(records.Count);

        foreach (var record in records)
        {
            if (!addresses.TryGetValue(record.RequesterUserId, out var address))
            {
                address = await _users.FindEmailAsync(record.RequesterUserId, cancellationToken);
                addresses[record.RequesterUserId] = address;
            }

            responses.Add(ToContract(record, address, isSelf: record.RequesterUserId == userId));
        }

        return Ok(responses);
    }

    private static DisclosureRecordResponse ToContract(AuditRecord record, string? requester, bool isSelf) => new()
    {
        Id = record.Id,
        Timestamp = record.Timestamp,
        Requester = requester,
        IsSelf = isSelf,
        AttributeKey = record.RequestedAttributeKey,
        Purpose = record.Purpose.ToString(),
        Outcome = record.Outcome.ToString(),
        DenyReason = record.DenyReason?.ToString(),
        Transform = record.Transform?.ToString(),
        TransformParameter = record.TransformParameter,
        JustifyingPrinciple = record.JustifyingPrinciple
    };
}
