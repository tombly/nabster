using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Nabster.Chat.Services;

/// <summary>
/// Provides answers to questions about a user's personal finances in YNAB.
/// </summary>
public class ChatService(ChatCompletionService _chatCompletionService, YnabService _ynabService, SmsService _smsService)
{
    public async Task<string> Reply(string message, ILogger logger)
    {
        logger.LogInformation("Processing message '{message}'", message);

        try
        {
            var builder = Kernel.CreateBuilder();
            builder.Services.AddLogging(services => services.AddConsole().SetMinimumLevel(LogLevel.Trace));
            builder.Services.AddSingleton(_chatCompletionService);
            var kernel = builder.Build();

            kernel.Plugins.AddFromObject(new YnabPlugin(_ynabService.Client));

            // Enable planning
            OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };

            var history = new ChatHistory();

            var instructions = new StringBuilder();
            instructions.AppendLine("You are an AI assistant that succinctly answers questions about the user's personal finances.");
            instructions.AppendLine("Do not include calculations, just answers.");
            instructions.AppendLine("Round values to the nearest dollar. Include thousands separator.");
            history.AddSystemMessage(instructions.ToString());

            history.AddUserMessage(message);

            var response = await _chatCompletionService.GetChatMessageContentAsync(
                history,
                executionSettings: openAIPromptExecutionSettings,
                kernel: kernel
            );

            logger.LogInformation("Replied to message '{message}' with '{response}'", message, response);
            return response.Content ?? "Hm, that didn't work";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error processing message '{message}'", message);
            return "Hm, that didn't work";
        }
    }

    public async Task ReplyViaSms(string message, string phoneNumber, ILogger logger)
    {
        var response = await Reply(message, logger);
        _smsService.Send(phoneNumber, response);
    }
}