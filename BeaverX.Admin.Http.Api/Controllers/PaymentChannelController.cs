using BeaverX.Admin.Application.Contracts.Payment;
using BeaverX.Admin.Application.Contracts.Payment.Dtos;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Domain.Shared.Rbac;
using BeaverX.Admin.Http.Api.Authorization;
using BeaverX.WebMvc.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace BeaverX.Admin.Http.Api.Controllers;

public class PaymentChannelController : BeaverXController
{
  private readonly IPaymentChannelAppService _channelAppService;

  public PaymentChannelController(IPaymentChannelAppService channelAppService)
  {
    _channelAppService = channelAppService;
  }

  [RequirePermission(RbacPermissionCodes.Payment.Channel.List)]
  [HttpGet("list")]
  public Task<PagedResultDto<PaymentChannelDto>> GetListAsync(
    [FromQuery] PaymentChannelQueryDto input,
    CancellationToken cancellationToken)
    => _channelAppService.GetListAsync(input, cancellationToken);

  [RequirePermission(RbacPermissionCodes.Payment.Order.List)]
  [HttpGet("enabled")]
  public Task<List<PaymentChannelDto>> GetEnabledListAsync(CancellationToken cancellationToken)
    => _channelAppService.GetEnabledListAsync(cancellationToken);

  [RequirePermission(RbacPermissionCodes.Payment.Channel.List)]
  [HttpGet("{id:long}")]
  public Task<PaymentChannelDto> GetAsync(long id, CancellationToken cancellationToken)
    => _channelAppService.GetAsync(id, cancellationToken);

  [RequirePermission(RbacPermissionCodes.Payment.Channel.Create)]
  [HttpPost]
  public Task<PaymentChannelDto> CreateAsync(
    [FromBody] CreatePaymentChannelDto input,
    CancellationToken cancellationToken)
    => _channelAppService.CreateAsync(input, cancellationToken);

  [RequirePermission(RbacPermissionCodes.Payment.Channel.Update)]
  [HttpPut("{id:long}")]
  public Task<PaymentChannelDto> UpdateAsync(
    long id,
    [FromBody] UpdatePaymentChannelDto input,
    CancellationToken cancellationToken)
    => _channelAppService.UpdateAsync(id, input, cancellationToken);

  [RequirePermission(RbacPermissionCodes.Payment.Channel.Delete)]
  [HttpDelete("{id:long}")]
  public Task DeleteAsync(long id, CancellationToken cancellationToken)
    => _channelAppService.DeleteAsync(id, cancellationToken);
}
