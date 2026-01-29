using Microsoft.Extensions.Logging;
using Bipins.AI.LLM;

namespace Northwind.Portal.AI.Services;

public class ChatProcessor : IChatProcessor
{
    private readonly ILogger<ChatProcessor> _logger;
    private readonly IChatService? _chatService;

    public ChatProcessor(ILogger<ChatProcessor> logger, IChatService? chatService = null)
    {
        _logger = logger;
        _chatService = chatService;
    }

    public async Task<ChatResponse> ProcessMessageAsync(string message, string? userId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return new ChatResponse
                {
                    Message = "Please provide a message.",
                    IsSuccess = false,
                    ErrorMessage = "Message cannot be empty"
                };
            }

            _logger.LogInformation("Processing chat message from user {UserId}: {Message}", userId, message);

            // Use AI service if available (Bipins.AI integration)
            if (_chatService != null)
            {
                try
                {
                    // Use the ChatAsync method from IChatService
                    // IChatService.ChatAsync takes (string message, string? systemPrompt = null)
                    var aiResponse = await _chatService.ChatAsync(message, string.Empty);
                    return new ChatResponse
                    {
                        Message = aiResponse ?? "I received your message, but couldn't generate a response.",
                        IsSuccess = true
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message with AI service");
                    // Fall through to default response
                }
            }

            // Default response if AI service is not available or fails
            return new ChatResponse
            {
                Message = $"Thank you for your message: \"{message}\". I'm here to help with your Northwind Portal questions. How can I assist you today?",
                IsSuccess = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            return new ChatResponse
            {
                Message = "I'm sorry, I encountered an error processing your message. Please try again.",
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
