using System.CommandLine;
using System.CommandLine.Binding;
using System.Net.Http.Headers;
using Spectre.Console;
using Ynab.Api;

namespace Nabster;

public static class Program
{
    static async Task<int> Main(string[] args)
    {
        var accessToken = Environment.GetEnvironmentVariable("YNAB_ACCESS_TOKEN");
        if (accessToken == null)
        {
            AnsiConsole.MarkupLine("[bold red1]Error: YNAB access token not found. Set the YNAB_ACCESS_TOKEN environment variable.[/]");
            return 1;
        }

        var rootCommand = new RootCommand("nabster is a tool for generating YNAB reports.");

        var planningCommand = new Command("planning", "Generate a planning report for a budget.");
        var planningBudgetNameOption = new Option<string>(
            aliases: ["--budget-name", "-b"],
            description: "The name of the budget to generate the report for.");
        planningCommand.AddOption(planningBudgetNameOption);
        planningCommand.SetHandler(async (budgetName, client) =>
            {
                await Planning.Report.Generate(budgetName, client);
            },
            planningBudgetNameOption,
            new YnabClientBinder());
        rootCommand.AddCommand(planningCommand);

        var performanceCommand = new Command("performance", "Generate a performance report for a budget.");
        var performanceBudgetNameOption = new Option<string>(
            aliases: ["--budget-name", "-b"],
            description: "The name of the budget to generate the report for.");
        performanceCommand.AddOption(performanceBudgetNameOption);
        var performanceFilePathOption = new Option<string>(
            aliases: ["--file-path", "-f"],
            description: "The path to the input file.");
        performanceCommand.AddOption(performanceFilePathOption);
        performanceCommand.SetHandler(async (budgetName, client, filePath) =>
            {
                await Performance.Report.Generate(budgetName, filePath, client);
            },
            performanceBudgetNameOption,
            performanceFilePathOption,
            new YnabClientBinder());
        rootCommand.AddCommand(performanceCommand);

        var spendCommand = new Command("spend", "Generate a spend report for a budget category.");
        var spendBudgetNameOption = new Option<string>(
            aliases: ["--budget-name", "-b"],
            description: "The name of the budget to generate the report for.");
        spendCommand.AddOption(spendBudgetNameOption);
        var spendCategoryNameOption = new Option<string>(
            aliases: ["--category-name", "-c"],
            description: "The name of the category to generate the report for.");
        spendCommand.AddOption(spendCategoryNameOption);
        spendCommand.SetHandler(async (budgetName, categoryName, client) =>
            {
                await AnsiConsole.Status()
                    .StartAsync("Generating", async ctx =>
                    {
                        await Spend.Report.Generate(budgetName, client, categoryName, "2024-05");
                    });
            },
            spendBudgetNameOption,
            spendCategoryNameOption,
            new YnabClientBinder());
        rootCommand.AddCommand(spendCommand);

        return await rootCommand.InvokeAsync(args);
    }
}

public class YnabClientBinder : BinderBase<Client>
{
    private readonly HttpClient httpClient = new();

    protected override Client GetBoundValue(BindingContext bindingContext)
    {
        var accessToken = Environment.GetEnvironmentVariable("YNAB_ACCESS_TOKEN");
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return new Client(httpClient);
    }
}