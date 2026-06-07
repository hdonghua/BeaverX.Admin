using BeaverX.Admin.Application.Contracts.Dict.Dtos;
using BeaverX.Admin.Domain.Dict;

namespace BeaverX.Admin.Application.Dict;

internal static class DictMapper
{
    public static DictTypeDto ToDictTypeDto(DictType entity) => new()
    {
        Id = entity.Id,
        Code = entity.Code,
        Name = entity.Name,
        Remark = entity.Remark,
        IsEnabled = entity.IsEnabled,
        CreationTime = entity.CreationTime
    };

    public static DictDataDto ToDictDataDto(DictData entity) => new()
    {
        Id = entity.Id,
        DictTypeId = entity.DictTypeId,
        DictTypeCode = entity.DictType?.Code ?? string.Empty,
        Label = entity.Label,
        Value = entity.Value,
        Sort = entity.Sort,
        IsEnabled = entity.IsEnabled,
        CssClass = entity.CssClass,
        ListClass = entity.ListClass,
        Remark = entity.Remark,
        CreationTime = entity.CreationTime
    };
}
