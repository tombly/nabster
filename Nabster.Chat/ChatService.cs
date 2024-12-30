using System.Text;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Nabster.Domain.Extensions;
using Nabster.Domain.Services;
using OpenAI.Chat;
using Ynab.Api.Client;

namespace Nabster.Chat;

public class ChatService(AzureOpenAIClient _openAIClient, YnabApiClient _ynabClient, SmsClient? _smsClient)
{
    public async Task<string> Reply(string message, ILogger logger)
    {
        try
        {
            var ynabSnapshot = await CreateYnabSnapshot();
            var response = GenerateCompletion(ynabSnapshot, message);
            logger.LogInformation("Replied to message '{message}' with '{response}'", message, response);
            return response;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error processing message '{message}'", message);
            return "Hm, that didn't work";
        }
    }

    public async Task ReplyViaSms(string message, string phoneNumber, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(_smsClient);
        var response = await Reply(message, logger);
        _smsClient.Send(phoneNumber, response);
    }

    private async Task<string> CreateYnabSnapshot(string? budgetName = null)
    {
        var budgetDetail = await _ynabClient.GetBudgetDetailAsync(budgetName);
        var accounts = (await _ynabClient.GetAccountsAsync(budgetDetail.Id.ToString(), null)!).Data.Accounts;

        var snapshot = new StringBuilder();
        foreach (var account in accounts.Where(a => !a.Deleted && !a.Closed))
            snapshot.AppendLine($"Account: Name={account.Name}, Balance=${account.Balance / 1000m}");

        var categories = await _ynabClient.GetCategoriesAsync(budgetDetail.Id.ToString(), null);
        foreach (var categoryGroup in categories.Data.Category_groups)
            foreach (var category in categoryGroup.Categories.Where(a => !a.Deleted && !a.Hidden))
            {
                snapshot.Append($"Category: Group={categoryGroup.Name}");
                snapshot.Append($", Name={category.Name}");
                snapshot.Append($", Balance=${category.Balance / 1000m}");
                snapshot.Append($", Activity=${category.Activity / 1000m}");
                snapshot.Append($", Need=${CalculateService.MonthlyNeed(category)}");
                snapshot.Append($", Funded=${ (category.Goal_overall_funded ?? 0) / 1000m}");
                snapshot.Append($", Target=${ (category.Goal_target ?? 0) / 1000m}");
                snapshot.AppendLine();
            }

        var instructions = new StringBuilder();
        instructions.AppendLine("You are an AI assistant that succinctly answers questions about the user's personal finances.");
        instructions.AppendLine("Do not include calculations, just answers.");
        instructions.AppendLine("Round values to the nearest dollar. Include thousands separator.");

        return $"{instructions}{snapshot}";
    }

    private string GenerateCompletion(string systemMessage, string userMessage)
    {
        var chatClient = _openAIClient.GetChatClient("gpt-4o-mini");
        ChatCompletion completion = chatClient.CompleteChat([
            new SystemChatMessage(systemMessage),
            new UserChatMessage(userMessage)]);

        return $"{completion.Content[0].Text}";
    }
}