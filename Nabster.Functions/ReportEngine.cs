using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Nabster.Domain.Exceptions;
using Nabster.Functions.Extensions;

namespace Nabster.Functions;

public class ReportEngine(
    ILogger<ReportEngine> _logger,
    Domain.Reports.Activity _activity,
    Domain.Reports.Performance _performance,
    Domain.Reports.Planning _planning,
    Domain.Reports.Spend _spend,
    Domain.Notifications.ActivityToSms _categoryActivityToSms)
{
    [Function("ReportEngine")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "report/{reportName}")] HttpRequest req, string reportName, FunctionContext _)
    {
        try
        {
            _logger.LogInformation("Report request received: {reportName}", reportName);

            var request = await req.AsJsonNode();
            return reportName switch
            {
                "activity" => await Activity(request),
                "performance" => await Performance(request),
                "planning" => await Planning(request),
                "spend" => await Spend(request),
                _ => new BadRequestObjectResult($"Invalid report name '{reportName}'"),
            };
        }
        catch (MissingArgumentException exception)
        {
            return new BadRequestObjectResult(exception.Message);
        }
        catch (CategoryOrGroupNotFoundException exception)
        {
            return new NotFoundObjectResult(exception.Message);
        }
        catch (BudgetNotFoundException exception)
        {
            return new NotFoundObjectResult(exception.Message);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error processing report request");
            return new StatusCodeResult(500);
        }
    }

    private async Task<IActionResult> Activity(JsonNode request)
    {
        var budgetName = request.GetOptionalStringValue("budget");
        var categoryOrGroupName = request.GetRequiredStringValue("category");
        var phoneNumbers = request.GetRequiredStringValue("phone");

        var report = await _activity.Generate(budgetName, categoryOrGroupName);
        _categoryActivityToSms.Notify(categoryOrGroupName, phoneNumbers, report);

        return new OkResult();
    }

    private async Task<IActionResult> Performance(JsonNode request)
    {
        var budgetName = request.GetOptionalStringValue("budget");
        var groups = request.GetRequiredStringListValue("groups");

        var report = await _performance.Generate(budgetName, groups);
        var file = Domain.Exports.PerformanceToHtml.Create(report);

        return new FileContentResult(file, "application/html");
    }

    private async Task<IActionResult> Planning(JsonNode request)
    {
        var budgetName = request.GetOptionalStringValue("budget");

        var report = await _planning.Generate(budgetName);
        var file = Domain.Exports.PlanningToExcel.Create(report);

        return new FileContentResult(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    private async Task<IActionResult> Spend(JsonNode request)
    {
        var budgetName = request.GetOptionalStringValue("budget");
        var categoryName = request.GetRequiredStringValue("category");
        var month = request.GetRequiredStringValue("month");

        var report = await _spend.Generate(budgetName, categoryName, month);
        var file = Domain.Exports.SpendToExcel.CreateFile(report);

        return new FileContentResult(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }
}