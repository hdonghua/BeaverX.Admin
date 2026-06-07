using BeaverX.Admin.Application.Contracts.Messages;
using BeaverX.Admin.Application.Contracts.Messages.Dtos;
using BeaverX.WebMvc.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeaverX.Admin.Http.Api.Controllers;

public class MessageController : BeaverXController
{
    private readonly IMessageAppService _messageAppService;

    public MessageController(IMessageAppService messageAppService)
    {
        _messageAppService = messageAppService;
    }

    [Authorize]
    [HttpGet("list")]
    public Task<List<MessageDto>> GetListAsync(CancellationToken cancellationToken)
        => _messageAppService.GetListAsync(cancellationToken);

    [Authorize]
    [HttpGet("unread-count")]
    public Task<int> GetUnreadCountAsync(CancellationToken cancellationToken)
        => _messageAppService.GetUnreadCountAsync(cancellationToken);

    [Authorize]
    [HttpPut("read")]
    public Task MarkReadAsync([FromBody] MarkMessagesReadDto input, CancellationToken cancellationToken)
        => _messageAppService.MarkReadAsync(input, cancellationToken);

    [Authorize]
    [HttpPut("read-all")]
    public Task MarkAllReadAsync([FromBody] MarkAllReadDto input, CancellationToken cancellationToken)
        => _messageAppService.MarkAllReadAsync(input, cancellationToken);
}
