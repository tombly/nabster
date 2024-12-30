using System.CommandLine;
using Nabster.Cli.Binders;
using Nabster.Cli.Options;
using Nabster.Reports.Exporters;
using Nabster.Reports.Generators;
using Spectre.Console;

namespace Nabster.Cli.Commands;

public static class PerformanceCommand
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
                    var report = await Reports.Generators.Performance.Generate(budgetName, ynabClient);

                    byte[] fileBytes;
                    string fileExtension;
                    switch (outputFormat)
                    {
                        case "xlsx":
                            fileBytes = PerformanceToExcel.Create(report);
                            fileExtension = "xlsx";
                            break;
                        default:
                            fileBytes = PerformanceToHtml.Create(report);
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