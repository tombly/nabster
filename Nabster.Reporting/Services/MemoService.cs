using System.Text;
using System.Text.RegularExpressions;
using Ynab.Api.Client;
using Ynab.Api.Client.Extensions;

namespace Nabster.Reporting.Services;

/// <summary>
/// Provides services for updating transaction memos in YNAB.
/// </summary>
public partial class MemoService(IYnabService _ynabService)
{
    private const string CategoryMarker = "🅒";
    private const string MemoMarker = "🅜";

    /// <summary>
    /// Retrieves and updates transactions from YNAB since a specified date.
    /// </summary>
    /// <param name="sinceDate">The date from which to retrieve transactions. Only transactions on or after this date will be retrieved.</param>
    public async Task UpdateTransactionsAsync(DateTimeOffset sinceDate)
    {
        // Get the budget detail
        var budgetDetail = await _ynabService.Client.GetBudgetDetailAsync(null);
        
        // Get a list of all transactions since the specified date
        var transactions = (await _ynabService.Client.GetTransactionsAsync(budgetDetail.Id.ToString(), sinceDate, null, null)).Data.Transactions;
        
        // Build a lookup dictionary for categories
        var categoryLookup = new Dictionary<Guid, (string GroupName, string CategoryName)>();
        foreach (var category in budgetDetail.Categories!)
        {
            // Skip deleted, hidden, or internal categories
            if (category.Deleted || category.Hidden)
                continue;

            // Skip categories in the "Archive" group, as these are typically not active categories.
            if (category.Name.Equals("Archive", StringComparison.OrdinalIgnoreCase))
                continue;

            var groupName = budgetDetail.Category_groups!.FirstOrDefault(g => g.Id == category.Category_group_id)?.Name ?? string.Empty;
            
            // Skip internal category groups
            if (groupName.Equals("Internal Master Category", StringComparison.OrdinalIgnoreCase) ||
                groupName.Equals("Credit Card Payments", StringComparison.OrdinalIgnoreCase))
                continue;
                
            categoryLookup[category.Id] = (groupName, category.Name);
        }
        
        // Collect transactions to update
        var transactionsToUpdate = new List<SaveTransactionWithIdOrImportId>();
        
        // Process each transaction
        foreach (var transaction in transactions)
        {
            // Skip transactions with subtransactions
            if (transaction.Subtransactions != null && transaction.Subtransactions.Any())
            {
                // Check subtransactions and warn about any that don't have expected memo format
                foreach (var subtransaction in transaction.Subtransactions)
                {
                    if (subtransaction.Category_id != null && subtransaction.Category_id != Guid.Empty)
                    {
                        if (categoryLookup.TryGetValue(subtransaction.Category_id.Value, out var categoryInfo))
                        {
                            var decoded = DecodeMemo(subtransaction.Memo ?? string.Empty);
                            var expectedPrefix = $"{RemoveNonAlphanumeric(categoryInfo.GroupName)}{RemoveNonAlphanumeric(categoryInfo.CategoryName)}";
                            
                            // Check if memo is in expected format
                            if (decoded.CategoryName != expectedPrefix)
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine($"WARNING: Subtransaction has unexpected memo format:");
                                Console.WriteLine($"  Date: {transaction.Date:yyyy-MM-dd}");
                                Console.WriteLine($"  Payee: {transaction.Payee_name}");
                                Console.WriteLine($"  Category: {categoryInfo.GroupName} / {categoryInfo.CategoryName}");
                                Console.WriteLine($"  Current memo: {subtransaction.Memo ?? "(empty)"}");
                                Console.WriteLine($"  Expected format: {CategoryMarker}[{expectedPrefix}]");
                                Console.ResetColor();
                                Console.WriteLine();
                            }
                        }
                    }
                }
                continue;
            }
            
            // Process parent transaction only (no subtransactions)
            var needsUpdate = CheckNeedsMemoUpdate(
                transaction.Category_id,
                transaction.Memo,
                categoryLookup,
                out var newMemo);
            
            if (needsUpdate)
            {
                Console.WriteLine($"Transaction: {transaction.Date:yyyy-MM-dd} - {transaction.Payee_name}");
                Console.WriteLine($"  Before: {transaction.Memo ?? "(empty)"}");
                Console.WriteLine($"  After:  {newMemo}");
                Console.WriteLine();
                
                transactionsToUpdate.Add(new SaveTransactionWithIdOrImportId
                {
                    Id = transaction.Id,
                    Memo = newMemo
                });
            }
        }
        
