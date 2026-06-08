namespace BeaverX.Admin.Application.Contracts.Realtime;

public class RealtimeMessage
{
    public string Event { get; set; } = null!;
    public object? Data { get; set; }
}
