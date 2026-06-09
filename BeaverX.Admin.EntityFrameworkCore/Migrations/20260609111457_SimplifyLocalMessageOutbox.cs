using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeaverX.Admin.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyLocalMessageOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("TRUNCATE TABLE local_message_outbox;");

            migrationBuilder.DropIndex(
                name: "IX_local_message_outbox_CapConsumeMessageId",
                table: "local_message_outbox");

            migrationBuilder.DropIndex(
                name: "IX_local_message_outbox_IdempotencyKey",
                table: "local_message_outbox");

            migrationBuilder.DropIndex(
                name: "IX_local_message_outbox_MessageType_BusinessKey",
                table: "local_message_outbox");

            migrationBuilder.DropColumn(
                name: "CapConsumeMessageId",
                table: "local_message_outbox");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "local_message_outbox");

            migrationBuilder.DropColumn(
                name: "IsConsumed",
                table: "local_message_outbox");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "local_message_outbox");

            migrationBuilder.DropColumn(
                name: "MessageType",
                table: "local_message_outbox");

            migrationBuilder.DropColumn(
                name: "Payload",
                table: "local_message_outbox");

            migrationBuilder.DropColumn(
                name: "PublishedTime",
                table: "local_message_outbox");

            migrationBuilder.DropColumn(
                name: "BusinessKey",
                table: "local_message_outbox");

            migrationBuilder.AddColumn<string>(
                name: "CapMessageId",
                table: "local_message_outbox",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("ALTER TABLE local_message_outbox ALTER COLUMN \"CapMessageId\" DROP DEFAULT;");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ConsumedTime",
                table: "local_message_outbox",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_local_message_outbox_CapMessageId",
                table: "local_message_outbox",
                column: "CapMessageId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_local_message_outbox_CapMessageId",
                table: "local_message_outbox");

            migrationBuilder.DropColumn(
                name: "CapMessageId",
                table: "local_message_outbox");

            migrationBuilder.AddColumn<string>(
                name: "BusinessKey",
                table: "local_message_outbox",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ConsumedTime",
                table: "local_message_outbox",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "CapConsumeMessageId",
                table: "local_message_outbox",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "local_message_outbox",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsConsumed",
                table: "local_message_outbox",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "local_message_outbox",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MessageType",
                table: "local_message_outbox",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Payload",
                table: "local_message_outbox",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedTime",
                table: "local_message_outbox",
                type: "timestamp with time zone",
                nullable: true);

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
    }
}
