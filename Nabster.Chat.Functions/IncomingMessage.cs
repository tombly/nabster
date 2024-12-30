using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Nabster.Chat.Functions.Extensions;

namespace Nabster.Chat.Functions;

public class IncomingMessage(ILogger<IncomingMessage> _logger, ChatService _chatService)
{
    [Function("IncomingMessage")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest request)
    {
        _logger.LogInformation("IncomingMessage triggered");

        var json = await request.AsJsonNode();
        var message = json.GetRequiredStringValue("message");
        var phoneNumber = json.GetOptionalStringValue("from");

        if (phoneNumber is not null)
        {
            await _chatService.ReplyViaSms(message, phoneNumber, _logger);
            return new NoContentResult();
        }
        else
        {
            var response = await _chatService.Reply(message, _logger);
            return new OkObjectResult(response);
        }
    }
}