namespace BeaverX.Admin.Domain.Shared.Demo;

public class DemoModeOptions
{
    public const string SectionName = "DemoMode";

    public bool Enabled { get; set; }

    public int ResetIntervalMinutes { get; set; } = 5;

    public string ProtectedAdminUserName { get; set; } = DemoModeDefaults.AdminUserName;
}
