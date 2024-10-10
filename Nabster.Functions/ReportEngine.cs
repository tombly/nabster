using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Nabster.Domain.Exceptions;
using Nabster.Functions.Extensions;
using Nabster.Domain.Services;

namespace Nabster.Functions;

public class ReportEngine(ILogger<ReportEngine> _logger, MessagingService _messagingService)
{
    [Function("ReportEngine")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "report/{reportName}")] HttpRequest req, string reportName, FunctionContext _)
    {
        try
        {
            _logger.LogInformation("Report request received");

            var request = await req.AsJsonNode();
            return reportName switch
            {
                "account" => await Account(request),
                "activity" => await Activity(request),
                "funded" => await Funded(request),
                _ => new BadRequestObjectResult($"Invalid report name '{reportName}'"),
            };
        }
        catch (AccountNotFoundException exception)
        {
            return new NotFoundObjectResult(exception.Message);
        }
        catch (BudgetNotFoundException exception)
        {
            return new NotFoundObjectResult(exception.Message);
        }
        catch (CategoryOrGroupNotFoundException exception)
        {
            return new NotFoundObjectResult(exception.Message);
        }
        catch (MissingArgumentException exception)
        {
            return new BadRequestObjectResult(exception.Message);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error processing report request");
            return new StatusCodeResult(500);
        }
    }

    private async Task<IActionResult> Account(JsonNode request)
    {
        var budgetName = request.GetOptionalStringValue("budget");
        var accountName = request.GetRequiredStringValue("account");
        var phoneNumbers = request.GetRequiredStringValue("phone");

        await _messagingService.ReplyAccount(budgetName, accountName, phoneNumbers);

        return new OkResult();
    }

    private async Task<IActionResult> Activity(JsonNode request)
    {
        var budgetName = request.GetOptionalStringValue("budget");
        var categoryOrGroupName = request.GetRequiredStringValue("category");
        var phoneNumbers = request.GetRequiredStringValue("phone");

        await _messagingService.ReplyActivity(budgetName, categoryOrGroupName, phoneNumbers);

        return new OkResult();
    }

    private async Task<IActionResult> Funded(JsonNode request)
    {
        var budgetName = request.GetOptionalStringValue("budget");
        var categoryOrGroupName = request.GetRequiredStringValue("category");
        var phoneNumbers = request.GetRequiredStringValue("phone");

        await _messagingService.ReplyFunded(budgetName, categoryOrGroupName, phoneNumbers);

        return new OkResult();
    }
}