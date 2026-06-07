namespace BeaverX.Admin.Application.Contracts.Messages.Dtos;

public class MessageDto
{
    public long Id { get; set; }
    public string Type { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string SubTitle { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string Content { get; set; } = null!;
    public string Time { get; set; } = null!;
    public int Status { get; set; }
    public int? MessageType { get; set; }
}

public class MarkMessagesReadDto
{
    public List<long> Ids { get; set; } = [];
}

public class MarkAllReadDto
{
    public string? Type { get; set; }
}
