using System.CommandLine.Binding;
using System.Net.Http.Headers;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Ynab.Api.Client;

namespace Nabster.Cli.Binders;

public class YnabClientBinder : BinderBase<YnabApiClient>
{
    private readonly HttpClient httpClient = new();

    protected override YnabApiClient GetBoundValue(BindingContext bindingContext)
    {
        var configFileName = bindingContext.ParseResult.GetValueForOption(ConfigFileOption.Value);
        var builder = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile(configFileName!, optional: false);
        var config = builder.Build();

        var keyVaultUrl = config["KeyVaultUrl"] ?? throw new InvalidOperationException("KeyVaultUrl not found in config file.");

        var client = new SecretClient(vaultUri: new Uri(keyVaultUrl), credential: new DefaultAzureCredential());
        var ynabAccessToken = client.GetSecret("YnabAccessToken");

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ynabAccessToken.Value.Value);

        return new YnabApiClient(httpClient);
    }
}