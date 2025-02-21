using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using M.Simple.Expand.Attributes;

namespace M.Simple.Expand.Test
{
    public class Person
    {
        public int Id { get; set; }
        [ModifyValue]
        public string Code { get; set; }
        [CompareValue]
        [ModifyValue]
        public string Name { get; set; }
        public string Title { get; set; }
        public int Age { get; set; }
        public DateTime CreartDate { get; set; }
        public DateTime UpdateDate { get; set; }
        public string CreartUser { get; set; }
        public int Vaild { get; set; }
    }
    public class Student
    {
        public int Id { get; set; }
        [ModifyValue("Code")]
        public string StudentCode { get; set; }
        [ModifyValue]
        public string Name { get; set; }
        public int Age { get; set; }
    }
}
