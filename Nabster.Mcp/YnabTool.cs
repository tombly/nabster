using System.ComponentModel;
using System.Net.Http.Headers;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Server;
using Ynab.Api.Client;

namespace Nabster.Mcp;

[McpServerToolType]
public static class YnabTool
{
    [McpServerTool, Description("Gets a list of budgets.")]
    public static async Task<string> GetBudgets()
    {
        // Read configuration from JSON file.
        var configBuilder = new ConfigurationBuilder()
                                    .SetBasePath(Directory.GetCurrentDirectory())
                                    .AddJsonFile("config.json", optional: false);
        var config = configBuilder.Build();

        // Pull secrets from Azure Key Vault.
        var keyVaultUrl = config["Urls:KeyVaultUrl"] ?? throw new InvalidOperationException("KeyVaultUrl not found.");
        var secretClient = new SecretClient(vaultUri: new Uri(keyVaultUrl), credential: new DefaultAzureCredential());

        var accessToken = secretClient.GetSecret("YnabAccessToken").Value.Value;

        // Call YNAB API.
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var client = new YnabApiClient(httpClient);
        var budgets = await client.GetBudgetsAsync(false);
        return budgets.Data?.Budgets?.FirstOrDefault()?.Name ?? "No budgets found";
    }
}
