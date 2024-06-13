using Ynab.Api.Client;

namespace Nabster.Domain.Reports;

/// <summary>
/// Generates a monthly spend report for a specific category that groups and
/// totals transactions based on prefixes in their memo text.
/// </summary>
public class Spend(YnabApiClient ynabClient)
{
    private readonly YnabApiClient _ynabClient = ynabClient;

    public async Task<SpendReport> Generate(string? budgetName, string categoryName, string month)
    {
        // Find a budget to use.
        var budgetDetail = await _ynabClient.GetBudgetDetailAsync(budgetName);

        // Get all the transactions for the current month for the given category.
        var categoryId = budgetDetail.Categories!.FirstOrDefault(c => c.Name == categoryName)?.Id;
        var startOfMonth = new DateTimeOffset(DateTime.Parse(month).Year, DateTime.Parse(month).Month, 1, 0, 0, 0, TimeSpan.Zero);
        var transactions = (await _ynabClient.GetTransactionsAsync(budgetDetail.Id.ToString(), startOfMonth, null, null)).Data.Transactions;

        transactions = transactions
            .Where(t => t.Category_id == categoryId)
            .Where(t => t.Date.Month == DateTime.Parse(month).Month)
            .ToList();

        foreach (var transaction in transactions)
            transaction.Memo ??= string.Empty;

        // Group the transactions by their memo text prefix.
        var model = new SpendReport
        {
            BudgetName = budgetDetail.Name,
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

        return model;
    }
}

#region Models

public class SpendReport
{
    public string BudgetName { get; set; } = string.Empty;
    public List<SpendGroup> Groups { get; set; } = [];
    public decimal Total => Groups.SelectMany(g => g.Transactions).Sum(t => t.Amount);
}

public class SpendGroup
{
    public string MemoPrefix { get; set; } = string.Empty;
    public List<SpendTransaction> Transactions { get; set; } = [];
    public decimal Total => Transactions.Sum(t => t.Amount);
}

public class SpendTransaction
{
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset Date { get; set; }
    public decimal Amount { get; set; }
}

#endregion