using System.CommandLine.Binding;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;

namespace Nabster.Cli.Binders;

public class FunctionHttpClientBinder : BinderBase<HttpClient>
{
    private readonly HttpClient httpClient = new();

    protected override HttpClient GetBoundValue(BindingContext bindingContext)
    {
        var configFileName = bindingContext.ParseResult.GetValueForOption(ConfigFileOption.Value);
        var builder = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile(configFileName!, optional: false);
        var config = builder.Build();

        var keyVaultUrl = config["KeyVaultUrl"] ?? throw new InvalidOperationException("KeyVaultUrl not found in config file.");
        var functionAppUrl = config["FunctionAppUrl"] ?? throw new InvalidOperationException("FunctionAppUrl not found in config file.");

        var client = new SecretClient(vaultUri: new Uri(keyVaultUrl), credential: new DefaultAzureCredential());
        var secret = client.GetSecret("FunctionAppKey");

        httpClient.BaseAddress = new Uri(functionAppUrl);
        httpClient.DefaultRequestHeaders.Add("x-functions-key", secret.Value.Value);

        return httpClient;
    }
}