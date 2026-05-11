using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nabster.Reporting.Config;
using Nabster.Server.Config;
using Nabster.Server.Services;

#if DEBUG

// For local development, grab the key vault URL from the local.settings.json
// file and then retrieve the secrets using the default Azure credential
// (usually you just have to be logged into the Azure CLI) and set them as
// environment variables just like what happens when running in Azure.
var keyVaultUrl = Environment.GetEnvironmentVariable("KEY_VAULT_URL") ?? throw new Exception($"KEY_VAULT_URL environment variable not set");
var client = new SecretClient(vaultUri: new Uri(keyVaultUrl), credential: new DefaultAzureCredential());
Environment.SetEnvironmentVariable("YNAB_ACCESS_TOKEN", client.GetSecret("YnabAccessToken").Value.Value);
Environment.SetEnvironmentVariable("SMTP2GO_API_KEY", client.GetSecret("Smtp2GoApiKey").Value.Value);
Environment.SetEnvironmentVariable("SMTP2GO_EMAIL_ADDRESS", client.GetSecret("Smtp2GoEmailAddress").Value.Value);

#endif

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        var ynabAccessToken = Environment.GetEnvironmentVariable("YNAB_ACCESS_TOKEN") ?? throw new Exception("YNAB_ACCESS_TOKEN not set");
        var smtp2GoApiKey = Environment.GetEnvironmentVariable("SMTP2GO_API_KEY") ?? throw new Exception("SMTP2GO_API_KEY not set");
        var smtp2GoEmailAddress = Environment.GetEnvironmentVariable("SMTP2GO_EMAIL_ADDRESS") ?? throw new Exception("SMTP2GO_EMAIL_ADDRESS not set");

        var settings = new HostApplicationBuilderSettings { Configuration = new ConfigurationManager() };
        settings.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Server:Smtp2GoApiKey"] = smtp2GoApiKey,
            ["Server:Smtp2GoEmailAddress"] = smtp2GoEmailAddress
        });
        services.Configure<ServerOptions>(settings.Configuration.GetSection(ServerOptions.Section));
        services.Configure<ReportOptions>(opts => opts.YnabAccessToken = ynabAccessToken);

        services.AddTransient<EmailService>();

        services.AddNabsterReports();
        services.AddHttpClient();
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
    })
    .Build();

host.Run();