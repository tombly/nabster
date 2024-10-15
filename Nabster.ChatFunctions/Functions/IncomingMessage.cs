using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Nabster.ChatFunctions.Extensions;
using Nabster.Domain.Services;

namespace Nabster.ChatFunctions.Functions;

public class IncomingMessage(ILogger<IncomingMessage> _logger, MessagingService _messagingService)
{
    [Function("IncomingMessage")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest request)
    {
        _logger.LogInformation("IncomingMessage triggered");

        var json = await request.AsJsonNode();
        var body = json.GetRequiredStringValue("body");
        var from = json.GetRequiredStringValue("from");

        await _messagingService.ReplyToMessage(body, from);

        return new NoContentResult();
    }
}