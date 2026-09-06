using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FacetIQ.Data.Migrations.App
{
    /// <inheritdoc />
    public partial class SeedStandings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "app",
                table: "standings",
                columns: new[] { "Id", "AcceptedAt", "IssuedAt", "Issuer", "IssuerKind", "RequesterUserId", "SubjectId", "Value" },
                values: new object[,]
                {
                    { new Guid("0a5f4d8e-0000-4000-8000-000000000030"), new DateTimeOffset(new DateTime(2026, 8, 1, 9, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 1, 9, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Example Teaching Hospital", "Institution", "seed-colleague-accepted", new Guid("0a5f4d8e-0000-4000-8000-000000000001"), "colleague" },
                    { new Guid("0a5f4d8e-0000-4000-8000-000000000031"), null, new DateTimeOffset(new DateTime(2026, 8, 1, 9, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "seed-amara", "Subject", "seed-colleague-pending", new Guid("0a5f4d8e-0000-4000-8000-000000000001"), "colleague" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "app",
                table: "standings",
                keyColumn: "Id",
                keyValue: new Guid("0a5f4d8e-0000-4000-8000-000000000030"));

            migrationBuilder.DeleteData(
                schema: "app",
                table: "standings",
                keyColumn: "Id",
                keyValue: new Guid("0a5f4d8e-0000-4000-8000-000000000031"));
        }
    }
}
