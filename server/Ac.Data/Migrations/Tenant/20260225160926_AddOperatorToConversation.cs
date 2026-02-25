using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ac.Data.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddOperatorToConversation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OperatorId",
                schema: "tenant_template",
                table: "Conversations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_OperatorId",
                schema: "tenant_template",
                table: "Conversations",
                column: "OperatorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_AspNetUsers_OperatorId",
                schema: "tenant_template",
                table: "Conversations",
                column: "OperatorId",
                principalSchema: "auth",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_AspNetUsers_OperatorId",
                schema: "tenant_template",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_OperatorId",
                schema: "tenant_template",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "OperatorId",
                schema: "tenant_template",
                table: "Conversations");
        }
    }
}
