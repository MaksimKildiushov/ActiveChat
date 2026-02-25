using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Ac.Data.Migrations.Api
{
    /// <inheritdoc />
    public partial class AddEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Events",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventType = table.Column<int>(type: "integer", nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    MaxRetries = table.Column<int>(type: "integer", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextRetryAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ErrorStackTrace = table.Column<string>(type: "text", nullable: true),
                    ProcessingId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ProcessingStartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Events_AspNetUsers_AuthorId",
                        column: x => x.AuthorId,
                        principalSchema: "auth",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Events_AspNetUsers_ModifierId",
                        column: x => x.ModifierId,
                        principalSchema: "auth",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Events_AuthorId",
                schema: "public",
                table: "Events",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_ModifierId",
                schema: "public",
                table: "Events",
                column: "ModifierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Events",
                schema: "public");
        }
    }
}
