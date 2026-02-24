using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Ac.Data.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddClients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Conversations_ChannelId",
                schema: "tenant_template",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_TenantId_ChannelId_ExternalUserId",
                schema: "tenant_template",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "ExternalUserId",
                schema: "tenant_template",
                table: "Conversations");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                schema: "tenant_template",
                table: "Conversations",
                newName: "ClientId");

            migrationBuilder.AddColumn<string>(
                name: "ChatId",
                schema: "tenant_template",
                table: "Conversations",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Clients",
                schema: "tenant_template",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChannelUserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    OverrideUserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Phone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TraceId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Timezone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: true),
                    AdditionalFieldsJson = table.Column<string>(type: "jsonb", nullable: true),
                    IsBlocked = table.Column<bool>(type: "boolean", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Clients_AspNetUsers_AuthorId",
                        column: x => x.AuthorId,
                        principalSchema: "auth",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Clients_AspNetUsers_ModifierId",
                        column: x => x.ModifierId,
                        principalSchema: "auth",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_ChannelId_ClientId",
                schema: "tenant_template",
                table: "Conversations",
                columns: new[] { "ChannelId", "ClientId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_ClientId",
                schema: "tenant_template",
                table: "Conversations",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_AuthorId",
                schema: "tenant_template",
                table: "Clients",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_ChannelUserId",
                schema: "tenant_template",
                table: "Clients",
                column: "ChannelUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clients_ModifierId",
                schema: "tenant_template",
                table: "Clients",
                column: "ModifierId");

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_Clients_ClientId",
                schema: "tenant_template",
                table: "Conversations",
                column: "ClientId",
                principalSchema: "tenant_template",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_Clients_ClientId",
                schema: "tenant_template",
                table: "Conversations");

            migrationBuilder.DropTable(
                name: "Clients",
                schema: "tenant_template");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_ChannelId_ClientId",
                schema: "tenant_template",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_ClientId",
                schema: "tenant_template",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "ChatId",
                schema: "tenant_template",
                table: "Conversations");

            migrationBuilder.RenameColumn(
                name: "ClientId",
                schema: "tenant_template",
                table: "Conversations",
                newName: "TenantId");

            migrationBuilder.AddColumn<string>(
                name: "ExternalUserId",
                schema: "tenant_template",
                table: "Conversations",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_ChannelId",
                schema: "tenant_template",
                table: "Conversations",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_TenantId_ChannelId_ExternalUserId",
                schema: "tenant_template",
                table: "Conversations",
                columns: new[] { "TenantId", "ChannelId", "ExternalUserId" },
                unique: true);
        }
    }
}
