using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Azure.Security.KeyVault.Secrets;
using Azure.Identity;
using Nabster.Chat;
using Ynab.Api.Client;
using System.Net.Http.Headers;
using Azure.AI.OpenAI;

#if DEBUG

// For local development, grab the key vault URL from the local.settings.json
// file and then retrieve the secrets using the default Azure credential
// (usually you just have to be logged into the Azure CLI) and set them as
// environment variables just like what happens when running in Azure.
var keyVaultUrl = Environment.GetEnvironmentVariable("KEY_VAULT_URL") ?? throw new Exception($"KEY_VAULT_URL environment variable not set");
var client = new SecretClient(vaultUri: new Uri(keyVaultUrl), credential: new DefaultAzureCredential());
Environment.SetEnvironmentVariable("YNAB_ACCESS_TOKEN", client.GetSecret("YnabAccessToken").Value.Value);
Environment.SetEnvironmentVariable("TWILIO_ACCOUNT_SID", client.GetSecret("TwilioAccountSid").Value.Value);
Environment.SetEnvironmentVariable("TWILIO_AUTH_TOKEN", client.GetSecret("TwilioAuthToken").Value.Value);
Environment.SetEnvironmentVariable("TWILIO_PHONE_NUMBER", client.GetSecret("TwilioPhoneNumber").Value.Value);

#endif

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddNabsterChat();
        services.AddSingleton<YnabApiClient>(sp =>
        {
            var ynabAccessToken = Environment.GetEnvironmentVariable("YNAB_ACCESS_TOKEN") ?? throw new Exception("YNAB_ACCESS_TOKEN not set");
            return new YnabApiClient(new HttpClient()
            {
                DefaultRequestHeaders = {
                    Authorization = new AuthenticationHeaderValue("Bearer", ynabAccessToken)
              }
            });
        });
        services.AddSingleton<AzureOpenAIClient>(sp =>
        {
            var openAiUrl = Environment.GetEnvironmentVariable("OPENAI_URL") ?? throw new Exception("OPENAI_URL not set");
            return new(new Uri(openAiUrl), new DefaultAzureCredential());
        });
    })
    .Build();

host.Run();