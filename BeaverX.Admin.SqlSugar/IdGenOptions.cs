namespace BeaverX.Admin.EntityFrameworkCore;

public class IdGenOptions
{
    public const string SectionName = "IdGen";

    /// <summary>
    /// IdGen worker / generator id（0–1023），多实例部署时每台需不同。
    /// </summary>
    public int GeneratorId { get; set; } = 0;
}
