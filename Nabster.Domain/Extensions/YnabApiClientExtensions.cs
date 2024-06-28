using Nabster.Domain.Exceptions;
using Ynab.Api.Client;

namespace Nabster.Domain.Extensions;

public static class YnabApiClientExtensions
{
    /// <summary>
    /// Retrieves the budget detail for the specified budget name. If no budget
    /// name is provided, the first budget is returned.
    /// </summary>
    public async static Task<BudgetDetail> GetBudgetDetailAsync(this YnabApiClient client, string? budgetName)
    {
        // Retrieve the budget summary.
        var budgetSummary = (budgetName != null ?
            (await client.GetBudgetsAsync(false)).Data.Budgets.FirstOrDefault(b => b.Name == budgetName) ?? throw new BudgetNotFoundException(budgetName) :
            (await client.GetBudgetsAsync(false)).Data.Budgets.First()) ?? throw new BudgetNotFoundException();

        // Retrieve the budget detail from the API.
        return (await client.GetBudgetByIdAsync(budgetSummary.Id.ToString(), null)).Data.Budget;
    }
}