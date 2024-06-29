using Nabster.Domain.Exceptions;
using Nabster.Domain.Extensions;
using Nabster.Domain.Services;
using Ynab.Api.Client;

namespace Nabster.Domain.Reports;

/// <summary>
/// Generates a simple report that shows the activity and amount needed for the
/// current month for either a specific category or a category group.
/// </summary>
public class Activity(CalculateService _calculateService, YnabApiClient _ynabClient)
{
    public async Task<ActivityReport> Generate(string? budgetName, string categoryOrGroupName)
    {
        var budgetDetail = await _ynabClient.GetBudgetDetailAsync(budgetName);
        var (category, categoryGroup) = budgetDetail.FindCategoryOrGroup(categoryOrGroupName);
        if(category != null)
            return await CreateCategoryReport(budgetDetail.Id.ToString(), category.Id.ToString());
        if(categoryGroup != null)
            return await CreateCategoryGroupReport(budgetDetail, categoryGroup.Id);
        throw new CategoryOrGroupNotFoundException(categoryOrGroupName);
    }

    private async Task<ActivityReport> CreateCategoryReport(string budgetId, string categoryId)
    {
        var category = (await _ynabClient.GetCategoryByIdAsync(budgetId, categoryId)).Data.Category;
        return new ActivityReport
        {
            Name = category.Name,
            Activity = Math.Abs(category.Activity) / 1000m,
            Need = _calculateService.MonthlyNeed(category)
        };
    }

    private async Task<ActivityReport> CreateCategoryGroupReport(BudgetDetail budgetDetail, Guid categoryGroupId)
    {
        var categories = await _ynabClient.GetCategoriesAsync(budgetDetail.Id.ToString(), null);
        var group = categories.Data.Category_groups.Single(g => g.Id == categoryGroupId);
        var groupCategories = group.Categories.Where(c => !c.Hidden && !c.Deleted).ToList();

        var activity = groupCategories.Sum(c => c.Activity);
        var need = groupCategories.Sum(c => _calculateService.MonthlyNeed(c));

        return new ActivityReport
        {
            Name = group.Name,
            Activity = Math.Abs(activity) / 1000m,
            Need = need
        };
    }
}

#region Models

public class ActivityReport
{
    public string Name { get; set; } = string.Empty;
    public decimal Activity { get; set; }
    public decimal Need { get; set; }
}

#endregion