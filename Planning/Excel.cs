using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Nabster.Planning;

public static class Excel
{
    public static void Create(PlanningReport report)
    {
        var fileName = $"{report.BudgetName} Planning {DateTime.Now:yyyyMMdd}.xlsx";
        var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
        var workbook = new XSSFWorkbook();

        CreateSheet(workbook, report);

        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        workbook.Write(stream);
    }

    private static ISheet CreateSheet(XSSFWorkbook workbook, PlanningReport report)
    {
        var sheet = workbook.CreateSheet("Planning");
        var rowCount = 0;

        CreateHeaderRow(sheet, rowCount++);
        foreach (var group in report.Groups)
        {
            CreateGroupTitleRow(sheet, rowCount++, group.CategoryGroupName);
            foreach (var category in group.Categories)
                CreateCategoryRow(sheet, rowCount++, category);
            CreateGroupTotalRow(sheet, rowCount++, group.MonthlyTotal, group.YearlyTotal);
        }
        sheet.CreateRow(rowCount++);
        CreateReportTotalRow(sheet, rowCount++, report.MonthlyTotal, report.YearlyTotal);

        var columnCount = 0;
        sheet.SetColumnWidth(columnCount++, 11000);
        var columnEnumerator = sheet.GetEnumerator();
        while (columnEnumerator.MoveNext())
                sheet.SetColumnWidth(columnCount++, 4500);

        return sheet;
    }

    private static void CreateHeaderRow(ISheet sheet, int rowCount)
    {
        var row = sheet.CreateRow(rowCount);
        row.CreateCell(0).SetCellValue("Category");
        row.CreateCell(1).SetCellValue("GoalCadence");
        row.CreateCell(2).SetCellValue("GoalDay");
        row.CreateCell(3).SetCellValue("GoalTarget");
        row.CreateCell(4).SetCellValue("MonthlyCost");
        row.CreateCell(5).SetCellValue("GoalPctComplete");

        row.ApplyStyle(sheet.Workbook.CreateCellStyle().AddFontStyle(sheet.Workbook, true));
        row.Cells[5].CellStyle = sheet.Workbook.CreateCellStyle().AddFontStyle(sheet.Workbook, isBold: true, isGray: true);
    }

    private static IRow CreateCategoryRow(ISheet sheet, int rowCount, PlanningCategory category)
    {
        var row = sheet.CreateRow(rowCount);
        row.CreateCell(0).SetCellValue(category.CategoryName);
        row.CreateCell(1).SetCellValue(category.GoalCadence);
        row.CreateCell(2).SetCellValue(category.GoalDay);
        row.CreateCell(3).SetCellType(CellType.Numeric);
        row.CreateCell(4).SetCellType(CellType.Numeric);

        row.Cells[3].SetCellValue((double)category.GoalTarget);
        row.Cells[4].SetCellValue((double)category.MonthlyCost);

        if (category.GoalPercentageComplete != null)
        {
            row.CreateCell(5).SetCellType(CellType.Numeric);
            row.Cells[5].SetCellValue((double)category.GoalPercentageComplete.Value);
            row.Cells[5].CellStyle = sheet.Workbook.CreateCellStyle().AddFontStyle(sheet.Workbook, isGray: true).AddPercentageStyle(sheet.Workbook);
        }

        row.Cells[0].CellStyle = sheet.Workbook.CreateCellStyle().AddFontStyle(sheet.Workbook);
        row.Cells[1].CellStyle = sheet.Workbook.CreateCellStyle().AddFontStyle(sheet.Workbook);
        row.Cells[2].CellStyle = sheet.Workbook.CreateCellStyle().AddFontStyle(sheet.Workbook);
        row.Cells[3].CellStyle = sheet.Workbook.CreateCellStyle().AddFontStyle(sheet.Workbook).AddCurrencyStyle(sheet.Workbook);
        row.Cells[4].CellStyle = sheet.Workbook.CreateCellStyle().AddFontStyle(sheet.Workbook).AddCurrencyStyle(sheet.Workbook);

        return row;
    }

    private static IRow CreateGroupTitleRow(ISheet sheet, int rowCount, string groupName)
    {
        var row = sheet.CreateRow(rowCount);
        row.CreateCell(0).SetCellValue(groupName);
        row.ApplyStyle(sheet.Workbook.CreateCellStyle().AddFontStyle(sheet.Workbook, true));
        return row;
    }

    private static IRow CreateGroupTotalRow(ISheet sheet, int rowCount, decimal monthlyTotal, decimal yearlyTotal)
    {
        var row = sheet.CreateRow(rowCount);
        row.CreateCell(0);
        row.CreateCell(1);
        row.CreateCell(2).SetCellValue("Group Total");

        row.CreateCell(3).SetCellType(CellType.Numeric);
        row.Cells[3].SetCellValue((double)yearlyTotal);
        row.ApplyStyle(sheet.Workbook.CreateCellStyle().AddFontStyle(sheet.Workbook, true).AddCurrencyStyle(sheet.Workbook));

        row.CreateCell(4).SetCellType(CellType.Numeric);
        row.Cells[4].SetCellValue((double)monthlyTotal);
        row.ApplyStyle(sheet.Workbook.CreateCellStyle().AddFontStyle(sheet.Workbook, true).AddCurrencyStyle(sheet.Workbook));

        return row;
    }

    private static IRow CreateReportTotalRow(ISheet sheet, int rowCount, decimal monthlyTotal, decimal yearlyTotal)
    {
        var row = sheet.CreateRow(rowCount);
        row.CreateCell(0);
        row.CreateCell(1);
        row.CreateCell(2).SetCellValue("Total");

        row.CreateCell(3).SetCellType(CellType.Numeric);
        row.Cells[3].SetCellValue((double)yearlyTotal);
        row.ApplyStyle(sheet.Workbook.CreateCellStyle().AddFontStyle(sheet.Workbook, true).AddCurrencyStyle(sheet.Workbook));

        row.CreateCell(4).SetCellType(CellType.Numeric);
        row.Cells[4].SetCellValue((double)monthlyTotal);
        row.ApplyStyle(sheet.Workbook.CreateCellStyle().AddFontStyle(sheet.Workbook, true).AddCurrencyStyle(sheet.Workbook));

        return row;
    }
}