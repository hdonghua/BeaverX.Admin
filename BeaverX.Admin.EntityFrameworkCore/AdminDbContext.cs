using BeaverX.Admin.Domain.Config;
using BeaverX.Admin.Domain.Dict;
using BeaverX.Admin.Domain.Exports;
using BeaverX.Admin.Domain.Messaging;
using BeaverX.Admin.Domain.Messages;
using BeaverX.Admin.Domain.Rbac;
using BeaverX.Domain.Users;
using BeaverX.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;

namespace BeaverX.Admin.EntityFrameworkCore;

public class AdminDbContext : BeaverXDbContext<AdminDbContext>
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

    public AdminDbContext(DbContextOptions<AdminDbContext> options, ICurrentUser currentUser)
        : base(options, currentUser)
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
            entity.HasIndex(x => x.IdempotencyKey).IsUnique();
            entity.HasIndex(x => new { x.MessageType, x.BusinessKey }).IsUnique();
            entity.HasIndex(x => x.CapConsumeMessageId)
                .IsUnique()
                .HasFilter("\"CapConsumeMessageId\" IS NOT NULL AND \"CapConsumeMessageId\" <> ''");
            entity.Property(x => x.MessageType).HasMaxLength(64).IsRequired();
            entity.Property(x => x.BusinessKey).HasMaxLength(128).IsRequired();
            entity.Property(x => x.IdempotencyKey).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Payload).HasMaxLength(4000);
            entity.Property(x => x.CapConsumeMessageId).HasMaxLength(128);
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
    }
}
