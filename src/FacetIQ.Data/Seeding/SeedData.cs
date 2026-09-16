using FacetIQ.Domain.Entities;
using FacetIQ.Domain.Enums;

namespace FacetIQ.Data.Seeding;

/// <summary>
/// Worked example seeded through migrations. Amara's three names are independent claims, so a
/// norm selects one rather than transforming another.
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

    // Must match the ids DevelopmentUserSeeder creates.
    public const string AmaraUserId = "seed-amara";

    public const string AcceptedColleagueUserId = "seed-colleague-accepted";

    public const string PendingColleagueUserId = "seed-colleague-pending";

    // Fixed, or every migration scaffold would rewrite the seed.
    private static readonly DateTimeOffset SeededAt = new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);

    public static Subject[] Subjects { get; } =
    [
        new Subject { Id = SubjectId, UserId = AmaraUserId }
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

            // Overrides the regulatory norm below, which would otherwise release the exact date.
            CollectedFor = Purpose.Social
        }
    ];

    /// <summary>Two colleague standings that differ only in whether they were accepted.</summary>
    public static Standing[] Standings { get; } =
    [
        new Standing
        {
            Id = AcceptedColleague,
            SubjectId = SubjectId,
            RequesterUserId = AcceptedColleagueUserId,
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
            RequesterUserId = PendingColleagueUserId,
            Value = "colleague",
            IssuerKind = IssuerKind.Subject,
            Issuer = AmaraUserId,
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
