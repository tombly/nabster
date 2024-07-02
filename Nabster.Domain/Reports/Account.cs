using Nabster.Domain.Exceptions;
using Nabster.Domain.Extensions;
using Ynab.Api.Client;

namespace Nabster.Domain.Reports;

/// <summary>
/// Generates a simple report that shows the balances of all accounts that match
/// the provided account name.
/// </summary>
public class Account(YnabApiClient _ynabClient)
{
    public async Task<AccountReport> Generate(string? budgetName, string accountName)
    {
        var budgetDetail = await _ynabClient.GetBudgetDetailAsync(budgetName);

        var accounts = await _ynabClient.GetAccountsAsync(budgetDetail.Id.ToString(), null);
        var matchingAccounts = accounts.Data.Accounts
                                .Where(a => !a.Deleted && !a.Closed)
                                .Where(a => a.Name.Contains(accountName, StringComparison.OrdinalIgnoreCase));

        return new AccountReport
        {
            Name = accountName,
            Balances = matchingAccounts.Select(a => new AccountBalance
            {
                Name = a.Name,
                Balance = a.Balance / 1000m
            }).ToList()
        };

        throw new AccountNotFoundException(accountName);
    }
}

#region Models

public class AccountReport
{
    public string Name { get; set; } = string.Empty;

    public List<AccountBalance> Balances { get; set; } = new List<AccountBalance>();
}

public class AccountBalance
{
    public string Name { get; set; } = string.Empty;

    public decimal Balance { get; set; }
}

#endregion