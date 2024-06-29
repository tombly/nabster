using Nabster.Domain.Exceptions;
using Nabster.Domain.Extensions;
using Ynab.Api.Client;

namespace Nabster.Domain.Reports;

/// <summary>
/// Generates a simple report that shows how much of a single category or the
/// each category in a group has been funded. Assumes all categories have
/// goal targets.
/// </summary>
public class Funded(YnabApiClient _ynabClient)
{
    public async Task<FundedReport> Generate(string? budgetName, string categoryOrGroupName)
    {
        var budgetDetail = await _ynabClient.GetBudgetDetailAsync(budgetName);
        var (category, categoryGroup) = budgetDetail.FindCategoryOrGroup(categoryOrGroupName);
        if(category != null)
            return await CreateCategoryReport(budgetDetail.Id.ToString(), category.Id.ToString());
        if(categoryGroup != null)
            return await CreateCategoryGroupReport(budgetDetail, categoryGroup.Id);
        throw new CategoryOrGroupNotFoundException(categoryOrGroupName);
    }

    private async Task<FundedReport> CreateCategoryReport(string budgetId, string categoryId)
    {
        var c = await _ynabClient.GetCategoryByIdAsync(budgetId, categoryId);
        var category = c.Data.Category;
        return new FundedReport
        {
            Name = category.Name,
            Categories = [
                new() {
                    Name = category.Name,
                    Funded = (category.Goal_overall_funded ?? 0) / 1000m,
                    Target = (category.Goal_target ?? 0) / 1000m,
                }]
        };
    }

    private async Task<FundedReport> CreateCategoryGroupReport(BudgetDetail budgetDetail, Guid categoryGroupId)
    {
        var categories = await _ynabClient.GetCategoriesAsync(budgetDetail.Id.ToString(), null);
        var group = categories.Data.Category_groups.Single(g => g.Id == categoryGroupId);
        var groupCategories = group.Categories.Where(c => !c.Hidden && !c.Deleted).ToList();

        return new FundedReport
        {
            Name = group.Name,
            Categories = groupCategories.Select(c => new FundedCategory
            {
                Name = c.Name,
                Funded = (c.Goal_overall_funded ?? 0) / 1000m,
                Target = (c.Goal_target ?? 0) / 1000m,
            }).ToList()
        };
    }
}

#region Models

public class FundedReport
{
    public string Name { get; set; } = string.Empty;
    public List<FundedCategory> Categories { get; set; } = new();
}

public class FundedCategory
{
    public string Name { get; set; } = string.Empty;
    public decimal Funded { get; set; }
    public decimal Target { get; set; }
}

#endregion