using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BeaverX.Admin.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceExportOutboxWithLocalMessageOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "export_task_outbox");

            migrationBuilder.CreateTable(
                name: "local_message_outbox",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MessageType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BusinessKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Payload = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    IsConsumed = table.Column<bool>(type: "boolean", nullable: false),
                    CapConsumeMessageId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    PublishedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConsumedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_local_message_outbox", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_local_message_outbox_CapConsumeMessageId",
                table: "local_message_outbox",
                column: "CapConsumeMessageId",
                unique: true,
                filter: "\"CapConsumeMessageId\" IS NOT NULL AND \"CapConsumeMessageId\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_local_message_outbox_IdempotencyKey",
                table: "local_message_outbox",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_local_message_outbox_MessageType_BusinessKey",
                table: "local_message_outbox",
                columns: new[] { "MessageType", "BusinessKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "local_message_outbox");

            migrationBuilder.CreateTable(
                name: "export_task_outbox",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExportTaskId = table.Column<long>(type: "bigint", nullable: false),
                    CapConsumeMessageId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ConsumedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IsConsumed = table.Column<bool>(type: "boolean", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    PublishedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_export_task_outbox", x => x.Id);
                    table.ForeignKey(
                        name: "FK_export_task_outbox_export_tasks_ExportTaskId",
                        column: x => x.ExportTaskId,
                        principalTable: "export_tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_export_task_outbox_CapConsumeMessageId",
                table: "export_task_outbox",
                column: "CapConsumeMessageId",
                unique: true,
                filter: "\"CapConsumeMessageId\" IS NOT NULL AND \"CapConsumeMessageId\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_export_task_outbox_ExportTaskId",
                table: "export_task_outbox",
                column: "ExportTaskId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_export_task_outbox_IdempotencyKey",
                table: "export_task_outbox",
                column: "IdempotencyKey",
                unique: true);
        }
    }
}
