using System.CommandLine;
using Nabster.Reporting.Reports.Spend;
using Spectre.Console;

namespace Nabster.Cli.Commands;

internal sealed class SpendCommand : Command
{
    public SpendCommand(IAnsiConsole ansiConsole, SpendReport spendReport)
        : base("spend", "Generate a spend report for a budget category.")
    {
        Options.Add(new Option<string>("--budget-name") { Description = "The name of the budget to generate the report for." });
        Options.Add(new Option<string>("--output-format") { Description = "The output file type, xlsx or html (default)." });
        Options.Add(new Option<string>("--category-name") { Description = "The name of the category to generate the report for.", Required = true });
        Options.Add(new Option<string>("--month") { Description = "The year and month to generate the report for, e.g.: 2025-08", Required = true });
        Options.Add(new Option<bool>("--demo") { Description = "Generate a demo report." });

        SetAction(async parseResult =>
        {
            await ansiConsole.Status().StartAsync("Building report", async ctx =>
                {
                    var budgetName = parseResult.GetValue<string>("--budget-name");
                    var outputFormat = parseResult.GetValue<string>("--output-format") ?? "html";
                    var categoryName = parseResult.GetValue<string>("--category-name");
                    var yearMonth = parseResult.GetValue<string>("--month");
                    var isDemo = parseResult.GetValue<bool>("--demo");

                    var report = await spendReport.Build(budgetName, categoryName!, yearMonth!, isDemo);

                    byte[] fileBytes;
                    string fileExtension;
                    switch (outputFormat)
                    {
                        case "xlsx":
                            fileBytes = report.ToExcel();
                            fileExtension = "xlsx";
                            break;
                        default:
                            fileBytes = report.ToHtml();
                            fileExtension = "html";
                            break;
                    }
                    var fileName = $"Spend - {categoryName} - {yearMonth}.{fileExtension}";
                    var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
                    using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                    await stream.WriteAsync(fileBytes);
                });
        });
    }
}