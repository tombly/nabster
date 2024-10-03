using Nabster.Domain.Extensions;
using Ynab.Api.Client;

namespace Nabster.Domain.Reports;

/// <summary>
/// Generates a report of cumulative, grouped, account balances. The report
/// includes an Excel spreadsheet with the data and a (self-contained) HTML
/// file with charts.
/// </summary>
public class Performance(YnabApiClient _ynabClient)
{
    public async Task<PerformanceReport> Generate(string? budgetName)
    {
        var budgetDetail = await _ynabClient.GetBudgetDetailAsync(budgetName);
        var accounts = (await _ynabClient.GetAccountsAsync(budgetDetail.Id.ToString(), null)!).Data.Accounts.Where(a => !a.Closed);

        // Create our list of account name prefixes that we'll group the
        // accounts by.
        var accountNames = accounts.Select(a => a.Name).ToList();
        var accountPrefixes = accountNames.Select(a => a.Split(' ').First()).Distinct().ToList();

        // Seed the model with one account group per prefix.
        var model = new PerformanceReport
        {
            BudgetName = budgetDetail.Name,
            AccountGroups = accountPrefixes.Select(prefix =>
            {
                return new PerformanceAccountGroup
                {
                    Name = NameForGroupPrefix(prefix),
                    Prefix = prefix,
                    Accounts = accountNames
                                    .Where(n => n.StartsWith(prefix))
                                    .Select(n => new PerformanceAccount { Name = n })
                                    .ToList()
                };
            }).ToList()
        };

        // Get all the transactions.
        var transactions = (await _ynabClient.GetTransactionsAsync(budgetDetail.Id.ToString(), null, null, null)).Data.Transactions.ToList();

        // Add each transaction to the appropriate account group based on its prefix.
        foreach (var transaction in transactions)
        {
            if (!accountNames.Contains(transaction.Account_name))
                continue;

            var transactionAccountPrefix = transaction.Account_name.Split(' ').First();
            var accountGroup = model.AccountGroups.First(g => g.Prefix == transactionAccountPrefix);
            var account = accountGroup.Accounts.First(a => transaction.Account_name == a.Name);

            var date = transaction.Date;
            var amount = transaction.Amount / 1000m;

            account.Transactions.Add(new PerformanceTransaction { Date = date, Amount = amount });
            accountGroup.AllTransactions.Add(new PerformanceTransaction { Date = date, Amount = amount });
        }

        // Calculate running balance.
        foreach (var accountGroup in model.AccountGroups)
        {
            AccumulateTransactions(accountGroup.AllTransactions);
            foreach (var account in accountGroup.Accounts)
                AccumulateTransactions(account.Transactions);
        }

        // Trim the transactions to the past year (must do this after we've
        // calculated the running balances).
        foreach (var accountGroup in model.AccountGroups)
        {
            RemoveOldTransactions(accountGroup.AllTransactions);
            foreach (var account in accountGroup.Accounts)
                RemoveOldTransactions(account.Transactions);
        }

        return model;
    }

    private static void AccumulateTransactions(List<PerformanceTransaction> transactions)
    {
        var cumulative = 0m;
        foreach (var transaction in transactions.OrderBy(t => t.Date))
        {
            cumulative += transaction.Amount;
            transaction.RunningBalance = cumulative;
        }
    }

    private static void RemoveOldTransactions(List<PerformanceTransaction> transactions)
    {
        transactions.RemoveAll(t => t.Date < DateTime.Now.AddDays(-365 - 14));
    }

    private static string NameForGroupPrefix(string prefix)
    {
        return prefix switch
        {
            "CASH" => "Cash",
            "LOC" => "Credit Cards",
            "LOAN" => "Loans",
            "CUS" => "Cushion",
            "RET" => "Retirement",
            "COL" => "College",
            "AST" => "Assets",
            _ => prefix
        };
    }
}

#region Models

public class PerformanceReport
{
    public string BudgetName { get; set; } = string.Empty;
    public List<PerformanceAccountGroup> AccountGroups { get; set; } = [];
}

public class PerformanceAccountGroup
{
    public string Name { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public List<PerformanceAccount> Accounts { get; set; } = [];
    public List<PerformanceTransaction> AllTransactions { get; set; } = [];
}

public class PerformanceAccount
{
    public string Name { get; set; } = string.Empty;
    public List<PerformanceTransaction> Transactions { get; set; } = [];
}

public class PerformanceTransaction
{
    public DateTimeOffset Date { get; set; }
    public decimal Amount { get; set; }
    public decimal RunningBalance { get; set; }
}

#endregion