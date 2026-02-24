using Libraries.Abstractions.Helpers;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ac.Data.Migrations.Api
{
    /// <inheritdoc />
    public partial class AddSystemAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
INSERT INTO auth.""AspNetUsers""(
""Id"", ""UserName"", ""NormalizedUserName"", ""Email"", ""NormalizedEmail"", ""EmailConfirmed"", ""PasswordHash"", ""SecurityStamp"", ""ConcurrencyStamp"", ""PhoneNumber"", ""PhoneNumberConfirmed"", ""TwoFactorEnabled"", ""LockoutEnd"", ""LockoutEnabled"", ""AccessFailedCount"", ""Created"", ""AuthorId"")
VALUES ('{SysAccountsHlp.Api.Id:D}', '{SysAccountsHlp.Api.Name}', '{SysAccountsHlp.Api.Name.ToUpper()}', '{SysAccountsHlp.Api.Name}', '{SysAccountsHlp.Api.Name.ToUpper()}',true, 'NO/ENTRY/POINT==', '000311A0DA3842DFA071FFAD95A003AE', '00367fb7-a0d8-4197-abdb-324a1ac60771', null, false, false, null, true, 0, now(), '{SysAccountsHlp.Api.Id:D}');
	");

            migrationBuilder.Sql($@"
INSERT INTO auth.""AspNetUsers""(
""Id"", ""UserName"", ""NormalizedUserName"", ""Email"", ""NormalizedEmail"", ""EmailConfirmed"", ""PasswordHash"", ""SecurityStamp"", ""ConcurrencyStamp"", ""PhoneNumber"", ""PhoneNumberConfirmed"", ""TwoFactorEnabled"", ""LockoutEnd"", ""LockoutEnabled"", ""AccessFailedCount"", ""Created"", ""AuthorId"")
VALUES ('{SysAccountsHlp.Ai.Id:D}', '{SysAccountsHlp.Ai.Name}', '{SysAccountsHlp.Ai.Name.ToUpper()}', '{SysAccountsHlp.Ai.Name}', '{SysAccountsHlp.Ai.Name.ToUpper()}',true, 'NO/ENTRY/POINT==', '001311A0DA3842DFA071FFAD95A003AE', '00167fb7-a0d8-4197-abdb-324a1ac60771', null, false, false, null, true, 0, now(), '{SysAccountsHlp.Api.Id:D}');
	");

            migrationBuilder.Sql($@"
INSERT INTO auth.""AspNetUsers""(
""Id"", ""UserName"", ""NormalizedUserName"", ""Email"", ""NormalizedEmail"", ""EmailConfirmed"", ""PasswordHash"", ""SecurityStamp"", ""ConcurrencyStamp"", ""PhoneNumber"", ""PhoneNumberConfirmed"", ""TwoFactorEnabled"", ""LockoutEnd"", ""LockoutEnabled"", ""AccessFailedCount"", ""Created"", ""AuthorId"")
VALUES ('{SysAccountsHlp.Auth.Id:D}', '{SysAccountsHlp.Auth.Name}', '{SysAccountsHlp.Auth.Name.ToUpper()}', '{SysAccountsHlp.Auth.Name}', '{SysAccountsHlp.Auth.Name.ToUpper()}',true, 'NO/ENTRY/POINT==', '002311A0DA3842DFA071FFAD95A003AE', '00267fb7-a0d8-4197-abdb-324a1ac60771', null, false, false, null, true, 0, now(), '{SysAccountsHlp.Api.Id:D}');
	");

            migrationBuilder.Sql($@"
INSERT INTO auth.""AspNetUsers""(
""Id"", ""UserName"", ""NormalizedUserName"", ""Email"", ""NormalizedEmail"", ""EmailConfirmed"", ""PasswordHash"", ""SecurityStamp"", ""ConcurrencyStamp"", ""PhoneNumber"", ""PhoneNumberConfirmed"", ""TwoFactorEnabled"", ""LockoutEnd"", ""LockoutEnabled"", ""AccessFailedCount"", ""Created"", ""AuthorId"")
VALUES ('{SysAccountsHlp.Channel.Id:D}', '{SysAccountsHlp.Channel.Name}', '{SysAccountsHlp.Channel.Name.ToUpper()}', '{SysAccountsHlp.Channel.Name}', '{SysAccountsHlp.Channel.Name.ToUpper()}',true, 'NO/ENTRY/POINT==', '004311A0DA3842DFA071FFAD95A003AE', '00367fb7-a0d8-4197-abdb-324a1ac60771', null, false, false, null, true, 0, now(), '{SysAccountsHlp.Api.Id:D}');
	");

            migrationBuilder.Sql($@"
INSERT INTO auth.""AspNetUsers""(
""Id"", ""UserName"", ""NormalizedUserName"", ""Email"", ""NormalizedEmail"", ""EmailConfirmed"", ""PasswordHash"", ""SecurityStamp"", ""ConcurrencyStamp"", ""PhoneNumber"", ""PhoneNumberConfirmed"", ""TwoFactorEnabled"", ""LockoutEnd"", ""LockoutEnabled"", ""AccessFailedCount"", ""Created"", ""AuthorId"")
VALUES ('{SysAccountsHlp.Job.Id:D}', '{SysAccountsHlp.Job.Name}', '{SysAccountsHlp.Job.Name.ToUpper()}', '{SysAccountsHlp.Job.Name}', '{SysAccountsHlp.Job.Name.ToUpper()}',true, 'NO/ENTRY/POINT==', '005311A0DA3842DFA071FFAD95A003AE', '00567fb7-a0d8-4197-abdb-324a1ac60771', null, false, false, null, true, 0, now(), '{SysAccountsHlp.Api.Id:D}');
	");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
