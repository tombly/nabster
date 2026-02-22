using System.CommandLine;
using Nabster.Reporting.Services;
using Spectre.Console;

namespace Nabster.Cli.Commands;

internal sealed class MemoCommand : Command
{
    public MemoCommand(IAnsiConsole ansiConsole, IEnumerable<IYnabService> ynabServices, MemoService memoService)
        : base("memo", "Update transaction memos in YNAB.")
    {
        Options.Add(new Option<string>("--since-date") { Description = "The date from which to retrieve transactions (YYYY-MM-DD).", Required = true });
        Options.Add(new Option<bool>("--demo") { Description = "Run with demo data." });

        SetAction(async parseResult =>
        {
            await ansiConsole.Status().StartAsync("Updating transaction memos", async ctx =>
            {
                var sinceDateStr = parseResult.GetValue<string>("--since-date");
                var isDemo = parseResult.GetValue<bool>("--demo");

                if (!DateTimeOffset.TryParse(sinceDateStr, out var sinceDate))
                {
                    ansiConsole.MarkupLine("[red]Invalid date format. Use YYYY-MM-DD.[/]");
                    return;
                }

                await memoService.UpdateTransactionsAsync(sinceDate);
                
                ansiConsole.MarkupLine("[green]Transaction memos updated successfully.[/]");
            });
        });
    }
}
