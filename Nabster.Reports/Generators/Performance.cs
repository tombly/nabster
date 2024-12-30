using Nabster.Domain.Extensions;
using Ynab.Api.Client;

namespace Nabster.Reports.Generators;

/// <summary>
/// Generates a report of cumulative, grouped, account balances. The report
/// includes an Excel spreadsheet with the data and a (self-contained) HTML
/// file with charts.
/// </summary>
public static class Performance
{
    public static async Task<PerformanceReport> Generate(string? budgetName, YnabApiClient ynabClient)
    {
        var budgetDetail = await ynabClient.GetBudgetDetailAsync(budgetName);
        var accounts = (await ynabClient.GetAccountsAsync(budgetDetail.Id.ToString(), null)!).Data.Accounts;

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
        var transactions = (await ynabClient.GetTransactionsAsync(budgetDetail.Id.ToString(), null, null, null)).Data.Transactions.ToList();

        foreach (var account in accounts)
        {
            var accountPrefix = account.Name.Split(' ').First();
            var accountGroupModel = model.AccountGroups.First(g => g.Prefix == accountPrefix);
            var accountModel = accountGroupModel.Accounts.First(a => account.Name == a.Name);

            var cumulative = 0m;
            foreach (var transaction in transactions.Where(t => t.Account_name == account.Name).OrderBy(t => t.Date))
            {
                var date = transaction.Date;
                var amount = transaction.Amount / 1000m;

                // Account for any loan interest.
                if (account.Debt_interest_rates!.Any() && cumulative != 0)
                {
                    var interestRate = LookupPeriodicValue(account.Debt_interest_rates!, date);
                    var interest = (Math.Abs(cumulative) * (interestRate / 100m)) / 12m;
                    amount -= interest;
                }

                // Account for any escrow amounts.
                if (account.Debt_escrow_amounts!.Any() && cumulative != 0)
                {
                    var escrowAmount = LookupPeriodicValue(account.Debt_escrow_amounts!, date);
                    amount -= escrowAmount;
                }

                accountModel.Transactions.Add(new PerformanceTransaction
                {
                    Date = date,
                    Amount = amount,
                    RunningBalance = cumulative + amount
                });

                cumulative += amount;
            }
        }

        // Accumulate all account transactions into the group's list.
        foreach (var accountGroup in model.AccountGroups)
        {
            var groupTransactions = accountGroup.Accounts.SelectMany(a => a.Transactions).OrderBy(t => t.Date).ToList();

            var cumulative = 0m;
            foreach (var transaction in groupTransactions)
            {
                accountGroup.AllTransactions.Add(new PerformanceTransaction
                {
                    Date = transaction.Date,
                    Amount = transaction.Amount,
                    RunningBalance = cumulative + transaction.Amount
                });

                cumulative += transaction.Amount;
            }
        }

        // Remove any groups that have no transactions (there is always a hidden one)
        var emptyGroups = model.AccountGroups.Where(g => g.AllTransactions.Count() <= 1).ToList();
        foreach (var group in emptyGroups)
            model.AccountGroups.Remove(group);

        return model;
    }

    private static decimal LookupPeriodicValue(LoanAccountPeriodicValue periodicValue, DateTimeOffset date)
    {
        if (!periodicValue.Any())
            throw new Exception("No periodic values");

        var foundValue = 0m;
        foreach (var rate in periodicValue.OrderBy(r => DateTime.Parse(r.Key)))
        {
            if (DateTime.Parse(rate.Key) > date)
                break;
            foundValue = rate.Value;
        }
        return foundValue / 1000m;
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