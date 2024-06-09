using Ynab.Api.Client;

namespace Nabster.Domain.Reports;

public class CategoryActivity(YnabApiClient ynabClient)
{
    private readonly YnabApiClient _ynabClient = ynabClient;

    public async Task<decimal> Generate(string budgetName, string categoryName)
    {
        var budgetId = (await _ynabClient.GetBudgetsAsync(false)).Data.Budgets.FirstOrDefault(b => b.Name == budgetName)?.Id
                            ?? throw new Exception($"Budget not found: '{budgetName}'");

        var budget = (await _ynabClient.GetBudgetByIdAsync(budgetId.ToString(), null)).Data.Budget;

        var categoryId = budget.Categories!.FirstOrDefault(c => c.Name == categoryName)?.Id
                            ?? throw new Exception($"Category not found: '{categoryName}'");

        var category = (await _ynabClient.GetCategoryByIdAsync(budgetId.ToString(), categoryId!.ToString())).Data.Category;

        var activity = category.Activity / 1000m;

        return activity;
    }
}