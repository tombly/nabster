using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Twilio.Security;

namespace Nabster.Functions
{
    public class IncomingSms(
                    ILogger<IncomingSms> _logger,
                    Domain.Reports.Activity _activity,
                    Domain.Notifications.ActivityToSms _categoryActivityToSms)
    {
        [Function("IncomingSms")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest request)
        {
            _logger.LogInformation("Incoming SMS received");

            if (!await ValidateRequest(request))
                return new ForbidResult();

            var content = await request.ReadFormAsync();
            foreach (var key in content.Keys)
                _logger.LogInformation($"{key}: {content[key]}");

            var message = content["Body"].ToString();
            if (message.StartsWith("activity:", StringComparison.InvariantCultureIgnoreCase))
                await ReplyCategoryActivity(message, content["From"].ToString());
            else
                _logger.LogWarning("Received unknown message {message}", message);

            return new OkResult();
        }

        private async Task ReplyCategoryActivity(string message, string phoneNumber)
        {
            var tokens = message.Split(' ');
            var commandName = tokens[0];
            var categoryName = tokens[1];
            var report = await _activity.Generate(null, categoryName);
            _categoryActivityToSms.Notify(categoryName, phoneNumber, report);
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