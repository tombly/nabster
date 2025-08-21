using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nabster.Chat.Config;

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
        var openAiUrl = Environment.GetEnvironmentVariable("OPENAI_URL") ?? throw new Exception("OPENAI_URL not set");
        var ynabAccessToken = Environment.GetEnvironmentVariable("YNAB_ACCESS_TOKEN") ?? throw new Exception("YNAB_ACCESS_TOKEN not set");
        var twilioPhoneNumber = Environment.GetEnvironmentVariable("TWILIO_PHONE_NUMBER") ?? throw new Exception("TWILIO_PHONE_NUMBER not set");
        var twilioAccountSid = Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID") ?? throw new Exception("TWILIO_ACCOUNT_SID not set");
        var twilioAuthToken = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN") ?? throw new Exception("TWILIO_AUTH_TOKEN not set");

        var settings = new HostApplicationBuilderSettings { Configuration = new ConfigurationManager() };
        settings.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Chat:OpenAiUrl"] = openAiUrl,
            ["Chat:YnabAccessToken"] = ynabAccessToken,
            ["Chat:TwilioAccountSid"] = twilioAccountSid,
            ["Chat:TwilioAuthToken"] = twilioAuthToken,
            ["Chat:TwilioPhoneNumber"] = twilioPhoneNumber,
        });
        services.Configure<ChatOptions>(settings.Configuration.GetSection(ChatOptions.Section));

        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddNabsterChat();
    })
    .Build();

host.Run();