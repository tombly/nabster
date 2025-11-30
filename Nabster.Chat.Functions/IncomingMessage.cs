using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Nabster.Chat.Functions.Extensions;
using Nabster.Chat.Services;

namespace Nabster.Chat.Functions;

internal class IncomingMessage(ILogger<IncomingMessage> _logger, ChatService _chatService)
{
    [Function("IncomingMessage")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest request)
    {
        _logger.LogInformation("IncomingMessage triggered");

        var json = await request.AsJsonNode();
        var message = json.GetRequiredStringValue("message");
        var phoneNumber = json.GetOptionalStringValue("from");
        var emailAddress = json.GetOptionalStringValue("fromEmailAddress");

        if (emailAddress is not null)
        {
            await _chatService.ReplyViaEmail(message, emailAddress, _logger);
            return new NoContentResult();
        }

        if (phoneNumber is not null)
        {
            await _chatService.ReplyViaSms(message, phoneNumber, _logger);
            return new NoContentResult();
        }

        var response = await _chatService.Reply(message, _logger);
        return new OkObjectResult(response);
    }
}