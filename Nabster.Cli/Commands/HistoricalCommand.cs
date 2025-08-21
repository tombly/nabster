using System.CommandLine;
using Nabster.Reporting.Reports.Historical;
using Spectre.Console;

namespace Nabster.Cli.Commands;

internal sealed class HistoricalCommand : Command
{
    public HistoricalCommand(IAnsiConsole ansiConsole, HistoricalReport historicalReport)
        : base("historical", "Creates a report of account balances over time for a budget.")
    {
        Options.Add(new Option<string>("--budget-name") { Description = "The name of the budget to generate the report for." });
        Options.Add(new Option<string>("--output-format") { Description = "The output file type, xlsx or html (default)." });

        SetAction(async parseResult =>
        {
            await ansiConsole.Status().StartAsync("Building report", async ctx =>
                {
                    var budgetName = parseResult.GetValue<string>("--budget-name");
                    var outputFormat = parseResult.GetValue<string>("--output-format") ?? "html";

                    var report = await historicalReport.Build(budgetName);

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

                    var fileName = $"{budgetName} Historical {DateTime.Now:yyyyMMdd}.{fileExtension}";
                    var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
                    using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                    await stream.WriteAsync(fileBytes);
                });
        });
    }
}