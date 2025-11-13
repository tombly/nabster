# AGENTS

Nabster is an app for analyzing and reporting on your personal finances in [YNAB](https://www.youneedabudget.com) written in C# and built on Azure. There are two features:

- An AI chatbot with integrated SMS that can answer questions about your financial data in YNAB.

- A suite of reports for historical account balances, monthly spending, and budget planning.

## Overview

Users interact with Nabster via the CLI or SMS. To generate reports, the user uses the CLI. To chat, the user can use either the CLI or SMS. When the user requests a report from the CLI, it retrieves the user's financial data from the YNAB API, generates the report, and then exports it to the user's desktop as an Excel spreadsheet or self-contained HTML file.

When the user asks a question via the CLI, the prompt is forwarded to OpenAI which queries the YNAB API for the necessary financial data and then returns the answer via the console or SMS. When the user asks a question via SMS, Twilio calls a web hook that is handled by a function app which then forwards the prompt to OpenAI which queries the YNAB API and then calls back to Twilio to send the answer to the user.

All secrets are stored in a key vault. The function app and logic app use system-assigned managed identities to authenticate with the key vault. The CLI uses the default Azure credential to authenticate with the key vault. Similarly, when running the function app locally, it also uses the default Azure credential to access the key vault. Both the function app and logic app use a consumption plan to minimize costs.

## Code structure

The code is organized into four projects:

- Nabster.Chat - uses Semantic Kernel backed by OpenAI with a plug-in that allows it to call the YNAB API to answer questions about the user's financial data with responses sent via SMS. This is used by both the function app and the CLI.

- Nabster.Chat.Functions - a wrapper around Nabster.Chat that implements Twilio web hooks allowing users to submit questions via SMS.

- Nabster.Reporting - a set of report generators that retrieve data from the YNAB API and generate reports in HTML or Excel.

- Nabster.Cli - a command-line interface to both the chat and reporting features.

## Reporting feature

### Historical Report

This report uses charts to visualize your account balances over the past year. It takes a list of account groups as input and for each group, shows a timeseries line chart of the total value of each group. The output is a self-contained HTML file with inline SVG charts.

This report is especially useful to visualize the performance of groups of investments, such as for retirement or college.

The typical use case for this is to group your investment accounts in various ways and then visualize their performance over time.

### Planning Report

This report lists each budget category along with any goals and the yearly and monthly cost for each. The purpose is to figure out for each category what the typical monthly cost is over the long term, not at a specific point in time (which is what YNAB's website is really good at). This can then be used for long-term cash flow planning.

If a category has a repeating target then the monthly cost is calculated directly from that. For example, if the target is $120 annually then the monthly cost is calculated as $10 (even if this year you've already funded the full amount).

It generates the report and saves it as either an Excel spreadsheet or self-contained HTML file to your desktop. Here's what it looks like:

### Spend Report

This report lists all transactions for a specific category and month. The transactions are grouped by the memo field. This is particularly useful if you have one category for your discretionary spending, but still want to be able to sub-categorize your spending.

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
# Build chat functionality
dotnet build Nabster.Chat/Nabster.Chat.csproj

# Build Azure Functions
dotnet build Nabster.Chat.Functions/Nabster.Chat.Functions.csproj

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
dotnet publish Nabster.Chat.Functions --configuration Release
```

### Running the Application

**CLI Interface:**
```bash
# Chat directly with the AI
dotnet run --project Nabster.Cli -- chat direct --message "What is my checking balance?"

# Generate reports
dotnet run --project Nabster.Cli -- report planning
dotnet run --project Nabster.Cli -- report historical
dotnet run --project Nabster.Cli -- report spend --category-name "Groceries" --month "2025-09"
```

**Azure Functions (Local Development):**
```bash
cd Nabster.Chat.Functions
func host start
```

### VS Code Tasks
The project includes predefined VS Code tasks (`.vscode/tasks.json`):
- `dotnet: build` - Build entire solution
- `build (functions)` - Build Azure Functions project
- `publish (functions)` - Create release build for deployment

### Testing

**Manual Testing:**
Current testing is performed manually using:

1. **VS Code Launch Configurations** (`.vscode/launch.json`):
   - `Chat: Direct` - Test AI responses directly
   - `Chat: Local` - Test Azure Functions locally
   - `Report: Planning` - Generate planning reports
   - `Report: Historical` - Generate historical reports
   - `Report: Spend` - Generate spending reports

2. **CLI Commands:**
   ```bash
   # Test chat functionality
   dotnet run --project Nabster.Cli -- chat direct --message "Test message"
   
   # Test report generation with demo data
   dotnet run --project Nabster.Cli -- report planning --demo
   ```

3. **Demo Mode:**
   All commands support `--demo` flag to test with mock data without requiring real YNAB access.

## Code style guidelines

### General Principles
- **Target Framework:** .NET 10.0 with C# 13 features
- **Nullable Reference Types:** Enabled across all projects
- **Implicit Usings:** Enabled for cleaner code
- **Code Analysis:** Follow Microsoft's recommended analyzer rules

### Naming Conventions
- **Classes:** PascalCase (`ChatService`, `YnabPlugin`, `ReportCommand`)
- **Methods:** PascalCase (`GetAccountsAsync`, `BuildReport`, `Reply`)
- **Properties:** PascalCase (`BudgetName`, `MonthlyTotal`, `Balance`)
- **Fields:** camelCase with underscore prefix (`_ynabService`, `_logger`, `_chatCompletionService`)
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
/// Provides AI-powered financial analysis capabilities using Semantic Kernel.
/// </summary>
/// <param name="_chatCompletionService">Azure OpenAI chat completion service</param>
/// <param name="_ynabService">YNAB API client wrapper</param>
public class ChatService(ChatCompletionService _chatCompletionService, YnabService _ynabService)
{
    /// <summary>
    /// Processes a natural language query and returns financial insights.
    /// </summary>
    /// <param name="message">The user's question about their finances</param>
    /// <param name="logger">Logger for tracking operations and errors</param>
    /// <returns>AI-generated response based on YNAB data</returns>
    public async Task<string> Reply(string message, ILogger logger)
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

### Example Code Style
```csharp
namespace Nabster.Chat.Services;

/// <summary>
/// Provides AI-powered chat functionality for financial queries.
/// </summary>
public class ChatService(
    ChatCompletionService _chatCompletionService,
    YnabService _ynabService,
    SmsService _smsService)
{
    /// <summary>
    /// Processes a user message and returns an AI-generated response.
    /// </summary>
    public async Task<string> Reply(string message, ILogger logger)
    {
        logger.LogInformation("Processing message: {Message}", message);
        
        try
        {
            var kernel = CreateKernel();
            var history = CreateChatHistory(message);
            
            var response = await _chatCompletionService.GetChatMessageContentAsync(
                history,
                executionSettings: new OpenAIPromptExecutionSettings
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                },
                kernel: kernel);
                
            return response.Content ?? "I couldn't process that request.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing message: {Message}", message);
            return "I'm having trouble right now. Please try again later.";
        }
    }
    
    private Kernel CreateKernel()
    {
        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton(_chatCompletionService);
        var kernel = builder.Build();
        
        kernel.Plugins.AddFromObject(new YnabPlugin(_ynabService.Client));
        return kernel;
    }
}
```
