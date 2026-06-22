using BeaverX.Admin.Application.Contracts.Demo;
using BeaverX.Admin.Domain.Shared.Demo;
using BeaverX.Admin.EntityFrameworkCore;
using BeaverX.Core.Dependency;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BeaverX.Admin.Infrastructure.Demo;

public class DemoDatabaseHardResetService : IDemoDatabaseHardResetService, IScopedDependency
{
    private readonly AdminDbContext _dbContext;
    private readonly ILogger<DemoDatabaseHardResetService> _logger;

    public DemoDatabaseHardResetService(
        AdminDbContext dbContext,
        ILogger<DemoDatabaseHardResetService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task ClearBusinessDemoDataAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Clearing demo business data...");

        await _dbContext.Database.ExecuteSqlRawAsync(
            "DELETE FROM pay_notify_logs",
            cancellationToken);
        await _dbContext.Database.ExecuteSqlRawAsync(
            "DELETE FROM pay_refunds",
            cancellationToken);
        await _dbContext.Database.ExecuteSqlRawAsync(
            "DELETE FROM pay_orders",
            cancellationToken);
        await _dbContext.Database.ExecuteSqlRawAsync(
            "DELETE FROM export_tasks",
            cancellationToken);
    }

    public async Task ClearMenusAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Clearing demo menus...");

        await _dbContext.Database.ExecuteSqlRawAsync(
            "DELETE FROM sys_role_menus",
            cancellationToken);

        while (true)
        {
            var leafIds = await _dbContext.Menus
                .IgnoreQueryFilters()
                .Where(menu => !_dbContext.Menus
                    .IgnoreQueryFilters()
                    .Any(child => child.ParentId == menu.Id))
                .Select(menu => menu.Id)
                .ToListAsync(cancellationToken);

            if (leafIds.Count == 0)
            {
                break;
            }

            await _dbContext.Menus
                .IgnoreQueryFilters()
                .Where(menu => leafIds.Contains(menu.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }
    }

    public async Task ClearDictsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Clearing demo dictionaries...");

        await _dbContext.Database.ExecuteSqlRawAsync(
            "DELETE FROM sys_dict_data",
            cancellationToken);
        await _dbContext.Database.ExecuteSqlRawAsync(
            "DELETE FROM sys_dict_types",
            cancellationToken);
    }

    public async Task ClearConfigsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Clearing demo configs...");

        await _dbContext.Database.ExecuteSqlRawAsync(
            "DELETE FROM sys_configs",
            cancellationToken);
    }

    public async Task ClearPaymentChannelsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Clearing demo payment channels...");

        await _dbContext.Database.ExecuteSqlRawAsync(
            "DELETE FROM pay_channels",
            cancellationToken);
    }

    public async Task ClearUserMessagesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Clearing demo user messages...");

        await _dbContext.Database.ExecuteSqlRawAsync(
            "DELETE FROM sys_user_messages",
            cancellationToken);
    }

    public async Task ClearNonAdminUsersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Clearing non-admin demo users...");

        await _dbContext.Users
            .IgnoreQueryFilters()
            .Where(user => user.UserName != DemoModeDefaults.AdminUserName)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
