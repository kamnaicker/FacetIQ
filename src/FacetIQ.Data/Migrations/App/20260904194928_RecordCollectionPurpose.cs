using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FacetIQ.Data.Migrations.App
{
    /// <inheritdoc />
    public partial class RecordCollectionPurpose : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CollectedFor",
                schema: "app",
                table: "subject_attributes",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.UpdateData(
                schema: "app",
                table: "subject_attributes",
                keyColumn: "Id",
                keyValue: new Guid("0a5f4d8e-0000-4000-8000-000000000010"),
                column: "CollectedFor",
                value: null);

            migrationBuilder.UpdateData(
                schema: "app",
                table: "subject_attributes",
                keyColumn: "Id",
                keyValue: new Guid("0a5f4d8e-0000-4000-8000-000000000011"),
                column: "CollectedFor",
                value: null);

            migrationBuilder.UpdateData(
                schema: "app",
                table: "subject_attributes",
                keyColumn: "Id",
                keyValue: new Guid("0a5f4d8e-0000-4000-8000-000000000012"),
                column: "CollectedFor",
                value: null);

            migrationBuilder.UpdateData(
                schema: "app",
                table: "subject_attributes",
                keyColumn: "Id",
                keyValue: new Guid("0a5f4d8e-0000-4000-8000-000000000013"),
                column: "CollectedFor",
                value: "Social");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CollectedFor",
                schema: "app",
                table: "subject_attributes");
        }
    }
}
