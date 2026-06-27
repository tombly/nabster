using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Nabster.Reporting.Reports.Daily;
using Nabster.Server.Extensions;
using Nabster.Server.Services;

namespace Nabster.Server;

internal class IncomingMessage(ILogger<IncomingMessage> _logger, EmailService _emailService, DailyReport _dailyReport)
{
    [Function("IncomingMessage")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest request)
    {
        _logger.LogInformation("IncomingMessage triggered");

        var json = await request.AsJsonNode();

        var isDemo = json.GetOptionalBooleanValue("isDemo") ?? false;
        var budgetName = json.GetOptionalStringValue("budgetName");
        var emailAddresses = json.GetOptionalStringArrayValue("emailAddresses");
        var categoryNames = json.GetOptionalStringArrayValue("categoryNames");

        var report = await _dailyReport.Build(budgetName, isDemo, categoryNames);
        var text = DailyReport.BuildText(report);

        if (emailAddresses is not null)
            await _emailService.Send(emailAddresses, text, DailyReport.BuildHtml(report));

        return new OkObjectResult(text);
    }
}