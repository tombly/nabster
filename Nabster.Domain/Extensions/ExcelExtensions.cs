using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;

namespace Nabster.Domain.Extensions;

public static class ExcelExtensions
{
    public static ICellStyle AddDateStyle(this ICellStyle style, IWorkbook workbook)
    {
        style.DataFormat = workbook.CreateDataFormat().GetFormat("m/d/yyyy");
        return style;
    }

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

    public static ICellStyle AddFontStyle(this ICellStyle style, IWorkbook workbook, bool isBold = false, bool isGray = false)
    {
        var font = workbook.CreateFont();
        font.FontHeightInPoints = 14;
        font.FontName = "Aptos";
        font.IsBold = isBold;
        if (isGray)
            font.Color = NPOI.HSSF.Util.HSSFColor.Grey50Percent.Index;
        style.SetFont(font);
        return style;
    }

    public static IRow ApplyStyle(this IRow row, ICellStyle style)
    {
        foreach (var cell in row.Cells)
            cell.CellStyle = style;
        return row;
    }

    public static byte[] ToByteArray(this IWorkbook workbook)
    {
        using var stream = new MemoryStream();
        workbook.Write(stream);
        stream.Flush();
        return stream.ToArray();
    }
}