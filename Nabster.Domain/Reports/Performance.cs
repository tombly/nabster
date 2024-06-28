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
    public async Task<PerformanceReport> Generate(string? budgetName, List<string> groups)
    {
        var budgetDetail = await _ynabClient.GetBudgetDetailAsync(budgetName);

        // Create our list of account groups.
        var accountGroupMap = CreateMapFromGroupList(groups);

        // Seed the report with the account groups.
        var model = new PerformanceReport
        {
            BudgetName = budgetDetail.Name,
            AccountGroups = accountGroupMap.Values.Distinct().Select(groupName =>
            {
                return new PerformanceAccountGroup
                {
                    Name = groupName, // e.g. Investments
                    AccountNames = accountGroupMap.Where(g => g.Value == groupName).Select(g => g.Key).ToList()
                };
            }).ToList()
        };

        // Get all the accounts so we can figure out which are inactive.
        var accounts = (await _ynabClient.GetAccountsAsync(budgetDetail.Id.ToString(), null)!).Data.Accounts;

        // Get all the transactions.
        var transactions = (await _ynabClient.GetTransactionsAsync(budgetDetail.Id.ToString(), null, null, null)).Data.Transactions.ToList();

        // Group the transactions into the account groups.
        foreach (var transaction in transactions)
        {
            var accountName = transaction.Account_name;
            if (accounts.Any(a => a.Name == accountName && a.Closed == false))
            {
                // Some accounts may not be in a group.
                if (accountGroupMap.TryGetValue(accountName, out string? groupName))
                {
                    model.AccountGroups.First(g => g.Name == groupName).Transactions.Add(
                        new PerformanceTransaction
                        {
                            Date = transaction.Date,
                            Amount = transaction.Amount / 1000m
                        });
                }
            }
        }

        // Calculate cumulative amounts.
        foreach (var accountGroup in model.AccountGroups)
        {
            var cumulative = 0m;
            foreach (var transaction in accountGroup.Transactions)
            {
                cumulative += transaction.Amount;
                transaction.CumulativeAmount = cumulative;
            }
        }

        // Trim the transactions to the past year (after we've calculated the
        // cumulative amount).
        foreach (var accountGroup in model.AccountGroups)
            accountGroup.Transactions = accountGroup.Transactions.Where(t => t.Date > DateTime.Now.AddDays(-365)).ToList();

        return model;
    }

    private static Dictionary<string, string> CreateMapFromGroupList(List<string> groups)
    {
        var accountGroups = new Dictionary<string, string>();
        foreach (var line in groups)
        {
            var accountName = line.Split(',').First();
            var accountGroup = line.Split(',').Last();
            accountGroups[accountName] = accountGroup;
        }
        return accountGroups;
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
    public List<string> AccountNames { get; set; } = [];
    public List<PerformanceTransaction> Transactions { get; set; } = [];
}

public class PerformanceTransaction
{
    public DateTimeOffset Date { get; set; }
    public decimal Amount { get; set; }
    public decimal CumulativeAmount { get; set; }
}

#endregion