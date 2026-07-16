using BeaverX.Admin.Application.Contracts.Payment;
using BeaverX.Admin.Application.Contracts.Payment.Dtos;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Application.Rbac;
using BeaverX.Admin.Domain.Payment;
using BeaverX.Admin.Domain.Shared.Payment;
using BeaverX.Core.Dependency;
using Microsoft.Extensions.Options;

namespace BeaverX.Admin.Application.Payment;

public class PaymentOrderAppService : IPaymentOrderAppService, IScopedDependency
{
    private readonly ISugarRepository<PaymentOrder> _orderRepository;
    private readonly ISugarRepository<PaymentChannel> _channelRepository;
    private readonly ISugarRepository<PaymentRefund> _refundRepository;
    private readonly IPaymentProviderResolver _providerResolver;
    private readonly PaymentNotifyUrlBuilder _notifyUrlBuilder;
    private readonly PaymentOptions _paymentOptions;
    private readonly IPaymentChannelContextBuilder _channelContextBuilder;

    public PaymentOrderAppService(
      ISugarRepository<PaymentOrder> orderRepository,
      ISugarRepository<PaymentChannel> channelRepository,
      ISugarRepository<PaymentRefund> refundRepository,
      IPaymentProviderResolver providerResolver,
      PaymentNotifyUrlBuilder notifyUrlBuilder,
      IOptions<PaymentOptions> paymentOptions,
      IPaymentChannelContextBuilder channelContextBuilder)
    {
        _orderRepository = orderRepository;
        _channelRepository = channelRepository;
        _refundRepository = refundRepository;
        _providerResolver = providerResolver;
        _notifyUrlBuilder = notifyUrlBuilder;
        _paymentOptions = paymentOptions.Value;
        _channelContextBuilder = channelContextBuilder;
    }

    public async Task<PagedResultDto<PaymentOrderDto>> GetListAsync(
      PaymentOrderQueryDto input,
      CancellationToken cancellationToken = default)
    {
        var query = _orderRepository.GetSugarQueryable();

        if (!string.IsNullOrWhiteSpace(input.OrderNo))
        {
            var orderNo = input.OrderNo.Trim();
            query = query.Where(x => x.OrderNo.Contains(orderNo));
        }

        if (!string.IsNullOrWhiteSpace(input.ChannelCode))
        {
            var channelCode = input.ChannelCode.Trim();
            query = query.Where(x => x.ChannelCode == channelCode);
        }

        if (input.Status.HasValue)
        {
            query = query.Where(x => x.Status == input.Status.Value);
        }

        if (input.StartTime.HasValue)
        {
            query = query.Where(x => x.CreationTime >= input.StartTime.Value);
        }

        if (input.EndTime.HasValue)
        {
            query = query.Where(x => x.CreationTime <= input.EndTime.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var (skip, take) = RbacQueryHelper.GetPaging(input.Page, input.PageSize);
        var items = await query
          .OrderByDescending(x => x.CreationTime)
          .Skip(skip)
          .Take(take)
          .ToListAsync(cancellationToken);

        return new PagedResultDto<PaymentOrderDto>
        {
            Total = total,
            Items = items.Select(PaymentMapper.ToOrderDto).ToList(),
        };
    }

    public async Task<PaymentOrderDto> GetAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await FindOrderAsync(id, cancellationToken);
        return PaymentMapper.ToOrderDto(entity);
    }

    public async Task<PaymentOrderDto> GetByOrderNoAsync(
      string orderNo,
      CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(orderNo))
        {
            throw new BusinessException("订单号不能为空");
        }

        var entity = await _orderRepository.GetSugarQueryable()
          .FirstAsync(x => x.OrderNo == orderNo.Trim(), cancellationToken);

        if (entity == null)
        {
            throw new BusinessException($"支付订单不存在: {orderNo}");
        }

        return PaymentMapper.ToOrderDto(entity);
    }

