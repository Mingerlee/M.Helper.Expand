using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace M.Simple.Expand.Test
{
    [TestClass]
    public class ExtensionsString
    {
        [TestMethod]
        public void TestString()
        {
            string str = "这是一段话";
            string str1 = "this is a biu biu biu";
            string str2 = "深圳";
            string str3 = string.Empty;
            string str4 = "一个中文，三个字节,中文符号也算三个字节";
            Console.WriteLine(str.ToFirstPinyin());
            Console.WriteLine(str1.ToCover());
            Console.WriteLine(str2.ToPinYin());
            Console.WriteLine(str3.GetValueOrDefault("默认值：default"));
            Console.WriteLine();
            Console.WriteLine(str.GetRealLength());
            Console.WriteLine(str1.GetRealLength());
            Console.WriteLine(str2.GetRealLength());
            Console.WriteLine(str3.GetRealLength());
            Console.WriteLine(str4.GetRealLength());

            Console.WriteLine();
            int i1 = 1234;
            decimal? i2 = null;
            string i3 = "this is a biu biu biu";
            decimal i4 = 4563M;
            Console.WriteLine(str4.ToHashCode(i1, i2, i3, i4));

            Console.WriteLine(str4.ToHashCode(i1, i4, i3, i2));

            Console.WriteLine(string.Concat(str4, i1, i2, i3, i4));
            Console.WriteLine(string.Concat(str4, i1, i4, i3, i2));
        }
        [TestMethod]
        public void TestCodeGenerator()
        {
            Console.WriteLine($"雪花编码：{SnowCodeGenerator.GetSnowCode()}");
            Console.WriteLine($"雪花编码(含时间戳)：{SnowCodeGenerator.GetTimeStampSnowCode()}");
            Console.WriteLine($"雪花编码SnowflakeIdGenerator：{new SnowflakeIdGenerator(1).NextId()}");
            
            Console.WriteLine($"唯一编码：{UniqueCodeGenerator.GenerateGuidCode()}");
            Console.WriteLine($"唯一编码(含时间戳)：{UniqueCodeGenerator.GenerateTimeStampGuidCode()}");
            Console.WriteLine($"短编码：{ShortCodeGenerator.GenerateShortCode()}");
        }
        [TestMethod]
        public void TestString1()
        {
            string str = "username";
            string str1 = "user_name_code";
            string str2 = "userNameCode";

            Console.WriteLine($"FirstLetterToUpper:{str.FirstLetterToUpper()}");
            Console.WriteLine($"UnderlineToCamel:{str1.UnderlineToCamel()}");
            Console.WriteLine($"CamelToUnderline:{str2.CamelToUnderline()}");
            Console.WriteLine($"ReverseString:{str.ReverseString()}");
            Console.WriteLine($"ReverseString:{str1.ReverseString()}");
            Console.WriteLine($"ReverseString:{str2.ReverseString()}");

            Console.WriteLine($"SwapCase:{str2.SwapCase()}");
            Console.WriteLine($"ContainsAny:{str1.ContainsAny("code", "name")}");
            Console.WriteLine($"ContainsAll:{str2.ContainsAll("code", "name")}");

        }
    }
}
