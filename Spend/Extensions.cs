using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;

namespace Nabster.Spend;

public static class Extensions
{
    public static ICellStyle AddCurrencyStyle(this ICellStyle style, IWorkbook workbook)
    {
        style.DataFormat = workbook.CreateDataFormat().GetFormat(string.Format("\"{0}\"#,##0.00", "$"));
        return style;
    }

    public static ICellStyle AddPercentageStyle(this ICellStyle style, IWorkbook workbook)
    {
        style.DataFormat = HSSFDataFormat.GetBuiltinFormat("0%");
        return style;
    }

    public static ICellStyle AddFontStyle(this ICellStyle style, IWorkbook workbook, bool isBold = false)
    {
        var font = workbook.CreateFont();
        font.FontHeightInPoints = 14;
        font.FontName = "Aptos";
        font.IsBold = isBold;
        style.SetFont(font);
        return style;
    }

    public static IRow ApplyStyle(this IRow row, ICellStyle style)
    {
        foreach (var cell in row.Cells)
            cell.CellStyle = style;
        return row;
    }
}