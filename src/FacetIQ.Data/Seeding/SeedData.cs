using FacetIQ.Domain.Entities;
using FacetIQ.Domain.Enums;

namespace FacetIQ.Data.Seeding;

/// <summary>
/// A worked example carried into the schema so the engine has something to decide about.
///
/// Amara holds three names. None is derived from another and none is canonical: the legal
/// name cannot be computed from the social one by any rule. Selecting between them is the
/// system's primary act, and the transforms below only shape whichever claim was selected.
/// </summary>
public static class SeedData
{
    public static readonly Guid SubjectId = new("0a5f4d8e-0000-4000-8000-000000000001");

    public static readonly Guid LegalName = new("0a5f4d8e-0000-4000-8000-000000000010");
    public static readonly Guid ProfessionalName = new("0a5f4d8e-0000-4000-8000-000000000011");
    public static readonly Guid SocialName = new("0a5f4d8e-0000-4000-8000-000000000012");
    public static readonly Guid DateOfBirth = new("0a5f4d8e-0000-4000-8000-000000000013");

    public static readonly Guid AcceptedColleague = new("0a5f4d8e-0000-4000-8000-000000000030");
    public static readonly Guid PendingColleague = new("0a5f4d8e-0000-4000-8000-000000000031");

    // Fixed so the seed is deterministic: a clock read here would rewrite the migration on
    // every scaffold.
    private static readonly DateTimeOffset SeededAt = new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);

    public static Subject[] Subjects { get; } =
    [
        new Subject { Id = SubjectId, UserId = "seed-amara" }
    ];

    public static SubjectAttribute[] Attributes { get; } =
    [
        new SubjectAttribute
        {
            Id = LegalName,
            SubjectId = SubjectId,
            Key = "name",
            Value = "Amara Chidinma Nwosu",
            Label = "legal"
        },
        new SubjectAttribute
        {
            Id = ProfessionalName,
            SubjectId = SubjectId,
            Key = "name",
            Value = "Dr Amara Nwosu",
            Label = "professional"
        },
        new SubjectAttribute
        {
            Id = SocialName,
            SubjectId = SubjectId,
            Key = "name",
            Value = "Amara",
            Label = "social"
        },
        new SubjectAttribute
        {
            Id = DateOfBirth,
            SubjectId = SubjectId,
            Key = "dateOfBirth",
            Value = "1994-03-11",
            Label = "legal",

            // Given for a social context. The subject still wrote a norm releasing the exact
            // date for regulatory requests, and the collection purpose is what overrides it.
            CollectedFor = Purpose.Social
        }
    ];

    /// <summary>
    /// Two requesters differing in one thing only. Both hold a colleague standing towards Amara;
    /// one was accepted and one never was. Nothing else separates them, so any difference in what
    /// they receive is attributable to the acceptance alone.
    /// </summary>
    public static Standing[] Standings { get; } =
    [
        new Standing
        {
            Id = AcceptedColleague,
            SubjectId = SubjectId,
            RequesterUserId = "seed-colleague-accepted",
            Value = "colleague",
            IssuerKind = IssuerKind.Institution,
            Issuer = "Example Teaching Hospital",
            IssuedAt = SeededAt,
            AcceptedAt = SeededAt
        },
        new Standing
        {
            Id = PendingColleague,
            SubjectId = SubjectId,
            RequesterUserId = "seed-colleague-pending",
            Value = "colleague",
            IssuerKind = IssuerKind.Subject,

            // Amara asserting a colleague relationship about someone else. It stays inert until
            // they accept, which is what stops her placing a person in a context unilaterally.
            Issuer = "seed-amara",
            IssuedAt = SeededAt,
            AcceptedAt = null
        }
    ];

    public static Norm[] Norms { get; } =
    [
        new Norm
        {
            Id = new Guid("0a5f4d8e-0000-4000-8000-000000000020"),
            Version = 1,
            SubjectId = SubjectId,
            AttributeId = LegalName,
            Purpose = Purpose.Regulatory,
            Action = ActionType.Return,
            Transform = TransformKind.None,
            JustifyingPrinciple = "GDPR Art. 6(1)(c): disclosure necessary for a legal obligation."
        },
        new Norm
        {
            Id = new Guid("0a5f4d8e-0000-4000-8000-000000000021"),
            Version = 1,
            SubjectId = SubjectId,
            AttributeId = ProfessionalName,
            Purpose = Purpose.Clinical,
            Relationship = "colleague",
            Action = ActionType.Return,
            Transform = TransformKind.None,
            JustifyingPrinciple = "Contextual integrity: the professional context expects the professional name."
        },
        new Norm
        {
            Id = new Guid("0a5f4d8e-0000-4000-8000-000000000022"),
            Version = 1,
            SubjectId = SubjectId,
            AttributeId = SocialName,
            Purpose = Purpose.Social,
            Action = ActionType.Return,
            Transform = TransformKind.None,
            JustifyingPrinciple = "Contextual integrity: a social enquiry warrants the name used socially."
        },
        new Norm
        {
            Id = new Guid("0a5f4d8e-0000-4000-8000-000000000023"),
            Version = 1,
            SubjectId = SubjectId,
            AttributeId = DateOfBirth,
            Purpose = Purpose.Social,
            Action = ActionType.Transform,
            Transform = TransformKind.Generalise,
            TransformParameter = "18",
            JustifyingPrinciple = "GDPR Art. 5(1)(c): a threshold satisfies the purpose without the date."
        },
        new Norm
        {
            Id = new Guid("0a5f4d8e-0000-4000-8000-000000000024"),
            Version = 1,
            SubjectId = SubjectId,
            AttributeId = DateOfBirth,
            Purpose = Purpose.Regulatory,
            Action = ActionType.Return,
            Transform = TransformKind.None,
            JustifyingPrinciple = "GDPR Art. 6(1)(c): identity verification requires the recorded date."
        }
    ];
}
