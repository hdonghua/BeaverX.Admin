using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeaverX.Admin.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class InitCreated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "biz_work_tickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketNo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImagesJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ProcessResult = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ProcessResultImagesJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    HandlerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProcessedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_biz_work_tickets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "export_tasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExportType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Parameters = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    FileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ObjectKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    FileUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CompletedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_export_tasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "local_message_outbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CapMessageId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ConsumedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_local_message_outbox", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "oa_approver_configs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rid = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AssigneeType = table.Column<int>(type: "integer", nullable: false),
                    LayerType = table.Column<int>(type: "integer", nullable: true),
                    Layer = table.Column<int>(type: "integer", nullable: true),
                    Assignees = table.Column<List<string>>(type: "text[]", nullable: false),
                    Roles = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oa_approver_configs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "oa_cc_configs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rid = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CcType = table.Column<int>(type: "integer", nullable: false),
                    Assignees = table.Column<List<string>>(type: "text[]", nullable: false),
                    Roles = table.Column<List<string>>(type: "text[]", nullable: false),
                    LayerType = table.Column<int>(type: "integer", nullable: true),
                    Layer = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oa_cc_configs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "oa_cc_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    NodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    ReadTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oa_cc_records", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "oa_comments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: true),
                    Commenter = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Attachment = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oa_comments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "oa_condition_groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oa_condition_groups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "oa_conditions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    VarName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Operator = table.Column<int>(type: "integer", nullable: false),
                    Values = table.Column<List<string>>(type: "text[]", nullable: true),
                    Operators = table.Column<List<int>>(type: "integer[]", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oa_conditions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "oa_departments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    LeaderUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Sort = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oa_departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "oa_form_fields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DefId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FieldType = table.Column<int>(type: "integer", nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsSummary = table.Column<bool>(type: "boolean", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    Placeholder = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Extras = table.Column<string>(type: "text", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oa_form_fields", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "oa_initiators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DefId = table.Column<Guid>(type: "uuid", nullable: false),
                    InitiatorType = table.Column<int>(type: "integer", nullable: false),
                    InitiatorIds = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oa_initiators", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "oa_instances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstanceNo = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    DefId = table.Column<Guid>(type: "uuid", nullable: false),
                    Initiator = table.Column<Guid>(type: "uuid", nullable: false),
                    FormValue = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oa_instances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "oa_nodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DefId = table.Column<Guid>(type: "uuid", nullable: false),
                    NodeName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NodeType = table.Column<int>(type: "integer", nullable: false),
                    ParentNodeId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsConditionBranch = table.Column<bool>(type: "boolean", nullable: false),
                    PriorityLevel = table.Column<int>(type: "integer", nullable: true),
                    ConditionExpression = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ChildNodeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovalType = table.Column<int>(type: "integer", nullable: false),
                    MultiInstanceApprovalType = table.Column<int>(type: "integer", nullable: true),
                    FlowNodeNoAuditorType = table.Column<int>(type: "integer", nullable: true),
                    FlowNodeNoAuditorAssignee = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    FlowNodeSelfAuditorType = table.Column<int>(type: "integer", nullable: true),
                    Extras = table.Column<string>(type: "text", nullable: true),
                    Backable = table.Column<bool>(type: "boolean", nullable: false),
                    Signable = table.Column<bool>(type: "boolean", nullable: false),
                    Assignable = table.Column<bool>(type: "boolean", nullable: false),
                    Signature = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oa_nodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "oa_operation_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: true),
                    Operator = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationType = table.Column<int>(type: "integer", nullable: false),
                    SourceNodeId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetNodeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oa_operation_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "oa_process_definitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionType = table.Column<int>(type: "integer", nullable: false),
                    BelongKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    Cancelable = table.Column<bool>(type: "boolean", nullable: false),
                    FlowAdminIds = table.Column<List<string>>(type: "text[]", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DefJson = table.Column<string>(type: "text", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oa_process_definitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "oa_process_groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oa_process_groups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "oa_tasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    NodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    NodeName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    FlowCmd = table.Column<int>(type: "integer", nullable: true),
                    CandidateUsers = table.Column<List<string>>(type: "text[]", nullable: true),
                    ParentTaskId = table.Column<Guid>(type: "uuid", nullable: true),
                    LoopCounter = table.Column<int>(type: "integer", nullable: true),
                    CompleteTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oa_tasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "oa_transact_configs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rid = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AssigneeType = table.Column<int>(type: "integer", nullable: false),
                    LayerType = table.Column<int>(type: "integer", nullable: true),
                    Layer = table.Column<int>(type: "integer", nullable: true),
                    Assignees = table.Column<List<string>>(type: "text[]", nullable: false),
                    Roles = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oa_transact_configs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "oa_user_departments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    ManagerUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oa_user_departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "pay_channels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ChannelName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProviderType = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ConfigJson = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    NotifyUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Remark = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Sort = table.Column<int>(type: "integer", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pay_channels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "pay_notify_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExpireTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaidTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ChannelOrderNo = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ChannelUserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    QrCodeUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    AppPayOrderString = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    RefundedAmount = table.Column<long>(type: "bigint", nullable: false),
                    ErrorCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pay_orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sys_configs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Label = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Group = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Remark = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Sort = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_configs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sys_dict_types",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Remark = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_dict_types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sys_menus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MenuType = table.Column<int>(type: "integer", nullable: false),
                    Perms = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Path = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Component = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Icon = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Sort = table.Column<int>(type: "integer", nullable: false),
                    IsVisible = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsExternal = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsCache = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_menus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sys_menus_sys_menus_ParentId",
                        column: x => x.ParentId,
                        principalTable: "sys_menus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sys_roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Sort = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sys_scheduled_jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    JobType = table.Column<int>(type: "integer", nullable: false),
                    CronExpression = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    TimeZoneId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    HttpMethod = table.Column<int>(type: "integer", nullable: false),
                    HttpUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    HttpHeadersJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    HttpBody = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    TimeoutSeconds = table.Column<int>(type: "integer", nullable: false),
                    LastRunTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastRunStatus = table.Column<int>(type: "integer", nullable: true),
                    LastRunMessage = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_scheduled_jobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sys_users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    NickName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Email = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Avatar = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "pay_refunds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RefundNo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PaymentOrderId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "sys_dict_data",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DictTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Value = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Sort = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CssClass = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ListClass = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Remark = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "sys_role_menus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_role_menus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sys_role_menus_sys_menus_MenuId",
                        column: x => x.MenuId,
                        principalTable: "sys_menus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_sys_role_menus_sys_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "sys_roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sys_scheduled_job_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DurationMs = table.Column<int>(type: "integer", nullable: true),
                    HttpStatusCode = table.Column<int>(type: "integer", nullable: true),
                    ResponseBody = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    IsManualTrigger = table.Column<bool>(type: "boolean", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_scheduled_job_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sys_scheduled_job_logs_sys_scheduled_jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "sys_scheduled_jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sys_user_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Title = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SubTitle = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Avatar = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Content = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    MessageType = table.Column<int>(type: "integer", nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_user_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sys_user_messages_sys_users_UserId",
                        column: x => x.UserId,
                        principalTable: "sys_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sys_user_refresh_tokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReplacedByTokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_user_refresh_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sys_user_refresh_tokens_sys_users_UserId",
                        column: x => x.UserId,
                        principalTable: "sys_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sys_user_roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_user_roles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sys_user_roles_sys_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "sys_roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_sys_user_roles_sys_users_UserId",
                        column: x => x.UserId,
                        principalTable: "sys_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_biz_work_tickets_Status_CreationTime",
                table: "biz_work_tickets",
                columns: new[] { "Status", "CreationTime" });

            migrationBuilder.CreateIndex(
                name: "IX_biz_work_tickets_TicketNo",
                table: "biz_work_tickets",
                column: "TicketNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_export_tasks_CreationTime",
                table: "export_tasks",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_export_tasks_UserId_Status",
                table: "export_tasks",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_local_message_outbox_CapMessageId",
                table: "local_message_outbox",
                column: "CapMessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_oa_approver_configs_NodeId",
                table: "oa_approver_configs",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_oa_cc_configs_NodeId",
                table: "oa_cc_configs",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_oa_cc_records_UserId_InstanceId",
                table: "oa_cc_records",
                columns: new[] { "UserId", "InstanceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_oa_comments_InstanceId",
                table: "oa_comments",
                column: "InstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_oa_condition_groups_NodeId",
                table: "oa_condition_groups",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_oa_conditions_GroupId",
                table: "oa_conditions",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_oa_departments_Code",
                table: "oa_departments",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_oa_departments_LeaderUserId",
                table: "oa_departments",
                column: "LeaderUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_oa_form_fields_DefId",
                table: "oa_form_fields",
                column: "DefId");

            migrationBuilder.CreateIndex(
                name: "IX_oa_initiators_DefId",
                table: "oa_initiators",
                column: "DefId");

            migrationBuilder.CreateIndex(
                name: "IX_oa_instances_DefId",
                table: "oa_instances",
                column: "DefId");

            migrationBuilder.CreateIndex(
                name: "IX_oa_instances_Initiator_Status",
                table: "oa_instances",
                columns: new[] { "Initiator", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_oa_instances_InstanceNo",
                table: "oa_instances",
                column: "InstanceNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_oa_nodes_DefId",
                table: "oa_nodes",
                column: "DefId");

            migrationBuilder.CreateIndex(
                name: "IX_oa_operation_logs_InstanceId",
                table: "oa_operation_logs",
                column: "InstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_oa_process_definitions_BelongKey_Version",
                table: "oa_process_definitions",
                columns: new[] { "BelongKey", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_oa_tasks_InstanceId",
                table: "oa_tasks",
                column: "InstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_oa_tasks_UserId_Status",
                table: "oa_tasks",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_oa_transact_configs_NodeId",
                table: "oa_transact_configs",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_oa_user_departments_UserId",
                table: "oa_user_departments",
                column: "UserId",
                unique: true,
                filter: "\"IsPrimary\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_oa_user_departments_UserId_DepartmentId",
                table: "oa_user_departments",
                columns: new[] { "UserId", "DepartmentId" },
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_sys_configs_Key",
                table: "sys_configs",
                column: "Key",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_sys_menus_ParentId",
                table: "sys_menus",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_sys_menus_Perms",
                table: "sys_menus",
                column: "Perms",
                unique: true,
                filter: "\"Perms\" IS NOT NULL AND \"Perms\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_sys_role_menus_MenuId",
                table: "sys_role_menus",
                column: "MenuId");

            migrationBuilder.CreateIndex(
                name: "IX_sys_role_menus_RoleId_MenuId",
                table: "sys_role_menus",
                columns: new[] { "RoleId", "MenuId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sys_roles_Code",
                table: "sys_roles",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sys_scheduled_job_logs_JobId_StartedAt",
                table: "sys_scheduled_job_logs",
                columns: new[] { "JobId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_sys_scheduled_jobs_JobCode",
                table: "sys_scheduled_jobs",
                column: "JobCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sys_user_messages_UserId_IsRead",
                table: "sys_user_messages",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_sys_user_messages_UserId_Type",
                table: "sys_user_messages",
                columns: new[] { "UserId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_sys_user_refresh_tokens_TokenHash",
                table: "sys_user_refresh_tokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sys_user_refresh_tokens_UserId_RevokedAt",
                table: "sys_user_refresh_tokens",
                columns: new[] { "UserId", "RevokedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_sys_user_roles_RoleId",
                table: "sys_user_roles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_sys_user_roles_UserId_RoleId",
                table: "sys_user_roles",
                columns: new[] { "UserId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sys_users_UserName",
                table: "sys_users",
                column: "UserName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "biz_work_tickets");

            migrationBuilder.DropTable(
                name: "export_tasks");

            migrationBuilder.DropTable(
                name: "local_message_outbox");

            migrationBuilder.DropTable(
                name: "oa_approver_configs");

            migrationBuilder.DropTable(
                name: "oa_cc_configs");

            migrationBuilder.DropTable(
                name: "oa_cc_records");

            migrationBuilder.DropTable(
                name: "oa_comments");

            migrationBuilder.DropTable(
                name: "oa_condition_groups");

            migrationBuilder.DropTable(
                name: "oa_conditions");

            migrationBuilder.DropTable(
                name: "oa_departments");

            migrationBuilder.DropTable(
                name: "oa_form_fields");

            migrationBuilder.DropTable(
                name: "oa_initiators");

            migrationBuilder.DropTable(
                name: "oa_instances");

            migrationBuilder.DropTable(
                name: "oa_nodes");

            migrationBuilder.DropTable(
                name: "oa_operation_logs");

            migrationBuilder.DropTable(
                name: "oa_process_definitions");

            migrationBuilder.DropTable(
                name: "oa_process_groups");

            migrationBuilder.DropTable(
                name: "oa_tasks");

            migrationBuilder.DropTable(
                name: "oa_transact_configs");

            migrationBuilder.DropTable(
                name: "oa_user_departments");

            migrationBuilder.DropTable(
                name: "pay_channels");

            migrationBuilder.DropTable(
                name: "pay_notify_logs");

            migrationBuilder.DropTable(
                name: "pay_refunds");

            migrationBuilder.DropTable(
                name: "sys_configs");

            migrationBuilder.DropTable(
                name: "sys_dict_data");

            migrationBuilder.DropTable(
                name: "sys_role_menus");

            migrationBuilder.DropTable(
                name: "sys_scheduled_job_logs");

            migrationBuilder.DropTable(
                name: "sys_user_messages");

            migrationBuilder.DropTable(
                name: "sys_user_refresh_tokens");

            migrationBuilder.DropTable(
                name: "sys_user_roles");

            migrationBuilder.DropTable(
                name: "pay_orders");

            migrationBuilder.DropTable(
                name: "sys_dict_types");

            migrationBuilder.DropTable(
                name: "sys_menus");

            migrationBuilder.DropTable(
                name: "sys_scheduled_jobs");

            migrationBuilder.DropTable(
                name: "sys_roles");

            migrationBuilder.DropTable(
                name: "sys_users");
        }
    }
}
