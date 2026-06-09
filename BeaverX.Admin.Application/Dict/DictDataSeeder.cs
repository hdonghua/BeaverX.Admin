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
            Code = "sys_menu_type",
            Name = "菜单类型",
            Remark = "菜单类型列表",
            IsEnabled = true
        };
        await _dictTypeRepository.InsertAsync(userSex, cancellationToken: cancellationToken);

        await _dictDataRepository.InsertManyAsync([
            new DictData
            {
                DictTypeId = userSex.Id,
                Label = "目录",
                Value = "0",
                Sort = 1,
                ListClass = "",
                IsEnabled = true
            },
            new DictData
            {
                DictTypeId = userSex.Id,
                Label = "菜单",
                Value = "1",
                Sort = 2,
                ListClass = "",
                IsEnabled = true
            },
            new DictData
            {
                DictTypeId = userSex.Id,
                Label = "按钮",
                Value = "2",
                Sort = 3,
                ListClass = "",
                IsEnabled = true
            }
        ], cancellationToken: cancellationToken);
    }
}
