using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ac.Data.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddConversationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastMessage",
                schema: "tenant_template",
                table: "Conversations",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MessagesCount",
                schema: "tenant_template",
                table: "Conversations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Rating",
                schema: "tenant_template",
                table: "Conversations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "tenant_template",
                table: "Conversations",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastMessage",
                schema: "tenant_template",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "MessagesCount",
                schema: "tenant_template",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "Rating",
                schema: "tenant_template",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "tenant_template",
                table: "Conversations");
        }
    }
}
