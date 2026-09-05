using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FacetIQ.Data.Migrations.App
{
    /// <inheritdoc />
    public partial class AddStanding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "standings",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequesterUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Value = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IssuerKind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Issuer = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_standings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_standings_SubjectId_RequesterUserId",
                schema: "app",
                table: "standings",
                columns: new[] { "SubjectId", "RequesterUserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "standings",
                schema: "app");
        }
    }
}
