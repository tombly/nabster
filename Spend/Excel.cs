using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Nabster.Spend;

public static class Excel
{
    public static void Create(SpendReport report)
    {
        var fileName = $"{report.BudgetName} {DateTime.Now:yyyyMMdd}.xlsx";
        var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
        var workbook = new XSSFWorkbook();

        CreateSheet(workbook, report.Categories);

        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        workbook.Write(stream);
    }

    private static ISheet CreateSheet(XSSFWorkbook workbook, List<SpendCategory> categories)
    {
        var sheet = workbook.CreateSheet("Budget");

        var rowCount = 0;
        var headerRow = sheet.CreateRow(rowCount++);
        headerRow.CreateCell(0).SetCellValue("CategoryGroup");
        headerRow.CreateCell(1).SetCellValue("Category");
        headerRow.CreateCell(2).SetCellValue("GoalCadence");
        headerRow.CreateCell(3).SetCellValue("GoalDay");
        headerRow.CreateCell(4).SetCellValue("GoalTarget");
        headerRow.CreateCell(5).SetCellValue("MonthlyCost");
        headerRow.CreateCell(6).SetCellValue("GoalPctComplete");

        headerRow.ApplyStyle(workbook.CreateCellStyle().AddFontStyle(workbook, true));

        foreach (var category in categories)
            CreateRow(sheet, rowCount++, category);

        var columnCount = 0;
        var columnEnumerator = sheet.GetEnumerator();
        while (columnEnumerator.MoveNext())
            sheet.SetColumnWidth(columnCount++, 5000);

        return sheet;
    }

    private static IRow CreateRow(ISheet sheet, int rowCount, SpendCategory category)
    {
        var row = sheet.CreateRow(rowCount);
        row.CreateCell(0).SetCellValue(category.CategoryGroupName);
        row.CreateCell(1).SetCellValue(category.CategoryName);
        row.CreateCell(2).SetCellValue(category.GoalCadence);
        row.CreateCell(3).SetCellValue(category.GoalDay);
        row.CreateCell(4);
        row.CreateCell(5);
        row.CreateCell(6);

        row.Cells[0].CellStyle = sheet.Workbook.CreateCellStyle().AddFontStyle(sheet.Workbook);
        row.Cells[1].CellStyle = sheet.Workbook.CreateCellStyle().AddFontStyle(sheet.Workbook);
        row.Cells[2].CellStyle = sheet.Workbook.CreateCellStyle().AddFontStyle(sheet.Workbook);
        row.Cells[3].CellStyle = sheet.Workbook.CreateCellStyle().AddFontStyle(sheet.Workbook);
        row.Cells[4].CellStyle = sheet.Workbook.CreateCellStyle().AddFontStyle(sheet.Workbook).AddCurrencyStyle(sheet.Workbook);
        row.Cells[5].CellStyle = sheet.Workbook.CreateCellStyle().AddFontStyle(sheet.Workbook).AddCurrencyStyle(sheet.Workbook);
        row.Cells[6].CellStyle = sheet.Workbook.CreateCellStyle().AddFontStyle(sheet.Workbook).AddPercentageStyle(sheet.Workbook);

        row.Cells[4].SetCellType(CellType.Numeric);
        if (!string.IsNullOrWhiteSpace(category.GoalTarget))
            row.Cells[4].SetCellValue(double.Parse(category.GoalTarget));

        row.Cells[5].SetCellType(CellType.Numeric);
        if (category.MonthlyCost != null)
            row.Cells[5].SetCellValue((double)category.MonthlyCost);

        row.Cells[6].SetCellType(CellType.Numeric);
        if (!string.IsNullOrWhiteSpace(category.GoalPercentageComplete))
            row.Cells[6].SetCellValue(double.Parse(category.GoalPercentageComplete));

        return row;
    }
}