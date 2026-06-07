namespace BeaverX.Admin.Application.Contracts.Dict.Dtos;

public class DictTypeDto
{
    public long Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Remark { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreationTime { get; set; }
}

public class DictTypeQueryDto
{
    public string? Keyword { get; set; }
    public bool? IsEnabled { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class CreateDictTypeDto
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Remark { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public class UpdateDictTypeDto
{
    public string? Name { get; set; }
    public string? Remark { get; set; }
    public bool? IsEnabled { get; set; }
}

public class DictDataDto
{
    public long Id { get; set; }
    public long DictTypeId { get; set; }
    public string DictTypeCode { get; set; } = null!;
    public string Label { get; set; } = null!;
    public string Value { get; set; } = null!;
    public int Sort { get; set; }
    public bool IsEnabled { get; set; }
    public string? CssClass { get; set; }
    public string? ListClass { get; set; }
    public string? Remark { get; set; }
    public DateTime CreationTime { get; set; }
}

public class DictDataQueryDto
{
    public long? DictTypeId { get; set; }
    public string? TypeCode { get; set; }
    public string? Keyword { get; set; }
    public bool? IsEnabled { get; set; }
}

public class CreateDictDataDto
{
    public long DictTypeId { get; set; }
    public string Label { get; set; } = null!;
    public string Value { get; set; } = null!;
    public int Sort { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? CssClass { get; set; }
    public string? ListClass { get; set; }
    public string? Remark { get; set; }
}

public class UpdateDictDataDto
{
    public string? Label { get; set; }
    public string? Value { get; set; }
    public int? Sort { get; set; }
    public bool? IsEnabled { get; set; }
    public string? CssClass { get; set; }
    public string? ListClass { get; set; }
    public string? Remark { get; set; }
}

public class DictOptionDto
{
    public string Label { get; set; } = null!;
    public string Value { get; set; } = null!;
    public string? ListClass { get; set; }
}
