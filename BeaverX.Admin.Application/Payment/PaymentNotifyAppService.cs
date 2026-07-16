using BeaverX.Admin.Application.Contracts.Payment;
using BeaverX.Admin.Domain.Payment;
using BeaverX.Admin.Domain.Shared.Payment;
using BeaverX.Core.Dependency;

namespace BeaverX.Admin.Application.Payment;

public class PaymentNotifyAppService : IPaymentNotifyAppService, IScopedDependency
{
    private readonly ISugarRepository<PaymentChannel> _channelRepository;
    private readonly ISugarRepository<PaymentOrder> _orderRepository;
    private readonly ISugarRepository<PaymentRefund> _refundRepository;
    private readonly ISugarRepository<PaymentNotifyLog> _notifyLogRepository;
    private readonly IPaymentProviderResolver _providerResolver;
    private readonly PaymentOrderAppService _orderAppService;
    private readonly IPaymentChannelContextBuilder _channelContextBuilder;

    public PaymentNotifyAppService(
      ISugarRepository<PaymentChannel> channelRepository,
      ISugarRepository<PaymentOrder> orderRepository,
      ISugarRepository<PaymentRefund> refundRepository,
      ISugarRepository<PaymentNotifyLog> notifyLogRepository,
      IPaymentProviderResolver providerResolver,
      PaymentOrderAppService orderAppService,
      IPaymentChannelContextBuilder channelContextBuilder)
    {
        _channelRepository = channelRepository;
        _orderRepository = orderRepository;
        _refundRepository = refundRepository;
        _notifyLogRepository = notifyLogRepository;
        _providerResolver = providerResolver;
        _orderAppService = orderAppService;
        _channelContextBuilder = channelContextBuilder;
    }

    public Task<(string Body, int StatusCode)> HandleWeChatPayNotifyAsync(
      PaymentNotifyContext context,
      CancellationToken cancellationToken = default)
      => HandlePayNotifyAsync(PaymentChannelCodes.WeChatQrcode, context, cancellationToken);

    public Task<(string Body, int StatusCode)> HandleAlipayNotifyAsync(
      PaymentNotifyContext context,
      CancellationToken cancellationToken = default)
      => HandleAlipayNotifyByOrderAsync(context, HandlePayNotifyAsync, cancellationToken);

    public Task<(string Body, int StatusCode)> HandleWeChatRefundNotifyAsync(
      PaymentNotifyContext context,
      CancellationToken cancellationToken = default)
      => HandleRefundNotifyAsync(PaymentChannelCodes.WeChatQrcode, context, cancellationToken);

    public Task<(string Body, int StatusCode)> HandleAlipayRefundNotifyAsync(
      PaymentNotifyContext context,
      CancellationToken cancellationToken = default)
      => HandleAlipayNotifyByOrderAsync(context, HandleRefundNotifyAsync, cancellationToken);

    private async Task<(string Body, int StatusCode)> HandleAlipayNotifyByOrderAsync(
      PaymentNotifyContext context,
      Func<string, PaymentNotifyContext, CancellationToken, Task<(string Body, int StatusCode)>> handler,
      CancellationToken cancellationToken)
    {
        var orderNo = ExtractAlipayOutTradeNo(context.RawBody);
        if (!string.IsNullOrWhiteSpace(orderNo))
        {
            var order = await _orderRepository.GetSugarQueryable()
              .FirstAsync(x => x.OrderNo == orderNo, cancellationToken);

            if (order != null && PaymentChannelCodes.IsAlipay(order.ChannelCode))
            {
                return await handler(order.ChannelCode, context, cancellationToken);
            }
        }

        foreach (var channelCode in new[] { PaymentChannelCodes.AlipayQrcode, PaymentChannelCodes.AlipayAppPay })
        {
            var channel = await _channelRepository.GetSugarQueryable()
              .FirstAsync(x => x.ChannelCode == channelCode, cancellationToken);

            if (channel != null)
            {
                return await handler(channelCode, context, cancellationToken);
            }
        }

        throw new InvalidOperationException("支付宝支付渠道不存在");
    }

    private static string? ExtractAlipayOutTradeNo(string rawBody)
    {
        foreach (var pair in rawBody.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2 && parts[0] == "out_trade_no")
            {
                return Uri.UnescapeDataString(parts[1].Replace('+', ' '));
            }
        }

        return null;
    }

    private async Task<(string Body, int StatusCode)> HandlePayNotifyAsync(
      string channelCode,
      PaymentNotifyContext context,
      CancellationToken cancellationToken)
    {
        var channel = await FindChannelAsync(channelCode, cancellationToken);
        var provider = _providerResolver.Resolve(channelCode);
        var providerChannel = await _channelContextBuilder.BuildAsync(
          channel.Id,
          channel.ChannelCode,
          channel.ProviderType,
          channel.ConfigJson,
          cancellationToken);
        var result = await provider.HandlePayNotifyAsync(
          providerChannel,
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
            var order = await _orderRepository.GetSugarQueryable()
              .FirstAsync(x => x.OrderNo == result.OrderNo, cancellationToken);

            if (order != null && order.TryMarkPaidFromNotify())
            {
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
        var providerChannel = await _channelContextBuilder.BuildAsync(
          channel.Id,
          channel.ChannelCode,
          channel.ProviderType,
          channel.ConfigJson,
          cancellationToken);
        var result = await provider.HandleRefundNotifyAsync(
          providerChannel,
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
            var refund = await _refundRepository.GetSugarQueryable()
              .FirstAsync(x => x.RefundNo == result.RefundNo, cancellationToken);

            if (refund != null && refund.CanApplyNotifySuccess)
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
        var channel = await _channelRepository.GetSugarQueryable()
          .FirstAsync(x => x.ChannelCode == channelCode, cancellationToken);

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
