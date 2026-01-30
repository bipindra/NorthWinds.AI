using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Northwind.Portal.AI.Services;
using System.Security.Claims;

namespace Northwind.Portal.Web.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize(Policy = "CustomerArea")]
public class ChatController : Controller
{
    private readonly IChatProcessor _chatProcessor;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IChatProcessor chatProcessor, ILogger<ChatController> logger)
    {
        _chatProcessor = chatProcessor;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendMessage([FromBody] ChatMessageRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.Message))
        {
            return Json(new { success = false, message = "Message cannot be empty" });
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
            var response = await _chatProcessor.ProcessMessageAsync(request.Message, userId, cancellationToken);

            return Json(new
            {
                success = response.IsSuccess,
                message = response.Message,
                timestamp = response.Timestamp,
                actions = response.Actions?.Select(a => new { type = a.Type, message = a.Message, data = a.Data })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending chat message");
            return Json(new { success = false, message = "An error occurred while processing your message." });
        }
    }
}

public class ChatMessageRequest
{
    public string Message { get; set; } = string.Empty;
}
