using Nabster.Domain.Extensions;
using Nabster.Domain.Reports;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Nabster.Domain.Exports;

public static class PerformanceToExcel
{
    public static byte[] Create(PerformanceReport report)
    {
        var workbook = new XSSFWorkbook();
        CreateSheet(workbook, report.AccountGroups);
        return workbook.ToByteArray();
    }

    private static ISheet CreateSheet(XSSFWorkbook workbook, List<PerformanceAccountGroup> accountGroups)
    {
        var sheet = workbook.CreateSheet("Performance");

        var rowCount = 0;
        var headerRow = sheet.CreateRow(rowCount++);
        headerRow.CreateCell(0).SetCellValue("AccountGroup");
        headerRow.CreateCell(1).SetCellValue("Date");
        headerRow.CreateCell(2).SetCellValue("Amount");

        headerRow.ApplyStyle(workbook.CreateCellStyle().AddFontStyle(workbook, true));

        foreach (var accountGroup in accountGroups)
            foreach (var transaction in accountGroup.AllTransactions)
                CreateRow(sheet, rowCount++, accountGroup.Name, transaction.Date, transaction.RunningBalance);

        var columnCount = 0;
        var columnEnumerator = sheet.GetEnumerator();
        while (columnEnumerator.MoveNext())
            sheet.SetColumnWidth(columnCount++, 5000);

        return sheet;
    }

    private static IRow CreateRow(ISheet sheet, int rowCount, string accountName, DateTimeOffset date, decimal amount)
    {
        var row = sheet.CreateRow(rowCount);
        row.CreateCell(0).SetCellValue(accountName);
        row.CreateCell(1);
        row.CreateCell(2);

        row.Cells[0].CellStyle = sheet.Workbook.CreateCellStyle().AddFontStyle(sheet.Workbook);
        row.Cells[1].CellStyle = sheet.Workbook.CreateCellStyle().AddFontStyle(sheet.Workbook).AddDateStyle(sheet.Workbook);
        row.Cells[2].CellStyle = sheet.Workbook.CreateCellStyle().AddFontStyle(sheet.Workbook).AddCurrencyStyle(sheet.Workbook);

        row.Cells[1].SetCellType(CellType.Numeric);
        row.Cells[1].SetCellValue(date.DateTime);

        row.Cells[2].SetCellType(CellType.Numeric);
        row.Cells[2].SetCellValue((double)amount);

        return row;
    }
}