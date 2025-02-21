using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Reflection;

namespace M.Helper.Expand
{
    /// <summary>
    ///     Excel文件帮助类
    /// </summary>
    public class ExcelHelper
    {
        private CellStyleOptions _exportColumnTitleStyle;
        private CellStyleOptions _exportCellStyleOptions;
        private IWorkbook _exportWorkbook;
        private Dictionary<string, ICellStyle> _exportCellStyleDictionary=new Dictionary<string, ICellStyle>();
        private IRow _rowLast;
        /// <summary>
        ///     构造函数
        /// </summary>
        /// <param name="exportColumnTitleStyleOptions">导出-表头样式</param>
        /// <param name="exportCellStyleOptions">导出-内容样式</param>
        public ExcelHelper(CellStyleOptions exportColumnTitleStyleOptions = null,CellStyleOptions exportCellStyleOptions=null)
        {
            _exportCellStyleOptions = exportCellStyleOptions;
            _exportColumnTitleStyle = exportColumnTitleStyleOptions;
        }
        /// <summary>
        ///     导出
        /// </summary>
        /// <param name="excelFileName">文件名</param>
        /// <param name="list">数据源</param>
        /// <param name="sheetName">表名(工作簿)</param>
        /// <returns></returns>
        public string Export<T>(string excelFileName, List<T> list,string sheetName="")
        {
            _exportWorkbook = CreateWorkbook(excelFileName);

            ISheet sheet = _exportWorkbook.CreateSheet(string.IsNullOrEmpty(sheetName)?"sheet1": sheetName.Trim());
            IRow row;
            ICell cell;
            PropertyInfo[] properties = list[0].GetType().GetProperties();
            bool flagColumns = true;
            for (int i = 0; i < list.Count; i++)//rows
            {
                row = flagColumns ? sheet.CreateRow(0) : sheet.CreateRow(i + 1);

                int j = 0;
                foreach (PropertyInfo pi in properties)//columns
                {
                    if (i == 0 && flagColumns)//第一行的时候填充表头
                    {
                        //填充表头
                        CreartColumnTitle(pi, sheet, row, j, list.Count);
                    }
                    else
                    {
                        //填充内容
                        cell = row.CreateCell(j);

                        FullCellValues(cell, pi, pi.GetValue(list[i],null).ToString());
                    }
                    j++;
                }
                if (i == 0 && flagColumns)
                {
                    i = -1;
                    flagColumns = false;
                }
            }
            using (FileStream fs = new FileStream(excelFileName, FileMode.OpenOrCreate, FileAccess.Write))
            {
                _exportWorkbook.Write(fs);
                fs.Close();
            }
            return excelFileName;
        }
        /// <summary>
        ///     导出
        /// </summary>
        /// <param name="excelFileName"></param>
        /// <param name="dataTable"></param>
        /// <param name="sheetName"></param>
        /// <returns></returns>
        public string Export(string excelFileName, DataTable dataTable, string sheetName = "")
        {
            _exportWorkbook = CreateWorkbook(excelFileName);

            ISheet sheet = _exportWorkbook.CreateSheet(string.IsNullOrEmpty(sheetName) ? "sheet1" : sheetName.Trim());
            IRow row = null;
            ICell cell = null;

            //填充表头
            row = sheet.CreateRow(0);
            for (int i = 0; i < dataTable.Columns.Count; i++)
            {
                cell = row.CreateCell(i);
                cell.SetCellValue(dataTable.Columns[i].ColumnName);
                sheet.SetColumnWidth(i, (12 + dataTable.Columns[i].ColumnName.Length) * 256); // 设置第 1 列宽度为 20

                var style = SetExportDefaultCellStyle(_exportWorkbook, "System.String.ColumnTitle", _exportColumnTitleStyle);
                cell.CellStyle = style;
            }
            //填充内容
            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                row = sheet.CreateRow(i + 1);

                for (int j = 0; j < dataTable.Columns.Count; j++)
                {
                    cell = row.CreateCell(j);
                    SetCellValues(cell, dataTable.Columns[j].DataType.ToString(), dataTable.Rows[i][j].ToString());
                }
            }
            FileStream fs = new FileStream(excelFileName, FileMode.OpenOrCreate, FileAccess.Write);
            _exportWorkbook.Write(fs);
            fs.Close();
            return excelFileName;
        }
        /// <summary>
        ///     合并单元格
        /// </summary>
        /// <param name="sheet">表名(工作簿)</param>
        /// <param name="firstRow">合并开始行下标</param>
        /// <param name="lastRow">合并结束行下标</param>
        /// <param name="firstCol">合并开始列下标</param>
        /// <param name="lastCol">合并结束列下标</param>
        public void MergeCells(ISheet sheet, int firstRow, int lastRow, int firstCol, int lastCol)
        {
            // 合并单元格
            var cellRangeAddress = new CellRangeAddress(firstRow, lastRow, firstCol, lastCol);
            sheet.AddMergedRegion(cellRangeAddress);
        }
        /// <summary>
        ///     向现有的 Excel 文件追加数据
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="dataTable"></param>
        public void AppendDataToExistingExcel(string filePath, DataTable dataTable)
        {
            IWorkbook workbook;
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                if (filePath.EndsWith(".xlsx"))
                {
                    workbook = new XSSFWorkbook(stream);
                }
                else
                {
                    workbook = new HSSFWorkbook(stream);
                }
            }

