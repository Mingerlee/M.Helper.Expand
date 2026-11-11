using Newtonsoft.Json;
using System.Data;

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
            var results = person.CompareModel(person1, true);
            Console.WriteLine(JsonConvert.SerializeObject(results));
            Console.WriteLine();

            var results1 = person1.CompareModel(person2, true);
            Console.WriteLine(JsonConvert.SerializeObject(results1));
            Console.WriteLine();

            var results2 = person.CompareModel(person1);
            Console.WriteLine(JsonConvert.SerializeObject(results2));
            Console.WriteLine();

            var results3 = person1.CompareModel(person2);
            Console.WriteLine(JsonConvert.SerializeObject(results3));
            Console.WriteLine();

            var flag = person.IsModify(person1, true);
            Console.WriteLine(flag);

            var flag1 = person1.IsModify(person2, true);
            Console.WriteLine(flag1);

            var flag2 = person.IsModify(person1);
            Console.WriteLine(flag2);

            var flag3 = person1.IsModify(person2);
            Console.WriteLine(flag3);
        }
        [TestMethod]
        public void TestCompareModel1()
        {
            var person = new TestCompareModel()
            {
                Id = 1,
                Name = "张三",
                Age = 18,
                Code = "C0001",
                CreartDate = DateTime.Now,
                CreartUser = "U00001",
                Title = "Teacher",
                UpdateDate = DateTime.Now,
                Vaild = 1,
                BoolFiled = true,
                BoolFiled2 = false,
                BoolFiled4 = true,
                BoolFiled5 = false,
                BoolFiled6 = true,
                CreartUser1 = "CreartUser1",
                CreartUser5 = "",
                CreartUser6 = "",
                DecimalFiled = 0.33M,
                DecimalFiled2 = 1,
                DecimalFiled3 = 1,
                DecimalFiled5 = 1,
                DecimalFiled6 = 1,
                PersonEnumFiled = PersonEnum.Teacher,
                PersonEnumFiled2 = PersonEnum.Professor,
                PersonEnumFiled3 = PersonEnum.Professor,
                PersonEnumFiled4 = PersonEnum.Professor,
                PersonEnumFiled5 = PersonEnum.Professor,
                Students = [new Student { Id = 1, Age = 18, Name = "ZhangSan", StudentCode = "S0001" }],
                Vaild2 = 1,
                Vaild5 = 1,
                Vaild6 = 1,
            };
            var person1 = new TestCompareModel()
            {
                Id = 1,
                Name = "张三",
                Age = 19,
                Code = "C0001",
                CreartDate = DateTime.Now,
                CreartUser = "U00002",
                Title = "Teacher",
                UpdateDate = DateTime.Now,
                Vaild = 1,
                BoolFiled = true,
                BoolFiled2 = false,
                BoolFiled4 = true,
                BoolFiled5 = false,
                BoolFiled6 = true,
                CreartUser1 = "CreartUser1",
                CreartUser5 = "CreartUser4",
                CreartUser6 = "CreartUser3",
                DecimalFiled = 0.33M,
                DecimalFiled2 = 1,
                DecimalFiled3 = 1,
                DecimalFiled5 = 1,
                DecimalFiled6 = 1,
                PersonEnumFiled = PersonEnum.Professor,
                PersonEnumFiled2 = PersonEnum.Teacher,
                PersonEnumFiled3 = PersonEnum.Student,
                PersonEnumFiled4 = PersonEnum.Professor,
                PersonEnumFiled5 = PersonEnum.Student,
                Students = [new Student { Id = 1, Age = 18, Name = "ZhangSan", StudentCode = "S0001" }],
                Vaild2 = 1,
                Vaild5 = 1,
                Vaild6 = 1,
            };
            var person2 = new TestCompareModel()
            {
                Id = 1,
                Name = "李四",
                Age = 20,
                Code = "C0002",
                CreartDate = DateTime.Now,
                CreartUser = "U00002",
                Title = "Professor",
                UpdateDate = DateTime.Now,
                Vaild = 2,
                BoolFiled = false,
                BoolFiled2 = true,
                BoolFiled4 = false,
                BoolFiled5 = true,
                BoolFiled6 = false,
                CreartUser1 = "CreartUser2",
                CreartUser5 = "CreartUser5",
                CreartUser6 = "CreartUser6",
                DecimalFiled = 0.333M,
                DecimalFiled2 = 0.334M,
                DecimalFiled3 = 0.3324M,
                DecimalFiled5 = 0.3334M,
                DecimalFiled6 = 0.3344M,
                PersonEnumFiled = PersonEnum.Student,
                PersonEnumFiled2 = PersonEnum.Professor,
                PersonEnumFiled3 = PersonEnum.Teacher,
                PersonEnumFiled4 = PersonEnum.Student,
                PersonEnumFiled5 = PersonEnum.Teacher,
                Students = [new Student { Id = 2, Age = 20, Name = "LiSi", StudentCode = "S0002" }],
                Vaild2 = 3,
                Vaild5 = 4,
                Vaild6 = 5,
            };

            for (int i = 0; i < 10; i++)
            {
                var results = person.CompareModel(person1, true);
                //Console.WriteLine(JsonConvert.SerializeObject(results));
                //Console.WriteLine();

                var results1 = person1.CompareModel(person2, true);
                //Console.WriteLine(JsonConvert.SerializeObject(results1));
                //Console.WriteLine();

                var results2 = person.CompareModel(person1);
                //Console.WriteLine(JsonConvert.SerializeObject(results2));
                //Console.WriteLine();

                var results3 = person1.CompareModel(person2);
                //Console.WriteLine(JsonConvert.SerializeObject(results3));
                //Console.WriteLine();
            }
        }

        [TestMethod]
        public void Collections()
        {
            var values = new[] { 1, 2, 3, 4, 5 };
            Console.WriteLine("分批：" + string.Join(" / ", values.SplitBatch(2).Select(batch => string.Join(",", batch))));
            // 默认按类型自身的相等规则比较，保留结果的原始顺序和重复项。
            var remaining = values.ExceptItems(new[] { 2, 4 }).ToArray();
            Console.WriteLine("排除：" + string.Join(",", remaining));

        }
    //[TestMethod]
    //public void TestGetModelChanges()
    //{
    //    var person = new TestCompareModel()
    //    {
    //        Id = 1,
    //        Name = "张三",
    //        Age = 18,
    //        Code = "C0001",
    //        CreartDate = DateTime.Now,
    //        CreartUser = "U00001",
    //        Title = "Teacher",
    //        UpdateDate = DateTime.Now,
    //        Vaild = 1,
    //        BoolFiled = true,
    //        BoolFiled2 = false,
    //        BoolFiled4 = true,
    //        BoolFiled5 = false,
    //        BoolFiled6 = true,
    //        CreartUser1 = "CreartUser1",
    //        CreartUser5 = "",
    //        CreartUser6 = "",
    //        DecimalFiled = 0.33M,
    //        DecimalFiled2 = 1,
    //        DecimalFiled3 = 1,
    //        DecimalFiled5 = 1,
    //        DecimalFiled6 = 1,
    //        PersonEnumFiled = PersonEnum.Teacher,
    //        PersonEnumFiled2 = PersonEnum.Professor,
    //        PersonEnumFiled3 = PersonEnum.Professor,
    //        PersonEnumFiled4 = PersonEnum.Professor,
    //        PersonEnumFiled5 = PersonEnum.Professor,
    //        Students = [new Student { Id = 1, Age = 18, Name = "ZhangSan", StudentCode = "S0001" }],
    //        Vaild2 = 1,
    //        Vaild5 = 1,
    //        Vaild6 = 1,
    //    };
    //    var person1 = new TestCompareModel()
    //    {
    //        Id = 1,
    //        Name = "张三",
    //        Age = 19,
    //        Code = "C0001",
    //        CreartDate = DateTime.Now,
    //        CreartUser = "U00002",
    //        Title = "Teacher",
    //        UpdateDate = DateTime.Now,
    //        Vaild = 1,
    //        BoolFiled = true,
    //        BoolFiled2 = false,
    //        BoolFiled4 = true,
    //        BoolFiled5 = false,
    //        BoolFiled6 = true,
    //        CreartUser1 = "CreartUser1",
    //        CreartUser5 = "CreartUser4",
    //        CreartUser6 = "CreartUser3",
    //        DecimalFiled = 0.33M,
    //        DecimalFiled2 = 1,
    //        DecimalFiled3 = 1,
    //        DecimalFiled5 = 1,
    //        DecimalFiled6 = 1,
    //        PersonEnumFiled = PersonEnum.Professor,
    //        PersonEnumFiled2 = PersonEnum.Teacher,
    //        PersonEnumFiled3 = PersonEnum.Student,
    //        PersonEnumFiled4 = PersonEnum.Professor,
    //        PersonEnumFiled5 = PersonEnum.Student,
    //        Students = [new Student { Id = 1, Age = 18, Name = "ZhangSan", StudentCode = "S0001" }],
    //        Vaild2 = 1,
    //        Vaild5 = 1,
    //        Vaild6 = 1,
    //    };
    //    var person2 = new TestCompareModel()
    //    {
    //        Id = 1,
    //        Name = "李四",
    //        Age = 20,
    //        Code = "C0002",
    //        CreartDate = DateTime.Now,
    //        CreartUser = "U00002",
    //        Title = "Professor",
    //        UpdateDate = DateTime.Now,
    //        Vaild = 2,
    //        BoolFiled = false,
    //        BoolFiled2 = true,
    //        BoolFiled4 = false,
    //        BoolFiled5 = true,
    //        BoolFiled6 = false,
    //        CreartUser1 = "CreartUser2",
    //        CreartUser5 = "CreartUser5",
    //        CreartUser6 = "CreartUser6",
    //        DecimalFiled = 0.333M,
    //        DecimalFiled2 = 0.334M,
    //        DecimalFiled3 = 0.3324M,
    //        DecimalFiled5 = 0.3334M,
    //        DecimalFiled6 = 0.3344M,
    //        PersonEnumFiled = PersonEnum.Student,
    //        PersonEnumFiled2 = PersonEnum.Professor,
    //        PersonEnumFiled3 = PersonEnum.Teacher,
    //        PersonEnumFiled4 = PersonEnum.Student,
    //        PersonEnumFiled5 = PersonEnum.Teacher,
    //        Students = [new Student { Id = 2, Age = 20, Name = "LiSi", StudentCode = "S0002" }],
    //        Vaild2 = 3,
    //        Vaild5 = 4,
    //        Vaild6 = 5,
    //    };

    //    for (int i = 0; i < 10000; i++)
    //    {
    //        var resultss = person.GetModelChanges(person1, true);
    //        //Console.WriteLine(JsonConvert.SerializeObject(resultss));
    //        //Console.WriteLine();

    //        var results11 = person1.GetModelChanges(person2, true);
    //        //Console.WriteLine(JsonConvert.SerializeObject(results11));
    //        //Console.WriteLine();

    //        var results22 = person.GetModelChanges(person1);
    //        //Console.WriteLine(JsonConvert.SerializeObject(results22));
    //        //Console.WriteLine();

    //        var results33 = person1.GetModelChanges(person2);
    //        //Console.WriteLine(JsonConvert.SerializeObject(results33));
    //        //Console.WriteLine();
    //    }
    //}
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
            List<Person> list = [new Person()
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
            },
                new Person()
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
                },
                new Person()
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
                }];
            var dt = list.ToDataTable();
            foreach (DataRow row in dt.Rows)
            {
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    Console.WriteLine(row[i].ToString());
                }
            }
        }
        [TestMethod]
        public void TestGroupBySelectFirst()
        {
            List<Person> list = [new Person()
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
            },
                new Person()
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
                },
                new Person()
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
                },
                new Person()
                {
                    Id = 4,
                    Name = "李四",
                    Age = 21,
                    Code = "C0004",
                    CreartDate = DateTime.Now,
                    CreartUser = "U00001",
                    Title = "Teacher",
                    UpdateDate = DateTime.Now,
                    Vaild = 1
                },
                new Person()
                {
                    Id = 5,
                    Name = "李四",
                    Age = 23,
                    Code = "C0005",
                    CreartDate = DateTime.Now,
                    CreartUser = "U00001",
                    Title = "Teacher",
                    UpdateDate = DateTime.Now,
                    Vaild = 1
                },
                new Person()
                {
                    Id = 6,
                    Name = "WangWu",
                    Age = 24,
                    Code = "C0006",
                    CreartDate = DateTime.Now,
                    CreartUser = "U00001",
                    Title = "Teacher",
                    UpdateDate = DateTime.Now,
                    Vaild = 1
                },
                new Person()
                {
                    Id = 7,
                    Name = "wangwu",
                    Age = 26,
                    Code = "C0006",
                    CreartDate = DateTime.Now,
                    CreartUser = "U00001",
                    Title = "Teacher",
                    UpdateDate = DateTime.Now,
                    Vaild = 1
                }];
            var dt = list.GroupBySelectFirst(p => p.Name);
            var dt1 = list.GroupByThenSortTakeFirst(p => p.Name, a => a.Age);

            foreach (var row in dt)
            {
                Console.WriteLine($"GroupBySelectFirst:{row.Id}");
            }
            foreach (var row in dt1)
            {
                Console.WriteLine($"GroupByThenSortTakeFirst:{row.Id}");
            }
        }
        [TestMethod]
        public void TestDefaultEmpty()
        {
            List<Person> list = null;
            list.DefaultEmpty();

            List<Student> list1 = [
                new Student { Id = 1, Age = 18, Name = "ZhangSan", StudentCode = "S0001" },
                new Student { Id = 2, Age = 19, Name = "LiSi", StudentCode = "S0002" },
                new Student { Id = 3, Age = 20, Name = "WangWu", StudentCode = "S0003" },
                new Student { Id = 4, Age = 21, Name = "LuLu", StudentCode = "S0004" },
                new Student { Id = 5, Age = 22, Name = "LiangQi", StudentCode = "S0005" },
                new Student { Id = 6, Age = 23, Name = "ZhaoBa", StudentCode = "S0006" },
            ];
            List<Student> list2 = [
                new Student { Id = 2, Age = 19, Name = "LiSi", StudentCode = "S0002" },
                new Student { Id = 4, Age = 21, Name = "LuLu", StudentCode = "S0008" },
                new Student { Id = 6, Age = 23, Name = "ZhaoBa", StudentCode = "S0006" },
            ];
            var student = new Student { Id = 6, Age = 23, Name = "ZhaoBa", StudentCode = "S0009" };
            var list3 = list1.ExceptItems(list2, list2.FirstOrDefault().ByProperty(a => a.Id));
            var list4 = list1.ExceptItems(list2);
            var list5 = list1.ExceptItem(student);
            var list6 = list1.ExceptItem(student, student.ByProperty(a => a.Id));

            Console.WriteLine($"list3自定义对比（Id）:{JsonConvert.SerializeObject(list3)}");
            Console.WriteLine($"list4默认全属性对比:{JsonConvert.SerializeObject(list4)}");
            Console.WriteLine();
            Console.WriteLine($"list5默认全属性对比:{JsonConvert.SerializeObject(list5)}");
            Console.WriteLine($"list6自定义对比（Id）:{JsonConvert.SerializeObject(list6)}");

            Console.WriteLine();
            Console.WriteLine($"list1数组长度是否>=6:{list1.HasAtLeast(6)}");
            Console.WriteLine($"list1数组长度是否>=7:{list1.HasAtLeast(7)}");

            Console.WriteLine();
            Console.WriteLine($"list1随机取一个元素:{JsonConvert.SerializeObject(list1.RandomTake())}");

        }
        [TestMethod]
        public void TestSplitBatch()
        {
            List<Person> list = null;
            list.DefaultEmpty();

            List<Student> list1 = [
                new Student { Id = 1, Age = 18, Name = "ZhangSan", StudentCode = "S0001" },
                new Student { Id = 2, Age = 19, Name = "LiSi", StudentCode = "S0002" },
                new Student { Id = 3, Age = 20, Name = "WangWu", StudentCode = "S0003" },
                new Student { Id = 4, Age = 21, Name = "LuLu", StudentCode = "S0004" },
                new Student { Id = 5, Age = 22, Name = "LiangQi", StudentCode = "S0005" },
                new Student { Id = 6, Age = 23, Name = "ZhaoBa", StudentCode = "S0006" },
                new Student { Id = 7, Age = 24, Name = "SunJiu", StudentCode = "S0007" },
                new Student { Id = 8, Age = 25, Name = "QianShi", StudentCode = "S0008" },
                new Student { Id = 9, Age = 26, Name = "ZhouYi", StudentCode = "S0009" },
                new Student { Id = 10, Age = 27, Name = "ZhenEr", StudentCode = "S0010" },
                new Student { Id = 11, Age = 28, Name = "YangSan", StudentCode = "S0011" },
                new Student { Id = 12, Age = 29, Name = "PanSi", StudentCode = "S0012" },
            ];
            var s = list1.SplitBatch(3);

            foreach (var item in s)
            {
                Console.WriteLine($"list1拆分批量数组:{JsonConvert.SerializeObject(item)}");
            }
            Console.WriteLine();
            var d = s.Flatten();
            Console.WriteLine($"list1组合批量数组:{JsonConvert.SerializeObject(d)}");
        }
        [TestMethod]
        public void TestWhereNotNull()
        {
            List<Person> list = null;
            list.DefaultEmpty();

            List<Student> list1 = [
                new Student { Id = 1, Age = 18, Name = "ZhangSan", StudentCode = "S0001" },
                new Student { Id = 2, Age = 19, Name = "LiSi", StudentCode = "S0002" },
                new Student { Id = 3, Age = 20, Name = "WangWu", StudentCode = "S0003" },
                new Student { Id = 4, Age = 21, Name = "LuLu", StudentCode = "S0004" },
                null,
                new Student { Id = 6, Age = 23, Name = "ZhaoBa", StudentCode = "S0006" },
                new Student { Id = 7, Age = 24, Name = "SunJiu", StudentCode = "S0007" },
                new Student { Id = 8, Age = 25, Name = "QianShi", StudentCode = "S0008" },
                null,
                new Student { Id = 10, Age = 27, Name = "ZhenEr", StudentCode = "S0010" },
                new Student { },
                new Student { Id = 12, Age = 29, Name = "PanSi", StudentCode = "S0012" },
            ];
            var s = list1.WhereNotNull();

            Console.WriteLine($"list1筛选非空元素数组:{JsonConvert.SerializeObject(s)}");


            List<int?> listInts = [1, 2, 3, null, 412, 456, 0, null];
            var w = listInts.WhereNotNull();

            Console.WriteLine($"list1筛选非空元素数组:{JsonConvert.SerializeObject(w)}");

        }
        [TestMethod]
        public void TestToDictionarySafe()
        {
            List<Student> list = [
                new Student { Id = 1, Age = 18, Name = "ZhangSan", StudentCode = "S0001" },
                new Student { Id = 2, Age = 19, Name = "LiSi", StudentCode = "S0002" },
                new Student { Id = 3, Age = 20, Name = "WangWu", StudentCode = "S0003" },
                new Student { Id = 4, Age = 21, Name = "LuLu", StudentCode = "S0004" },
                new Student { Id = 5, Age = 22, Name = "LiangQi", StudentCode = "S0005" },
                new Student { Id = 6, Age = 23, Name = "ZhaoBa", StudentCode = "S0006" },
                new Student { Id = 7, Age = 24, Name = "SunJiu", StudentCode = "S0007" },
                new Student { Id = 8, Age = 25, Name = "QianShi", StudentCode = "S0008" },
                new Student { Id = 9, Age = 26, Name = "ZhouYi", StudentCode = "S0009" },
                new Student { Id = 10, Age = 27, Name = "ZhenEr", StudentCode = "S0010" },
                new Student { Id = 11, Age = 28, Name = "YangSan", StudentCode = "S0011" },
                new Student { Id = 12, Age = 29, Name = "PanSi", StudentCode = "S0012" },
            ];
            var dir = list.ToDictionarySafe(a => a.StudentCode);

            Console.WriteLine($"list转字典:{JsonConvert.SerializeObject(dir)}");
            Console.WriteLine();
            var d = list.Shuffle();
            Console.WriteLine($"list随机打乱数组:{JsonConvert.SerializeObject(d)}");
            Console.WriteLine();
            var f = list.TakeLast(5);
            Console.WriteLine($"list取数组后面5条数据:{JsonConvert.SerializeObject(f)}");

        }
    }
}
