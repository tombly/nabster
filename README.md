# nabster
An app for analyzing and reporting on your financial data in [YNAB](https://www.youneedabudget.com) written in C# and built on Azure.

## What it does

This app generates different kinds of financial reports that are useful in budgeting and planning. The reports are delivered as Excel spreadsheets, web pages, or are pushed directly to users via SMS.

### Category Activity Report

This simple report sends an SMS with the activity for a specific category for the current month.

### Performance Report

This report shows the performance of your accounts over the past year. It takes a list of account groups as input and for each group, shows a timeseries line chart of the total value of each group. The output is a self-contained HTML file with inline SVG charts.

The typical use case for this is to group your investment accounts in various ways and then visualize their performance over time.

### Planning Report

This report lists each budget category along with any goal information and its monthly cost. The purpose is to figure out for each category what the typical monthly cost is over the long term, not at a specific point in time (which is what YNAB's website is really good at). This can then be used for long-term cash flow planning.

If a category has repeating target then the monthly cost is calculated directly from that. For example, if the target is $120 annually then the monthly cost is calculated as $10 (even if this year you've already funded the full amount).

If a category does not have a repeating target then we still want to capture it in the report so we calculate the monthly cost based on the remaining time and money needed to reach the target. For example, if the target is $600 by June 1st and the report is generated in January, and half has already been funded at that point, then the monthly cost is (($600 - $300) / 6) = $50. This differs from how the repeating targets are calculated (which ignore how much as been funded) but its more accurate for these sorts of one-off goals.

It generates the report and saves it as an Excel file to your Desktop. Here's what it looks like:

| CategoryGroup | Category | GoalCadence | GoalDay | GoalTarget | MonthlyCost | GoalPctComplete |
|-------------------|----------|-------------|---------|------------|-------------|------------------------|
| Fixed Monthly | Mortgage | Monthly | 3rd | $400.00 | $400.00 | 100% |
| Fixed Yearly | Car Registration | Yearly | Feb-28 | $500.00 | $41.67 | 100% |
| Goals | Vacation | None | Jun-15 | $1000.00 | $150.00 | 30% |
...

### Spend Report

This report lists all transactions for a specific category and month. The transactions are grouped by the memo field. This is particularly useful if you have one category for your discretionary spending, but still want to be able to sub-categorize your spending.

## How to use
Clone the repo and open up the folder (I use VS Code for Mac with the C# Dev Kit, Azure, and Function App extensions).

**1. Edit the configuration files**

Edit the `Deployments/infra.bicepparam` file to add your YNAB personal access token, Twilio credentials, and your YNAB budget info. If you want to run the CLI via VS Code, then edit the `.vscode/launch.json` to adjust the parameters for your YNAB budget info.

**2. Deploy the infrastructure**

`cd` into the `Deployments` folder and run the `deploy.sh` script. Alternatively, the function app can also be run locally.

**3. Run the CLI**

Run the app via VS Code or directly via the command line.

## How it works

The reports are generated by an Azure Function that is called by the console app for on-demand reports and by a logic app for scheduled reports. All secrets are stored in a key vault. The function app and logic app use system-assigned managed identities to authenticate with the key vault. The CLI uses the default credential to authenticate with the key vault to obtain the necessary secrets to run the function app. Similarly, when running the function app locally, it also uses the default credential to access the key vault. Both the function app and logic app use a consumption plan so the app costs about $10/mo.

### How the YNAB client was created
I used [NSwag Studio](https://github.com/RicoSuter/NSwag/wiki/NSwagStudio) to generate the client for the YNAB API (I couldn't find an existing one that supports .NET 8). These are the configuration options used:
- Namespace: Ynab.Api
- ☑ Generate optional schema properties as nullable
- ☑ Generate nullable Reference Type annotations
- Select: SystemTextJson
- Output: Path to a single .cs file

It generates one big file so I used [Rider](https://www.jetbrains.com/rider/) to automatically separate all the classes and types into individual files (and I also renamed some of the classes to be more descriptive).