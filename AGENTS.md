# AGENTS

Nabster is an app for analyzing and reporting on your personal finances in [YNAB](https://www.youneedabudget.com) written in C# and built on Azure. It provides a suite of reports for historical account balances, monthly spending, and budget planning, plus a daily summary that is emailed automatically on a schedule.

## Overview

Users interact with Nabster via the CLI. To generate ad-hoc reports, the CLI retrieves the user's financial data from the YNAB API, generates the report, and exports it to the user's desktop as an Excel spreadsheet or self-contained HTML file.

The daily summary report is produced by a function app. A logic app calls the function app on a recurring schedule; the function app pulls the latest activity from the YNAB API, formats a short summary, and emails it to the configured recipients via SMTP2GO. The same daily report can also be generated locally from the CLI, either by building it directly or by invoking the function app over HTTP.

All secrets are stored in a key vault. The function app and logic app use system-assigned managed identities to authenticate with the key vault. The CLI uses the default Azure credential to authenticate with the key vault. Similarly, when running the function app locally, it also uses the default Azure credential to access the key vault. Both the function app and logic app use a consumption plan to minimize costs.

## Code structure

The code is organized into four projects:

- Nabster.Reporting - a set of report generators that retrieve data from the YNAB API and generate reports in plain text, HTML, or Excel.

- Nabster.Server - an Azure Functions app that exposes an `IncomingMessage` HTTP endpoint. When called (typically by the logic app), it generates the daily report and optionally emails it to one or more recipients via SMTP2GO.

- Nabster.Cli - a command-line interface to the reporting features.

- Nabster.Mcp - an MCP server that allows agents to retrieve budget and account data from the YNAB API.

## Reporting feature

### Historical Report

This report uses charts to visualize your account balances over the past year. It takes a list of account groups as input and for each group, shows a timeseries line chart of the total value of each group. The output is a self-contained HTML file with inline SVG charts.

This report is especially useful to visualize the performance of groups of investments, such as for retirement or college.

The typical use case for this is to group your investment accounts in various ways and then visualize their performance over time.

### Planning Report

This report lists each budget category along with any goals and the yearly and monthly cost for each. The purpose is to figure out for each category what the typical monthly cost is over the long term, not at a specific point in time (which is what YNAB's website is really good at). This can then be used for long-term cash flow planning.

If a category has a repeating target then the monthly cost is calculated directly from that. For example, if the target is $120 annually then the monthly cost is calculated as $10 (even if this year you've already funded the full amount).

It generates the report and saves it as either an Excel spreadsheet or self-contained HTML file to your desktop.

### Spend Report

This report lists all transactions for a specific category and month. The transactions are grouped by the memo field. This is particularly useful if you have one category for your discretionary spending, but still want to be able to sub-categorize your spending.

### Daily Report

This report is a short plain-text summary of the current month's activity for a selected set of categories, showing the spending amount and percentage of the monthly budget used. It is intended to be delivered as an email on a schedule via the function app, but can also be generated locally from the CLI.

## Build and test commands

### Prerequisites
- .NET 10.0 SDK
- Azure CLI (for Key Vault access)
- Azure Functions Core Tools (for local function development)

### Build Commands

**Build entire solution:**
```bash
dotnet build
```

**Build specific projects:**
```bash
# Build the function app
dotnet build Nabster.Server/Nabster.Server.csproj

# Build CLI
dotnet build Nabster.Cli/Nabster.Cli.csproj

# Build reporting
dotnet build Nabster.Reporting/Nabster.Reporting.csproj
```

**Clean and build:**
```bash
dotnet clean
dotnet build
```

**Release build for deployment:**
```bash
dotnet publish Nabster.Server --configuration Release
```

### Running the Application

**CLI Interface:**
```bash
# Generate reports
dotnet run --project Nabster.Cli -- report planning
dotnet run --project Nabster.Cli -- report historical
dotnet run --project Nabster.Cli -- report spend --category-name "Groceries" --month "2025-09"
dotnet run --project Nabster.Cli -- report daily --category-names Discretionary Groceries Unplanned

# Generate the daily report by calling the function app
dotnet run --project Nabster.Cli -- report daily --url http://localhost:7071/api/IncomingMessage
```

**Azure Functions (Local Development):**
```bash
cd Nabster.Server
func host start
```

### VS Code Tasks
The project includes predefined VS Code tasks (`.vscode/tasks.json`):
- `dotnet: build` - Build entire solution
- `build (functions)` - Build the function app project
- `publish (functions)` - Create release build for deployment

### Testing

**Manual Testing:**
Current testing is performed manually using:

1. **VS Code Launch Configurations** (`.vscode/launch.json`):
   - `Report: Historical` - Generate historical reports
   - `Report: Planning` - Generate planning reports
   - `Report: Spend` - Generate spending reports
   - `Report: Daily` - Generate the daily report directly via the CLI
   - `Report: Daily (server)` - Generate the daily report via the local function app
   - `Local Server` - Attach the debugger to a running local function app

2. **CLI Commands:**
   ```bash
   # Generate a daily report directly
   dotnet run --project Nabster.Cli -- report daily --category-names Discretionary Groceries

   # Test report generation with demo data
   dotnet run --project Nabster.Cli -- report planning --demo
   ```

3. **Demo Mode:**
   Reports support a `--demo` flag to test with mock data without requiring real YNAB access.

## Code style guidelines

### General Principles
- **Target Framework:** .NET 10.0 with C# 13 features
- **Nullable Reference Types:** Enabled across all projects
- **Implicit Usings:** Enabled for cleaner code
- **Code Analysis:** Follow Microsoft's recommended analyzer rules

### Naming Conventions
- **Classes:** PascalCase (`DailyReport`, `EmailService`, `ReportCommand`)
- **Methods:** PascalCase (`GetAccountsAsync`, `Build`, `Send`)
- **Properties:** PascalCase (`BudgetName`, `MonthlyTotal`, `Balance`)
- **Fields:** camelCase with underscore prefix (`_ynabService`, `_logger`, `_emailService`)
- **Parameters:** camelCase (`budgetName`, `categoryName`, `message`)
- **Local Variables:** camelCase (`report`, `response`, `transactions`)

### Code Organization
- **Dependency Injection:** Use constructor injection consistently with primary constructors
- **Async/Await:** Use async patterns for all I/O operations, append "Async" to method names
- **Extension Methods:** Group in dedicated `Extensions` folders
- **Models:** Separate data models in `Models` folders per feature
- **Services:** Business logic in `Services` folders
- **Configuration:** Configuration classes in `Config` folders

### Performance
- Prefer to use arrays for collections.
- Only use `List`s for collections that are actually modified.
- Use `HashSet`s whenever the code calls `Contains()` on the collection.

### File Structure
```
ProjectName/
├── Config/
│   ├── ProjectOptions.cs
│   └── DependencyModule.cs
├── Extensions/
│   └── SpecificExtensions.cs
├── Models/
│   └── FeatureModels.cs
├── Services/
│   └── FeatureService.cs
└── ProjectName.csproj
```

### Documentation Standards
- **XML Comments:** Required for all public APIs
- **Method Documentation:** Include `<summary>`, `<param>`, and `<returns>` tags
- **Class Documentation:** Describe purpose, usage patterns, and dependencies
- **Example Documentation:**
```csharp
/// <summary>
/// Generates a daily summary of budget category activity for the current month.
/// </summary>
/// <param name="_ynabServices">Registered YNAB services (real and mock)</param>
public class DailyReport(IEnumerable<IYnabService> _ynabServices)
{
    /// <summary>
    /// Builds a plain-text summary of activity per category.
    /// </summary>
    /// <param name="budgetName">Optional budget name; defaults to the last-used budget</param>
    /// <param name="isDemo">If true, the report is generated from mock data</param>
    /// <param name="categoryNames">Optional category filter; all categories are included if omitted</param>
    /// <returns>The formatted report text</returns>
    public async Task<string> Build(string? budgetName, bool isDemo, string[]? categoryNames = null)
    {
        // Implementation
    }
}
```

### Error Handling
- **Exception Handling:** Use try-catch blocks for external API calls
- **Logging:** Log errors with appropriate log levels (Error, Warning, Information)
- **Graceful Degradation:** Return meaningful error messages to users
- **Resource Cleanup:** Use `using` statements for disposable resources

### Performance Guidelines
- **Async Operations:** Use `ConfigureAwait(false)` for library code
- **Memory Management:** Dispose of resources properly, especially in Azure Functions
- **Caching:** Consider caching for expensive operations (YNAB API responses)
- **Streaming:** Use streaming for large data sets when possible

### Security Practices
- **Input Validation:** Validate all user inputs and API parameters
- **Secrets Management:** Never hardcode secrets, use Azure Key Vault
- **Logging Security:** Don't log sensitive financial data or secrets
- **Authentication:** Use managed identities where possible
