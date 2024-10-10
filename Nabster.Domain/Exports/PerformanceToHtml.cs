using System.Text;
using Nabster.Domain.Reports;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace Nabster.Domain.Exports;

public static class PerformanceToHtml
{
    private static readonly List<string> _lineColors = ["#003f5c", "#2f4b7c", "#665191", "#a05195", "#d45087", "#f95d6a", "#ff7c43", "#ffa600"];

    public static byte[] Create(PerformanceReport report)
    {
        var html = new StringBuilder();
        html.AppendLine($"<html><body style=\"font-family: monospace;\">");

        var colorMap = new Dictionary<string, string>();
        foreach (var account in report.AccountGroups.SelectMany(g => g.Accounts))
            colorMap[account.Name] = _lineColors[colorMap.Count % _lineColors.Count];

        foreach (var accountGroup in report.AccountGroups.Where(g => g.AllTransactions.Any()))
        {
            // Find the earliest and latest transactions.
            var firstTransaction = accountGroup.AllTransactions.OrderBy(t => t.Date).First();
            var lastTransaction = accountGroup.AllTransactions.OrderBy(t => t.Date).Last();

            var chartSvg = CreateChart(accountGroup, colorMap);

            html.AppendLine($"<div style=\"font-size: x-large; margin-bottom: 8px;\">{accountGroup.Name}</div>");

            // Container for chart and details.
            html.AppendLine("<div style=\"display: flex;\">");

            // Chart on the left.
            html.AppendLine("<div>");
            html.AppendLine(chartSvg);
            html.AppendLine("</div>");

            // Details on the right.
            html.AppendLine("<div style=\"flex: 1\">");
            html.AppendLine($"Begin:&nbsp; {firstTransaction.RunningBalance:C0} ({firstTransaction.Date:M/yyyy})<br>");
            html.AppendLine($"End:&nbsp;&nbsp;&nbsp; {lastTransaction.RunningBalance:C0} ({lastTransaction.Date:M/yyyy})<br>");
            html.AppendLine($"Change: {lastTransaction.RunningBalance - firstTransaction.RunningBalance:C0}<br>");
            html.AppendLine("<br>");

            foreach (var accountName in accountGroup.Accounts.Select(a => a.Name))
                html.AppendLine($"{accountName} <span style='height: 10px;  width: 10px; background-color: {colorMap[accountName]}; border-radius: 50%; display: inline-block;'></span><br>");

            html.AppendLine("</div>");
            html.AppendLine("</div>");
        }

        html.AppendLine($"</body></html>");

        return Encoding.UTF8.GetBytes(html.ToString());
    }

    private static string CreateChart(PerformanceAccountGroup accountGroup, Dictionary<string, string> colorMap)
    {
        var xAxis = new DateTimeAxis
        {
            Position = AxisPosition.Bottom,
            StringFormat = "M/yyyy",
            MinorIntervalType = DateTimeIntervalType.Days,
            IntervalType = DateTimeIntervalType.Days,
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineStyle = LineStyle.None,
            Font = "monospace",
            FontSize = 10
        };

        var minYValue = (double)accountGroup.AllTransactions.Min(t => t.RunningBalance);
        var maxYValue = (double)accountGroup.AllTransactions.Max(t => t.RunningBalance);

        double minimum;
        double maximum;
        double step;

        if (maxYValue > 0 && minYValue > 0)
        {
            minimum = 0;
            maximum = maxYValue * 1.05;
            step = maximum / 10.0;
        }
        else if (maxYValue < 0 && minYValue < 0)
        {
            minimum = minYValue * 1.05;
            maximum = 0;
            step = Math.Abs(minimum) / 10.0;
        }
        else
        {
            minimum = minYValue * 1.05;
            maximum = maxYValue * 1.05;
            step = maximum - minimum / 10.0;
        }

        var yAxis = new LinearAxis
        {
            Position = AxisPosition.Left,
            StringFormat = "C0",
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineStyle = LineStyle.None,
            MinorTickSize = 0,
            Minimum = minimum,
            Maximum = maximum,
            MajorStep = step,
            Font = "monospace",
            FontSize = 10,
        };

        var plotModel = new PlotModel();
        plotModel.Axes.Add(xAxis);
        plotModel.Axes.Add(yAxis);
        plotModel.PlotMargins = new OxyThickness(75, 0, 0, 50);

        AddDataSeries(plotModel, InterpolateMonthly(accountGroup.AllTransactions), "#333333", [3, 3]);
        foreach (var account in accountGroup.Accounts)
            AddDataSeries(plotModel, InterpolateMonthly(account.Transactions), colorMap[account.Name], [0, 0]);

        using var stream = new MemoryStream();
        SvgExporter.Export(plotModel, stream, 450, 450, isDocument: false);

        // Note that we have to remove the UTF8 byte order mask (EFBBBF) when
        // converting the byte array to a string.
        return Encoding.UTF8.GetString(stream.ToArray().Skip(3).ToArray());
    }

    private static List<DateBalance> InterpolateMonthly(List<PerformanceTransaction> data)
    {
        // Sort the input data by date.
        data = [.. data.OrderBy(d => d.Date)];

        if (data.Count == 0)
            return [];

        // Determine the start and end dates.
        var startDate = data.First().Date;
        var endDate = data.Last().Date;

        // Generate a list of dates, one for each month between the start and end dates.
        var interpolatedData = new List<DateBalance>();
        var currentDate = new DateTime(startDate.Year, startDate.Month, 1);

        while (currentDate <= endDate)
        {
            // Find the two closest dates in the input list.
            var previousData = data.LastOrDefault(d => d.Date <= currentDate);
            var nextData = data.FirstOrDefault(d => d.Date > currentDate);

            if (previousData == null || nextData == null)
            {
                // If there is no previous or next data, use the available data point.
                interpolatedData.Add(new DateBalance { Date = currentDate, Balance = previousData?.RunningBalance ?? nextData.RunningBalance });
            }
            else
            {
                // Interpolate the value based on these two dates.
                var totalDays = (nextData.Date - previousData.Date).TotalDays;
                var elapsedDays = (currentDate - previousData.Date).TotalDays;
                var interpolatedAmount = previousData.RunningBalance + (nextData.RunningBalance - previousData.RunningBalance) * (decimal)(elapsedDays / totalDays);

                interpolatedData.Add(new DateBalance { Date = currentDate, Balance = interpolatedAmount });
            }

            // Move to the next month.
            currentDate = currentDate.AddMonths(1);
        }

        return interpolatedData;
    }

    private static void AddDataSeries(PlotModel plotModel, List<DateBalance> transactions, string colorHex, double[] dashes)
    {
        var dataSeries = new FunctionSeries
        {
            Color = OxyColor.Parse(colorHex),
            InterpolationAlgorithm = InterpolationAlgorithms.CanonicalSpline,
            Dashes = dashes,
        };

        foreach (var transaction in transactions)
            dataSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(transaction.Date.Date), (double)transaction.Balance));

        plotModel.Series.Add(dataSeries);
    }

    internal class DateBalance
    {
        public DateTime Date { get; set; }
        public decimal Balance { get; set; }
    }
}