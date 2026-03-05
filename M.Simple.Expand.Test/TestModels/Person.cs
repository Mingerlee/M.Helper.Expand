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
    public class TestCompareModel
    {
        public int Id { get; set; }
        public string Code { get; set; }
        [CompareValue]
        public string Name { get; set; }
        public string Title { get; set; }
        public int Age { get; set; }
        public DateTime CreartDate { get; set; }
        public DateTime UpdateDate { get; set; }
        [CompareValue]
        public string CreartUser { get; set; }
        public int Vaild { get; set; }
        public decimal DecimalFiled { get; set; }
        public bool BoolFiled { get; set; }
        [CompareValue]
        public PersonEnum PersonEnumFiled { get; set; }
        public List<Student> Students { get; set; }
        public decimal DecimalFiled2 { get; set; }
        public bool BoolFiled2 { get; set; }
        public string CreartUser1 { get; set; }
        public int Vaild2 { get; set; }
        public decimal DecimalFiled3 { get; set; }
        public bool BoolFiled4 { get; set; }
        public string CreartUser5 { get; set; }
        public int Vaild5 { get; set; }
        public decimal DecimalFiled5 { get; set; }
        public bool BoolFiled5 { get; set; }
        public string CreartUser6 { get; set; }
        public int Vaild6 { get; set; }
        public decimal DecimalFiled6 { get; set; }
        public bool BoolFiled6 { get; set; }
        public PersonEnum PersonEnumFiled2 { get; set; }
        public PersonEnum PersonEnumFiled3 { get; set; }
        public PersonEnum PersonEnumFiled4 { get; set; }
        public PersonEnum PersonEnumFiled5 { get; set; }
    }
    // 自定义相等比较器（按 Id 比较）
    public class StudentIdComparer : IEqualityComparer<Student>
    {
        public bool Equals(Student x, Student y)
        {
            if (x == null || y == null) return false;
            return x.Id == y.Id;
        }

        public int GetHashCode(Student obj)
        {
            return obj.Id.GetHashCode();
        }
    }
}
