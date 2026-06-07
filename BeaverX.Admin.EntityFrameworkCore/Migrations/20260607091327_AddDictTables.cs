using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BeaverX.Admin.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddDictTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sys_dict_types",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Remark = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifierId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeleterId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_dict_types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sys_dict_data",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DictTypeId = table.Column<long>(type: "bigint", nullable: false),
                    Label = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Value = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Sort = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CssClass = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ListClass = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Remark = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifierId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeleterId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_dict_data", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sys_dict_data_sys_dict_types_DictTypeId",
                        column: x => x.DictTypeId,
                        principalTable: "sys_dict_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sys_dict_data_DictTypeId_Value",
                table: "sys_dict_data",
                columns: new[] { "DictTypeId", "Value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sys_dict_types_Code",
                table: "sys_dict_types",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sys_dict_data");

            migrationBuilder.DropTable(
                name: "sys_dict_types");
        }
    }
}
