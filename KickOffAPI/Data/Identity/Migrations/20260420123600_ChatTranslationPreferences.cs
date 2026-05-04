using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KickOffAPI.Data.Identity.Migrations
{
    [DbContext(typeof(AppIdentityDbContext))]
    [Migration("20260420123600_ChatTranslationPreferences")]
    public partial class ChatTranslationPreferences : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreferredChatLanguage",
                table: "Users",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowOriginalChatTextByDefault",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreferredChatLanguage",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ShowOriginalChatTextByDefault",
                table: "Users");
        }
    }
}
