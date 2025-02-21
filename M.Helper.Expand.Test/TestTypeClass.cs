using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace M.Helper.Expand.Test
{
    public class TestTypeClass
    {
        /// <summary>
        ///     字段
        /// </summary>
        private string _className=string.Empty;

        //属性
        [DisplayName("序号")]
        public int Id { get; set; }
        [DisplayName("数量")]
        [Formula(FormulaType.SUM)]
        public decimal Amonut { get; set; }
        [DisplayName("是否是类")]
        public bool IsClass { get; set; }
        [DisplayName("编码")]
        public string Code { get; set; }
        [DisplayName("名称")]
        public string Name { get; set; }
        [DisplayName("描述")]
        public string Description { get; set; }
        [DisplayName("创建时间")]
        public DateTime CreateDate { get; set; }
        [DisplayName("身份证")]
        public long ICD { get; set; }
        [DisplayName("浮点")]
        [Formula(FormulaType.AVERAGE,2)]
        public double IDouble { get; set; }
        [DisplayName("浮点2")]
        [CellFormula(CellFormulaType.ROUND,1)]
        public float IFloat { get; set; }
        public TestTypeClass(string className) 
        {
            _className=className;
        }
        public void SayHello()
        {
            Console.WriteLine($"Hello dear {_className},Welcome to my world");
        }
        public void SayHello(string msg)
        {
            Console.WriteLine($"Hello dear {msg},Welcome to my world");
        }
        public void SayHello(string msg,string massage)
        {
            Console.WriteLine($"Hello dear {msg},Welcome to my world {massage}");
        }
    }
}
