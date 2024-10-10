using System.CommandLine;
using Nabster.Cli.Binders;
using Spectre.Console;

namespace Nabster.Cli.Commands.Reports;

public static class Performance
{
    public static void AddPerformance(this Command parentCommand)
    {
        var command = new Command("performance", "Generate a performance report for a budget.");

        var budgetNameOption = new Option<string>(
            aliases: ["--budget-name", "-b"],
            description: "The name of the budget to generate the report for.");

        var outputFormatOption = new Option<string>(
            aliases: ["--output-format", "-o"],
            description: "The output file type, xlsx or html (default).");

        command.AddOption(budgetNameOption);
        command.AddOption(outputFormatOption);

        command.SetHandler(async (budgetName, outputFormat, configFile, ynabClient) =>
        {
            await AnsiConsole.Status().StartAsync("Generating", async ctx =>
                {
                    var report = await Domain.Reports.Performance.Generate(budgetName, ynabClient);

                    byte[] fileBytes;
                    string fileExtension;
                    switch (outputFormat)
                    {
                        case "xlsx":
                            fileBytes = Domain.Exports.PerformanceToExcel.Create(report);
                            fileExtension = "xlsx";
                            break;
                        default:
                            fileBytes = Domain.Exports.PerformanceToHtml.Create(report);
                            fileExtension = "html";
                            break;
                    }

                    var fileName = $"{budgetName} Performance {DateTime.Now:yyyyMMdd}.{fileExtension}";
                    var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
                    using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                    await stream.WriteAsync(fileBytes);
                });
        }, budgetNameOption, outputFormatOption, ConfigFileOption.Value, new YnabClientBinder());

        parentCommand.AddCommand(command);
    }
}