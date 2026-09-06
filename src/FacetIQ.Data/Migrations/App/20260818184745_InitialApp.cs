using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FacetIQ.Data.Migrations.App
{
    /// <inheritdoc />
    public partial class InitialApp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "app");

            migrationBuilder.CreateTable(
                name: "audit_records",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RequesterUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedAttributeKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Purpose = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Channel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Outcome = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DenyReason = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    NormId = table.Column<Guid>(type: "uuid", nullable: true),
                    NormVersion = table.Column<int>(type: "integer", nullable: true),
                    Transform = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    TransformParameter = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    JustifyingPrinciple = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_records", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "subjects",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subjects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "subject_attributes",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Value = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Label = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subject_attributes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_subject_attributes_subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalSchema: "app",
                        principalTable: "subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "norms",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    SupersededAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttributeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Relationship = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Purpose = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Channel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Action = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Transform = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TransformParameter = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    DenyReason = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    JustifyingPrinciple = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_norms", x => new { x.Id, x.Version });
                    table.ForeignKey(
                        name: "FK_norms_subject_attributes_AttributeId",
                        column: x => x.AttributeId,
                        principalSchema: "app",
                        principalTable: "subject_attributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "app",
                table: "subjects",
                columns: new[] { "Id", "UserId" },
                values: new object[] { new Guid("0a5f4d8e-0000-4000-8000-000000000001"), "seed-amara" });

            migrationBuilder.InsertData(
                schema: "app",
                table: "subject_attributes",
                columns: new[] { "Id", "Key", "Label", "SubjectId", "Value" },
                values: new object[,]
                {
                    { new Guid("0a5f4d8e-0000-4000-8000-000000000010"), "name", "legal", new Guid("0a5f4d8e-0000-4000-8000-000000000001"), "Amara Chidinma Nwosu" },
                    { new Guid("0a5f4d8e-0000-4000-8000-000000000011"), "name", "professional", new Guid("0a5f4d8e-0000-4000-8000-000000000001"), "Dr Amara Nwosu" },
                    { new Guid("0a5f4d8e-0000-4000-8000-000000000012"), "name", "social", new Guid("0a5f4d8e-0000-4000-8000-000000000001"), "Amara" },
                    { new Guid("0a5f4d8e-0000-4000-8000-000000000013"), "dateOfBirth", "legal", new Guid("0a5f4d8e-0000-4000-8000-000000000001"), "1994-03-11" }
                });

            migrationBuilder.InsertData(
                schema: "app",
                table: "norms",
                columns: new[] { "Id", "Version", "Action", "AttributeId", "Channel", "DenyReason", "JustifyingPrinciple", "Purpose", "Relationship", "SubjectId", "SupersededAt", "Transform", "TransformParameter" },
                values: new object[,]
                {
                    { new Guid("0a5f4d8e-0000-4000-8000-000000000020"), 1, "Return", new Guid("0a5f4d8e-0000-4000-8000-000000000010"), null, null, "GDPR Art. 6(1)(c): disclosure necessary for a legal obligation.", "Regulatory", null, new Guid("0a5f4d8e-0000-4000-8000-000000000001"), null, "None", null },
                    { new Guid("0a5f4d8e-0000-4000-8000-000000000021"), 1, "Return", new Guid("0a5f4d8e-0000-4000-8000-000000000011"), null, null, "Contextual integrity: the professional context expects the professional name.", "Clinical", "colleague", new Guid("0a5f4d8e-0000-4000-8000-000000000001"), null, "None", null },
                    { new Guid("0a5f4d8e-0000-4000-8000-000000000022"), 1, "Return", new Guid("0a5f4d8e-0000-4000-8000-000000000012"), null, null, "Contextual integrity: a social enquiry warrants the name used socially.", "Social", null, new Guid("0a5f4d8e-0000-4000-8000-000000000001"), null, "None", null },
                    { new Guid("0a5f4d8e-0000-4000-8000-000000000023"), 1, "Transform", new Guid("0a5f4d8e-0000-4000-8000-000000000013"), null, null, "GDPR Art. 5(1)(c): a threshold satisfies the purpose without the date.", "Social", null, new Guid("0a5f4d8e-0000-4000-8000-000000000001"), null, "Generalise", "18" },
                    { new Guid("0a5f4d8e-0000-4000-8000-000000000024"), 1, "Return", new Guid("0a5f4d8e-0000-4000-8000-000000000013"), null, null, "GDPR Art. 6(1)(c): identity verification requires the recorded date.", "Regulatory", null, new Guid("0a5f4d8e-0000-4000-8000-000000000001"), null, "None", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_records_SubjectId_Timestamp",
                schema: "app",
                table: "audit_records",
                columns: new[] { "SubjectId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_norms_AttributeId",
                schema: "app",
                table: "norms",
                column: "AttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_norms_SubjectId_SupersededAt",
                schema: "app",
                table: "norms",
                columns: new[] { "SubjectId", "SupersededAt" });

            migrationBuilder.CreateIndex(
                name: "IX_subject_attributes_SubjectId_Key",
                schema: "app",
                table: "subject_attributes",
                columns: new[] { "SubjectId", "Key" });

            migrationBuilder.CreateIndex(
                name: "IX_subjects_UserId",
                schema: "app",
                table: "subjects",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_records",
                schema: "app");

            migrationBuilder.DropTable(
                name: "norms",
                schema: "app");

            migrationBuilder.DropTable(
                name: "subject_attributes",
                schema: "app");

            migrationBuilder.DropTable(
                name: "subjects",
                schema: "app");
        }
    }
}
