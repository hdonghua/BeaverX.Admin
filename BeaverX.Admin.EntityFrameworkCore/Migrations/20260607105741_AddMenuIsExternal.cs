using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeaverX.Admin.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuIsExternal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsExternal",
                table: "sys_menus",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsExternal",
                table: "sys_menus");
        }
    }
}
