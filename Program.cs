﻿using System.Net.Http.Headers;

namespace Nabster;

public static class Program
{
    private static readonly HttpClient httpClient = new();

    static async Task Main(string[] args)
    {
        var reportName = args[0];
        var budgetName = args[1];
        var accessToken = args[2];
        var inputFilePath = args[3];

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var client = new Ynab.Api.Client(httpClient);

        switch(reportName)
        {
            case "spend":
                await Spend.Report.Generate(budgetName, client);
                return;
            case "historical":
                await Historical.Report.Generate(budgetName, client, inputFilePath);
                return;
        }
    }
}