using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ac.Data.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class UpdateMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "tenant_template",
                table: "Messages");

            migrationBuilder.RenameColumn(
                name: "Text",
                schema: "tenant_template",
                table: "Messages",
                newName: "Type");

            migrationBuilder.AddColumn<string>(
                name: "Content",
                schema: "tenant_template",
                table: "Messages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                schema: "tenant_template",
                table: "Messages",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FileSize",
                schema: "tenant_template",
                table: "Messages",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MimeType",
                schema: "tenant_template",
                table: "Messages",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReadAt",
                schema: "tenant_template",
                table: "Messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "tenant_template",
                table: "Messages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Thumb",
                schema: "tenant_template",
                table: "Messages",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Content",
                schema: "tenant_template",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "FilePath",
                schema: "tenant_template",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "FileSize",
                schema: "tenant_template",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "MimeType",
                schema: "tenant_template",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ReadAt",
                schema: "tenant_template",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "tenant_template",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "Thumb",
                schema: "tenant_template",
                table: "Messages");

            migrationBuilder.RenameColumn(
                name: "Type",
                schema: "tenant_template",
                table: "Messages",
                newName: "Text");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "tenant_template",
                table: "Messages",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));
        }
    }
}
