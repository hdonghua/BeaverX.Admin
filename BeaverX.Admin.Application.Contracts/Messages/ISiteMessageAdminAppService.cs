using BeaverX.Admin.Application.Contracts.Messages.Dtos;

namespace BeaverX.Admin.Application.Contracts.Messages;

public interface ISiteMessageAdminAppService
{
    Task<SendSiteMessageResultDto> SendAsync(
        SendSiteMessageDto input,
        CancellationToken cancellationToken = default);
}
