using Ynab.Api;

namespace Nabster.Spend;

/// <summary>
/// Generates a monthly spend report. Monthly and non-monthly (quarterly,
/// annually, etc.) recurring goals are supported, as well as non-recurring
/// goals.
/// </summary>
public static class Report
{
    public static async Task Generate(string budgetName, Ynab.Api.Client client)
    {
        // Find the budget we're looking for.
        var budgetId = (await client.GetBudgetsAsync(false)).Data.Budgets.FirstOrDefault(b => b.Name == budgetName)?.Id;
        if (budgetId == null)
            throw new Exception($"Budget not found: '{budgetName}'");

        // Retrieve the budget from the API.
        var budget = (await client.GetBudgetByIdAsync(budgetId.ToString()!, null))?.Data.Budget;

        // The categories don't have their group name property set automatically
        // (but they're returned in a separate collection) so we patch them here.
        foreach (var category in budget!.Categories!)
            category.Category_group_name = budget.Category_groups!.FirstOrDefault(g => g.Id == category.Category_group_id)?.Name;

        // Build our model.
        var model = new SpendReport { BudgetName = budgetName };
        foreach (var category in budget.Categories!
            .Where(c => !c.Deleted)
            .Where(c => !c.Hidden)
            .Where(c => c.Category_group_id != Guid.Parse("1a129df6-4857-4ed9-8961-5b803b27707e")) // Skip credit card categories.
            .Where(c => c.Category_group_id != Guid.Parse("5fb28acc-c607-42dc-ab04-7963a6fe718d")) // Skip internal categories.
            .OrderBy(c => c.Category_group_name))
        {
            model.Categories.Add(new SpendCategory
            {
                CategoryName = category.Name,
                CategoryGroupName = category.Category_group_name!,
                GoalCadence = BuildGoalCadence(category.Goal_cadence, category.Goal_cadence_frequency),
                GoalDay = BuildDueDate(category.Goal_cadence, category.Goal_day, category.Goal_target_month),
                GoalTarget = category.Goal_target > 0 ? (category.Goal_target.Value / 1000.0).ToString() : string.Empty,
                GoalPercentageComplete = (category.Goal_percentage_complete / 100.0).ToString()!,
                MonthlyCost = BuildMonthlyCost(category)
            });
        }

        // Save to an Excel file.
        Excel.Create(model);
    }

    private static string BuildGoalCadence(int? goalCadence, int? goalCadenceFrequency)
    {
        if (goalCadence == null)
            return "No Repeat";

        var cadence = string.Empty;
        if (new List<int?> { 0, 1, 2, 13 }.Contains(goalCadence))
        {
            // The goal's due date repeats every goal_cadence * goal_cadence_frequency,
            // where 0 = None, 1 = Monthly, 2 = Weekly, and 13 = Yearly. For example,
            // goal_cadence 1 with goal_cadence_frequency 2 means the goal is due every
            // other month.
            var repeatFrequency = string.Empty;
            if (goalCadenceFrequency > 1)
                repeatFrequency = $" (Every {goalCadenceFrequency})";
            switch (goalCadence)
            {
                case 0:
                    cadence = "None";
                    break;
                case 1:
                    cadence = $"Monthly{repeatFrequency}";
                    break;
                case 2:
                    cadence = $"Weekly{repeatFrequency}";
                    break;
                case 13:
                    cadence = $"Yearly{repeatFrequency}";
                    break;
            }
        }
        else
        {
            // goal_cadence_frequency is ignored and the goal's due date
            // repeats every goal_cadence, where 3 = Every 2 Months, 4 = Every 3 Months,
            // ..., 12 = Every 11 Months, and 14 = Every 2 Years.
            if (goalCadence == 14)
                cadence = "Every 2 years";
            else
                cadence = $"Every {goalCadence - 1} months";
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

    /// <summary>
    /// Figures out what the monthly cost of a category is. It first calculates
    /// a multiplier based on the repeat frequency and then uses it to calculate
    /// the monthly cost based on the remaining amount to reach the target. For
    /// non-recurring goals it simply divides the remaining amount by the
    /// remaining months.
    /// </summary>
    private static decimal? BuildMonthlyCost(Category category)
    {
        var multiplier = default(decimal?);
        switch (category.Goal_cadence)
        {
            case 0: // No repeat
                break;
            case 1: // Monthly
                multiplier = 1.0m / category.Goal_cadence_frequency;
                break;
            case 2: // Weekly
                multiplier = 4 * category.Goal_cadence_frequency;
                break;
            case 3: // Every 2 months
            case 4: // Every 3 months
            case 5: // Every 4 months
            case 6: // Every 5 months
            case 7: // Every 6 months
            case 8: // Every 7 months
            case 9: // Every 8 months
            case 10: // Every 9 months
            case 11: // Every 10 months
            case 12: // Every 11 months
                multiplier = 1m / (category.Goal_cadence - 1);
                break;
            case 13: // Yearly
                multiplier = 1m / (12m * category.Goal_cadence_frequency);
                break;
            case 14: // Every 2 years
                multiplier = 1m / 24m;
                break;
        }

        // If we have a multiplier then it's recurring.
        if (multiplier != null)
        {
            return category.Goal_target / 1000 * multiplier ?? 0;
        }
        else
        {
            if (category.Goal_overall_left > 0)
                return category.Goal_overall_left / 1000 / category.Goal_months_to_budget ?? 0;
            else
                return 0;
        }    }

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