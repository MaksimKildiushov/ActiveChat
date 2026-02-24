using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ac.Data.Migrations.Api
{
    /// <inheritdoc />
    public partial class UpdateTenantUsersLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TenantUsers_AspNetUsers_AuthorId",
                table: "TenantUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_TenantUsers_AspNetUsers_ModifierId",
                table: "TenantUsers");

            migrationBuilder.AddForeignKey(
                name: "FK_TenantUsers_AspNetUsers_AuthorId",
                table: "TenantUsers",
                column: "AuthorId",
                principalSchema: "auth",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TenantUsers_AspNetUsers_ModifierId",
                table: "TenantUsers",
                column: "ModifierId",
                principalSchema: "auth",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TenantUsers_AspNetUsers_AuthorId",
                table: "TenantUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_TenantUsers_AspNetUsers_ModifierId",
                table: "TenantUsers");

            migrationBuilder.AddForeignKey(
                name: "FK_TenantUsers_AspNetUsers_AuthorId",
                table: "TenantUsers",
                column: "AuthorId",
                principalSchema: "auth",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TenantUsers_AspNetUsers_ModifierId",
                table: "TenantUsers",
                column: "ModifierId",
                principalSchema: "auth",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
