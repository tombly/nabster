using Azure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Nabster.Chat.Config;

namespace Nabster.Chat.Services;

/// <summary>
/// A DI-friendly wrapper around <see cref="AzureOpenAIChatCompletionService"/>.
/// </summary>
public class ChatCompletionService(IOptions<ChatOptions> options)
{
    private readonly AzureOpenAIChatCompletionService _service = new("gpt-4o-mini", options.Value.OpenAiUrl, new DefaultAzureCredential());

    public async Task<ChatMessageContent> GetChatMessageContentAsync(ChatHistory history, OpenAIPromptExecutionSettings executionSettings, Kernel kernel)
    {
        return await _service.GetChatMessageContentAsync(history, executionSettings, kernel);
    }
}