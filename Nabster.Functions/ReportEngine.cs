using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Nabster.Functions.Extensions;

namespace Nabster.Functions;

public class ReportEngine(
    ILogger<ReportEngine> logger,
    Domain.Reports.CategoryActivity categoryActivity,
    Domain.Reports.Performance performance,
    Domain.Reports.Planning planning,
    Domain.Reports.Spend spend,
    Domain.Notifications.CategoryActivityToSms categoryActivityToSms)
{
    private readonly ILogger<ReportEngine> _logger = logger;
    private readonly Domain.Reports.CategoryActivity _categoryActivity = categoryActivity;
    private readonly Domain.Reports.Performance _performance = performance;
    private readonly Domain.Reports.Planning _planning = planning;
    private readonly Domain.Reports.Spend _spend = spend;
    private readonly Domain.Notifications.CategoryActivityToSms _categoryActivityToSms = categoryActivityToSms;

    [Function("ReportEngine")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "report/{reportName}")] HttpRequest req, string reportName, FunctionContext _)
    {
        var request = await req.AsJsonNode();
        return reportName switch
        {
            "category-activity" => await CategoryActivity(request),
            "performance" => await Performance(request),
            "planning" => await Planning(request),
            "spend" => await Spend(request),
            _ => new BadRequestResult(),
        };
    }

    private async Task<IActionResult> CategoryActivity(JsonNode request)
    {
        var budgetName = request.GetStringValue("budget");
        var categoryName = request.GetStringValue("category");
        var phoneNumber = request.GetStringValue("phone");

        _logger.LogInformation("Generating report");

        var activity = await _categoryActivity.Generate(budgetName, categoryName);
        _categoryActivityToSms.Notify(categoryName, phoneNumber, activity);

        return new OkResult();
    }

    private async Task<IActionResult> Performance(JsonNode request)
    {
        var budgetName = request.GetStringValue("budget");
        var groups = request.GetStringListValue("groups");

        var report = await _performance.Generate(budgetName, groups);
        var file = Domain.Exports.PerformanceToHtml.Create(report);

        return new FileContentResult(file, "application/html");
    }

    private async Task<IActionResult> Planning(JsonNode request)
    {
        var budgetName = request.GetStringValue("budget");

        var report = await _planning.Generate(budgetName);
        var file = Domain.Exports.PlanningToExcel.Create(report);

        return new FileContentResult(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    private async Task<IActionResult> Spend(JsonNode request)
    {
        var budgetName = request.GetStringValue("budget");
        var categoryName = request.GetStringValue("category");
        var month = request.GetStringValue("month");

        var report = await _spend.Generate(budgetName, categoryName, month);
        var file = Domain.Exports.SpendToExcel.CreateFile(report);

        return new FileContentResult(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }
}