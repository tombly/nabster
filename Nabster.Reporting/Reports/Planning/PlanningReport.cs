using Nabster.Reporting.Reports.Planning.Models;
using Nabster.Reporting.Services;
using Ynab.Api.Client.Extensions;

namespace Nabster.Reporting.Reports.Planning;

/// <summary>
/// Generates a monthly planning report. Monthly and non-monthly (quarterly,
/// annually, etc.) recurring goals are supported, as well as non-recurring
/// goals.
/// </summary>
public class PlanningReport(IEnumerable<IYnabService> _ynabServices)
{
    public async Task<PlanningReportModel> Build(string? budgetName, bool isDemo)
    {
        var ynabService = _ynabServices.Single(s => s.IsDemo == isDemo)!;

        var budgetDetail = await ynabService.Client.GetBudgetDetailAsync(budgetName);

        // The categories don't have their group name property set automatically
        // (but they're returned in a separate collection) so we patch them here.
        foreach (var category in budgetDetail.Categories!)
            category.Category_group_name = budgetDetail.Category_groups!.FirstOrDefault(g => g.Id == category.Category_group_id)?.Name;

        // Build our model.
        var model = new PlanningReportModel
        {
            BudgetName = budgetDetail.Name,
            Groups = [.. budgetDetail.Categories
                .Where(c => c.Category_group_id != Guid.Parse("1a129df6-4857-4ed9-8961-5b803b27707e")) // Skip credit card categories.
                .Where(c => c.Category_group_id != Guid.Parse("5fb28acc-c607-42dc-ab04-7963a6fe718d")) // Skip internal categories.
                .Select(c => c.Category_group_name)
                .Distinct()
                .Select(groupName =>
                    new PlanningGroupModel
                    {
                        CategoryGroupName = groupName!,
                        Categories = [.. budgetDetail.Categories
                            .Where(c => c.Category_group_name == groupName)
                            .Where(c => !c.Deleted)
                            .Where(c => !c.Hidden)
                            .Select(c => new PlanningCategoryModel
                            {
                                CategoryName = c.Name,
                                GoalCadence = BuildGoalCadence(c.Goal_cadence, c.Goal_cadence_frequency),
                                GoalDay = BuildDueDate(c.Goal_cadence, c.Goal_day, c.Goal_target_month),
                                GoalTarget = c.Goal_target > 0 ? c.Goal_target.Value.FromMilliunits() : 0,
                                GoalPercentageComplete = c.Goal_cadence == 0 ? c.Goal_percentage_complete / 100m ?? 0 : null,
                                MonthlyCost = c.MonthlyNeed().FromMilliunits(),
                                YearlyCost = (c.MonthlyNeed() * 12).FromMilliunits()
                            })
                            .OrderBy(c => c.CategoryName)]
                    })
                .OrderBy(group => group.CategoryGroupName)]
        };

        foreach (var group in model.Groups)
        {
            group.MonthlyTotal = group.Categories.Sum(c => c.MonthlyCost);
            group.YearlyTotal = group.Categories.Sum(c => c.YearlyCost);
        }

        foreach (var group in model.Groups)
        {
            model.MonthlyTotal += group.MonthlyTotal;
            model.YearlyTotal += group.YearlyTotal;
        }

        return model;
    }

    private static string BuildGoalCadence(int? goalCadence, int? goalCadenceFrequency)
    {
        if (goalCadence == null)
            return "None";

        var cadence = string.Empty;
        if (new List<int?> { 0, 1, 2, 13 }.Contains(goalCadence))
        {
            // The goal's due date repeats every goal_cadence * goal_cadence_frequency,
            // where 0 = None, 1 = Monthly, 2 = Weekly, and 13 = Yearly. For example,
            // goal_cadence 1 with goal_cadence_frequency 2 means the goal is due every
            // other month.
            switch (goalCadence)
            {
                case 0:
                    cadence = "Once";
                    break;
                case 1:
                    if (goalCadenceFrequency == 1)
                        cadence = "Monthly";
                    else
                    if (goalCadenceFrequency == 3)
                        cadence = $"Quarterly";
                    else
                        cadence = $"{goalCadenceFrequency} Months";
                    break;
                case 2:
                    if (goalCadenceFrequency == 1)
                        cadence = "Weekly";
                    else
                        cadence = $"{goalCadenceFrequency} Weeks";
                    break;
                case 13:
                    if (goalCadenceFrequency == 1)
                        cadence = "Yearly";
                    else
                        cadence = $"{goalCadenceFrequency} Years";
                    break;
            }
        }
        else
        {
            // goal_cadence_frequency is ignored and the goal's due date
            // repeats every goal_cadence, where 3 = Every 2 Months, 4 = Every 3 Months,
            // ..., 12 = Every 11 Months, and 14 = Every 2 Years.
            if (goalCadence == 14)
                cadence = "2 Years";
            else
                cadence = $"{goalCadence - 1} Months";
        }

        return cadence;
    }

    private static string BuildDueDate(int? goalCadence, int? goalDay, DateTimeOffset? goalTargetMonth)
    {
        // A day offset modifier for the goal's due date. When goal_cadence is 2
        // (Weekly), this value specifies which day of the week the goal is due 
        // (0 = Sunday, 6 = Saturday). Otherwise, this value specifies which day of the
        // month the goal is due (1 = 1st, 31 = 31st, null = Last day of Month).
        var day = string.Empty;
        if (goalCadence == null || goalCadence == 0)
            day = "None";
        else if (goalCadence == 2)
            day = ((DayOfWeek)goalDay!).ToString();
        else
            day = goalDay == null ? "Last day of month" : $"{goalDay}{SuffixForDay(goalDay.Value)}";

        var targetMonth = goalTargetMonth.HasValue ? $"{goalTargetMonth.Value.LocalDateTime:MMM-dd}" : null;

        // There will never be both a goal day and a goal target month so we just
        // show one of them as the due date.
        return targetMonth ?? day;
    }

    private static string SuffixForDay(int day)
    {
        if (day >= 11 && day <= 13)
            return "th";

        return (day % 10) switch
        {
            1 => "st",
            2 => "nd",
            3 => "rd",
            _ => "th",
        };
    }
}