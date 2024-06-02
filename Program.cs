using System.Net.Http.Headers;

namespace Nabster;

public static class Program
{
    private static readonly HttpClient httpClient = new();

    static async Task Main(string[] args)
    {
        var reportName = args[0];
        var budgetName = args[1];
        var accessToken = args[2];
        var parameter = args[3];

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var client = new Ynab.Api.Client(httpClient);

        switch(reportName)
        {
            case "planning":
                await Planning.Report.Generate(budgetName, client);
                return;
            case "performance":
                await Performance.Report.Generate(budgetName, client, parameter);
                return;
            case "spend":
                await Spend.Report.Generate(budgetName, client, parameter, "2024-05");
                return;
        }
    }
}