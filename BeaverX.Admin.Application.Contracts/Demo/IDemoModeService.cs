namespace BeaverX.Admin.Application.Contracts.Demo;

public interface IDemoModeService
{
    bool IsEnabled { get; }

    void EnsureMenuWritable();

    void EnsureAdminUserOperable(string? userName);

    void EnsureSuperAdminRoleOperable(string? roleCode);
}
