using Aspose.Cells;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Extgstate;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using NPOI.Util;
using System;
using System.IO;

namespace M.Helper.Expand
{
    /// <summary>
    ///     PDF帮助类
    /// </summary>
    public class PDFHelper
    {
        private const string LicenseStr = "Aspose.Cells.xml";
        private static readonly FileStream lStream = new FileStream(path: LicenseStr, FileMode.Open);

        /// <summary>
        ///     构造函数
        /// </summary>
        public PDFHelper()
        {
            //License license = new License();
            //license.SetLicense(lStream);
        }
        /// <summary>
        ///     将Excel转PDF文件
        /// </summary>
        /// <param name="excelFileName"></param>
        /// <param name="pdfFileName"></param>
        /// <param name="options"></param>
        /// <param name="fileReadWriteOptions"></param>
        /// <returns></returns>
        public bool ConvertExcelToPdf(string excelFileName, string pdfFileName, PdfSaveOptions options, FileReadWriteOptions fileReadWriteOptions = null)
        {
            Workbook workbook = GetWorkbook(excelFileName);
            MemoryStream memorystream = new MemoryStream();
            workbook.Worksheets[0].PageSetup.PrintGridlines = true;
            workbook.Save(memorystream, options);

            if (fileReadWriteOptions != null && fileReadWriteOptions.WaterMarkOption != null)
            {
                memorystream.Position = 0;
                var stream = GeneratePdfWithWatermark(fileReadWriteOptions.WaterMarkOption, memorystream);//添加水印
                File.WriteAllBytes($"{pdfFileName}.pdf", stream.ToArray());
                return true;
            }
            File.WriteAllBytes($"{pdfFileName}.pdf", memorystream.ToArray());
            return true;
        }
        private Workbook GetWorkbook(string filePath)
        {
            Workbook workbook;
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                workbook = new Workbook(stream);
            }
            return workbook;
        }
        private MemoryStream GeneratePdfWithWatermark(WaterMarkOption waterMarkOption, Stream stream)
        {
            var workStream = new MemoryStream();
            using (PdfReader reader = new PdfReader(stream))
            {
                using (PdfWriter writer = new PdfWriter(workStream))
                {
                    writer.SetCloseStream(false);
                    using (PdfDocument pdfDoc = new PdfDocument(reader, writer))
                    {
                        GeneratePdfWithWatermark(waterMarkOption, pdfDoc);
                    }
                }
                workStream.Position = 0;

                return workStream;
            }
        }
        private void GeneratePdfWithWatermark(WaterMarkOption waterMarkOption, PdfDocument pdfDoc)
        {
            PdfFont font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            Paragraph paragraph = new Paragraph(waterMarkOption.Text)
                .SetFont(font)
                .SetFontSize(waterMarkOption.Size);
            //.SetMultipliedLeading(0.8f); // 设置行间距

            PdfExtGState gs = new PdfExtGState();
            gs.SetFillOpacity(0.10F);
            PageSize pageSize = pdfDoc.GetDefaultPageSize();

            // 计算文本宽度和高度
            float labelWidth = (float)Math.Round(font.GetWidth(waterMarkOption.Text, waterMarkOption.Size), 3);
            float labelHeight = (float)Math.Round(waterMarkOption.Size + Math.Sin(waterMarkOption.Angle) * labelWidth, 3);


            for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
            {
                PdfArray viewPort = pdfDoc.GetPage(i).GetPdfObject().GetAsArray(PdfName.MediaBox);
                float width = viewPort.GetAsNumber(2).FloatValue();
                float height = viewPort.GetAsNumber(3).FloatValue();

                // 计算在页面上填充水印所需的列和行
                int columns = waterMarkOption.Columns ?? (int)Math.Floor(width / labelWidth);
                int rows = waterMarkOption.Rows ?? (int)Math.Floor(height / labelHeight);

                float angleInterval = (float)Math.Round(Math.Sin(waterMarkOption.Angle) * waterMarkOption.Size, 3);
                float verticalInterval = (float)(height - rows * labelHeight) / (rows - 1);//垂直间距
                float levelInterval = (float)(width - columns * labelWidth) / (columns - 1);//水平间距

                PdfCanvas over = new PdfCanvas(pdfDoc.GetPage(i));
                over.SetFillColor(ColorConstants.BLACK);
                over.SetExtGState(gs);

                for (int x = 0; x < columns; x++)
                {
                    for (int y = 0; y < rows; y++)
                    {
                        float pX = x * (labelWidth + levelInterval) + angleInterval;
                        float pY = y * (labelHeight + verticalInterval); // 可调整 Y 坐标以控制垂直间隔

                        // 创建 Canvas 并添加水印
                        Canvas canvas = new Canvas(over, pageSize);
                        canvas.ShowTextAligned(paragraph.SetWidth(labelWidth), pX, pY, i,
                            TextAlignment.LEFT, VerticalAlignment.BOTTOM, waterMarkOption.Angle);
                        canvas.Close();
                    }
                }
            }
        }
    }

    public class FileReadWriteOptions
    {
        /// <summary>
        ///     水印
        /// </summary>
        public WaterMarkOption WaterMarkOption { get; set; }
    }
    /// <summary>
    ///     水印
    /// </summary>
    public class WaterMarkOption
    {
        /// <summary>
        ///     水印文本
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        ///     水印字体大小
        /// </summary>
        public int Size { get; set; }
        /// <summary>
        ///     水印角度
        /// </summary>
        public float Angle { get; set; }
        /// <summary>
        ///     水印列数
        /// </summary>
        public int? Columns { get; set; }
        /// <summary>
        ///     水印行数
        /// </summary>
        public int? Rows { get; set; }
    }
}
