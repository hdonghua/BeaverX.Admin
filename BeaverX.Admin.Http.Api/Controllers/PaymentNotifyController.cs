using BeaverX.Admin.Application.Contracts.Payment;
using BeaverX.WebMvc.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeaverX.Admin.Http.Api.Controllers;

/// <summary>
/// 支付/退款异步回调（供微信、支付宝等渠道调用，无需 JWT）
/// </summary>
[AllowAnonymous]
public class PaymentNotifyController : BeaverXControllerBase
{
  private readonly IPaymentNotifyAppService _notifyAppService;

  public PaymentNotifyController(IPaymentNotifyAppService notifyAppService)
  {
    _notifyAppService = notifyAppService;
  }

  [HttpPost("wechat/pay")]
  public async Task<IActionResult> WeChatPayAsync(CancellationToken cancellationToken)
  {
    var context = await ReadNotifyContextAsync();
    var (body, statusCode) = await _notifyAppService.HandleWeChatPayNotifyAsync(context, cancellationToken);
    return Content(body, "application/json", System.Text.Encoding.UTF8);
  }

  [HttpPost("wechat/refund")]
  public async Task<IActionResult> WeChatRefundAsync(CancellationToken cancellationToken)
  {
    var context = await ReadNotifyContextAsync();
    var (body, statusCode) = await _notifyAppService.HandleWeChatRefundNotifyAsync(context, cancellationToken);
    return Content(body, "application/json", System.Text.Encoding.UTF8);
  }

  [HttpPost("alipay/pay")]
  public async Task<IActionResult> AlipayPayAsync(CancellationToken cancellationToken)
  {
    var context = await ReadNotifyContextAsync();
    var (body, statusCode) = await _notifyAppService.HandleAlipayNotifyAsync(context, cancellationToken);
    return Content(body, "text/plain", System.Text.Encoding.UTF8);
  }

  [HttpPost("alipay/refund")]
  public async Task<IActionResult> AlipayRefundAsync(CancellationToken cancellationToken)
  {
    var context = await ReadNotifyContextAsync();
    var (body, _) = await _notifyAppService.HandleAlipayRefundNotifyAsync(context, cancellationToken);
    return Content(body, "text/plain", System.Text.Encoding.UTF8);
  }

  private async Task<PaymentNotifyContext> ReadNotifyContextAsync()
  {
    using var reader = new StreamReader(Request.Body);
    var rawBody = await reader.ReadToEndAsync();
    var headers = Request.Headers
      .ToDictionary(x => x.Key, x => x.Value.ToString(), StringComparer.OrdinalIgnoreCase);

    return new PaymentNotifyContext
    {
      RawBody = rawBody,
      Headers = headers,
    };
  }
}
