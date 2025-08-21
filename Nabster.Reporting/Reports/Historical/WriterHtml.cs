using System.Globalization;
using System.Text;
using Nabster.Reporting.Reports.Historical.Models;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace Nabster.Reporting.Reports.Historical;

public static class WriterHtml
{
    private static readonly List<string> _lineColors = ["#003f5c", "#d45087", "#665191", "#a05195", "#2f4b7c", "#f95d6a", "#ff7c43", "#ffa600"];

    public static byte[] ToHtml(this HistoricalReportModel report)
    {
        var charts = FromPerformanceReport(report);

        var html = new StringBuilder();
        html.AppendLine($"<html><head><meta charset=\"utf-8\"></head><body style=\"font-family: monospace;\">");

        foreach (var chart in charts)
        {
            // Find the earliest and latest transactions.
            var firstTransaction = chart.DataSeries.Single(s => s.Title == "Total").DataPoints.First();
            var lastTransaction = chart.DataSeries.Single(s => s.Title == "Total").DataPoints.Last();

            var chartSvg = CreateChart(chart);

            html.AppendLine($"<div style=\"font-size: x-large; margin-bottom: 8px;\">{chart.Title}</div>");

            // Container for chart and details.
            html.AppendLine("<div style=\"display: flex;\">");

            // Chart on the left.
            html.AppendLine("<div style=\"margin-right: 24px;\">");
            html.AppendLine(chartSvg);
            html.AppendLine("</div>");

            // Details on the right.
            html.AppendLine("<div style=\"flex: 1\">");
            var isNegative = lastTransaction.Balance < 0;
            var balanceBg = isNegative ? "#f8d7da" : "#d4edda";
            var balanceColor = isNegative ? "#dc3545" : "#155724";
            html.AppendLine($"<span style='background: {balanceBg}; color: {balanceColor}; border-radius: 6px; font-size: 1.5em; padding: 4px 10px; display: inline-block;'>{lastTransaction.Balance:C0}</span><br><br>");
            var change = lastTransaction.Balance - firstTransaction.Balance;
            var arrow = change > 0
                ? "<span style='color: #155724; font-size: 12px;'>⬆</span>"
                : change < 0
                    ? "<span style='color: #dc3545; font-size: 12px;'>⬇</span>"
                    : "";
            html.AppendLine($"Change: {change:C0} {arrow}<br>");
            html.AppendLine($"Start:&nbsp; {firstTransaction.Balance:C0} ({firstTransaction.Date:M/yyyy})<br>");
            html.AppendLine("<br>");

            foreach (var dataSeries in chart.DataSeries)
                html.AppendLine($"{dataSeries.Title} <span style='height: 10px;  width: 10px; background-color: {dataSeries.Color}; border-radius: 50%; display: inline-block;'></span><br>");

            html.AppendLine("</div>");
            html.AppendLine("</div>");
        }

        html.AppendLine($"</body></html>");

        return Encoding.UTF8.GetBytes(html.ToString());
    }

    /// <summary>
    /// Build the chart models from the report.
    /// </summary>
    /// <param name="report"></param>
    /// <returns></returns>
    private static List<ChartData> FromPerformanceReport(HistoricalReportModel report)
    {
        // Assign a color to each account.
        var colorMap = new Dictionary<string, string>();
        foreach (var account in report.AccountGroups.SelectMany(g => g.Accounts))
            colorMap[account.Name] = _lineColors[colorMap.Count % _lineColors.Count];

        // Create a chart for each account group.
        var charts = new List<ChartData>();
        foreach (var accountGroup in report.AccountGroups)
        {
            var data = new ChartData
            {
                Title = accountGroup.Name,
                DataSeries = [new ChartDataSeries
                {
                    Title = "Total",
                    Color = "#333333",
                    Dashes = [3, 3],
                    DataPoints = AverageWeekly(accountGroup.AllTransactions)
                                    .Select(t => new ChartDataPoint { Date = t.Date.Date, Balance = t.Balance })
                                    .Where(t => t.Date > DateTime.Now.AddDays(-365))
                                    .ToList()
                }]
            };

            foreach (var account in accountGroup.Accounts)
            {
                data.DataSeries.Add(new ChartDataSeries
                {
                    Title = account.Name,
                    Color = colorMap[account.Name],
                    Dashes = [0, 0],
                    DataPoints = AverageWeekly(account.Transactions)
                                    .Select(t => new ChartDataPoint { Date = t.Date.Date, Balance = t.Balance })
                                    .Where(t => t.Date > DateTime.Now.AddDays(-365))
                                    .ToList()
                });
            }

            charts.Add(data);
        }

        return charts;
    }

