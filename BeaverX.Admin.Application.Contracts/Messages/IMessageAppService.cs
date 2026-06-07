using BeaverX.Admin.Application.Contracts.Messages.Dtos;

namespace BeaverX.Admin.Application.Contracts.Messages;

public interface IMessageAppService
{
    Task<List<MessageDto>> GetListAsync(CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default);
    Task MarkReadAsync(MarkMessagesReadDto input, CancellationToken cancellationToken = default);
    Task MarkAllReadAsync(MarkAllReadDto input, CancellationToken cancellationToken = default);
}
