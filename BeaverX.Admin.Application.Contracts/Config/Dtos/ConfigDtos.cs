namespace BeaverX.Admin.Application.Contracts.Config.Dtos;

public class ConfigDto
{
    public long Id { get; set; }
    public string Key { get; set; } = null!;
    public string Value { get; set; } = null!;
    public string Label { get; set; } = null!;
    public string? Group { get; set; }
    public string? Remark { get; set; }
    public int Sort { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreationTime { get; set; }
}

public class ConfigQueryDto
{
    public string? Keyword { get; set; }
    public string? Group { get; set; }
    public bool? IsEnabled { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class CreateConfigDto
{
    public string Key { get; set; } = null!;
    public string Value { get; set; } = null!;
    public string Label { get; set; } = null!;
    public string? Group { get; set; }
    public string? Remark { get; set; }
    public int Sort { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public class UpdateConfigDto
{
    public string? Value { get; set; }
    public string? Label { get; set; }
    public string? Group { get; set; }
    public string? Remark { get; set; }
    public int? Sort { get; set; }
    public bool? IsEnabled { get; set; }
}
