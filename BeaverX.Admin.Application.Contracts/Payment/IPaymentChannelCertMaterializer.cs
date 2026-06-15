namespace BeaverX.Admin.Application.Contracts.Payment;

public interface IPaymentChannelCertMaterializer
{
    /// <summary>按 ConfigJson 中的证书 URL 首次落盘到本地，返回 SDK 可用的 ConfigJson（路径为绝对路径）</summary>
    Task<string> ResolveAlipayConfigJsonAsync(
      long channelId,
      string configJson,
      CancellationToken cancellationToken = default);
}
