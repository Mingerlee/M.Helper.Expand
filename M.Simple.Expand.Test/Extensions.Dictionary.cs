using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace M.Simple.Expand.Test
{
    [TestClass]
    public class ExtensionsDictionary
    {
        [TestMethod]
        public void TestMerge()
        {
            Dictionary<int, string> dic = [];
            dic.Merge(1, "张三");
            Console.WriteLine(JsonConvert.SerializeObject(dic));
            dic.Merge(2, "李四");
            dic.Merge(1, "王五");
            Console.WriteLine(JsonConvert.SerializeObject(dic));
            dic.MergeIgnore(3, "陆六");
            dic.MergeIgnore(1, "梁七");
            Console.WriteLine(JsonConvert.SerializeObject(dic));

            Dictionary<int, string> dic1 = [];
            dic1.Merge(1, "张三");
            dic1.Merge(4, "刘八");
            dic1.Merge(5, "赵九");

            dic1.Merges(dic);
            Console.WriteLine(JsonConvert.SerializeObject(dic1));

            Dictionary<int, string> dic2 = [];
            dic2.Merge(1, "张三");
            dic2.Merge(4, "刘八");
            dic2.Merge(5, "赵九");

            dic2.MergeIgnores(dic);
            Console.WriteLine(JsonConvert.SerializeObject(dic2));

            var value = dic2.GetValue(1);
            Console.WriteLine(value);
            var value1 = dic2.GetValue(5);
            Console.WriteLine(value1);

            var value2 = dic2.RemovePop(3);
            Console.WriteLine(value2);

            dic2.Remove(4, out string value3);
            Console.WriteLine(value3);

            dic2.Removes(new List<int> { 1, 2, 6 });
            Console.WriteLine(JsonConvert.SerializeObject(dic2));


        }
        [TestMethod]
        public void TestToDictionary()
        {
            List<Person> persons =
            [
                new Person
                {
                    Id = 1,
                    Name = "张三"
                },
                new Person
                {
                    Id = 2,
                    Name = "李四"
                },
                new Person
                {
                    Id = 3,
                    Name = "王五"
                },
                new Person
                {
                    Id = 4,
                    Name = "陆六"
                },
                new Person
                {
                    Id = 1,
                    Name = "梁七"
                },
            ];

            var dic = persons.ToDictionarys(a => a.Id, b => b.Name);
            Console.WriteLine(JsonConvert.SerializeObject(dic));
        }
    }
}
