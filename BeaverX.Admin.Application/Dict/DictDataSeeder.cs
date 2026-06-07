using BeaverX.Admin.Domain.DataSeeder;
using BeaverX.Admin.Domain.Dict;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace BeaverX.Admin.Application.Dict;

public class DictDataSeeder : IScopedDependency, IDataSeeder
{
    private readonly IRepository<DictType> _dictTypeRepository;
    private readonly IRepository<DictData> _dictDataRepository;
    private readonly ILogger<DictDataSeeder> _logger;

    public DictDataSeeder(
        IRepository<DictType> dictTypeRepository,
        IRepository<DictData> dictDataRepository,
        ILogger<DictDataSeeder> logger)
    {
        _dictTypeRepository = dictTypeRepository;
        _dictDataRepository = dictDataRepository;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _dictTypeRepository.AnyAsync(_ => true, cancellationToken))
        {
            return;
        }

        _logger.LogInformation("Seeding dictionary demo data...");

        var userSex = new DictType
        {
            Code = "sys_user_sex",
            Name = "用户性别",
            Remark = "用户性别列表",
            IsEnabled = true
        };
        await _dictTypeRepository.InsertAsync(userSex, cancellationToken: cancellationToken);

        await _dictDataRepository.InsertManyAsync([
            new DictData
            {
                DictTypeId = userSex.Id,
                Label = "男",
                Value = "1",
                Sort = 1,
                ListClass = "blue",
                IsEnabled = true
            },
            new DictData
            {
                DictTypeId = userSex.Id,
                Label = "女",
                Value = "2",
                Sort = 2,
                ListClass = "pink",
                IsEnabled = true
            },
            new DictData
            {
                DictTypeId = userSex.Id,
                Label = "未知",
                Value = "0",
                Sort = 3,
                ListClass = "gray",
                IsEnabled = true
            }
        ], cancellationToken: cancellationToken);

        var commonStatus = new DictType
        {
            Code = "sys_common_status",
            Name = "通用状态",
            Remark = "启用/禁用",
            IsEnabled = true
        };
        await _dictTypeRepository.InsertAsync(commonStatus, cancellationToken: cancellationToken);

        await _dictDataRepository.InsertManyAsync([
            new DictData
            {
                DictTypeId = commonStatus.Id,
                Label = "启用",
                Value = "1",
                Sort = 1,
                ListClass = "green",
                IsEnabled = true
            },
            new DictData
            {
                DictTypeId = commonStatus.Id,
                Label = "禁用",
                Value = "0",
                Sort = 2,
                ListClass = "red",
                IsEnabled = true
            }
        ], cancellationToken: cancellationToken);
    }
}
