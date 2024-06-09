using System.CommandLine;
using System.Text;
using System.Text.Json;
using Spectre.Console;

namespace Nabster.Cli;

public static class CategoryActivity
{
    public static void AddCategoryActivity(this RootCommand rootCommand)
    {
        var command = new Command("category-activity", "Send an SMS with a category's current activity.");

        var budgetNameOption = new Option<string>(
            aliases: ["--budget-name", "-b"],
            description: "The name of the budget to generate the report for.");

        var categoryNameOption = new Option<string>(
            aliases: ["--category-name", "-c"],
            description: "The name of the category to generate the report for.");

        var phoneNumberOption = new Option<string>(
            aliases: ["--phone-number", "-p"],
            description: "The phone number to send the SMS to");

        command.AddOption(budgetNameOption);
        command.AddOption(categoryNameOption);
        command.AddOption(phoneNumberOption);

        command.SetHandler(async (budgetName, categoryName, phoneNumber, configFile, httpClient) =>
            {
                await AnsiConsole.Status().StartAsync("Generating", async ctx =>
                    {
                        using StringContent jsonContent = new(
                                JsonSerializer.Serialize(new
                                {
                                    budget = budgetName,
                                    category = categoryName,
                                    phone = phoneNumber
                                }),
                                Encoding.UTF8,
                                "application/json");

                        using var response = await httpClient.PostAsync(
                            "report/category-activity",
                            jsonContent);

                        response.EnsureSuccessStatusCode();
                    });
            },
            budgetNameOption,
            categoryNameOption,
            phoneNumberOption,
            ConfigFileOption.Value,
            new FunctionHttpClientBinder());

        rootCommand.AddCommand(command);
    }
}