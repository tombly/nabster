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

    /// <summary>
    /// Finds a category or category group with the given name. It first looks
    /// for an exact match on the category or group name and if not found then
    /// it looks for a partial match. Only active and visible categories and
    /// groups are searched. A category or group is returned, never both. If
    /// no match is found then null is returned for both.
    /// </summary>
    public static (Category?, CategoryGroup?) FindCategoryOrGroup(this BudgetDetail budgetDetail, string categoryOrGroupName)
    {
        var categories = budgetDetail.Categories!.Where(c => c.Hidden == false && c.Deleted == false).ToList();
        var categoryGroups = budgetDetail.Category_groups!.Where(g => g.Hidden == false && g.Deleted == false).ToList();

        // See if the name term matches a category exactly.
        var category = categories.FirstOrDefault(c => c.Name.Equals(categoryOrGroupName, StringComparison.InvariantCultureIgnoreCase));
        if (category != null)
            return (category, null);

        // See if the name matches a category group exactly.
        var categoryGroup = categoryGroups.FirstOrDefault(g => g.Name.Equals(categoryOrGroupName, StringComparison.InvariantCultureIgnoreCase));
        if (categoryGroup != null)
            return (null, categoryGroup);

        // See if the name matches a category partially.
        category = categories.FirstOrDefault(c => c.Name.Contains(categoryOrGroupName, StringComparison.InvariantCultureIgnoreCase));
        if (category != null)
            return (category, null);

        // See if the name matches a category group partially.
        categoryGroup = categoryGroups.FirstOrDefault(g => g.Name.Contains(categoryOrGroupName, StringComparison.InvariantCultureIgnoreCase));
        if (categoryGroup != null)
            return (null, categoryGroup);

        return (null, null);
    }
}