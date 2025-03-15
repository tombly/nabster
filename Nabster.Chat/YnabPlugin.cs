using System.ComponentModel;
using Microsoft.SemanticKernel;
using Ynab.Api.Client;
using Ynab.Api.Client.Extensions;

namespace Nabster.Chat;

/// <summary>
/// A Semantic Kernel plugin for YNAB. The YNAB API responses are mapped to
/// simpler models to (1) reduce the number of tokens that are sent to the
/// model and (2) to allow for customization of the data, such as calculating
/// the monthly need for a category, and (3) sending the API models seems to
/// cause the GetChatMessageContentAsync() method to hang in some cases.
/// </summary>
/// <param name="_ynabApiClient"></param>
public class YnabPlugin(YnabApiClient _ynabApiClient)
{
    [KernelFunction("get_accounts")]
    [Description("Gets a list of account balances")]
    public async Task<IEnumerable<AccountModel>> GetAccountsAsync()
    {
        var budgetDetail = await _ynabApiClient.GetBudgetDetailAsync();
        var accounts = (await _ynabApiClient.GetAccountsAsync(budgetDetail.Id.ToString(), null)!).Data.Accounts;

        return accounts
                .Where(a => !a.Deleted && !a.Closed)
                .Select(a => new AccountModel
                {
                    Name = a.Name,
                    Type = a.Type,
                    OnBudget = a.On_budget,
                    Note = a.Note,
                    Balance = a.Balance.FromMilliunits(),
                    ClearedBalance = a.Cleared_balance.FromMilliunits(),
                    UnclearedBalance = a.Uncleared_balance.FromMilliunits(),
                    LastReconciledAt = a.Last_reconciled_at.HasValue ? a.Last_reconciled_at.Value.DateTime : null
                });
    }

    [KernelFunction("get_categories")]
    [Description("Gets a list of budget categories")]
    public async Task<IEnumerable<CategoryModel>> GetCategoriesAsync()
    {
        var budgetDetail = await _ynabApiClient.GetBudgetDetailAsync();
        var categories = await _ynabApiClient.GetCategoriesAsync(budgetDetail.Id.ToString(), null);

        return categories.Data.Category_groups
                .SelectMany(a => a.Categories)
                .Where(a => !a.Deleted && !a.Hidden)
                .Select(c => new CategoryModel
                {
                    CategoryGroupName = c.Category_group_name,
                    Name = c.Name,
                    Budgeted = c.Budgeted.FromMilliunits(),
                    Activity = c.Activity.FromMilliunits(),
                    Balance = c.Balance.FromMilliunits(),
                    GoalType = c.Goal_type,
                    GoalTarget = c.Goal_target.HasValue ? c.Goal_target.Value.FromMilliunits() : null,
                    GoalPercentageComplete = c.Goal_percentage_complete,
                    MonthlyNeed = c.MonthlyNeed().FromMilliunits(),
                });
    }

    [KernelFunction("get_month_summaries")]
    [Description("Gets a summary of all budget months")]
    public async Task<IEnumerable<MonthSummaryModel>> GetMonthSummaryAsync()
    {
        var budgetDetail = await _ynabApiClient.GetBudgetDetailAsync();
        var monthSummaries = await _ynabApiClient.GetBudgetMonthsAsync(budgetDetail.Id.ToString(), null);
        return monthSummaries.Data.Months
                .Select(m => new MonthSummaryModel
                {
                    Month = m.Month,
                    Income = m.Income.FromMilliunits(),
                    Budgeted = m.Budgeted.FromMilliunits(),
                    Activity = m.Activity.FromMilliunits(),
                    ReadyToAssign = m.To_be_budgeted.FromMilliunits(),
                    AgeOfMoney = m.Age_of_money
                });
    }
}

public class AccountModel
{
    public string Name { get; set; } = default!;
    public AccountType Type { get; set; } = default!;
    public bool OnBudget { get; set; } = default!;
    public string? Note { get; set; }
    public decimal Balance { get; set; } = default!;
    public decimal ClearedBalance { get; set; } = default!;
    public decimal UnclearedBalance { get; set; } = default!;
    public DateTimeOffset? LastReconciledAt { get; set; }
}

public class CategoryModel
{
    public string? CategoryGroupName { get; set; }
    public string Name { get; set; } = default!;
    public decimal Budgeted { get; set; } = default!;
    public decimal Activity { get; set; } = default!;
    public decimal Balance { get; set; } = default!;
    public CategoryGoalType? GoalType { get; set; }
    public decimal? GoalTarget { get; set; }
    public int? GoalPercentageComplete { get; set; }
    public decimal? MonthlyNeed { get; set; }
}

public class MonthSummaryModel
{
    public DateTimeOffset Month { get; set; } = default!;
    public decimal Income { get; set; } = default!;
    public decimal Budgeted { get; set; } = default!;
    public decimal Activity { get; set; } = default!;
    public decimal ReadyToAssign { get; set; } = default!;
    public int? AgeOfMoney { get; set; }
}