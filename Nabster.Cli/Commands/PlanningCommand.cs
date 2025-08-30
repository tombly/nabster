using System.CommandLine;
using Nabster.Reporting.Reports.Planning;
using Spectre.Console;

namespace Nabster.Cli.Commands;

internal sealed class PlanningCommand : Command
{
    public PlanningCommand(IAnsiConsole ansiConsole, PlanningReport planningReport)
        : base("planning", "Generate a planning report for a budget.")
    {
        Options.Add(new Option<string>("--budget-name") { Description = "The name of the budget to generate the report for." });
        Options.Add(new Option<string>("--output-format") { Description = "The output file type, xlsx or html (default)." });
        Options.Add(new Option<bool>("--demo") { Description = "Generate a demo report." });

        SetAction(async parseResult =>
        {
            await ansiConsole.Status().StartAsync("Building report", async ctx =>
                {
                    var budgetName = parseResult.GetValue<string>("--budget-name");
                    var outputFormat = parseResult.GetValue<string>("--output-format") ?? "html";
                    var isDemo = parseResult.GetValue<bool>("--demo");

                    var report = await planningReport.Build(budgetName, isDemo);

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
                    var fileName = $"{budgetName} Planning {DateTime.Now:yyyyMMdd}.{fileExtension}";
                    var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
                    using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                    await stream.WriteAsync(fileBytes);
                });
        });
    }
}