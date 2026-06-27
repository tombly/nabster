using Nabster.Reporting.Reports.Historical.Models;
using Nabster.Reporting.Services;
using Ynab.Api.Client;
using Ynab.Api.Client.Extensions;

namespace Nabster.Reporting.Reports.Historical;

/// <summary>
/// Generates a report of cumulative, grouped, account balances. The report
/// includes an Excel spreadsheet with the data and a (self-contained) HTML
/// file with charts.
/// </summary>
public class HistoricalReport(IEnumerable<IYnabService> _ynabServices)
{
    public async Task<HistoricalReportModel> Build(string? budgetName, bool isDemo)
    {
        var ynabService = _ynabServices.Single(s => s.IsDemo == isDemo)!;

        var budgetDetail = await ynabService.Client.GetBudgetDetailAsync(budgetName);
        var accounts = (await ynabService.Client.GetAccountsAsync(budgetDetail.Id.ToString(), null)!).Data.Accounts.Where(a => !a.Closed);

        // Create our list of account name prefixes that we'll group the
        // accounts by.
        var accountNames = accounts.Select(a => a.Name).ToList();
        var accountPrefixes = accountNames.Select(a => a.Split(' ').First()).Distinct().ToList();

        // Seed the model with one account group per prefix.
        var model = new HistoricalReportModel
        {
            BudgetName = budgetDetail.Name,
            AccountGroups = accountPrefixes.Select(prefix =>
            {
                return new HistoricalAccountGroupModel
                {
                    Name = NameForGroupPrefix(prefix),
                    Prefix = prefix,
                    Accounts = accountNames
                                    .Where(n => n.StartsWith(prefix))
                                    .Select(n => new HistoricalAccountModel { Name = n })
                                    .ToList()
                };
            }).ToList()
        };

        // Only download the past year of transactions. Anything older isn't
        // shown on the report, and downloading the entire history is slow for
        // long-lived budgets.
        var sinceDate = DateTimeOffset.Now.AddDays(-365);
        var transactions = (await ynabService.Client.GetTransactionsAsync(budgetDetail.Id.ToString(), sinceDate, null, null)).Data.Transactions.ToList();

        // Each account's balance at the start of the window, so the running
        // balances stay anchored to absolute account balances instead of the
        // change over the window.
        var startingBalances = new Dictionary<string, decimal>();

        foreach (var account in accounts)
        {
            var accountPrefix = account.Name.Split(' ').First();
            var accountGroupModel = model.AccountGroups.First(g => g.Prefix == accountPrefix);
            var accountModel = accountGroupModel.Accounts.First(a => account.Name == a.Name);

            var accountTransactions = transactions.Where(t => t.Account_name == account.Name).OrderBy(t => t.Date).ToList();

            // An account's current balance is the sum of every transaction it
            // has ever had. Since we only download the past year, derive the
            // balance at the start of the window by subtracting the downloaded
            // transactions from the current balance.
            var startingBalance = account.Balance.FromMilliunits() - accountTransactions.Sum(t => t.Amount.FromMilliunits());
            startingBalances[account.Name] = startingBalance;

            var cumulative = startingBalance;
            foreach (var transaction in accountTransactions)
            {
                var date = transaction.Date;
                var amount = transaction.Amount.FromMilliunits();

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

                accountModel.Transactions.Add(new HistoricalTransactionModel
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

            // Seed the group's running balance with the combined starting
            // balance of its accounts so the totals stay anchored too.
            var cumulative = accountGroup.Accounts.Sum(a => startingBalances.GetValueOrDefault(a.Name));
            foreach (var transaction in groupTransactions)
            {
                accountGroup.AllTransactions.Add(new HistoricalTransactionModel
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

        var foundValue = 0L;
        foreach (var rate in periodicValue.OrderBy(r => DateTime.Parse(r.Key)))
        {
            if (DateTime.Parse(rate.Key) > date)
                break;
            foundValue = rate.Value;
        }
        return foundValue.FromMilliunits();
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
            "HSA" => "Health Savings",
            "EMR" => "Emergency",
            _ => prefix
        };
    }
}