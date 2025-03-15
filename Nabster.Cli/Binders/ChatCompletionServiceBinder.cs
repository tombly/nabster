using System.CommandLine.Binding;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Nabster.Cli.Options;

namespace Nabster.Cli.Binders;

public class ChatCompletionServiceBinder : BinderBase<IChatCompletionService>
{
    private IChatCompletionService? _service;

    protected override IChatCompletionService GetBoundValue(BindingContext bindingContext)
    {
        if (_service != null)
            return _service;

        var configFileName = bindingContext.ParseResult.GetValueForOption(ConfigFileOption.Value);
        var configBuilder = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile(configFileName!, optional: false);
        var config = configBuilder.Build();
        var openAiUrl = config["OpenAIUrl"] ?? throw new InvalidOperationException("OpenAIUrl not found in config file.");

        _service = new AzureOpenAIChatCompletionService("gpt-4o-mini", openAiUrl, new DefaultAzureCredential());
        return _service;
    }
}