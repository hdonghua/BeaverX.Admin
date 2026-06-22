using BeaverX.Admin.Application.Contracts.Demo;
using BeaverX.Admin.Domain.DataSeeder;
using BeaverX.Admin.Domain.Dict;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BeaverX.Admin.Application.Dict;

public class DictDataSeeder : IScopedDependency, IDataSeeder, IOverwriteDataSeeder
{
    private const string MenuTypeDictCode = "sys_menu_type";

    private readonly IRepository<DictType> _dictTypeRepository;
    private readonly IRepository<DictData> _dictDataRepository;
    private readonly IDemoDatabaseHardResetService _demoHardResetService;
    private readonly ILogger<DictDataSeeder> _logger;

    public DictDataSeeder(
      IRepository<DictType> dictTypeRepository,
      IRepository<DictData> dictDataRepository,
      IDemoDatabaseHardResetService demoHardResetService,
      ILogger<DictDataSeeder> logger)
    {
        _dictTypeRepository = dictTypeRepository;
        _dictDataRepository = dictDataRepository;
        _demoHardResetService = demoHardResetService;
        _logger = logger;
    }

    public int Order => 20;

    public async Task OverwriteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Overwriting dictionary demo data...");
        await _demoHardResetService.ClearDictsAsync(cancellationToken);
        await SeedAsync(cancellationToken);
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var menuTypeDict = await _dictTypeRepository.GetQueryable()
          .FirstOrDefaultAsync(x => x.Code == MenuTypeDictCode, cancellationToken);

        if (menuTypeDict == null)
        {
            _logger.LogInformation("Seeding dictionary type {Code}...", MenuTypeDictCode);

            menuTypeDict = new DictType
            {
                Code = MenuTypeDictCode,
                Name = "菜单类型",
                Remark = "系统菜单类型：目录、菜单、按钮",
                IsEnabled = true,
            };
            await _dictTypeRepository.InsertAsync(menuTypeDict, cancellationToken: cancellationToken);
        }

        await EnsureDictDataAsync(menuTypeDict.Id, "0", () => new DictData
        {
            DictTypeId = menuTypeDict.Id,
            Label = "目录",
            Value = "0",
            Sort = 1,
            ListClass = "arcoblue",
            IsEnabled = true,
        }, cancellationToken);

        await EnsureDictDataAsync(menuTypeDict.Id, "1", () => new DictData
        {
            DictTypeId = menuTypeDict.Id,
            Label = "菜单",
            Value = "1",
            Sort = 2,
            ListClass = "green",
            IsEnabled = true,
        }, cancellationToken);

        await EnsureDictDataAsync(menuTypeDict.Id, "2", () => new DictData
        {
            DictTypeId = menuTypeDict.Id,
            Label = "按钮",
            Value = "2",
            Sort = 3,
            ListClass = "orange",
            IsEnabled = true,
        }, cancellationToken);
    }

    private async Task EnsureDictDataAsync(
      long dictTypeId,
      string value,
      Func<DictData> factory,
      CancellationToken cancellationToken)
    {
        if (await _dictDataRepository.AnyAsync(
            x => x.DictTypeId == dictTypeId && x.Value == value,
            cancellationToken))
        {
            return;
        }

        _logger.LogInformation("Seeding dictionary data {Value}...", value);
        await _dictDataRepository.InsertAsync(factory(), cancellationToken: cancellationToken);
    }
}
