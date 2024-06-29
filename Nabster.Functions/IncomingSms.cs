using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Twilio.Security;

namespace Nabster.Functions
{
    public class IncomingSms(ILogger<IncomingSms> _logger, Domain.Services.MessagingService _messagingService)
    {
        [Function("IncomingSms")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest request)
        {
            _logger.LogInformation("Incoming SMS received");

            if (!await ValidateRequest(request))
                return new ForbidResult();

            var content = await request.ReadFormAsync();

            try
            {
                await _messagingService.ReplyToMessage(content["Body"].ToString(), content["From"].ToString());
            }
            catch(Exception exception)
            {
                _logger.LogError(exception, "Failed processing incoming SMS");
                _messagingService.ReplyMessage("Hm, that didn't work", content["From"].ToString());
            }

            return new OkResult();
        }

        private static async Task<bool> ValidateRequest(HttpRequest request)
        {
            var authToken = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN") ?? throw new Exception("TWILIO_AUTH_TOKEN not set");

            Dictionary<string, string>? parameters = null;
            if (request.HasFormContentType)
            {
                var form = await request.ReadFormAsync();
                parameters = form.ToDictionary(p => p.Key, p => p.Value.ToString());
            }

            var requestUrl = $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}";
            var requestValidator = new RequestValidator(authToken);
            var signature = request.Headers["X-Twilio-Signature"];
            var isValid = requestValidator.Validate(requestUrl, parameters, signature);

            return isValid;
        }
    }
}