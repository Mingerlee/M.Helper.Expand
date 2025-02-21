using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace M.Simple.Expand.Test
{
    [TestClass]
    public class ExtensionsEntityCompare
    {
        [TestMethod]
        public void TestCompareModel()
        {
            var person = new Person()
            {
                Id = 1,
                Name = "张三",
                Age = 18,
                Code = "C0001",
                CreartDate = DateTime.Now,
                CreartUser = "U00001",
                Title = "Teacher",
                UpdateDate = DateTime.Now,
                Vaild = 1
            };
            var person1 = new Person()
            {
                Id = 1,
                Name = "张三",
                Age = 19,
                Code = "C0001",
                CreartDate = DateTime.Now,
                CreartUser = "U00001",
                Title = "Teacher",
                UpdateDate = DateTime.Now,
                Vaild = 1
            };
            var person2 = new Person()
            {
                Id = 1,
                Name = "李四",
                Age = 19,
                Code = "C0001",
                CreartDate = DateTime.Now,
                CreartUser = "U00001",
                Title = "Teacher",
                UpdateDate = DateTime.Now,
                Vaild = 1
            };
            var results = person.CompareModel(person1,true);
            Console.WriteLine(JsonConvert.SerializeObject(results));
            var flag = person.IsModify(person1, true);
            Console.WriteLine(flag);

            var results1 = person1.CompareModel(person2, true);
            Console.WriteLine(JsonConvert.SerializeObject(results1));
            var flag1 = person1.IsModify(person2, true);
            Console.WriteLine(flag1);

            var results2 = person.CompareModel(person1);
            Console.WriteLine(JsonConvert.SerializeObject(results2));
            var flag2 = person.IsModify(person1);
            Console.WriteLine(flag2);

            var results3 = person1.CompareModel(person2);
            Console.WriteLine(JsonConvert.SerializeObject(results3));
            var flag3 = person1.IsModify(person2);
            Console.WriteLine(flag3);
        }
        [TestMethod]
        public void TestModifyField()
        {
            var person = new Person()
            {
                Id = 1,
                Name = "张三",
                Age = 50,
                Code = "C0001",
                CreartDate = DateTime.Now,
                CreartUser = "U00001",
                Title = "Teacher",
                UpdateDate = DateTime.Now,
                Vaild = 1
            };
            var student = new Student()
            {
                Id = 11,
                Age = 19,
                Name = "张三",
                StudentCode = "S0001"
            };
            Console.WriteLine(JsonConvert.SerializeObject(person));
            Console.WriteLine(JsonConvert.SerializeObject(student));
            Console.WriteLine("修改前↑   修改后↓");
            person.ModifyField(student);
            Console.WriteLine(JsonConvert.SerializeObject(person));
            Console.WriteLine(JsonConvert.SerializeObject(student));
        }
        [TestMethod]
        public void TestListToDataTable()
        {
            List<Person> list = [ new Person()
            {
                Id = 1,
                Name = "张三",
                Age = 18,
                Code = "C0001",
                CreartDate = DateTime.Now,
                CreartUser = "U00001",
                Title = "Teacher",
                UpdateDate = DateTime.Now,
                Vaild = 1
            },new Person()
            {
                Id = 2,
                Name = "张三",
                Age = 19,
                Code = "C0002",
                CreartDate = DateTime.Now,
                CreartUser = "U00001",
                Title = "Teacher",
                UpdateDate = DateTime.Now,
                Vaild = 1
            }, new Person()
            {
                Id = 3,
                Name = "李四",
                Age = 20,
                Code = "C0003",
                CreartDate = DateTime.Now,
                CreartUser = "U00001",
                Title = "Teacher",
                UpdateDate = DateTime.Now,
                Vaild = 1
            } ];
           var dt= list.ToDataTable();
            foreach (DataRow row in dt.Rows)
            {
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    Console.WriteLine(row[i].ToString());
                }
            }
        }

    }
}
