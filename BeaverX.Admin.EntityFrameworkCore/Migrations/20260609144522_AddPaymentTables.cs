using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BeaverX.Admin.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pay_channels",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChannelCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ChannelName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProviderType = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ConfigJson = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    NotifyUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Remark = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Sort = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_pay_channels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "pay_notify_logs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NotifyType = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ChannelCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OrderNo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RefundNo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RawBody = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    ProcessSuccess = table.Column<bool>(type: "boolean", nullable: false),
                    ProcessMessage = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pay_notify_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "pay_orders",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderNo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ChannelCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Subject = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    Currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ClientIp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Attach = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    BusinessType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    BusinessId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    ExpireTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaidTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ChannelOrderNo = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ChannelUserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    QrCodeUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    RefundedAmount = table.Column<long>(type: "bigint", nullable: false),
                    ErrorCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
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
                    table.PrimaryKey("PK_pay_orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "pay_refunds",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RefundNo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PaymentOrderId = table.Column<long>(type: "bigint", nullable: false),
                    OrderNo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ChannelCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    TotalAmount = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ChannelRefundNo = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ChannelOrderNo = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Reason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    RefundTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
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
                    table.PrimaryKey("PK_pay_refunds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pay_refunds_pay_orders_PaymentOrderId",
                        column: x => x.PaymentOrderId,
                        principalTable: "pay_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pay_channels_ChannelCode",
                table: "pay_channels",
                column: "ChannelCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pay_notify_logs_CreatedTime",
                table: "pay_notify_logs",
                column: "CreatedTime");

            migrationBuilder.CreateIndex(
                name: "IX_pay_orders_ChannelCode",
                table: "pay_orders",
                column: "ChannelCode");

            migrationBuilder.CreateIndex(
                name: "IX_pay_orders_OrderNo",
                table: "pay_orders",
                column: "OrderNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pay_orders_Status_CreationTime",
                table: "pay_orders",
                columns: new[] { "Status", "CreationTime" });

            migrationBuilder.CreateIndex(
                name: "IX_pay_refunds_OrderNo",
                table: "pay_refunds",
                column: "OrderNo");

            migrationBuilder.CreateIndex(
                name: "IX_pay_refunds_PaymentOrderId",
                table: "pay_refunds",
                column: "PaymentOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_pay_refunds_RefundNo",
                table: "pay_refunds",
                column: "RefundNo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pay_channels");

            migrationBuilder.DropTable(
                name: "pay_notify_logs");

            migrationBuilder.DropTable(
                name: "pay_refunds");

            migrationBuilder.DropTable(
                name: "pay_orders");
        }
    }
}
