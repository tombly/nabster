using Ynab.Api.Client;

namespace Nabster.Domain;

public static class YnabApiClientExtensions
{
    public async static Task<BudgetDetail> GetBudgetDetailAsync(this YnabApiClient client, string? budgetName)
    {
        // Retrieve the budget summary.
        var budgetSummary = budgetName != null ?
            (await client.GetBudgetsAsync(false)).Data.Budgets.FirstOrDefault(b => b.Name == budgetName) :
            (await client.GetBudgetsAsync(false)).Data.Budgets.First();

        if (budgetSummary == null)
            throw new Exception($"No budgets found or by name '{budgetName}'");

        // Retrieve the budget detail from the API.
        return (await client.GetBudgetByIdAsync(budgetSummary.Id.ToString(), null)).Data.Budget;
    }
}