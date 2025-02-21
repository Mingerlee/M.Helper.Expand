using Aspose.Cells;
using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace M.Helper.Expand.Test
{
    //[TestClass]
    public class OfficeTest
    {
        [TestMethod]
        public void ExcelTest()
        {
            ExcelHelper excelHelper = new ExcelHelper(new CellStyleOptions
            {
                FontName = "微软雅黑",
                FontHeightInPoints = 14,
                IsBold = true,
                BorderBottom = BorderStyle.Thin,
                BorderRight = BorderStyle.Thin,
                BorderLeft = BorderStyle.Thin,
                BorderTop = BorderStyle.Thin,
            }, new CellStyleOptions
            {
                FontName = "微软雅黑",
                FontHeightInPoints = 12,
                BorderBottom = BorderStyle.Thin,
                BorderRight = BorderStyle.Thin,
                BorderLeft = BorderStyle.Thin,
                BorderTop = BorderStyle.Thin,
            });
            List<TestTypeClass> list = new List<TestTypeClass> {
                        new TestTypeClass(""){ Amonut=1,
                                                              Code="A001" ,
                                                              CreateDate=DateTime.Now,
                                                              Description="描述1",
                                                              ICD=12324324234,
                                                              Id=1,
                                                              IDouble=1,
                                                              IFloat = 3.35f,
                                                              Name="张三"
                                                        },
                        new TestTypeClass(""){ Amonut=2,
                                                              Code="A002" ,
                                                              CreateDate=DateTime.Now,
                                                              Description="描述2",
                                                              ICD=23425324234,
                                                              Id=2,
                                                              IDouble=2,
                                                              IFloat = 4.35f,
                                                              Name="李四"
                                                },
            };

            for (int i = 1; i <= 10; i++)
            {
                var r = new Random();
                list.Add(new TestTypeClass("")
                {
                    Amonut = r.Next(1, 10000),
                    Code = $"A00{i + 2}",
                    CreateDate = DateTime.Now.AddDays(r.Next(1, 365)),
                    Description = $"描述{i}",
                    ICD = 12324324234 + i,
                    Id = i,
                    IDouble = Math.Round(r.Next(1, 10000) / 3.0, 2),
                    IFloat = (float)Math.Round(r.Next(5000) / 3.0, 2),
                    Name = $"张三{i}"
                });
            }

            excelHelper.Export("Test3.xlsx", list.ToDataTable(), "Test");

            //excelHelper.AppendDataToExistingExcel("Test2.xlsx", list);
        }
        [TestMethod]
        public void PdfTest()
        {
            PDFHelper pdf = new PDFHelper();
            pdf.ConvertExcelToPdf("Test2.xlsx","test2", new PdfSaveOptions { 
                AllColumnsInOnePagePerSheet = true, 
                GridlineType = GridlineType.Dotted, 
            }, new FileReadWriteOptions
            {
                WaterMarkOption = new WaterMarkOption
                {
                    Size = 14,
                    Text = $"928FAB {DateTime.Now:yyMMdd}",
                    Angle = (float)(Math.PI / 180 * 45),
                    //Columns = 3,
                    //Rows = 3,
                }
            });
        }

    }
}