            var sheet = workbook.GetSheetAt(0);
            int lastRowNum = sheet.LastRowNum;

            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                var dataRow = sheet.CreateRow(lastRowNum + i + 1);
                for (int j = 0; j < dataTable.Columns.Count; j++)
                {
                    dataRow.CreateCell(j).SetCellValue(dataTable.Rows[i][j].ToString());
                }
            }

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Write))
            {
                workbook.Write(stream);
            }
        }
        /// <summary>
        ///     向现有的 Excel 文件追加数据
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="list"></param>
        public void AppendDataToExistingExcel<T>(string filePath, List<T> list)
        {
            IWorkbook workbook;
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                if (_exportWorkbook != null)
                {
                    workbook = _exportWorkbook;
                }
                else
                {
                    if (filePath.EndsWith(".xlsx"))
                    {
                        workbook = new XSSFWorkbook(stream);
                    }
                    else
                    {
                        workbook = new HSSFWorkbook(stream);
                    }
                    _exportWorkbook= workbook;
                }
            }

            var sheet = workbook.GetSheetAt(0);
            int lastRowNum = sheet.LastRowNum;

            PropertyInfo[] properties = list[0].GetType().GetProperties();
            for (int i = 0; i < list.Count; i++)//rows
            {
                var row = sheet.CreateRow(lastRowNum + i + 1);

                int j = 0;
                foreach (PropertyInfo pi in properties)//columns
                {
                        //填充内容
                       var cell = row.CreateCell(j);

                        FullCellValues(cell, pi, pi.GetValue(list[i], null).ToString());
                    j++;
                }
            }

            using (var stream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write))
            {
                workbook.Write(stream);
            }

        }
        #region 私有方法
        private IWorkbook CreateWorkbook(string excelFileName)
        {
            IWorkbook iWorkbook = null;
            if (excelFileName.EndsWith(".xlsx"))
            {
                iWorkbook = new XSSFWorkbook();
            }
            else if (excelFileName.EndsWith(".xls"))
            {
                iWorkbook = new HSSFWorkbook();
            }
            return iWorkbook;
        }
        private void SetCellValues(ICell cell, string cellType, string cellValue)
        {
            var style= SetExportDefaultCellStyle(_exportWorkbook, cellType, _exportCellStyleOptions);

            switch (cellType)
            {
                case "System.String": //字符串类型
                    double result;
                    if (double.TryParse(cellValue, out result))
                        cell.SetCellValue(result);
                    else
                        cell.SetCellValue(cellValue);
                    break;
                case "System.DateTime": //日期类型
                    DateTime dateV;
                    DateTime.TryParse(cellValue, out dateV);
                    cell.SetCellValue(dateV);
                    break;
                case "System.Boolean": //布尔型
                    bool boolV = false;
                    bool.TryParse(cellValue, out boolV);
                    cell.SetCellValue(boolV ? "是" : "否");
                    break;
                case "System.Int16": //整型
                case "System.Int32":
                case "System.Byte":
                    int intV = 0;
                    int.TryParse(cellValue, out intV);
                    cell.SetCellValue(intV);
                    break;
                case "System.Int64":
                    cell.SetCellValue(cellValue);
                    break;
                case "System.Decimal": //浮点型
                case "System.Single":
                case "System.Double":
                    double doubV = 0;
                    double.TryParse(cellValue, out doubV);
                    cell.SetCellValue(doubV);
                    break;
                case "System.DBNull": //空值处理
                    cell.SetCellValue("");
                    break;
                default:
                    cell.SetCellValue("");
                    break;
            }

            cell.CellStyle = style;
        }
        private string Getzm(int Index)
        {
            var dividend = Index;
            string columnName = string.Empty;

            while (dividend > 0)
            {
                var modulo = (dividend - 1) % 26;
                columnName = Convert.ToChar(65 + modulo) + columnName;
                dividend = (dividend - modulo) / 26;
            }
            return columnName;
        }
        private void CreartColumnTitle(PropertyInfo pi,ISheet sheet,IRow row,int j,int count)
        {
            var displayNameAttribute = pi.GetCustomAttribute<DisplayNameAttribute>();//列名
            
            if (displayNameAttribute != null)
            {
                var cell = row.CreateCell(j);
                cell.SetCellValue(displayNameAttribute.DisplayName);
                sheet.SetColumnWidth(j, (12 + displayNameAttribute.DisplayName.Length) * 256); // 设置第 1 列宽度为 20

                var style= SetExportDefaultCellStyle(_exportWorkbook, "System.String.ColumnTitle", _exportColumnTitleStyle);
                cell.CellStyle= style;

                var customFormulaAttribute = pi.GetCustomAttribute<FormulaAttribute>();//公式
                if (customFormulaAttribute != null)
                {
                    //在最后新增一行
                    var columnM = Getzm(j+1);

                    _rowLast ??= sheet.CreateRow(count + 1);
                    var cellLastColumn = _rowLast.CreateCell(j);
                    cellLastColumn.SetCellType(CellType.Formula);

                    var styleFormulaType = SetExportDefaultCellStyle(_exportWorkbook, "System.String.FormulaType", new CellStyleOptions
                    {
                        BorderBottom = BorderStyle.Thin,
                        BorderRight = BorderStyle.Thin,
                        BorderLeft = BorderStyle.Thin,
                        BorderTop = BorderStyle.Thin,
                        Color = IndexedColors.Red.Index,
                        FontName = "微软雅黑",
                    });

                    switch (customFormulaAttribute.FormulaType)
                    {
                        case FormulaType.SUM: cellLastColumn.SetCellFormula($" \"总计：\" & SUM({columnM}2:{columnM}{count + 1})"); break;
                        case FormulaType.AVERAGE: cellLastColumn.SetCellFormula($" \"平均值：\" & ROUND(AVERAGE({columnM}2:{columnM}{count + 1}), {customFormulaAttribute.NumDigits})"); break;
                        case FormulaType.COUNT: cellLastColumn.SetCellFormula($" COUNT({columnM}2:{columnM}{count + 1}) & \"条数据\" "); break;
                        case FormulaType.MAX: cellLastColumn.SetCellFormula($" \"最大值：\" & MAX({columnM}2:{columnM}{count + 1})"); break;
                        case FormulaType.MIN: cellLastColumn.SetCellFormula($" \"最小值：\" & MIN({columnM}2:{columnM}{count + 1})"); break;
                        default: break;
                    }

                    cellLastColumn.CellStyle = styleFormulaType;

                }
            }

        }
        private void FullCellValues(ICell cell, PropertyInfo pi,string value)
        {
            SetCellValues(cell, pi.PropertyType.FullName, value);

            var customFormulaAttribute = pi.GetCustomAttribute<CellFormulaAttribute>();//公式
            if (customFormulaAttribute != null)
            {
                cell.SetCellType(CellType.Formula);
                switch (customFormulaAttribute.CellFormulaType)
                {
                    case CellFormulaType.ROUND: cell.SetCellFormula($"ROUND({value},{customFormulaAttribute.NumDigits})"); break;
                    case CellFormulaType.ABS:cell.SetCellFormula($"ABS({value})"); break;
                        
                    default: break;
                }
            }
        }
        private ICellStyle SetExportDefaultCellStyle(IWorkbook workbook, string cellType, CellStyleOptions cellStyleOptions)
        {
            ICellStyle exportStyle;
            var flag = _exportCellStyleDictionary.TryGetValue(cellType,out exportStyle);
            if (!flag)
            {
                exportStyle = workbook.CreateCellStyle();
            }
            else
            {
                return exportStyle;
            }
            //var style = workbook.CreateCellStyle();

            exportStyle.Alignment = cellStyleOptions.Alignment;
            exportStyle.VerticalAlignment = cellStyleOptions.VerticalAlignment;
            exportStyle.WrapText = cellStyleOptions.WrapText;

            // 设置字体
            var font = workbook.CreateFont();
            font.FontHeightInPoints = cellStyleOptions.FontHeightInPoints;
            font.FontName = cellStyleOptions.FontName;
            font.IsBold = cellStyleOptions.IsBold;
            font.Color = cellStyleOptions.Color;
            exportStyle.SetFont(font);

            // 设置边框
            exportStyle.BorderBottom = cellStyleOptions.BorderBottom;
            exportStyle.BorderLeft = cellStyleOptions.BorderLeft;
            exportStyle.BorderRight = cellStyleOptions.BorderRight;
            exportStyle.BorderTop = cellStyleOptions.BorderTop;

            exportStyle.LeftBorderColor = cellStyleOptions.LeftBorderColor;
            exportStyle.RightBorderColor = cellStyleOptions.RightBorderColor;
            exportStyle.BottomBorderColor = cellStyleOptions.BottomBorderColor;
            exportStyle.TopBorderColor = cellStyleOptions.TopBorderColor;

            // 设置背景颜色
            exportStyle.FillForegroundColor = cellStyleOptions.FillForegroundColor;
            exportStyle.FillPattern = cellStyleOptions.FillPattern;

            // 设置单元格值格式（日期，数字等）
            IDataFormat format = workbook.CreateDataFormat();

            switch (cellType)
            {
                case "System.DateTime": //日期类型
                    exportStyle.DataFormat = format.GetFormat("yyyy/m/d hh:mm");
                    break;
                case "System.Int16": //整型
                case "System.Int32":
                case "System.Byte":
                    exportStyle.DataFormat = format.GetFormat("0");
                    break;
                case "System.Decimal": //浮点型
                case "System.Single":
                case "System.Double":
                    exportStyle.DataFormat = format.GetFormat("0.00");
                    break;
                default: break;
            }

            _exportCellStyleDictionary.Add(cellType, exportStyle);

            return exportStyle;

        }
        #endregion
    }


    /// <summary>
    ///     Excel文件帮助类
    /// </summary>
   internal class ExcelHelper_bak
    {
        #region 私有变量

        private int _perSheetCount = 40000;//每个sheet要保存的条数

        private string _filePath;

        private IWorkbook _currentWorkbook;

        private List<string> _sheets;

        private Dictionary<string, DataTable> _dataDict;
        private ICellStyle _style;
        private int _currentRowIndex;
        #endregion

        #region 属性
        /// <summary>
        ///     工作簿列表
        /// </summary>
        public List<string> Sheets
        {
            get { return _sheets ?? (_sheets = GetSheets()); }
        }

        #endregion

        #region 构造器

        /// <summary>
        /// 默认构造器(读取)
        /// </summary>
        /// <param name="filePath"></param>
        public ExcelHelper_bak(string filePath)
        {
            _filePath = filePath;
            _dataDict = new Dictionary<string, DataTable>();
        }
        /// <summary>
        ///     默认构造器
        /// </summary>
        public ExcelHelper_bak()
        {

        }

        /// <summary>
        /// 加载数据
        /// </summary>
        public bool LoadData()
        {
            try
            {
                if (string.IsNullOrEmpty(_filePath))
                {
                    Console.WriteLine("请使用读取构造器初始化文件路径");
                    return false;
                }
                using (var fs = new FileStream(_filePath, FileMode.OpenOrCreate, FileAccess.Read))
                {
                    if (_filePath.EndsWith(".xlsx"))
                    {
                        _currentWorkbook = new XSSFWorkbook(fs);
                    }
                    else if (_filePath.EndsWith(".xls"))
                    {
                        _currentWorkbook = new HSSFWorkbook(fs);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// 最大接收5万条每页，大于5万时，使用系统默认的值(4万)
        /// </summary>
        /// <param name="perSheetCounts"></param>
        public ExcelHelper_bak(int perSheetCounts)
        {
            if (_perSheetCount <= 50000)
                _perSheetCount = perSheetCounts;
        }

        #endregion
        #region 公有方法
        /// <summary>
        ///     获取工作簿集合
        /// </summary>
        /// <returns></returns>
        public List<string> GetSheets()
        {
            var sheets = new List<string>();
            if (_currentWorkbook == null)
                return sheets;
            for (int i = 0; i < _currentWorkbook.NumberOfSheets; i++)
            {
                sheets.Add(_currentWorkbook.GetSheetName(i));
            }
            return sheets;
        }
        /// <summary>
        ///     获取指定单元格Value值(数值)
        /// </summary>
        /// <param name="sheetName"></param>
        /// <param name="rowIndex"></param>
        /// <param name="colIndex"></param>
        /// <returns></returns>
        public object GetNumericCellValue(string sheetName, int rowIndex, int colIndex)
        {
            if (!Sheets.Contains(sheetName))
                return null;
            var cell = _currentWorkbook.GetSheet(sheetName).GetRow(rowIndex).GetCell(colIndex - 1);
            return cell.NumericCellValue;
        }
        /// <summary>
        ///     获取指定单元格Value值(字符串)
        /// </summary>
        /// <param name="sheetName"></param>
        /// <param name="rowIndex"></param>
        /// <param name="colIndex"></param>
        /// <returns></returns>
        public object GetStringCellValue(string sheetName, int rowIndex, int colIndex)
        {
            if (!Sheets.Contains(sheetName))
                return null;
            var cell = _currentWorkbook.GetSheet(sheetName).GetRow(rowIndex).GetCell(colIndex - 1);
            if (cell == null)
                return null;
            if (cell.CellType == CellType.Formula)
            {
                if (cell.CachedFormulaResultType == CellType.String)
                    return cell.StringCellValue;
                if (cell.CachedFormulaResultType == CellType.Numeric)
                    return cell.NumericCellValue;
                if (cell.CachedFormulaResultType == CellType.Boolean)
                    return cell.BooleanCellValue;
                return null;
            }
            if (cell.CellType == CellType.Numeric)
                return cell.NumericCellValue;
            if (cell.CellType == CellType.Boolean)
                return cell.NumericCellValue;
            return cell.StringCellValue;
        }
        /// <summary>
        ///     获取指定单元格Value值(时间)
        /// </summary>
        /// <param name="sheetName"></param>
        /// <param name="rowIndex"></param>
        /// <param name="colIndex"></param>
        /// <returns></returns>
        public object GetDateTimeCellValue(string sheetName, int rowIndex, int colIndex)
        {
            if (!Sheets.Contains(sheetName))
                return null;
            var cell = _currentWorkbook.GetSheet(sheetName).GetRow(rowIndex).GetCell(colIndex - 1);
            if (cell.CellType == CellType.String)
                return cell.StringCellValue;
            return cell.DateCellValue;
        }
        /// <summary>
        ///     获取单元格
        /// </summary>
        /// <param name="sheetName"></param>
        /// <param name="rowIndex"></param>
        /// <param name="colIndex"></param>
        /// <returns></returns>
        public ICell GetCell(string sheetName, int rowIndex, int colIndex)
        {
            if (!Sheets.Contains(sheetName))
                return null;
            var sheet = _currentWorkbook.GetSheet(sheetName);
            if (sheet == null)
                return null;
            var row = sheet.GetRow(rowIndex);
            if (row == null)
                return null;
            var cell = row.GetCell(colIndex - 1);
            if (cell == null)
                return null;
            return cell;
        }

        /// <summary>
        /// 获取单元格里面的值
        /// </summary>
        /// <param name="sheetName">表名</param>
        /// <param name="rowIndex">行索引从1开始</param>
        /// <param name="colIndex">列索引从1开始</param>
        /// <returns></returns>
        public object GetCellValue(string sheetName, int rowIndex, int colIndex)
        {
            if (!Sheets.Contains(sheetName))
                return null;

            DataTable dt = null;
            if (!_dataDict.ContainsKey(sheetName))
            {
                dt = Import(sheetName);
                _dataDict.Add(sheetName, dt);
            }
            else
            {
                dt = _dataDict[sheetName];
            }
            if (dt == null)
                return null;

            if (dt.Rows.Count < rowIndex)
                return null;

            var rowIdx = rowIndex - 1;
            var row = dt.Rows[rowIdx];
            var colIdx = colIndex - 1;
            return row[colIdx];
        }
        /// <summary>
        ///     设置单元格的值
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="cellType"></param>
        /// <param name="cellValue"></param>
        private void SetCellValues(ICell cell, string cellType, string cellValue)
        {
            switch (cellType)
            {
                case "System.String": //字符串类型
                    double result;
                    if (double.TryParse(cellValue, out result))
                        cell.SetCellValue(result);
                    else
                        cell.SetCellValue(cellValue);
                    break;
                case "System.DateTime": //日期类型
                    DateTime dateV;
                    DateTime.TryParse(cellValue, out dateV);
                    cell.SetCellValue(dateV);
                    break;
                case "System.Boolean": //布尔型
                    bool boolV = false;
                    bool.TryParse(cellValue, out boolV);
                    cell.SetCellValue(boolV ? "是" : "否");
                    break;
                case "System.Int16": //整型
                case "System.Int32":
                case "System.Byte":
                    int intV = 0;
                    int.TryParse(cellValue, out intV);
                    cell.SetCellValue(intV);
                    break;
                case "System.Int64":
                    cell.SetCellValue(cellValue);
                    break;
                case "System.Decimal": //浮点型
                case "System.Double":
                    double doubV = 0;
                    double.TryParse(cellValue, out doubV);
                    cell.SetCellValue(doubV);
                    break;
                case "System.DBNull": //空值处理
                    cell.SetCellValue("");
                    break;
                default:
                    cell.SetCellValue("");
                    break;
            }
        }
        /// <summary>
        ///     导入
        /// </summary>
        /// <param name="sheetName"></param>
        /// <returns></returns>
        public DataTable Import(string sheetName)
        {
            sheetName = string.IsNullOrEmpty(sheetName) ? "Sheet1" : sheetName;

            ISheet sheet = _currentWorkbook.GetSheet(sheetName);
            if (sheet == null)
            {
                sheet = _currentWorkbook.GetSheetAt(0);
            }

            IEnumerator ie = sheet.GetRowEnumerator();

            IRow row = null;
            var maxCol = 0;
            while (ie.MoveNext())
            {
                row = ie.Current as IRow;//取一行,为了得到column的总数
                if (row.LastCellNum > maxCol)
                    maxCol = row.LastCellNum;
            }

            var dt = new DataTable();
            for (int i = 0; i < maxCol; i++)
            {
                dt.Columns.Add(string.Format("Col{0}", i));
            }

            ie.Reset();
            DataRow drow = null;
            ICell cell = null;
            var isHeader = true;
            while (ie.MoveNext())
            {
                if (isHeader)
                {
                    isHeader = false;
                    continue;
                }
                row = ie.Current as IRow;
                drow = dt.NewRow();
                for (int i = 0; i < row.LastCellNum; i++)
                {
                    if (row.GetCell(i) == null)
                    {
                        drow[i] = null;
                        continue;
                    }
                    cell = row.GetCell(i) as ICell;

                    switch (cell.CellType)
                    {
                        case CellType.Blank:
                            drow[i] = string.Empty;
                            break;
                        case CellType.Boolean:
                            drow[i] = cell.BooleanCellValue;
                            break;
                        case CellType.Error:
                            drow[i] = cell.ErrorCellValue;
                            break;
                        case CellType.Formula:
                            drow[i] = "=" + cell.CellFormula;
                            break;
                        case CellType.Numeric:
                            if (DateUtil.IsCellDateFormatted(cell))
                            {
                                drow[i] = cell.DateCellValue;
                            }
                            else
                            {
                                drow[i] = cell.NumericCellValue;
                            }
                            break;
                        case CellType.String:
                            drow[i] = cell.StringCellValue;
                            break;
                        case CellType.Unknown:
                            break;
                        default:
                            drow[i] = null;
                            break;
                    }
                }
                dt.Rows.Add(drow);
            }
            return dt;
        }
        /// <summary>
        ///     导出
        /// </summary>
        /// <param name="excelFileName"></param>
        /// <param name="dataTables"></param>
        /// <returns></returns>
        public string Export(string excelFileName, List<DataTable> dataTables)
        {
            var workbook = CreateWorkbook(excelFileName);
            ISheet sheet = null;
            IRow row = null;
            ICell cell = null;

            var index = 0;
            foreach (var dataTable in dataTables)
            {
                var tableName = dataTable.TableName;
                if (string.IsNullOrEmpty(tableName))
                    tableName = "Sheet" + (++index);
                sheet = workbook.CreateSheet(tableName);

                //填充表头
                row = sheet.CreateRow(0);
                for (int i = 0; i < dataTable.Columns.Count; i++)
                {
                    cell = row.CreateCell(i);
                    cell.SetCellValue(dataTable.Columns[i].ColumnName);
                }

                //填充内容
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    row = sheet.CreateRow(i + 1);
                    for (int j = 0; j < dataTable.Columns.Count; j++)
                    {
                        cell = row.CreateCell(j);
                        SetCellValues(cell, dataTable.Columns[j].DataType.ToString(), dataTable.Rows[i][j].ToString());
                    }
                }
            }
            if (File.Exists(excelFileName)) File.Delete(excelFileName);
            using (var fs = new FileStream(excelFileName, FileMode.CreateNew, FileAccess.Write)) workbook.Write(fs);
            return excelFileName;
        }
        /// <summary>
        ///     导出
        /// </summary>
        /// <param name="excelFileName"></param>
        /// <param name="dataTable"></param>
        /// <returns></returns>
        public string Export(string excelFileName, DataTable dataTable)
        {
            IWorkbook workbook = CreateWorkbook(excelFileName);

            ISheet sheet = null;
            IRow row = null;
            ICell cell = null;
            int sheetCount = 1;//当前的sheet数量
            int currentSheetCount = 0;//循环时当前保存的条数，每页都会清零

            //表头样式
            ICellStyle style = workbook.CreateCellStyle();
            style.Alignment = HorizontalAlignment.Center;
            //内容样式
            style = workbook.CreateCellStyle();
            style.Alignment = HorizontalAlignment.Center;

            sheet = workbook.CreateSheet("Sheet" + sheetCount);
            //填充表头
            row = sheet.CreateRow(0);
            for (int i = 0; i < dataTable.Columns.Count; i++)
            {
                cell = row.CreateCell(i);
                cell.SetCellValue(dataTable.Columns[i].ColumnName);
                cell.CellStyle = style;
            }
            //填充内容
            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                if (currentSheetCount >= _perSheetCount)
                {
                    sheetCount++;
                    currentSheetCount = 0;
                    sheet = workbook.CreateSheet("Sheet" + sheetCount);
                }
                if (sheetCount == 1)//因为第一页有表头，所以从第二页开始写
                    row = sheet.CreateRow(currentSheetCount + 1);
                else//以后没有表头了，所以从开始写，都是基于0的
                    row = sheet.CreateRow(currentSheetCount);
                currentSheetCount++;
                for (int j = 0; j < dataTable.Columns.Count; j++)
                {
                    cell = row.CreateCell(j);
                    cell.CellStyle = style;
                    SetCellValues(cell, dataTable.Columns[j].DataType.ToString(), dataTable.Rows[i][j].ToString());
                }
            }
            FileStream fs = new FileStream(excelFileName, FileMode.CreateNew, FileAccess.Write);
            workbook.Write(fs);
            fs.Close();
            return excelFileName;
        }
        /// <summary>
        ///     导出
        /// </summary>
        /// <param name="excelFileName"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public string Export<T>(string excelFileName, List<T> list)
        {
            IWorkbook workbook = CreateWorkbook(excelFileName);

            ISheet sheet = null;
            IRow row = null;
            ICell cell = null;
            int sheetCount = 1;//当前的sheet数量
            int currentSheetCount = 0;//循环时当前保存的条数，每页都会清零

            sheet = workbook.CreateSheet("Sheet" + sheetCount);

            var dataTable = list.ToDataTable();

            //填充表头
            row = sheet.CreateRow(0);
            var ColumnTiltes = FormatColumnName(list);

            //表头样式
            ICellStyle style = workbook.CreateCellStyle();
            style.Alignment = HorizontalAlignment.Center;

            for (int i = 0; i < ColumnTiltes.Count; i++)
            {
                cell = row.CreateCell(i);
                cell.CellStyle = style;
                sheet.SetColumnWidth(i, (12 + ColumnTiltes[i].ColumnTitle.Length) * 256); // 设置第 1 列宽度为 20
                cell.SetCellValue(ColumnTiltes[i].ColumnTitle);
                SetCellStyle(workbook, cell, "System.String", new CellStyleOptions
                {
                    FontName = "微软雅黑",
                    FontHeightInPoints = 14,
                    IsBold = true,
                    BorderBottom = BorderStyle.Thin,
                    BorderRight = BorderStyle.Thin,
                    BorderLeft = BorderStyle.Thin,
                    BorderTop = BorderStyle.Thin,
                });

                if (ColumnTiltes[i].ColumnFormulaType == FormulaType.SUM)
                {
                    //在最后新增一行
                    var columnM = Getzm(ColumnTiltes[i].ColumnIndex.GetValueOrDefault());
                    var countNumber = dataTable.Rows.Count;
                    var row1 = sheet.CreateRow(countNumber + 1);
                    var cell1 = row1.CreateCell(i);
                    cell1.SetCellType(CellType.Formula);
                    cell1.SetCellFormula($"SUM({columnM}2:{columnM}{countNumber + 1})");
                }
            }

            //填充内容
            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                if (currentSheetCount >= _perSheetCount)
                {
                    sheetCount++;
                    currentSheetCount = 0;
                    sheet = workbook.CreateSheet("Sheet" + sheetCount);
                }
                if (sheetCount == 1)//因为第一页有表头，所以从第二页开始写
                    row = sheet.CreateRow(currentSheetCount + 1);
                else//以后没有表头了，所以从开始写，都是基于0的
                    row = sheet.CreateRow(currentSheetCount);
                currentSheetCount++;
                for (int j = 0; j < dataTable.Columns.Count; j++)
                {
                    cell = row.CreateCell(j);
                    var cellType = dataTable.Columns[j].DataType.ToString();
                    SetCellValues(cell, cellType, dataTable.Rows[i][j].ToString());
                    SetCellStyle(workbook, cell, cellType, new CellStyleOptions
                    {
                        FontName = "微软雅黑",
                        FontHeightInPoints = 12,
                        Color = IndexedColors.Red.Index,
                        BorderBottom = BorderStyle.Thin,
                        BorderRight = BorderStyle.Thin,
                        BorderLeft = BorderStyle.Thin,
                        BorderTop = BorderStyle.Thin,
                    });
                }
            }

            FileStream fs = new FileStream(excelFileName, FileMode.OpenOrCreate, FileAccess.Write);
            workbook.Write(fs);
            fs.Close();
            return excelFileName;
        }
        /// <summary>
        ///     设置列宽
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sheetIndex"></param>
        /// <param name="columnIndex"></param>
        public void SetColumnWidth(IWorkbook workbook, int sheetIndex, int columnIndex)
        {
            var sheet = workbook.GetSheetAt(sheetIndex);

            // 设置列宽（单位是 1/256 字符宽度）
            sheet.SetColumnWidth(columnIndex, 20 * 256); // 设置第 1 列宽度为 20
        }
        /// <summary>
        ///     设置列宽
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sheet"></param>
        /// <param name="columnIndex"></param>
        public void SetColumnWidth(IWorkbook workbook, ISheet sheet, int columnIndex)
        {
            // 设置列宽（单位是 1/256 字符宽度）
            sheet.SetColumnWidth(columnIndex, 20 * 256); // 设置第 1 列宽度为 20
        }
        /// <summary>
        ///     设置行高
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sheetIndex"></param>
        /// <param name="rowIndex"></param>
        public void SetRowHeight(IWorkbook workbook, int sheetIndex, int rowIndex)
        {
            var sheet = workbook.GetSheetAt(sheetIndex);

            // 设置行高（单位是点数）
            var row = sheet.GetRow(rowIndex) ?? sheet.CreateRow(rowIndex);
            row.HeightInPoints = 25; // 设置行高为 25 点
        }
        /// <summary>
        ///     设置统一的行高
        /// </summary>
        /// <param name="sheet"></param>
        /// <param name="heightInPoints"></param>
        public void SetUniformRowHeight(ISheet sheet, float heightInPoints)
        {
            for (int i = 0; i <= sheet.LastRowNum; i++)
            {
                var row = sheet.GetRow(i) ?? sheet.CreateRow(i);
                row.HeightInPoints = heightInPoints; // 设置行高
            }
        }
        /// <summary>
        ///     设置统一的列宽
        /// </summary>
        /// <param name="sheet"></param>
        /// <param name="widthInCharacters"></param>
        public void SetUniformColumnWidth(ISheet sheet, int widthInCharacters)
        {
            for (int i = 0; i < sheet.GetRow(0).LastCellNum; i++) // 以第一行的单元格数量为列数
            {
                sheet.SetColumnWidth(i, widthInCharacters * 256); // 设置列宽
            }
        }
        /// <summary>
        ///     应用统一的行高和列宽
        /// </summary>
        /// <param name="sheet"></param>
        /// <param name="rowHeightInPoints"></param>
        /// <param name="columnWidthInCharacters"></param>
        public void SetUniformRowHeightAndColumnWidth(ISheet sheet, float rowHeightInPoints, int columnWidthInCharacters)
        {
            SetUniformRowHeight(sheet, rowHeightInPoints);
            SetUniformColumnWidth(sheet, columnWidthInCharacters);
        }
        /// <summary>
        ///     合并单元格
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sheetIndex"></param>
        /// <param name="firstRow"></param>
        /// <param name="lastRow"></param>
        /// <param name="firstCol"></param>
        /// <param name="lastCol"></param>
        public void MergeCells(IWorkbook workbook, int sheetIndex, int firstRow, int lastRow, int firstCol, int lastCol)
        {
            var sheet = workbook.GetSheetAt(sheetIndex);

            // 合并单元格
            var cellRangeAddress = new CellRangeAddress(firstRow, lastRow, firstCol, lastCol);
            sheet.AddMergedRegion(cellRangeAddress);

            // 可以对合并后的单元格设置样式
            var cell = sheet.GetRow(firstRow).GetCell(firstCol) ?? sheet.GetRow(firstRow).CreateCell(firstCol);
            var style = workbook.CreateCellStyle();
            style.Alignment = HorizontalAlignment.Center;
            cell.CellStyle = style;
        }
        /// <summary>
        ///     合并单元格
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sheet"></param>
        /// <param name="firstRow"></param>
        /// <param name="lastRow"></param>
        /// <param name="firstCol"></param>
        /// <param name="lastCol"></param>
        public void MergeCells(IWorkbook workbook, ISheet sheet, int firstRow, int lastRow, int firstCol, int lastCol)
        {
            // 合并单元格
            var cellRangeAddress = new CellRangeAddress(firstRow, lastRow, firstCol, lastCol);
            sheet.AddMergedRegion(cellRangeAddress);

            // 可以对合并后的单元格设置样式
            var cell = sheet.GetRow(firstRow).GetCell(firstCol) ?? sheet.GetRow(firstRow).CreateCell(firstCol);
            var style = workbook.CreateCellStyle();
            style.Alignment = HorizontalAlignment.Center;
            cell.CellStyle = style;
        }
        /// <summary>
        ///     设置单元格样式（字体、边框、背景色等） 
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sheetIndex"></param>
        /// <param name="rowIndex"></param>
        /// <param name="colIndex"></param>
        ///  <param name="cellType"></param>
        ///  <param name="cellStyleOptions"></param>
        public void SetCellStyle(IWorkbook workbook, int sheetIndex, int rowIndex, int colIndex, string cellType, CellStyleOptions cellStyleOptions)
        {
            var sheet = workbook.GetSheetAt(sheetIndex);
            var cell = sheet.GetRow(rowIndex).GetCell(colIndex) ?? sheet.GetRow(rowIndex).CreateCell(colIndex);

            var style = workbook.CreateCellStyle();

            // 设置字体
            var font = workbook.CreateFont();
            font.FontHeightInPoints = cellStyleOptions.FontHeightInPoints;
            font.FontName = cellStyleOptions.FontName;
            font.IsBold = cellStyleOptions.IsBold;
            font.Color = cellStyleOptions.Color;
            style.SetFont(font);

            // 设置边框
            style.BorderBottom = cellStyleOptions.BorderBottom;
            style.BorderLeft = cellStyleOptions.BorderLeft;
            style.BorderRight = cellStyleOptions.BorderRight;
            style.BorderTop = cellStyleOptions.BorderTop;

            style.LeftBorderColor = cellStyleOptions.LeftBorderColor;
            style.RightBorderColor = cellStyleOptions.RightBorderColor;
            style.BottomBorderColor = cellStyleOptions.BottomBorderColor;
            style.TopBorderColor = cellStyleOptions.TopBorderColor;

            // 设置背景颜色
            style.FillForegroundColor = cellStyleOptions.FillForegroundColor;
            style.FillPattern = cellStyleOptions.FillPattern;

            // 设置单元格值格式（日期，数字等）
            IDataFormat format = workbook.CreateDataFormat();

            switch (cellType)
            {
                case "System.String": //字符串类型
                    style.DataFormat = format.GetFormat("");
                    break;
                case "System.DateTime": //日期类型
                    style.DataFormat = format.GetFormat("yyyy/mm/dd hh:mm");
                    break;
                case "System.Int16": //整型
                case "System.Int32":
                case "System.Byte":
                    style.DataFormat = format.GetFormat("0");
                    break;
                case "System.Decimal": //浮点型
                case "System.Double":
                    style.DataFormat = format.GetFormat("0.00");
                    break;
                case "System.DBNull": //空值处理
                    break;
                default:
                    break;
            }

            cell.CellStyle = style;
        }
        /// <summary>
        ///     设置单元格样式（字体、边框、背景色等） 
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sheet"></param>
        /// <param name="rowIndex"></param>
        /// <param name="colIndex"></param>
        /// <param name="cellType"></param>
        ///  <param name="cellStyleOptions"></param>
        public void SetCellStyle(IWorkbook workbook, ISheet sheet, int rowIndex, int colIndex, string cellType, CellStyleOptions cellStyleOptions)
        {
            var cell = sheet.GetRow(rowIndex).GetCell(colIndex) ?? sheet.GetRow(rowIndex).CreateCell(colIndex);

            var style = workbook.CreateCellStyle();

            // 设置字体
            var font = workbook.CreateFont();
            font.FontHeightInPoints = cellStyleOptions.FontHeightInPoints;
            font.FontName = cellStyleOptions.FontName;
            font.IsBold = cellStyleOptions.IsBold;
            font.Color = cellStyleOptions.Color;
            style.SetFont(font);

            // 设置边框
            style.BorderBottom = cellStyleOptions.BorderBottom;
            style.BorderLeft = cellStyleOptions.BorderLeft;
            style.BorderRight = cellStyleOptions.BorderRight;
            style.BorderTop = cellStyleOptions.BorderTop;

            style.LeftBorderColor = cellStyleOptions.LeftBorderColor;
            style.RightBorderColor = cellStyleOptions.RightBorderColor;
            style.BottomBorderColor = cellStyleOptions.BottomBorderColor;
            style.TopBorderColor = cellStyleOptions.TopBorderColor;

            // 设置背景颜色
            style.FillForegroundColor = cellStyleOptions.FillForegroundColor;
            style.FillPattern = cellStyleOptions.FillPattern;

            // 设置单元格值格式（日期，数字等）
            IDataFormat format = workbook.CreateDataFormat();

            switch (cellType)
            {
                case "System.String": //字符串类型
                    style.DataFormat = format.GetFormat("");
                    break;
                case "System.DateTime": //日期类型
                    style.DataFormat = format.GetFormat("yyyy/mm/dd hh:mm");
                    break;
                case "System.Int16": //整型
                case "System.Int32":
                case "System.Int64":
                case "System.Byte":
                    style.DataFormat = format.GetFormat("0");
                    break;
                case "System.Decimal": //浮点型
                case "System.Double":
                    style.DataFormat = format.GetFormat("0.00");
                    break;
                case "System.DBNull": //空值处理
                    break;
                default:
                    break;
            }

            cell.CellStyle = style;
        }
        /// <summary>
        ///     设置单元格样式
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="cell"></param>
        /// <param name="cellType"></param>
        /// <param name="cellStyleOptions"></param>
        public void SetCellStyle(IWorkbook workbook, ICell cell, string cellType, CellStyleOptions cellStyleOptions)
        {
            if (_style == null || _currentRowIndex != cell.RowIndex)
            {
                _style = workbook.CreateCellStyle();
            }
            //var style = workbook.CreateCellStyle();

            _style.Alignment = cellStyleOptions.Alignment;
            _style.VerticalAlignment = cellStyleOptions.VerticalAlignment;
            _style.WrapText = cellStyleOptions.WrapText;

            // 设置字体
            var font = workbook.CreateFont();
            font.FontHeightInPoints = cellStyleOptions.FontHeightInPoints;
            font.FontName = cellStyleOptions.FontName;
            font.IsBold = cellStyleOptions.IsBold;
            font.Color = cellStyleOptions.Color;
            _style.SetFont(font);

            // 设置边框
            _style.BorderBottom = cellStyleOptions.BorderBottom;
            _style.BorderLeft = cellStyleOptions.BorderLeft;
            _style.BorderRight = cellStyleOptions.BorderRight;
            _style.BorderTop = cellStyleOptions.BorderTop;

            _style.LeftBorderColor = cellStyleOptions.LeftBorderColor;
            _style.RightBorderColor = cellStyleOptions.RightBorderColor;
            _style.BottomBorderColor = cellStyleOptions.BottomBorderColor;
            _style.TopBorderColor = cellStyleOptions.TopBorderColor;

            // 设置背景颜色
            _style.FillForegroundColor = cellStyleOptions.FillForegroundColor;
            _style.FillPattern = cellStyleOptions.FillPattern;

            // 设置单元格值格式（日期，数字等）
            IDataFormat format = workbook.CreateDataFormat();

            switch (cellType)
            {
                case "System.DateTime": //日期类型
                    _style.DataFormat = format.GetFormat("yyyy/m/d hh:mm");
                    break;
                case "System.Int16": //整型
                case "System.Int32":
                case "System.Byte":
                    _style.DataFormat = format.GetFormat("0");
                    break;
                case "System.Decimal": //浮点型
                case "System.Double":
                    _style.DataFormat = format.GetFormat("0.00");
                    break;
                default: break;
            }

            cell.CellStyle = _style;

            _currentRowIndex = cell.RowIndex;
        }
        /// <summary>
        ///     向现有的 Excel 文件追加数据
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="dataTable"></param>
        public void AppendDataToExistingExcel(string filePath, DataTable dataTable)
        {
            IWorkbook workbook;
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                if (filePath.EndsWith(".xlsx"))
                {
                    workbook = new XSSFWorkbook(stream);
                }
                else
                {
                    workbook = new HSSFWorkbook(stream);
                }
            }

            var sheet = workbook.GetSheetAt(0);
            int lastRowNum = sheet.LastRowNum;

            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                var dataRow = sheet.CreateRow(lastRowNum + i + 1);
                for (int j = 0; j < dataTable.Columns.Count; j++)
                {
                    dataRow.CreateCell(j).SetCellValue(dataTable.Rows[i][j].ToString());
                }
            }

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Write))
            {
                workbook.Write(stream);
            }
        }
        #endregion

        #region 私有方法
        private string GetFileMask(string excelFileName)
        {
            var maskStartIndex = excelFileName.LastIndexOf(".");
            var fileMask = excelFileName[maskStartIndex..];//简写： line.Substring(maskStartIndex, line.Length - maskStartIndex);

            return fileMask;
        }
        private IWorkbook CreateWorkbook(string excelFileName)
        {
            IWorkbook iWorkbook = null;
            if (excelFileName.EndsWith(".xlsx"))
            {
                iWorkbook = new XSSFWorkbook();
            }
            else if (excelFileName.EndsWith(".xls"))
            {
                iWorkbook = new HSSFWorkbook();
            }
            return iWorkbook;
        }
        private List<WorkSheetColumnTitle> FormatColumnName<T>(List<T> list)
        {
            var properties = typeof(T).GetProperties();
            List<WorkSheetColumnTitle> ColumnNames = new List<WorkSheetColumnTitle>();
            for (int i = 0; i < properties.Length; i++)
            {

                var propertyInfo = properties[i];
                var displayNameAttribute = propertyInfo.GetCustomAttribute<DisplayNameAttribute>();
                var customFormulaAttribute = propertyInfo.GetCustomAttribute<FormulaAttribute>();

                if (displayNameAttribute != null)
                {
                    WorkSheetColumnTitle worksheet = new WorkSheetColumnTitle()
                    {
                        ColumnTitle = displayNameAttribute.DisplayName,
                        ColumnFormulaType = customFormulaAttribute?.FormulaType,
                        ColumnIndex = i,
                    };
                    ColumnNames.Add(worksheet);
                }
            }

            return ColumnNames;
        }
        private string Getzm(int Index)
        {
            var dividend = Index;
            string columnName = string.Empty;

            while (dividend > 0)
            {
                var modulo = (dividend - 1) % 26;
                columnName = Convert.ToChar(65 + modulo) + columnName;
                dividend = (dividend - modulo) / 26;
            }
            return columnName;
        }

        #endregion
    }
}
