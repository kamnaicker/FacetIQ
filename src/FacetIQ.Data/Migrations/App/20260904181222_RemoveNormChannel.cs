using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FacetIQ.Data.Migrations.App
{
    /// <inheritdoc />
    public partial class RemoveNormChannel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Channel",
                schema: "app",
                table: "norms");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Channel",
                schema: "app",
                table: "norms",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.UpdateData(
                schema: "app",
                table: "norms",
                keyColumns: new[] { "Id", "Version" },
                keyValues: new object[] { new Guid("0a5f4d8e-0000-4000-8000-000000000020"), 1 },
                column: "Channel",
                value: null);

            migrationBuilder.UpdateData(
                schema: "app",
                table: "norms",
                keyColumns: new[] { "Id", "Version" },
                keyValues: new object[] { new Guid("0a5f4d8e-0000-4000-8000-000000000021"), 1 },
                column: "Channel",
                value: null);

            migrationBuilder.UpdateData(
                schema: "app",
                table: "norms",
                keyColumns: new[] { "Id", "Version" },
                keyValues: new object[] { new Guid("0a5f4d8e-0000-4000-8000-000000000022"), 1 },
                column: "Channel",
                value: null);

            migrationBuilder.UpdateData(
                schema: "app",
                table: "norms",
                keyColumns: new[] { "Id", "Version" },
                keyValues: new object[] { new Guid("0a5f4d8e-0000-4000-8000-000000000023"), 1 },
                column: "Channel",
                value: null);

            migrationBuilder.UpdateData(
                schema: "app",
                table: "norms",
                keyColumns: new[] { "Id", "Version" },
                keyValues: new object[] { new Guid("0a5f4d8e-0000-4000-8000-000000000024"), 1 },
                column: "Channel",
                value: null);
        }
    }
}
