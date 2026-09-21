using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FacetIQ.Data.Migrations.Auth
{
    /// <inheritdoc />
    public partial class AddPendingRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PendingRegistrations",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    CodeHash = table.Column<string>(type: "text", nullable: false),
                    CancellationTokenHash = table.Column<string>(type: "text", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    Resends = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingRegistrations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PendingRegistrations_CancellationTokenHash",
                schema: "auth",
                table: "PendingRegistrations",
                column: "CancellationTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PendingRegistrations_NormalizedEmail",
                schema: "auth",
                table: "PendingRegistrations",
                column: "NormalizedEmail");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PendingRegistrations",
                schema: "auth");
        }
    }
}
