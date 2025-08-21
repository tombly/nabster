using Nabster.Reporting.Reports.Spend.Models;
using Nabster.Reporting.Services;
using Ynab.Api.Client;
using Ynab.Api.Client.Extensions;

namespace Nabster.Reporting.Reports.Spend;

/// <summary>
/// Generates a monthly spend report for a specific category that groups and
/// totals transactions based on prefixes in their memo text.
/// </summary>
public class SpendReport(YnabService _ynabService)
{
    public async Task<SpendReportModel> Build(string? budgetName, string categoryName, string month)
    {
        var budgetDetail = await _ynabService.Client.GetBudgetDetailAsync(budgetName);

        // Get all the transactions for the current month for the given category.
        var categoryId = budgetDetail.Categories!.FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.InvariantCultureIgnoreCase))?.Id;
        var startOfMonth = new DateTimeOffset(DateTime.Parse(month).Year, DateTime.Parse(month).Month, 1, 0, 0, 0, TimeSpan.Zero);
        var transactions = (await _ynabService.Client.GetTransactionsAsync(budgetDetail.Id.ToString(), startOfMonth, null, null)).Data.Transactions;

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
        var model = new SpendReportModel
        {
            BudgetName = budgetDetail.Name,
            MonthName = DateTime.Parse(month).ToString("MMMM yyyy"),
            Groups = allTransactions.GroupBy(t => t.Memo!.Split(':')[0]).Select(g => new SpendGroupModel
            {
                MemoPrefix = g.Key,
                Transactions = g.Select(t => new SpendTransactionModel
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
        if (payee.Contains("amazon", StringComparison.InvariantCultureIgnoreCase)) return "Amazon";
        if (payee.Contains("kindle", StringComparison.InvariantCultureIgnoreCase)) return "Kindle";
        if (payee.Contains("microsoft", StringComparison.InvariantCultureIgnoreCase)) return "Microsoft";
        if (payee.Contains("nintendo", StringComparison.InvariantCultureIgnoreCase)) return "Nintendo";
        if (payee.Contains("apple", StringComparison.InvariantCultureIgnoreCase)) return "Apple";
        return payee;
    }
}