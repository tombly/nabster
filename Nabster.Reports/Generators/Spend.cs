using Ynab.Api.Client;
using Ynab.Api.Client.Extensions;

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

        // Flatten subtransactions into the main transaction list.
        var allTransactions = new List<TransactionDetail>();
        foreach (var transaction in transactions)
        {
            allTransactions.Add(transaction);
            if (transaction.Subtransactions != null && transaction.Subtransactions.Any())
            {
                foreach (var sub in transaction.Subtransactions)
                {
                    // Create a new TransactionDetail for the subtransaction, inheriting from the parent.
                    var subDetail = new TransactionDetail
                    {
                        Id = sub.Id,
                        Date = transaction.Date,
                        Amount = sub.Amount,
                        Memo = sub.Memo ?? transaction.Memo,
                        Payee_name = transaction.Payee_name,
                        Category_id = sub.Category_id ?? transaction.Category_id,
                        Account_id = transaction.Account_id,
                        Account_name = transaction.Account_name,
                        Subtransactions = new List<SubTransaction>() // subtransactions of subtransactions are not supported
                    };
                    allTransactions.Add(subDetail);
                }
            }
        }

        // Now filter and process allTransactions instead of transactions
        allTransactions = allTransactions
            .Where(t => t.Category_id == categoryId)
            .Where(t => t.Date.Month == DateTime.Parse(month).Month)
            .ToList();

        foreach (var transaction in allTransactions)
        {
            transaction.Memo ??= string.Empty;
            transaction.Payee_name ??= string.Empty;
        }

        // Group the transactions by their memo text prefix.
        var model = new SpendReport
        {
            BudgetName = budgetDetail.Name,
            MonthName = DateTime.Parse(month).ToString("MMMM yyyy"),
            Groups = allTransactions.GroupBy(t => t.Memo!.Split(':')[0]).Select(g => new SpendGroup
            {
                MemoPrefix = g.Key,
                Transactions = g.Select(t => new SpendTransaction
                {
                    Description = BuildDescription(g.Key, t),
                    Date = t.Date,
                    Amount = t.Amount.FromMilliunits()
                }).ToList().OrderBy(t => t.Description).ToList()
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