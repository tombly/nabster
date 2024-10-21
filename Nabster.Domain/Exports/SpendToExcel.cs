using Nabster.Domain.Extensions;
using Nabster.Domain.Reports;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Nabster.Domain.Exports;

public static class SpendToExcel
{
    public static byte[] Create(SpendReport report)
    {
        var workbook = new XSSFWorkbook();
        CreateSheet(workbook, report);
        return workbook.ToByteArray();
    }

    private static ISheet CreateSheet(XSSFWorkbook workbook, SpendReport report)
    {
        var sheet = workbook.CreateSheet("Spend");
        var rowCount = 0;

        CreateTitleRow(sheet, rowCount++, $"Spend Report - {report.BudgetName} - {report.MonthName}");
        foreach (var group in report.Groups.OrderBy(g => g.MemoPrefix))
        {
            CreateGroupTitleRow(sheet, rowCount++, group.MemoPrefix);
            foreach (var transaction in group.Transactions)
                CreateTransactionRow(sheet, rowCount++, transaction);
            CreateGroupTotalRow(sheet, rowCount++, group.Total);
        }
        sheet.CreateRow(rowCount++);
        CreateReportTotalRow(sheet, rowCount++, report.Total);

        var columnCount = 0;
        sheet.SetColumnWidth(columnCount++, 11000);
        var columnEnumerator = sheet.GetEnumerator();
        while (columnEnumerator.MoveNext())
            sheet.SetColumnWidth(columnCount++, 4500);

        return sheet;
    }

    private static IRow CreateTitleRow(ISheet sheet, int rowCount, string title)
    {
        var row = sheet.CreateRow(rowCount);
        row.CreateCell(0).SetCellValue(title);
        row.ApplyStyle(sheet.Workbook.CreateCellStyle().AddFontStyle(sheet.Workbook, true));
        return row;
    }

    private static IRow CreateGroupTitleRow(ISheet sheet, int rowCount, string memoPrefix)
    {
        var row = sheet.CreateRow(rowCount);
        row.CreateCell(0).SetCellValue(memoPrefix);
        row.ApplyStyle(sheet.Workbook.CreateCellStyle().AddFontStyle(sheet.Workbook, true));
        return row;
    }

    private static IRow CreateTransactionRow(ISheet sheet, int rowCount, SpendTransaction transaction)
    {
        var row = sheet.CreateRow(rowCount);
        row.CreateCell(0).SetCellValue(transaction.Description);
        row.CreateCell(1).SetCellValue(transaction.Date.DateTime);
        row.CreateCell(2).SetCellType(CellType.Numeric);

        row.Cells[2].SetCellValue((double)transaction.Amount);

        row.Cells[0].CellStyle = sheet.Workbook.CreateCellStyle().AddFontStyle(sheet.Workbook);
        row.Cells[1].CellStyle = sheet.Workbook.CreateCellStyle().AddFontStyle(sheet.Workbook).AddDateStyle(sheet.Workbook);
        row.Cells[2].CellStyle = sheet.Workbook.CreateCellStyle().AddFontStyle(sheet.Workbook).AddCurrencyStyle(sheet.Workbook);

        return row;
    }

    private static IRow CreateGroupTotalRow(ISheet sheet, int rowCount, decimal total)
    {
        var row = sheet.CreateRow(rowCount);
        row.CreateCell(0);
        row.CreateCell(1).SetCellValue("Total");

        row.CreateCell(2).SetCellType(CellType.Numeric);
        row.Cells[2].SetCellValue((double)total);
        row.ApplyStyle(sheet.Workbook.CreateCellStyle().AddFontStyle(sheet.Workbook, true).AddCurrencyStyle(sheet.Workbook));

        return row;
    }

    private static IRow CreateReportTotalRow(ISheet sheet, int rowCount, decimal total)
    {
        var row = sheet.CreateRow(rowCount);
        row.CreateCell(0);
        row.CreateCell(1).SetCellValue("Report Total");

        row.CreateCell(2).SetCellType(CellType.Numeric);
        row.Cells[2].SetCellValue((double)total);
        row.ApplyStyle(sheet.Workbook.CreateCellStyle().AddFontStyle(sheet.Workbook, true).AddCurrencyStyle(sheet.Workbook));

        return row;
    }
}