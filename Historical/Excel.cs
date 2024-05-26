using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Nabster.Historical;

public static class Excel
{
    public static void Create(HistoricalReport report)
    {
        var fileName = $"{report.BudgetName} Historical {DateTime.Now:yyyyMMdd}.xlsx";
        var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
        var workbook = new XSSFWorkbook();

        CreateSheet(workbook, report.AccountGroups);

        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        workbook.Write(stream);
    }

    private static ISheet CreateSheet(XSSFWorkbook workbook, List<HistoricalAccountGroup> accountGroups)
    {
        var sheet = workbook.CreateSheet("Historical");

        var rowCount = 0;
        var headerRow = sheet.CreateRow(rowCount++);
        headerRow.CreateCell(0).SetCellValue("AccountGroup");
        headerRow.CreateCell(1).SetCellValue("Date");
        headerRow.CreateCell(2).SetCellValue("Amount");

        headerRow.ApplyStyle(workbook.CreateCellStyle().AddFontStyle(workbook, true));

        foreach (var accountGroup in accountGroups)
            foreach (var transaction in accountGroup.Transactions)
                CreateRow(sheet, rowCount++, accountGroup.Name, transaction.Date, transaction.CumulativeAmount);

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