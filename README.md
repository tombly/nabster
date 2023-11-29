# nabster
A console app for analyzing and reporting on your financial data in [YNAB](https://www.youneedabudget.com) written in C#.

## What it does
This app generates a monthly spend report that lists each budget category along with any goal information and its monthly cost. The purpose is to figure out for each category what the typical monthly cost is over the long term, not at a specific point in time (which is what YNAB's website is really good at). This can then be used for cash flow planning.

If a category has repeating target then the monthly cost is calculated directly from that. For example, if the target is $120 annually then the monthly cost is calculated as $10 (even if this year you've already funded the full amount).

If a category does not have a repeating target then we still want to capture it in the report so we calculate the monthly cost based on the remaining time and money needed to reach the target. For example, if the target is $600 by June 1st and the report is generated in January, and half has already been funded at that point, then the monthly cost is (($600 - $300) / 6) = $50. This differs from how the repeating targets are calculated (which ignore how much as been funded) but its more accurate for these sorts of one-off goals.

## How to use
Clone, build, and run the app (I'm using the C# Dev Kit extension for VS Code on Mac). It expects two arguments, the name of the budget and a personal access token ([details](https://api.ynab.com/)):

``` bash
$ dotnet run "My Budget" "oEjna39mUK9PsfgC33EQwoegHqXPMRsei9nbaeKNenb"
```

It generates the report and saves it as an Excel file to your Desktop. Here's what it looks like:

| CategoryGroup | Category | GoalCadence | GoalDay | GoalTarget | MonthlyCost | GoalPctComplete |
|-------------------|----------|-------------|---------|------------|-------------|------------------------|
| Fixed Monthly | Mortgage | Monthly | 3rd | $400.00 | $400.00 | 100% |
| Fixed Yearly | Car Registration | Yearly | Feb-28 | $500.00 | $41.67 | 100% |
| Goals | Vacation | None | Jun-15 | $1000.00 | $150.00 | 30% |
...

## How it was built
I used [NSwag Studio](https://github.com/RicoSuter/NSwag/wiki/NSwagStudio) to generate the client for the YNAB API (I couldn't find an existing one that supports .NET 8). These are the configuration options used:
- Namespace: Ynab.Api
- ☑ Generate optional schema properties as nullable
- ☑ Generate nullable Reference Type annotations
- Select: SystemTextJson
- Output: Path to a single .cs file

It generates one big file so I used [Rider](https://www.jetbrains.com/rider/) to automatically separate all the classes and types into individual files (and I also renamed some of the classes to be more descriptive).