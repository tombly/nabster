using Nabster.Domain.Extensions;
using Ynab.Api.Client;

namespace Nabster.Reports.Generators;

/// <summary>
/// Generates a monthly spend report for a specific category that groups and
/// totals transactions based on prefixes in their memo text.
/// </summary>
public static class Spend
{
    public static async Task<SpendReport> Generate(string? budgetName, string categoryName, string month, YnabApiClient _ynabClient)
    {
        var budgetDetail = await _ynabClient.GetBudgetDetailAsync(budgetName);

        // Get all the transactions for the current month for the given category.
        var categoryId = budgetDetail.Categories!.FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.InvariantCultureIgnoreCase))?.Id;
        var startOfMonth = new DateTimeOffset(DateTime.Parse(month).Year, DateTime.Parse(month).Month, 1, 0, 0, 0, TimeSpan.Zero);
        var transactions = (await _ynabClient.GetTransactionsAsync(budgetDetail.Id.ToString(), startOfMonth, null, null)).Data.Transactions;

        transactions = transactions
            .Where(t => t.Category_id == categoryId)
            .Where(t => t.Date.Month == DateTime.Parse(month).Month)
            .ToList();

        foreach (var transaction in transactions)
        {
            transaction.Memo ??= string.Empty;
            transaction.Payee_name ??= string.Empty;
        }

        // Group the transactions by their memo text prefix.
        var model = new SpendReport
        {
            BudgetName = budgetDetail.Name,
            MonthName = DateTime.Parse(month).ToString("MMMM yyyy"),
            Groups = transactions.GroupBy(t => t.Memo!.Split(':')[0]).Select(g => new SpendGroup
            {
                MemoPrefix = g.Key,
                Transactions = g.Select(t => new SpendTransaction
                {
                    Description = BuildDescription(g.Key, t),
                    Date = t.Date,
                    Amount = t.Amount / 1000m
                }).ToList()
            }).ToList()
        };

        return model;
    }

    private static string BuildDescription(string memoPrefix, TransactionDetail transaction)
    {
        var payee = CleanPayee(transaction.Payee_name!);
        var memo = transaction.Memo!.Replace(memoPrefix + ":", string.Empty);
        return string.IsNullOrWhiteSpace(memo) ? payee : $"{payee} - {memo}";
    }

    private static string CleanPayee(string payee)
    {
        if(payee.Contains("amazon", StringComparison.InvariantCultureIgnoreCase)) return "Amazon";
        if(payee.Contains("kindle", StringComparison.InvariantCultureIgnoreCase)) return "Kindle";
        if(payee.Contains("microsoft", StringComparison.InvariantCultureIgnoreCase)) return "Microsoft";
        if(payee.Contains("nintendo", StringComparison.InvariantCultureIgnoreCase)) return "Nintendo";
        if(payee.Contains("apple", StringComparison.InvariantCultureIgnoreCase)) return "Apple";
        return payee;
    }
}

#region Models

public class SpendReport
{
    public string BudgetName { get; set; } = string.Empty;
    public string MonthName { get; set; } = string.Empty;
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