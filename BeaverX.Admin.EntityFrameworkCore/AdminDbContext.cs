using BeaverX.Admin.Domain.Config;
using BeaverX.Admin.Domain.Dict;
using BeaverX.Admin.Domain.Exports;
using BeaverX.Admin.Domain.Messaging;
using BeaverX.Admin.Domain.Messages;
using BeaverX.Admin.Domain.Oa;
using BeaverX.Admin.Domain.Payment;
using BeaverX.Admin.Domain.Rbac;
using BeaverX.Admin.Domain.Scheduling;
using BeaverX.Admin.Domain.Ticket;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;

namespace BeaverX.Admin.EntityFrameworkCore;

[ConnectionStringName("Default")]
public class AdminDbContext : AbpDbContext<AdminDbContext>
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Menu> Menus => Set<Menu>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RoleMenu> RoleMenus => Set<RoleMenu>();
    public DbSet<UserMessage> UserMessages => Set<UserMessage>();
    public DbSet<UserRefreshToken> UserRefreshTokens => Set<UserRefreshToken>();
    public DbSet<DictType> DictTypes => Set<DictType>();
    public DbSet<DictData> DictData => Set<DictData>();
    public DbSet<SysConfig> SysConfigs => Set<SysConfig>();
    public DbSet<ExportTask> ExportTasks => Set<ExportTask>();
    public DbSet<LocalMessageOutbox> LocalMessageOutboxes => Set<LocalMessageOutbox>();
    public DbSet<PaymentChannel> PaymentChannels => Set<PaymentChannel>();
    public DbSet<PaymentOrder> PaymentOrders => Set<PaymentOrder>();
    public DbSet<PaymentRefund> PaymentRefunds => Set<PaymentRefund>();
    public DbSet<PaymentNotifyLog> PaymentNotifyLogs => Set<PaymentNotifyLog>();
    public DbSet<ScheduledJob> ScheduledJobs => Set<ScheduledJob>();
    public DbSet<ScheduledJobLog> ScheduledJobLogs => Set<ScheduledJobLog>();
    public DbSet<WorkTicket> WorkTickets => Set<WorkTicket>();
    public DbSet<OaDepartment> OaDepartments => Set<OaDepartment>();
    public DbSet<OaUserDepartment> OaUserDepartments => Set<OaUserDepartment>();
    public DbSet<OaProcessGroup> OaProcessGroups => Set<OaProcessGroup>();
    public DbSet<OaProcessDefinition> OaProcessDefinitions => Set<OaProcessDefinition>();
    public DbSet<OaFormField> OaFormFields => Set<OaFormField>();
    public DbSet<OaInitiator> OaInitiators => Set<OaInitiator>();
    public DbSet<OaNode> OaNodes => Set<OaNode>();
    public DbSet<OaConditionGroup> OaConditionGroups => Set<OaConditionGroup>();
    public DbSet<OaCondition> OaConditions => Set<OaCondition>();
    public DbSet<OaApproverConfig> OaApproverConfigs => Set<OaApproverConfig>();
    public DbSet<OaCcConfig> OaCcConfigs => Set<OaCcConfig>();
    public DbSet<OaTransactConfig> OaTransactConfigs => Set<OaTransactConfig>();
    public DbSet<OaInstance> OaInstances => Set<OaInstance>();
    public DbSet<OaTask> OaTasks => Set<OaTask>();
    public DbSet<OaCcRecord> OaCcRecords => Set<OaCcRecord>();
    public DbSet<OaComment> OaComments => Set<OaComment>();
    public DbSet<OaOperationLog> OaOperationLogs => Set<OaOperationLog>();

    public AdminDbContext(DbContextOptions<AdminDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("sys_users");
            entity.HasIndex(x => x.UserName).IsUnique();
            entity.Property(x => x.UserName).HasMaxLength(64).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(256).IsRequired();
            entity.Property(x => x.NickName).HasMaxLength(64);
            entity.Property(x => x.Email).HasMaxLength(128);
            entity.Property(x => x.Phone).HasMaxLength(32);
            entity.Property(x => x.Avatar).HasMaxLength(512);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("sys_roles");
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(256);
        });

        modelBuilder.Entity<Menu>(entity =>
        {
            entity.ToTable("sys_menus");
            entity.HasIndex(x => x.Perms).IsUnique().HasFilter("\"Perms\" IS NOT NULL AND \"Perms\" <> ''");
            entity.Property(x => x.Name).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Perms).HasMaxLength(128);
            entity.Property(x => x.Path).HasMaxLength(256);
            entity.Property(x => x.Component).HasMaxLength(256);
            entity.Property(x => x.Icon).HasMaxLength(64);
            entity.Property(x => x.IsExternal).HasDefaultValue(false);
            entity.Property(x => x.IsCache).HasDefaultValue(true);
            entity.HasOne(x => x.Parent)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("sys_user_roles");
            entity.HasIndex(x => new { x.UserId, x.RoleId }).IsUnique();
            entity.HasOne(x => x.User)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Role)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RoleMenu>(entity =>
        {
            entity.ToTable("sys_role_menus");
            entity.HasIndex(x => new { x.RoleId, x.MenuId }).IsUnique();
            entity.HasOne(x => x.Role)
                .WithMany(x => x.RoleMenus)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Menu)
                .WithMany(x => x.RoleMenus)
                .HasForeignKey(x => x.MenuId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserRefreshToken>(entity =>
        {
            entity.ToTable("sys_user_refresh_tokens");
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => new { x.UserId, x.RevokedAt });
            entity.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            entity.Property(x => x.ReplacedByTokenHash).HasMaxLength(128);
            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DictType>(entity =>
        {
            entity.ToTable("sys_dict_types");
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Remark).HasMaxLength(256);
        });

        modelBuilder.Entity<DictData>(entity =>
        {
            entity.ToTable("sys_dict_data");
            entity.HasIndex(x => new { x.DictTypeId, x.Value }).IsUnique();
            entity.Property(x => x.Label).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Value).HasMaxLength(64).IsRequired();
            entity.Property(x => x.CssClass).HasMaxLength(64);
            entity.Property(x => x.ListClass).HasMaxLength(64);
            entity.Property(x => x.Remark).HasMaxLength(256);
            entity.HasOne(x => x.DictType)
                .WithMany(x => x.DictData)
                .HasForeignKey(x => x.DictTypeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExportTask>(entity =>
        {
            entity.ToTable("export_tasks");
            entity.HasIndex(x => new { x.UserId, x.Status });
            entity.HasIndex(x => x.CreationTime);
            entity.Property(x => x.ExportType).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Parameters).HasMaxLength(4000);
            entity.Property(x => x.FileName).HasMaxLength(256).IsRequired();
            entity.Property(x => x.ObjectKey).HasMaxLength(512);
            entity.Property(x => x.FileUrl).HasMaxLength(2048);
            entity.Property(x => x.ErrorMessage).HasMaxLength(1024);
        });

        modelBuilder.Entity<LocalMessageOutbox>(entity =>
        {
            entity.ToTable("local_message_outbox");
            entity.HasIndex(x => x.CapMessageId).IsUnique();
            entity.Property(x => x.CapMessageId).HasMaxLength(128).IsRequired();
        });

        modelBuilder.Entity<SysConfig>(entity =>
        {
            entity.ToTable("sys_configs");
            entity.HasIndex(x => x.Key).IsUnique();
            entity.Property(x => x.Key).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Value).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.Label).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Group).HasMaxLength(64);
            entity.Property(x => x.Remark).HasMaxLength(256);
        });

        modelBuilder.Entity<PaymentChannel>(entity =>
        {
            entity.ToTable("pay_channels");
            entity.HasIndex(x => x.ChannelCode).IsUnique();
            entity.Property(x => x.ChannelCode).HasMaxLength(32).IsRequired();
            entity.Property(x => x.ChannelName).HasMaxLength(64).IsRequired();
            entity.Property(x => x.ConfigJson).HasMaxLength(8000).IsRequired();
            entity.Property(x => x.NotifyUrl).HasMaxLength(512);
            entity.Property(x => x.Remark).HasMaxLength(256);
        });

        modelBuilder.Entity<PaymentOrder>(entity =>
        {
            entity.ToTable("pay_orders");
            entity.HasIndex(x => x.OrderNo).IsUnique();
            entity.HasIndex(x => new { x.Status, x.CreationTime });
            entity.HasIndex(x => x.ChannelCode);
            entity.Property(x => x.OrderNo).HasMaxLength(64).IsRequired();
            entity.Property(x => x.ChannelCode).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Subject).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(256);
            entity.Property(x => x.Currency).HasMaxLength(8).IsRequired();
            entity.Property(x => x.ClientIp).HasMaxLength(64);
            entity.Property(x => x.Attach).HasMaxLength(512);
            entity.Property(x => x.BusinessType).HasMaxLength(64);
            entity.Property(x => x.BusinessId).HasMaxLength(64);
            entity.Property(x => x.ChannelOrderNo).HasMaxLength(128);
            entity.Property(x => x.ChannelUserId).HasMaxLength(128);
            entity.Property(x => x.QrCodeUrl).HasMaxLength(1024);
            entity.Property(x => x.AppPayOrderString).HasMaxLength(4096);
            entity.Property(x => x.ErrorCode).HasMaxLength(64);
            entity.Property(x => x.ErrorMessage).HasMaxLength(512);
        });

        modelBuilder.Entity<PaymentRefund>(entity =>
        {
            entity.ToTable("pay_refunds");
            entity.HasIndex(x => x.RefundNo).IsUnique();
            entity.HasIndex(x => x.OrderNo);
            entity.Property(x => x.RefundNo).HasMaxLength(64).IsRequired();
            entity.Property(x => x.OrderNo).HasMaxLength(64).IsRequired();
            entity.Property(x => x.ChannelCode).HasMaxLength(32).IsRequired();
            entity.Property(x => x.ChannelRefundNo).HasMaxLength(128);
            entity.Property(x => x.ChannelOrderNo).HasMaxLength(128);
            entity.Property(x => x.Reason).HasMaxLength(256);
            entity.Property(x => x.ErrorCode).HasMaxLength(64);
            entity.Property(x => x.ErrorMessage).HasMaxLength(512);
            entity.HasOne(x => x.PaymentOrder)
                .WithMany()
                .HasForeignKey(x => x.PaymentOrderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PaymentNotifyLog>(entity =>
        {
            entity.ToTable("pay_notify_logs");
            entity.HasIndex(x => x.CreatedTime);
            entity.Property(x => x.NotifyType).HasMaxLength(16).IsRequired();
            entity.Property(x => x.ChannelCode).HasMaxLength(32).IsRequired();
            entity.Property(x => x.OrderNo).HasMaxLength(64);
            entity.Property(x => x.RefundNo).HasMaxLength(64);
            entity.Property(x => x.RawBody).HasMaxLength(8000).IsRequired();
            entity.Property(x => x.ProcessMessage).HasMaxLength(512);
        });

        modelBuilder.Entity<UserMessage>(entity =>
        {
            entity.ToTable("sys_user_messages");
            entity.HasIndex(x => new { x.UserId, x.IsRead });
            entity.HasIndex(x => new { x.UserId, x.Type });
            entity.Property(x => x.Type).HasMaxLength(16).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(128).IsRequired();
            entity.Property(x => x.SubTitle).HasMaxLength(128);
            entity.Property(x => x.Avatar).HasMaxLength(512);
            entity.Property(x => x.Content).HasMaxLength(1024).IsRequired();
            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ScheduledJob>(entity =>
        {
            entity.ToTable("sys_scheduled_jobs");
            entity.HasIndex(x => x.JobCode).IsUnique();
            entity.Property(x => x.JobCode).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(128).IsRequired();
            entity.Property(x => x.CronExpression).HasMaxLength(128).IsRequired();
            entity.Property(x => x.TimeZoneId).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(512);
            entity.Property(x => x.HttpUrl).HasMaxLength(2048).IsRequired();
            entity.Property(x => x.HttpHeadersJson).HasMaxLength(4000);
            entity.Property(x => x.HttpBody).HasMaxLength(8000);
            entity.Property(x => x.LastRunMessage).HasMaxLength(1024);
        });

        modelBuilder.Entity<ScheduledJobLog>(entity =>
        {
            entity.ToTable("sys_scheduled_job_logs");
            entity.HasIndex(x => new { x.JobId, x.StartedAt });
            entity.Property(x => x.ResponseBody).HasMaxLength(4000);
            entity.Property(x => x.ErrorMessage).HasMaxLength(1024);
            entity.HasOne(x => x.Job)
                .WithMany()
                .HasForeignKey(x => x.JobId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkTicket>(entity =>
        {
            entity.ToTable("biz_work_tickets");
            entity.HasIndex(x => x.TicketNo).IsUnique();
            entity.HasIndex(x => new { x.Status, x.CreationTime });
            entity.Property(x => x.TicketNo).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Content).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.ImagesJson).HasMaxLength(4000);
            entity.Property(x => x.ProcessResult).HasMaxLength(2000);
            entity.Property(x => x.ProcessResultImagesJson).HasMaxLength(4000);
        });

        ConfigureOa(modelBuilder);
    }

    private static void ConfigureOa(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OaDepartment>(entity =>
        {
            entity.ToTable("oa_departments");
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Code).HasMaxLength(64);
            entity.HasIndex(x => x.Code).IsUnique();
        });
        modelBuilder.Entity<OaUserDepartment>(entity =>
        {
            entity.ToTable("oa_user_departments");
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.HasIndex(x => new { x.UserId, x.DepartmentId }).IsUnique();
        });
        modelBuilder.Entity<OaProcessGroup>(entity =>
        {
            entity.ToTable("oa_process_groups");
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
        });
        modelBuilder.Entity<OaProcessDefinition>(entity =>
        {
            entity.ToTable("oa_process_definitions");
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.BelongKey).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Icon).HasMaxLength(50);
            entity.Property(x => x.DefJson).HasColumnType("text").IsRequired();
            entity.HasIndex(x => new { x.BelongKey, x.Version }).IsUnique();
        });
        modelBuilder.Entity<OaFormField>(entity =>
        {
            entity.ToTable("oa_form_fields");
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.FieldKey).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Label).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Placeholder).HasMaxLength(200);
            entity.HasIndex(x => x.DefId);
        });
        modelBuilder.Entity<OaInitiator>(entity =>
        {
            entity.ToTable("oa_initiators");
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.HasIndex(x => x.DefId);
        });
        modelBuilder.Entity<OaNode>(entity =>
        {
            entity.ToTable("oa_nodes");
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.NodeName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ConditionExpression).HasMaxLength(1000);
            entity.Property(x => x.FlowNodeNoAuditorAssignee).HasMaxLength(64);
            entity.HasIndex(x => x.DefId);
        });
        modelBuilder.Entity<OaConditionGroup>(entity =>
        {
            entity.ToTable("oa_condition_groups");
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.GroupKey).HasMaxLength(64);
            entity.HasIndex(x => x.NodeId);
        });
        modelBuilder.Entity<OaCondition>(entity =>
        {
            entity.ToTable("oa_conditions");
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.VarName).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => x.GroupId);
        });
        modelBuilder.Entity<OaApproverConfig>(entity =>
        {
            entity.ToTable("oa_approver_configs");
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.Rid).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => x.NodeId);
        });
        modelBuilder.Entity<OaCcConfig>(entity =>
        {
            entity.ToTable("oa_cc_configs");
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.Rid).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => x.NodeId);
        });
        modelBuilder.Entity<OaTransactConfig>(entity =>
        {
            entity.ToTable("oa_transact_configs");
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.Rid).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => x.NodeId);
        });
        modelBuilder.Entity<OaInstance>(entity =>
        {
            entity.ToTable("oa_instances");
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.InstanceNo).HasMaxLength(13).IsRequired();
            entity.Property(x => x.FormValue).HasColumnType("jsonb").IsRequired();
            entity.HasIndex(x => x.InstanceNo).IsUnique();
            entity.HasIndex(x => new { x.Initiator, x.Status });
            entity.HasIndex(x => x.DefId);
        });
        modelBuilder.Entity<OaTask>(entity =>
        {
            entity.ToTable("oa_tasks");
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.NodeName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Remark).HasMaxLength(500);
            entity.HasIndex(x => new { x.UserId, x.Status });
            entity.HasIndex(x => x.InstanceId);
        });
        modelBuilder.Entity<OaCcRecord>(entity =>
        {
            entity.ToTable("oa_cc_records");
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.HasIndex(x => new { x.UserId, x.InstanceId }).IsUnique();
        });
        modelBuilder.Entity<OaComment>(entity =>
        {
            entity.ToTable("oa_comments");
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.Content).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.Attachment).HasMaxLength(4000);
            entity.HasIndex(x => x.InstanceId);
        });
        modelBuilder.Entity<OaOperationLog>(entity =>
        {
            entity.ToTable("oa_operation_logs");
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.Remark).HasMaxLength(500);
            entity.HasIndex(x => x.InstanceId);
        });
    }
}
