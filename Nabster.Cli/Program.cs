using System.Text;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nabster.Chat.Config;
using Nabster.Cli.Commands;
using Nabster.Cli.Config;
using Nabster.Reporting.Config;
using Spectre.Console;
using RootCommand = Nabster.Cli.Commands.RootCommand;

namespace Nabster.Cli;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        using var app = BuildApplication();
        await app.StartAsync();

        var rootCommand = app.Services.GetRequiredService<RootCommand>();
        var parseResult = rootCommand.Parse(args);
        var exitCode = await parseResult.InvokeAsync();

        await app.StopAsync();
        return exitCode;
    }

    private static IHost BuildApplication()
    {
        var builder = Host.CreateEmptyApplicationBuilder(BuildSettings("config.json"));

        using var factory = LoggerFactory.Create(builder => builder.AddConsole());
        builder.Services.AddSingleton(factory.CreateLogger("Program"));

        builder.Services.AddSingleton(AnsiConsole.Create(new AnsiConsoleSettings()
        {
            Ansi = AnsiSupport.Detect,
            Interactive = InteractionSupport.Detect,
            ColorSystem = ColorSystemSupport.Detect
        }));

        // Inject options.
        builder.Services.Configure<ChatOptions>(builder.Configuration.GetSection(ChatOptions.Section));
        builder.Services.Configure<ReportOptions>(builder.Configuration.GetSection(ReportOptions.Section));
        builder.Services.Configure<FunctionOptions>(builder.Configuration.GetSection(FunctionOptions.Section));

        // Inject feature services.
        builder.Services.AddHttpClient();
        builder.Services.AddNabsterChat();
        builder.Services.AddNabsterReports();

        // Inject command handlers.
        builder.Services.AddTransient<RootCommand>();
        builder.Services.AddTransient<ChatCommand>();
        builder.Services.AddTransient<FuncCommand>();
        builder.Services.AddTransient<DirectCommand>();
        builder.Services.AddTransient<ReportCommand>();
        builder.Services.AddTransient<HistoricalCommand>();
        builder.Services.AddTransient<PlanningCommand>();
        builder.Services.AddTransient<SpendCommand>();

        return builder.Build();
    }

    private static HostApplicationBuilderSettings BuildSettings(string configFileName)
    {
        var settings = new HostApplicationBuilderSettings { Configuration = new ConfigurationManager() };

        // Read configuration from JSON file.
        var configBuilder = new ConfigurationBuilder()
                                    .SetBasePath(Directory.GetCurrentDirectory())
                                    .AddJsonFile(configFileName!, optional: false);
        var config = configBuilder.Build();
        settings.Configuration.AddConfiguration(config);

        // Pull secrets from Azure Key Vault.
        var keyVaultUrl = config["Urls:KeyVaultUrl"] ?? throw new InvalidOperationException("KeyVaultUrl not found.");
        var secretClient = new SecretClient(vaultUri: new Uri(keyVaultUrl), credential: new DefaultAzureCredential());

        // Add a config section for the Chat feature.
        settings.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Chat:TwilioAccountSid"] = secretClient.GetSecret("TwilioAccountSid").Value.Value,
            ["Chat:TwilioAuthToken"] = secretClient.GetSecret("TwilioAuthToken").Value.Value,
            ["Chat:TwilioPhoneNumber"] = secretClient.GetSecret("TwilioPhoneNumber").Value.Value,
            ["Chat:OpenAiUrl"] = config["Urls:OpenAiUrl"],
            ["Chat:YnabAccessToken"] = secretClient.GetSecret("YnabAccessToken").Value.Value
        });

        // Add a config section for the Reports feature.
        settings.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Reports:YnabAccessToken"] = secretClient.GetSecret("YnabAccessToken").Value.Value
        });

        // Add a config section for the Function command.
        settings.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Function:Url"] = config["Urls:FunctionAppUrl"],
            ["Function:Key"] = secretClient.GetSecret("FunctionAppKey").Value.Value
        });

        return settings;
    }
}