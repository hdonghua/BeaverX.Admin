using BeaverX.Admin.Domain.Rbac;
using BeaverX.Domain.Users;
using BeaverX.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;

namespace BeaverX.Admin.EntityFrameworkCore;

public class AdminDbContext : BeaverXDbContext<AdminDbContext>
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Menu> Menus => Set<Menu>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RoleMenu> RoleMenus => Set<RoleMenu>();

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

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("sys_permissions");
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Path).HasMaxLength(256);
            entity.Property(x => x.Method).HasMaxLength(16);
            entity.HasOne(x => x.Parent)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Menu>(entity =>
        {
            entity.ToTable("sys_menus");
            entity.Property(x => x.Name).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Path).HasMaxLength(256);
            entity.Property(x => x.Component).HasMaxLength(256);
            entity.Property(x => x.Icon).HasMaxLength(64);
            entity.Property(x => x.PermissionCode).HasMaxLength(128);
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

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("sys_role_permissions");
            entity.HasIndex(x => new { x.RoleId, x.PermissionId }).IsUnique();
            entity.HasOne(x => x.Role)
                .WithMany(x => x.RolePermissions)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Permission)
                .WithMany(x => x.RolePermissions)
                .HasForeignKey(x => x.PermissionId)
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
    }
}
