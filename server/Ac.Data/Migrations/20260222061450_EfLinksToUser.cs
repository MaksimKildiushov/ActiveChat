using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ac.Data.Migrations
{
    /// <inheritdoc />
    public partial class EfLinksToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_AuthorId",
                schema: "auth",
                table: "AspNetUsers",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_ModifierId",
                schema: "auth",
                table: "AspNetUsers",
                column: "ModifierId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_AspNetUsers_AuthorId",
                schema: "auth",
                table: "AspNetUsers",
                column: "AuthorId",
                principalSchema: "auth",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_AspNetUsers_ModifierId",
                schema: "auth",
                table: "AspNetUsers",
                column: "ModifierId",
                principalSchema: "auth",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_AspNetUsers_AuthorId",
                schema: "auth",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_AspNetUsers_ModifierId",
                schema: "auth",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_AuthorId",
                schema: "auth",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_ModifierId",
                schema: "auth",
                table: "AspNetUsers");
        }
    }
}
