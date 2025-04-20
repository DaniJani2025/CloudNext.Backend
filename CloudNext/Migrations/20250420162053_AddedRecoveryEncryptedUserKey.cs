using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudNext.Migrations
{
    /// <inheritdoc />
    public partial class AddedRecoveryEncryptedUserKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RecoveryEncryptedUserKey",
                table: "Users",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecoveryEncryptedUserKey",
                table: "Users");
        }
    }
}
