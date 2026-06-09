using BeaverX.Admin.Application.Contracts.Payment;
using BeaverX.Admin.Domain.Payment;
using BeaverX.Admin.Domain.Shared.Payment;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BeaverX.Admin.Application.Payment;

public class PaymentNotifyAppService : IPaymentNotifyAppService, IScopedDependency
{
  private readonly IRepository<PaymentChannel> _channelRepository;
  private readonly IRepository<PaymentOrder> _orderRepository;
  private readonly IRepository<PaymentRefund> _refundRepository;
  private readonly IRepository<PaymentNotifyLog> _notifyLogRepository;
  private readonly IPaymentProviderResolver _providerResolver;
  private readonly PaymentOrderAppService _orderAppService;

  public PaymentNotifyAppService(
    IRepository<PaymentChannel> channelRepository,
    IRepository<PaymentOrder> orderRepository,
    IRepository<PaymentRefund> refundRepository,
    IRepository<PaymentNotifyLog> notifyLogRepository,
    IPaymentProviderResolver providerResolver,
    PaymentOrderAppService orderAppService)
  {
    _channelRepository = channelRepository;
    _orderRepository = orderRepository;
    _refundRepository = refundRepository;
    _notifyLogRepository = notifyLogRepository;
    _providerResolver = providerResolver;
    _orderAppService = orderAppService;
  }

  public Task<(string Body, int StatusCode)> HandleWeChatPayNotifyAsync(
    PaymentNotifyContext context,
    CancellationToken cancellationToken = default)
    => HandlePayNotifyAsync(PaymentChannelCodes.WeChatNative, context, cancellationToken);

  public Task<(string Body, int StatusCode)> HandleAlipayNotifyAsync(
    PaymentNotifyContext context,
    CancellationToken cancellationToken = default)
    => HandlePayNotifyAsync(PaymentChannelCodes.AlipayNative, context, cancellationToken);

  public Task<(string Body, int StatusCode)> HandleSandboxPayNotifyAsync(
    PaymentNotifyContext context,
    CancellationToken cancellationToken = default)
    => HandlePayNotifyAsync(PaymentChannelCodes.SandboxNative, context, cancellationToken);

  public Task<(string Body, int StatusCode)> HandleWeChatRefundNotifyAsync(
    PaymentNotifyContext context,
    CancellationToken cancellationToken = default)
    => HandleRefundNotifyAsync(PaymentChannelCodes.WeChatNative, context, cancellationToken);

  public Task<(string Body, int StatusCode)> HandleAlipayRefundNotifyAsync(
    PaymentNotifyContext context,
    CancellationToken cancellationToken = default)
    => HandleRefundNotifyAsync(PaymentChannelCodes.AlipayNative, context, cancellationToken);

  public Task<(string Body, int StatusCode)> HandleSandboxRefundNotifyAsync(
    PaymentNotifyContext context,
    CancellationToken cancellationToken = default)
    => HandleRefundNotifyAsync(PaymentChannelCodes.SandboxNative, context, cancellationToken);

  private async Task<(string Body, int StatusCode)> HandlePayNotifyAsync(
    string channelCode,
    PaymentNotifyContext context,
    CancellationToken cancellationToken)
  {
    var channel = await FindChannelAsync(channelCode, cancellationToken);
    var provider = _providerResolver.Resolve(channelCode);
    var result = await provider.HandlePayNotifyAsync(
      PaymentMapper.ToProviderChannel(channel),
      context,
      cancellationToken);

    await SaveNotifyLogAsync(
      PaymentNotifyType.Payment,
      channelCode,
      result.OrderNo,
      null,
      context.RawBody,
      result.Success,
      result.ProcessMessage,
      cancellationToken);

    if (result.Success && !string.IsNullOrWhiteSpace(result.OrderNo))
    {
      var order = await _orderRepository.GetQueryable()
        .FirstOrDefaultAsync(x => x.OrderNo == result.OrderNo, cancellationToken);

      if (order != null &&
          order.Status is not PaymentOrderStatus.Success
            and not PaymentOrderStatus.Refunding
            and not PaymentOrderStatus.Refunded
            and not PaymentOrderStatus.PartialRefunded)
      {
        order.Status = PaymentOrderStatus.Success;
        order.PaidTime = DateTime.UtcNow;
        await _orderRepository.UpdateAsync(order, cancellationToken: cancellationToken);
      }
    }

    return (result.ResponseBody, result.Success ? 200 : 400);
  }

  private async Task<(string Body, int StatusCode)> HandleRefundNotifyAsync(
    string channelCode,
    PaymentNotifyContext context,
    CancellationToken cancellationToken)
  {
    var channel = await FindChannelAsync(channelCode, cancellationToken);
    var provider = _providerResolver.Resolve(channelCode);
    var result = await provider.HandleRefundNotifyAsync(
      PaymentMapper.ToProviderChannel(channel),
      context,
      cancellationToken);

    await SaveNotifyLogAsync(
      PaymentNotifyType.Refund,
      channelCode,
      null,
      result.RefundNo,
      context.RawBody,
      result.Success,
      result.ProcessMessage,
      cancellationToken);

    if (result.Success && !string.IsNullOrWhiteSpace(result.RefundNo))
    {
      var refund = await _refundRepository.GetQueryable()
        .FirstOrDefaultAsync(x => x.RefundNo == result.RefundNo, cancellationToken);

      if (refund != null && refund.Status != PaymentRefundStatus.Success)
      {
        var order = await _orderRepository.FindAsync(x => x.Id == refund.PaymentOrderId, cancellationToken);
        if (order != null)
        {
          await _orderAppService.ApplyRefundSuccess(order, refund, cancellationToken);
        }
      }
    }

    return (result.ResponseBody, result.Success ? 200 : 400);
  }

  private async Task<PaymentChannel> FindChannelAsync(
    string channelCode,
    CancellationToken cancellationToken)
  {
    var channel = await _channelRepository.GetQueryable()
      .FirstOrDefaultAsync(x => x.ChannelCode == channelCode, cancellationToken);

    if (channel == null)
    {
      throw new InvalidOperationException($"支付渠道不存在: {channelCode}");
    }

    return channel;
  }

  private async Task SaveNotifyLogAsync(
    string notifyType,
    string channelCode,
    string? orderNo,
    string? refundNo,
    string rawBody,
    bool success,
    string? message,
    CancellationToken cancellationToken)
  {
    var log = new PaymentNotifyLog
    {
      NotifyType = notifyType,
      ChannelCode = channelCode,
      OrderNo = orderNo,
      RefundNo = refundNo,
      RawBody = rawBody.Length > 8000 ? rawBody[..8000] : rawBody,
      ProcessSuccess = success,
      ProcessMessage = message,
      CreatedTime = DateTime.UtcNow,
    };

    await _notifyLogRepository.InsertAsync(log, cancellationToken: cancellationToken);
  }
}
