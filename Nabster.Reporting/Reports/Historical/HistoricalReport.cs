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
public class HistoricalReport(YnabService _ynabService)
{
    public async Task<HistoricalReportModel> Build(string? budgetName)
    {
        var budgetDetail = await _ynabService.Client.GetBudgetDetailAsync(budgetName);
        var accounts = (await _ynabService.Client.GetAccountsAsync(budgetDetail.Id.ToString(), null)!).Data.Accounts.Where(a => !a.Closed);

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

        // Get all the transactions.
        var transactions = (await _ynabService.Client.GetTransactionsAsync(budgetDetail.Id.ToString(), null, null, null)).Data.Transactions.ToList();

        foreach (var account in accounts)
        {
            var accountPrefix = account.Name.Split(' ').First();
            var accountGroupModel = model.AccountGroups.First(g => g.Prefix == accountPrefix);
            var accountModel = accountGroupModel.Accounts.First(a => account.Name == a.Name);

            var cumulative = 0m;
            foreach (var transaction in transactions.Where(t => t.Account_name == account.Name).OrderBy(t => t.Date))
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

            var cumulative = 0m;
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