        // Batch update all transactions
        if (transactionsToUpdate.Count > 0)
        {
            Console.WriteLine($"Updating {transactionsToUpdate.Count} transaction(s)...");
            
            var wrapper = new PatchTransactionsWrapper
            {
                Transactions = transactionsToUpdate
            };
            
            await _ynabService.Client.UpdateTransactionsAsync(budgetDetail.Id.ToString(), wrapper, CancellationToken.None);
            
            Console.WriteLine("Update complete!");
        }
        else
        {
            Console.WriteLine("No transactions to update.");
        }
    }

    /// <summary>
    /// Checks if a transaction or subtransaction needs its memo updated.
    /// </summary>
    /// <returns>True if the memo needs updating; otherwise false.</returns>
    private static bool CheckNeedsMemoUpdate(
        Guid? categoryId,
        string? existingMemo,
        Dictionary<Guid, (string GroupName, string CategoryName)> categoryLookup,
        out string? newMemo)
    {
        newMemo = null;
        
        // Skip if no category assigned
        if (categoryId == null || categoryId == Guid.Empty)
            return false;
            
        // Get category info - this won't be found for deleted/hidden/internal
        // categories, which we already skipped in the lookup.
        if (!categoryLookup.TryGetValue(categoryId.Value, out var categoryInfo))
            return false;
        
        // Decode existing memo to preserve user content
        var existingUserMemo = DecodeMemo(existingMemo ?? string.Empty).UserMemo;
        
        // Encode new memo
        newMemo = EncodeMemo(categoryInfo.GroupName, categoryInfo.CategoryName, existingUserMemo);
        
        // Return true if memo has changed
        return newMemo != existingMemo;
    }

    /// <summary>
    /// Encodes category and memo information into a formatted string.
    /// </summary>
    /// <param name="categoryGroup">The category group name</param>
    /// <param name="categoryName">The category name</param>
    /// <param name="userMemo">Optional user-provided memo content</param>
    /// <returns>Encoded memo string in format: 🅒[GROUP+CATEGORY] 🅜[User memo]</returns>
    private static string EncodeMemo(string categoryGroup, string categoryName, string? userMemo)
    {
        var cleanGroup = RemoveNonAlphanumeric(categoryGroup);
        var cleanCategory = RemoveNonAlphanumeric(categoryName);
        
        var sb = new StringBuilder();
        sb.Append($"{CategoryMarker}[{cleanGroup}{cleanCategory}]");
        
        if (!string.IsNullOrWhiteSpace(userMemo))
        {
            sb.Append($" {MemoMarker}[{userMemo.Trim()}]");
        }
        
        return sb.ToString();
    }

    /// <summary>
    /// Decodes a formatted memo string back into its components.
    /// </summary>
    /// <param name="memo">The encoded memo string</param>
    /// <returns>Decoded category group, category name, and user memo</returns>
    private static (string CategoryGroup, string CategoryName, string? UserMemo) DecodeMemo(string memo)
    {
        if (string.IsNullOrWhiteSpace(memo))
            return (string.Empty, string.Empty, null);
        
        var categoryMatch = CategoryRegex().Match(memo);
        var memoMatch = MemoRegex().Match(memo);
        
        if (categoryMatch.Success)
        {
            var combined = categoryMatch.Groups[1].Value;
            var userMemo = memoMatch.Success ? memoMatch.Groups[1].Value : null;
            
            return (string.Empty, combined, userMemo);
        }
        
        // If not in our format, treat entire memo as user content
        return (string.Empty, string.Empty, memo);
    }

    /// <summary>
    /// Removes all non-alphanumeric characters from a string.
    /// </summary>
    private static string RemoveNonAlphanumeric(string input)
    {
        return AlphanumericRegex().Replace(input, string.Empty);
    }

    [GeneratedRegex(@"🅒\[([^\]]+)\]")]
    private static partial Regex CategoryRegex();
    
    [GeneratedRegex(@"🅜\[([^\]]+)\]")]
    private static partial Regex MemoRegex();
    
    [GeneratedRegex(@"[^a-zA-Z0-9]")]
    private static partial Regex AlphanumericRegex();
}