    public async Task<CreatePaymentOrderResultDto> CreatePayOrderAsync(
      CreatePaymentOrderDto input,
      string? clientIp,
      long? userId,
      CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.ChannelCode))
        {
            throw new BusinessException("支付渠道不能为空");
        }

        var channelCode = input.ChannelCode.Trim();
        var channel = await _channelRepository.GetSugarQueryable()
          .FirstAsync(x => x.ChannelCode == channelCode, cancellationToken);

        if (channel == null)
        {
            throw new BusinessException($"支付渠道不存在: {channelCode}");
        }

        channel.EnsureAvailableForPayment();

        var expireMinutes = input.ExpireMinutes ?? _paymentOptions.DefaultExpireMinutes;
        if (expireMinutes <= 0)
        {
            expireMinutes = _paymentOptions.DefaultExpireMinutes;
        }

        var order = PaymentOrder.CreatePending(
          PaymentNoGenerator.NewOrderNo(),
          channelCode,
          input.Subject,
          input.Amount,
          DateTime.UtcNow.AddMinutes(expireMinutes),
          input.Description,
          clientIp,
          input.Attach,
          input.BusinessType,
          input.BusinessId,
          userId);

        await _orderRepository.InsertAsync(order, cancellationToken: cancellationToken);

        var provider = _providerResolver.Resolve(channelCode);
        var notifyUrl = _notifyUrlBuilder.BuildPayNotifyUrl(channelCode, channel.NotifyUrl);
        var providerChannel = await _channelContextBuilder.BuildAsync(
          channel.Id,
          channel.ChannelCode,
          channel.ProviderType,
          channel.ConfigJson,
          cancellationToken);
        var providerOrder = PaymentMapper.ToProviderOrder(order);

        string? qrCodeUrl = null;
        string? appPayOrderString = null;
        string? channelOrderNo = null;

        if (channel.ProviderType == PaymentProviderType.AlipayApp)
        {
            var appResult = await provider.CreateAppPayAsync(
              providerChannel,
              providerOrder,
              notifyUrl,
              cancellationToken);

            if (!appResult.Success || string.IsNullOrWhiteSpace(appResult.AppPayOrderString))
            {
                order.MarkChannelPayFailed(appResult.ErrorCode, appResult.ErrorMessage ?? "未获取 App 支付参数");
                await _orderRepository.UpdateAsync(order, cancellationToken: cancellationToken);
                throw new BusinessException(order.ErrorMessage!);
            }

            appPayOrderString = appResult.AppPayOrderString;
            channelOrderNo = appResult.ChannelOrderNo;
        }
        else
        {
            var qrcodeResult = await provider.CreateQrcodePayAsync(
              providerChannel,
              providerOrder,
              notifyUrl,
              cancellationToken);

            if (!qrcodeResult.Success || string.IsNullOrWhiteSpace(qrcodeResult.QrCodeUrl))
            {
                order.MarkChannelPayFailed(qrcodeResult.ErrorCode, qrcodeResult.ErrorMessage ?? "未获取支付二维码");
                await _orderRepository.UpdateAsync(order, cancellationToken: cancellationToken);
                throw new BusinessException(order.ErrorMessage!);
            }

            qrCodeUrl = qrcodeResult.QrCodeUrl;
            channelOrderNo = qrcodeResult.ChannelOrderNo;
        }

        order.MarkPaying(qrCodeUrl, appPayOrderString, channelOrderNo);
        await _orderRepository.UpdateAsync(order, cancellationToken: cancellationToken);

        return new CreatePaymentOrderResultDto
        {
            Order = PaymentMapper.ToOrderDto(order),
            QrCodeUrl = qrCodeUrl,
            AppPayOrderString = appPayOrderString,
        };
    }

    public async Task<PaymentOrderDto> SyncOrderAsync(
      long id,
      CancellationToken cancellationToken = default)
    {
        var order = await FindOrderAsync(id, cancellationToken);
        if (order.ShouldSkipProviderSync)
        {
            return PaymentMapper.ToOrderDto(order);
        }

        var channel = await FindChannelByCodeAsync(order.ChannelCode, cancellationToken);
        var provider = _providerResolver.Resolve(order.ChannelCode);
        var providerChannel = await _channelContextBuilder.BuildAsync(
          channel.Id,
          channel.ChannelCode,
          channel.ProviderType,
          channel.ConfigJson,
          cancellationToken);
        var queryResult = await provider.QueryPayAsync(
          providerChannel,
          PaymentMapper.ToProviderOrder(order),
          cancellationToken);

        await ApplyQueryResult(order, queryResult, cancellationToken);
        return PaymentMapper.ToOrderDto(order);
    }

    public async Task<PaymentOrderDto> CloseOrderAsync(
      long id,
      CancellationToken cancellationToken = default)
    {
        var order = await FindOrderAsync(id, cancellationToken);
        order.Close();
        await _orderRepository.UpdateAsync(order, cancellationToken: cancellationToken);
        return PaymentMapper.ToOrderDto(order);
    }

    public async Task<PaymentRefundDto> RefundAsync(
      CreatePaymentRefundDto input,
      CancellationToken cancellationToken = default)
    {
        var order = await FindOrderAsync(input.PaymentOrderId, cancellationToken);
        var refundable = order.GetRefundableAmount();
        var refundAmount = input.Amount ?? refundable;

        var refund = PaymentRefund.CreatePending(
          order,
          PaymentNoGenerator.NewRefundNo(),
          refundAmount,
          input.Reason);

        await _refundRepository.InsertAsync(refund, cancellationToken: cancellationToken);

        order.BeginRefunding();
        await _orderRepository.UpdateAsync(order, cancellationToken: cancellationToken);

        var channel = await FindChannelByCodeAsync(order.ChannelCode, cancellationToken);
        var provider = _providerResolver.Resolve(order.ChannelCode);
        var notifyUrl = _notifyUrlBuilder.BuildRefundNotifyUrl(order.ChannelCode, channel.NotifyUrl);
        var providerChannel = await _channelContextBuilder.BuildAsync(
          channel.Id,
          channel.ChannelCode,
          channel.ProviderType,
          channel.ConfigJson,
          cancellationToken);
        var refundResult = await provider.RefundAsync(
          providerChannel,
          PaymentMapper.ToProviderOrder(order),
          PaymentMapper.ToProviderRefund(refund),
          notifyUrl,
          cancellationToken);

        if (!refundResult.Success)
        {
            refund.MarkFailed(refundResult.ErrorCode, refundResult.ErrorMessage ?? "退款失败");
            order.RevertRefundingAfterRefundFailed();
            await _refundRepository.UpdateAsync(refund, cancellationToken: cancellationToken);
            await _orderRepository.UpdateAsync(order, cancellationToken: cancellationToken);
            throw new BusinessException(refund.ErrorMessage!);
        }

        refund.ApplyProviderResult(
          refundResult.Success,
          refundResult.Status,
          refundResult.ChannelRefundNo,
          refundResult.RefundTime);

        if (refund.Status == PaymentRefundStatus.Success)
        {
            await ApplyRefundSuccess(order, refund, cancellationToken);
        }
        else
        {
            await _refundRepository.UpdateAsync(refund, cancellationToken: cancellationToken);
        }

        return PaymentMapper.ToRefundDto(refund);
    }

    internal async Task ApplyQueryResult(
      PaymentOrder order,
      QueryPayResult queryResult,
      CancellationToken cancellationToken)
    {
        order.ApplyProviderQuery(
          queryResult.Status,
          queryResult.PaidTime,
          queryResult.ChannelOrderNo);
        await _orderRepository.UpdateAsync(order, cancellationToken: cancellationToken);
    }

    internal async Task ApplyRefundSuccess(
      PaymentOrder order,
      PaymentRefund refund,
      CancellationToken cancellationToken)
    {
        refund.MarkSuccess(refund.ChannelRefundNo, refund.RefundTime);
        order.RecordRefundSuccess(refund.Amount);

        await _refundRepository.UpdateAsync(refund, cancellationToken: cancellationToken);
        await _orderRepository.UpdateAsync(order, cancellationToken: cancellationToken);
    }

    private async Task<PaymentOrder> FindOrderAsync(long id, CancellationToken cancellationToken)
    {
        var entity = await _orderRepository.FindAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
        {
            throw new BusinessException($"支付订单不存在: {id}");
        }

        return entity;
    }

    private async Task<PaymentChannel> FindChannelByCodeAsync(
      string channelCode,
      CancellationToken cancellationToken)
    {
        var channel = await _channelRepository.GetSugarQueryable()
          .FirstAsync(x => x.ChannelCode == channelCode, cancellationToken);

        if (channel == null)
        {
            throw new BusinessException($"支付渠道不存在: {channelCode}");
        }

        return channel;
    }
}
