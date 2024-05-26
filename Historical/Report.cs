using Ynab.Api;

namespace Nabster.Historical;

/// <summary>
/// Generates a report of cumulative, grouped, account balances. The report
/// includes an Excel spreadsheet with the data and a (self-contained) HTML
/// file with charts.
/// </summary>
public static class Report
{
    public static async Task Generate(string budgetName, Ynab.Api.Client client, string inputFilePath)
    {
        // Create our list of account groups.
        var accountGroupMap = ReadAccountGroupFile(inputFilePath);

        // Seed the report with the account groups.
        var model = new HistoricalReport
        {
            BudgetName = budgetName,
            AccountGroups = accountGroupMap.Values.Distinct().Select(groupName =>
            {
                return new HistoricalAccountGroup
                {
                    Name = groupName, // e.g. Investments
                    AccountNames = accountGroupMap.Where(g => g.Value == groupName).Select(g => g.Key).ToList()
                };
            }).ToList()
        };

        // Find the budget we're looking for.
        var budgetId = (await client.GetBudgetsAsync(false)).Data.Budgets.FirstOrDefault(b => b.Name == budgetName)?.Id;
        if (budgetId == null)
            throw new Exception($"Budget not found: '{budgetName}'");

        // Get all the accounts so we can figure out which are inactive.
        var accounts = (await client.GetAccountsAsync(budgetId.ToString(), null)).Data.Accounts;

        // Get all the transactions.
        var transactions = (await client.GetTransactionsAsync(budgetId.ToString(), null, null, null)).Data.Transactions.ToList();

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
                        new HistoricalTransaction
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

        // Write output to files.
        Excel.Create(model);
        Html.Create(model);
    }

    private static Dictionary<string, string> ReadAccountGroupFile(string inputFilePath)
    {
        var accountGroups = new Dictionary<string, string>();
        var filePath = Path.Combine(inputFilePath, "AccountGroups.csv");
        var lines = File.ReadAllLines(filePath);
        foreach (var line in lines)
        {
            var accountName = line.Split(',').First();
            var accountGroup = line.Split(',').Last();
            accountGroups[accountName] = accountGroup;
        }
        return accountGroups;
    }
}