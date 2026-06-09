using BeaverX.Admin.Application.Contracts.Payment;
using BeaverX.Admin.Application.Contracts.Payment.Dtos;
using BeaverX.Admin.Application.Contracts.Rbac;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Application.Rbac;
using BeaverX.Admin.Domain.Payment;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BeaverX.Admin.Application.Payment;

public class PaymentChannelAppService : IPaymentChannelAppService, IScopedDependency
{
  private readonly IRepository<PaymentChannel> _channelRepository;

  public PaymentChannelAppService(IRepository<PaymentChannel> channelRepository)
  {
    _channelRepository = channelRepository;
  }

  public async Task<PagedResultDto<PaymentChannelDto>> GetListAsync(
    PaymentChannelQueryDto input,
    CancellationToken cancellationToken = default)
  {
    var query = _channelRepository.GetQueryable();

    if (!string.IsNullOrWhiteSpace(input.Keyword))
    {
      var keyword = input.Keyword.Trim();
      query = query.Where(x =>
        x.ChannelCode.Contains(keyword) ||
        x.ChannelName.Contains(keyword));
    }

    if (input.IsEnabled.HasValue)
    {
      query = query.Where(x => x.IsEnabled == input.IsEnabled.Value);
    }

    var total = await query.LongCountAsync(cancellationToken);
    var (skip, take) = RbacQueryHelper.GetPaging(input.Page, input.PageSize);
    var items = await query
      .OrderBy(x => x.Sort)
      .ThenByDescending(x => x.CreationTime)
      .Skip(skip)
      .Take(take)
      .ToListAsync(cancellationToken);

    return new PagedResultDto<PaymentChannelDto>
    {
      Total = total,
      Items = items.Select(PaymentMapper.ToChannelDto).ToList(),
    };
  }

  public async Task<List<PaymentChannelDto>> GetEnabledListAsync(
    CancellationToken cancellationToken = default)
  {
    var items = await _channelRepository.GetQueryable()
      .Where(x => x.IsEnabled)
      .OrderBy(x => x.Sort)
      .ToListAsync(cancellationToken);

    return items.Select(PaymentMapper.ToChannelDto).ToList();
  }

  public async Task<PaymentChannelDto> GetAsync(long id, CancellationToken cancellationToken = default)
  {
    var entity = await FindAsync(id, cancellationToken);
    return PaymentMapper.ToChannelDto(entity);
  }

  public async Task<PaymentChannelDto> CreateAsync(
    CreatePaymentChannelDto input,
    CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(input.ChannelCode) ||
        string.IsNullOrWhiteSpace(input.ChannelName))
    {
      throw new RbacException("渠道编码和名称不能为空");
    }

    var code = input.ChannelCode.Trim();
    if (await _channelRepository.AnyAsync(x => x.ChannelCode == code, cancellationToken))
    {
      throw new RbacException("渠道编码已存在");
    }

    var entity = new PaymentChannel
    {
      ChannelCode = code,
      ChannelName = input.ChannelName.Trim(),
      ProviderType = input.ProviderType,
      IsEnabled = input.IsEnabled,
      ConfigJson = string.IsNullOrWhiteSpace(input.ConfigJson) ? "{}" : input.ConfigJson.Trim(),
      NotifyUrl = NormalizeOptional(input.NotifyUrl),
      Remark = NormalizeOptional(input.Remark),
      Sort = input.Sort,
    };

    await _channelRepository.InsertAsync(entity, cancellationToken: cancellationToken);
    return PaymentMapper.ToChannelDto(entity);
  }

  public async Task<PaymentChannelDto> UpdateAsync(
    long id,
    UpdatePaymentChannelDto input,
    CancellationToken cancellationToken = default)
  {
    var entity = await FindAsync(id, cancellationToken);

    if (input.ChannelName != null)
    {
      entity.ChannelName = input.ChannelName.Trim();
    }

    if (input.IsEnabled.HasValue)
    {
      entity.IsEnabled = input.IsEnabled.Value;
    }

    if (input.ConfigJson != null)
    {
      entity.ConfigJson = input.ConfigJson.Trim();
    }

    if (input.NotifyUrl != null)
    {
      entity.NotifyUrl = NormalizeOptional(input.NotifyUrl);
    }

    if (input.Remark != null)
    {
      entity.Remark = NormalizeOptional(input.Remark);
    }

    if (input.Sort.HasValue)
    {
      entity.Sort = input.Sort.Value;
    }

    await _channelRepository.UpdateAsync(entity, cancellationToken: cancellationToken);
    return PaymentMapper.ToChannelDto(entity);
  }

  public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
  {
    await _channelRepository.DeleteAsync(id, cancellationToken: cancellationToken);
  }

  private async Task<PaymentChannel> FindAsync(long id, CancellationToken cancellationToken)
  {
    var entity = await _channelRepository.FindAsync(x => x.Id == id, cancellationToken);
    if (entity == null)
    {
      throw new RbacException($"支付渠道不存在: {id}");
    }

    return entity;
  }

  private static string? NormalizeOptional(string? value) =>
    string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
