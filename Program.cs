using System.Net.Http.Headers;

namespace Nabster;

public static class Program
{
    private static readonly HttpClient httpClient = new();

    static async Task Main(string[] args)
    {
        var budgetName = args[0];
        var accessToken = args[1];

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var client = new Ynab.Api.Client(httpClient);

        await Spend.Report.Generate(budgetName, client);
    }
}