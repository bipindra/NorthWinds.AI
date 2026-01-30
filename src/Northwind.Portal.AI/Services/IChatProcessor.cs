namespace Northwind.Portal.AI.Services;

public interface IChatProcessor
{
    Task<ChatResponse> ProcessMessageAsync(string message, string? userId = null, CancellationToken cancellationToken = default);
}

public class ChatResponse
{
    public string Message { get; set; } = string.Empty;
    public bool IsSuccess { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public List<ChatAction> Actions { get; set; } = new();
}

public class ChatAction
{
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object>? Data { get; set; }
}
