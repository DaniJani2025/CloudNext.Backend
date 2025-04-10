using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudNext.Migrations
{
    /// <inheritdoc />
    public partial class AddedRelativePath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RelativePath",
                table: "UserFolders",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RelativePath",
                table: "UserFolders");
        }
    }
}
