using System.Text;
using Nabster.Domain.Reports;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace Nabster.Domain.Exports;

public static class PerformanceToHtml
{
    public static byte[] Create(PerformanceReport report)
    {
        var html = new StringBuilder();
        html.AppendLine($"<html><body style=\"font-family: monospace;\">");

        foreach (var accountGroup in report.AccountGroups)
        {
            // Find the min and max values.
            var minValue = accountGroup.Transactions.Min(t => (t.CumulativeAmount, t.Date));
            var maxValue = accountGroup.Transactions.Max(t => (t.CumulativeAmount, t.Date));

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

            var yAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                StringFormat = "C0",
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.None,
                MinorTickSize = 0,
                Minimum = 0,
                Maximum = (double)maxValue.CumulativeAmount,
                MajorStep = Math.Abs((double)maxValue.CumulativeAmount / 10.0),
                Font = "monospace",
                FontSize = 10,
            };

            var dataSeries = new FunctionSeries
            {
                Color = OxyColors.SteelBlue
            };
            foreach (var transaction in accountGroup.Transactions)
                dataSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(transaction.Date.Date), (double)transaction.CumulativeAmount));

            var plotModel = new PlotModel();
            plotModel.Series.Add(dataSeries);
            plotModel.Axes.Add(xAxis);
            plotModel.Axes.Add(yAxis);
            plotModel.PlotMargins = new OxyThickness(50, 0, 0, 50);

            using var stream = new MemoryStream();
            SvgExporter.Export(plotModel, stream, 400, 400, false);
            string svg = Encoding.UTF8.GetString(stream.ToArray());

            html.AppendLine($"<div style=\"font-size: x-large; margin-bottom: 8px;\">{accountGroup.Name}</div>");

            // Container for chart and details.
            html.AppendLine("<div style=\"display: flex;\">");

            // Chart on the left.
            html.AppendLine("<div>");
            html.AppendLine(svg);
            html.AppendLine("</div>");

            // Details on the right.
            html.AppendLine("<div style=\"flex: 1\">");
            html.AppendLine($"Begin:&nbsp; {minValue.CumulativeAmount:C0} ({minValue.Date:M/yyyy})<br>");
            html.AppendLine($"End:&nbsp;&nbsp;&nbsp; {maxValue.CumulativeAmount:C0} ({maxValue.Date:M/yyyy})<br>");
            html.AppendLine($"Change: {maxValue.CumulativeAmount - minValue.CumulativeAmount:C0}<br>");
            html.AppendLine("<br>");

            foreach (var accountName in accountGroup.AccountNames)
                html.AppendLine($"{accountName}<br>");
            html.AppendLine("</div>");

            html.AppendLine("</div>");
        }

        html.AppendLine($"</body></html>");

        return Encoding.UTF8.GetBytes(html.ToString());
    }
}