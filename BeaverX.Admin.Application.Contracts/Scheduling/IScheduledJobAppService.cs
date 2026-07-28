using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Application.Contracts.Scheduling.Dtos;

namespace BeaverX.Admin.Application.Contracts.Scheduling;

public interface IScheduledJobAppService
{
    Task<PagedResultDto<ScheduledJobDto>> GetListAsync(
        ScheduledJobQueryDto input,
        CancellationToken cancellationToken = default);

    Task<ScheduledJobDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ScheduledJobDto> CreateAsync(
        CreateScheduledJobDto input,
        CancellationToken cancellationToken = default);

    Task<ScheduledJobDto> UpdateAsync(
        Guid id,
        UpdateScheduledJobDto input,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task TriggerAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResultDto<ScheduledJobLogDto>> GetLogsAsync(
        Guid id,
        ScheduledJobLogQueryDto input,
        CancellationToken cancellationToken = default);

    ValidateCronResultDto ValidateCron(ValidateCronDto input);
}