    private static string CreateChart(ChartData chart)
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

        var minYValue = (double)chart.DataSeries.Single(s => s.Title == "Total").DataPoints.Min(w => w.Balance);
        var maxYValue = (double)chart.DataSeries.Single(s => s.Title == "Total").DataPoints.Max(w => w.Balance);

        double minimum;
        double maximum;
        double step;

        if (maxYValue > 0 && minYValue > 0)
        {
            minimum = 0;
            maximum = RoundAmount(maxYValue);
            step = maximum / 10.0;
        }
        else if (maxYValue < 0 && minYValue < 0)
        {
            minimum = RoundAmount(minYValue);
            maximum = 0;
            step = Math.Abs(minimum) / 10.0;
        }
        else
        {
            RoundAmount(minimum = minYValue);
            RoundAmount(maximum = maxYValue);
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

        foreach (var dataSeries in chart.DataSeries)
            AddDataSeries(plotModel, dataSeries);

        using var stream = new MemoryStream();
        SvgExporter.Export(plotModel, stream, 420, 420, isDocument: false);

        // Note that we have to remove the UTF8 byte order mask (EFBBBF) when
        // converting the byte array to a string.
        return Encoding.UTF8.GetString(stream.ToArray().Skip(3).ToArray());
    }

    private static double RoundAmount(double number)
    {
        if (Math.Abs(number) < 10000)
            return number > 0 ? Math.Ceiling(number / 1000) * 1000 : Math.Floor(number / 1000) * 1000;
        else if (Math.Abs(number) < 1000000)
            return number > 0 ? Math.Ceiling(number / 10000) * 10000 : Math.Floor(number / 10000) * 10000;
        else
            return number > 0 ? Math.Ceiling(number / 100000) * 100000 : Math.Floor(number / 100000) * 100000;
    }

    /// <summary>
    /// Calculates the average weekly balance from a list of transactions. Skip
    /// the last transaction as it is the current balance and not part of the
    /// weekly average.
    /// </summary>
    private static List<ChartDataPoint> AverageWeekly(List<HistoricalTransactionModel> transactions)
    {
        var weeklyAverages = transactions.SkipLast(1)
            .GroupBy(t => new { t.Date.Year, Week = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(t.Date.Date, CalendarWeekRule.FirstDay, DayOfWeek.Monday) })
            .Select(g => new ChartDataPoint
            {
                Date = FirstDateOfWeek(g.Key.Year, g.Key.Week),
                Balance = g.Average(t => t.RunningBalance)
            })
            .OrderBy(t => t.Date)
            .ToList();

        // Add a data point for the last transaction so that the chart ends with
        // the current balance.
        weeklyAverages.Add(new ChartDataPoint
        {
            Date = transactions.Last().Date.Date,
            Balance = transactions.Last().RunningBalance
        });

        return weeklyAverages;
    }

    private static DateTime FirstDateOfWeek(int year, int weekOfYear)
    {
        var jan1 = new DateTime(year, 1, 1);
        var daysOffset = (int)CultureInfo.InvariantCulture.DateTimeFormat.FirstDayOfWeek - (int)jan1.DayOfWeek;
        var firstMonday = jan1.AddDays(daysOffset);
        var firstWeek = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(firstMonday, CalendarWeekRule.FirstDay, DayOfWeek.Monday);

        if (firstWeek <= 1)
            weekOfYear -= 1;

        return firstMonday.AddDays(weekOfYear * 7);
    }

    private static void AddDataSeries(PlotModel plotModel, ChartDataSeries chartDataSeries)
    {
        var dataSeries = new FunctionSeries
        {
            Color = OxyColor.Parse(chartDataSeries.Color),
            InterpolationAlgorithm = InterpolationAlgorithms.CanonicalSpline,
            Dashes = chartDataSeries.Dashes,
        };

        foreach (var dataPoint in chartDataSeries.DataPoints)
            dataSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(dataPoint.Date.Date), (double)dataPoint.Balance));

        plotModel.Series.Add(dataSeries);
    }

    #region Models

    internal class ChartData
    {
        public string Title { get; set; } = string.Empty;
        public List<ChartDataSeries> DataSeries { get; set; } = [];
    }

    internal class ChartDataSeries
    {
        public string Title { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public double[] Dashes { get; set; } = [];
        public List<ChartDataPoint> DataPoints { get; set; } = [];
    }

    internal class ChartDataPoint
    {
        public DateTime Date { get; set; }
        public decimal Balance { get; set; }
    }

    #endregion
}