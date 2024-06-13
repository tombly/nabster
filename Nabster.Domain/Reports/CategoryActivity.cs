using Ynab.Api.Client;

namespace Nabster.Domain.Reports;

public class CategoryActivity(YnabApiClient ynabClient)
{
    private readonly YnabApiClient _ynabClient = ynabClient;

    public async Task<CategoryActivityReport> Generate(string? budgetName, string categoryName)
    {
        // Find a budget to use.
        var budgetDetail = await _ynabClient.GetBudgetDetailAsync(budgetName);

        var categoryId = budgetDetail.Categories!.FirstOrDefault(c => c.Name == categoryName)?.Id
                            ?? throw new Exception($"Category not found: '{categoryName}'");

        var category = (await _ynabClient.GetCategoryByIdAsync(budgetDetail.Id.ToString(), categoryId!.ToString())).Data.Category;

        return new CategoryActivityReport
        {
            Activity = Math.Abs(category.Activity) / 1000m,
            Target = category.Goal_target / 1000m
        };
    }
}

#region Models

public class CategoryActivityReport
{
    public decimal Activity { get; set; }
    public decimal? Target { get; set; }
}

#endregion