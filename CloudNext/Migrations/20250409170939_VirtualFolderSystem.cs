using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudNext.Migrations
{
    /// <inheritdoc />
    public partial class VirtualFolderSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RelativePath",
                table: "UserFolders",
                newName: "VirtualPath");

            migrationBuilder.AddColumn<string>(
                name: "OriginalName",
                table: "UserFiles",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OriginalName",
                table: "UserFiles");

            migrationBuilder.RenameColumn(
                name: "VirtualPath",
                table: "UserFolders",
                newName: "RelativePath");
        }
    }
}
