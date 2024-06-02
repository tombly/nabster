using Ynab.Api;

namespace Nabster.Spend;

/// <summary>
/// Generates a monthly spend report for a specific category that groups and
/// totals transactions based on prefixes in their memo text.
/// </summary>
public static class Report
{
    public static async Task Generate(string budgetName, Client client, string categoryName, string month)
    {
        // Find the budget we're looking for.
        var budgetId = (await client.GetBudgetsAsync(false)).Data.Budgets.FirstOrDefault(b => b.Name == budgetName)?.Id;
        if (budgetId == null)
            throw new Exception($"Budget not found: '{budgetName}'");

        // Retrieve the budget from the API.
        var budget = (await client.GetBudgetByIdAsync(budgetId.ToString()!, null))?.Data.Budget;

        // Get all the transactions for the current month for the given category.
        var categoryId = budget!.Categories!.FirstOrDefault(c => c.Name == categoryName)?.Id;
        var startOfMonth = new DateTimeOffset(DateTime.Parse(month).Year, DateTime.Parse(month).Month, 1, 0, 0, 0, TimeSpan.Zero);
        var transactions = (await client.GetTransactionsAsync(budgetId.ToString(), startOfMonth, null, null)).Data.Transactions;

        transactions = transactions
            .Where(t => t.Category_id == categoryId)
            .Where(t => t.Date.Month == DateTime.Parse(month).Month)
            .ToList();

        foreach (var transaction in transactions)
            transaction.Memo ??= string.Empty;

        // Group the transactions by their memo text prefix.
        var model = new SpendReport
        {
            BudgetName = budgetName,
            Groups = transactions.GroupBy(t => t.Memo!.Split(':')[0]).Select(g => new SpendGroup
            {
                MemoPrefix = g.Key,
                Transactions = g.Select(t => new SpendTransaction
                {
                    Description = t.Payee_name + " " + t.Memo,
                    Date = t.Date,
                    Amount = t.Amount / 1000m
                }).ToList()
            }).ToList()
        };

        // Save to an Excel file.
        Excel.Create(model);
    }
}