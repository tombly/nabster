using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Twilio.Security;

namespace Nabster.Functions
{
    public class IncomingSms(ILogger<IncomingSms> logger)
    {
        [Function("IncomingSms")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest request)
        {
            logger.LogInformation("Incoming SMS received");

            if (!await ValidateRequest(request))
                return new ForbidResult();

            var content = await request.ReadFormAsync();
            foreach (var key in content.Keys)
            {
                logger.LogWarning($"{key}: {content[key]}");
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
