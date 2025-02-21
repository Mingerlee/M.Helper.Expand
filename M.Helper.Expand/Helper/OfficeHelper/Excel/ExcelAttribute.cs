using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace M.Helper.Expand
{
    /// <summary>
    ///     公式
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Constructor, AllowMultiple = true, Inherited = false)]
    public class FormulaAttribute : Attribute
    {
        //参数构造器
        /// <summary>
        ///     
        /// </summary>
        /// <param name="formulaType">公式类型</param>
        /// <param name="numDigits">精度</param>
        public FormulaAttribute(FormulaType formulaType, int numDigits=0)
        {
            FormulaType = formulaType;
            NumDigits = numDigits;
        }
        /// <summary>
        ///     公式类型
        /// </summary>
        public FormulaType FormulaType { get; private set; }
        /// <summary>
        ///     精度
        /// </summary>
        public int NumDigits { get; private set; }
    }
    /// <summary>
    ///     单元格公式
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Constructor, AllowMultiple = true, Inherited = false)]
    public class CellFormulaAttribute : Attribute
    {
        //参数构造器
        /// <summary>
        ///     
        /// </summary>
        /// <param name="cellFormulaType">公式类型</param>
        /// <param name="numDigits">精度</param>
        public CellFormulaAttribute(CellFormulaType cellFormulaType, int numDigits = 0)
        {
            CellFormulaType = cellFormulaType;
            NumDigits = numDigits;
        }
        /// <summary>
        ///     公式类型
        /// </summary>
        public CellFormulaType CellFormulaType { get; private set; }
        /// <summary>
        ///     精度
        /// </summary>
        public int NumDigits { get; private set; }
    }
}
