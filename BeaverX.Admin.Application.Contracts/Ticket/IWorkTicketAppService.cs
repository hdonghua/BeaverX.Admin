using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Application.Contracts.Ticket.Dtos;

namespace BeaverX.Admin.Application.Contracts.Ticket;

public interface IWorkTicketAppService
{
    Task<PagedResultDto<WorkTicketDto>> GetListAsync(
        WorkTicketQueryDto input,
        CancellationToken cancellationToken = default);

    Task<WorkTicketDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<WorkTicketDto> CreateAsync(
        CreateWorkTicketDto input,
        CancellationToken cancellationToken = default);

    Task<WorkTicketDto> UpdateAsync(
        Guid id,
        UpdateWorkTicketDto input,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResultDto<WorkTicketDto>> GetProcessListAsync(
        WorkTicketQueryDto input,
        CancellationToken cancellationToken = default);

    Task<WorkTicketDto> ProcessAsync(
        Guid id,
        ProcessWorkTicketDto input,
        CancellationToken cancellationToken = default);
}
