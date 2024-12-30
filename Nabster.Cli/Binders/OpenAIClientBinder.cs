using System.CommandLine.Binding;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Nabster.Cli.Options;

namespace Nabster.Cli.Binders;

public class OpenAIClientBinder : BinderBase<AzureOpenAIClient>
{
    private AzureOpenAIClient? _openAiClient;

    protected override AzureOpenAIClient GetBoundValue(BindingContext bindingContext)
    {
        if (_openAiClient != null)
            return _openAiClient;

        var configFileName = bindingContext.ParseResult.GetValueForOption(ConfigFileOption.Value);
        var builder = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile(configFileName!, optional: false);
        var config = builder.Build();

        var openAiUrl = config["OpenAIUrl"] ?? throw new InvalidOperationException("OpenAIUrl not found in config file.");
        _openAiClient = new(new Uri(openAiUrl), new DefaultAzureCredential());
        return _openAiClient;
    }
}