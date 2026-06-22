namespace BeaverX.Admin.Application.Contracts.Demo;

public interface IDemoDatabaseHardResetService
{
    Task ClearBusinessDemoDataAsync(CancellationToken cancellationToken = default);

    Task ClearMenusAsync(CancellationToken cancellationToken = default);

    Task ClearDictsAsync(CancellationToken cancellationToken = default);

    Task ClearConfigsAsync(CancellationToken cancellationToken = default);

    Task ClearPaymentChannelsAsync(CancellationToken cancellationToken = default);

    Task ClearUserMessagesAsync(CancellationToken cancellationToken = default);

    Task ClearNonAdminUsersAsync(CancellationToken cancellationToken = default);
}
