using M.Helper.Expand.Helper;
using Newtonsoft.Json;
using M.Helper.Expand;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Reflection;
using Newtonsoft.Json.Linq;
using System.Security.Claims;
using static M.Helper.Expand.EncryptHelper;
using iText.Layout.Element;

namespace M.Helper.Expand.Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestFormatMoney()
        {
            decimal test = 1264598.4568465M;
            Console.WriteLine(test.FormatMoney(FormatMoneyEnum.C3_en_US));

            Console.WriteLine($"{{0}}");
        }
        [TestMethod]
        public void TestConvertToChinese()
        {
            decimal test = 1264598.4568465M;
            Console.WriteLine(test.ConvertToChinese());
        }
        [TestMethod]
        public void TestToCover()
        {
            string mobile = "15812369658";
            string name = "欧阳青云";
            Console.WriteLine(mobile.ToCover());
            Console.WriteLine(name.ToCover());
        }
        [TestMethod]
        public void TestGetFirstPinyin()
        {
            string name = "李佳航";
            Console.WriteLine(name.GetFirstPinyin());
        }
        [TestMethod]
        public void TestConvertToPinYin()
        {
            string name = "李佳航";
            Console.WriteLine(name.ConvertToPinYin());
        }
        [TestMethod]
        public void TestGetSnowCode()
        {
            Console.WriteLine(KeyCodeHelper.GetSnowCode());
        }
        [TestMethod]
        public void TestGetValueOrDefault()
        {
            Console.WriteLine(KeyCodeHelper.GetSnowCode());
        }
        [TestMethod]
        public void TestDictionaryPut()
        {
            Dictionary<int, string> dr = new Dictionary<int, string>();
            dr.Put(1, "张三");
            dr.Put(2, "李四");
            dr.Put(3, "王五");
            dr.Put(1, "赵六");
            dr.Put(2, "梁柒");
            Console.WriteLine(JsonConvert.SerializeObject(dr));
        }
        [TestMethod]
        public void TestDictionaryPutIgnoreExists()
        {
            Dictionary<int, string> dr = new Dictionary<int, string>();
            dr.Put(1, "张三");
            dr.Put(2, "李四");
            dr.Put(3, "王五");
            dr.Put(1, "赵六", false);
            dr.Put(2, "梁柒");
            Console.WriteLine(JsonConvert.SerializeObject(dr));
        }
        [TestMethod]
        public void TestType()
        {
            TestTypeClass testTypeClass = new TestTypeClass("Andy")
            {
                Id = 1,
                Code = "123456789",
                Name = "Ming",
                Description = "boy"
            };
            var properties = testTypeClass.GetType().GetProperties();
            foreach (var item in properties)
            {
                var propertieType = item.PropertyType;
                //if (item.Name != "Id")
                //{
                //    item.SetValue(testTypeClass, "78864546");
                //}
                Console.WriteLine($"{item.Name}:{item.GetValue(testTypeClass)}");
            }
        }
        [TestMethod]
        public void TestTypeInvokeMethod()
        {
            TestTypeClass testTypeClass = new TestTypeClass("Andy");
            //无参函数
            var method1 = testTypeClass.GetType().GetMethod("SayHello", new Type[] { });
            method1.Invoke(testTypeClass, null);
            //一个参数
            var method = testTypeClass.GetType().GetMethod("SayHello", new Type[] { typeof(string) });
            method.Invoke(testTypeClass, new object[] { "Summy" });
            //2个参数
            var method2 = testTypeClass.GetType().GetMethod("SayHello", new Type[] { typeof(string), typeof(string) });
            method2.Invoke(testTypeClass, new object[] { "Summy", $" {DateTime.Now}" });
        }
        [TestMethod]
        public void TestToken()
        {
            var claims = new[]
            {
                new Claim("Id","1"),
                new Claim("Amonut","12.36"),
                new Claim("IsClass", "True"),
                new Claim("Code","C2546516" ),
                new Claim("Name", "Summy"),
                new Claim("Description", "this is a test appliction..."),
                new Claim("CreateDate","2024-11-11" ),
                new Claim("ICD","12345678912"),
                new Claim("IDouble", "123.3254"),
                new Claim("IFloat","1.2536"),
            };

            var TestTypeClass = claims.GetUserToken(new TestTypeClass(""));
        }
        [TestMethod]
        public void TestDemo()
        {
            ChineseCalendar chineseCalendar = new ChineseCalendar(2025,1,23,false);
            Console.WriteLine(chineseCalendar.Date.ToDefaultDate());
        }
        [TestMethod]
        public void TestEndsWith()
        {
                var dividend = (int)Math.Pow(26,2);
                string columnName = string.Empty;

                while (dividend > 0)
                {
                    var modulo = (dividend - 1) % 26;
                    columnName = Convert.ToChar(65 + modulo) + columnName;
                    dividend = (dividend - modulo) / 26;
                }
                Console.WriteLine(columnName);
        }
        [TestMethod]
        public void TestRas()
        {
            var list = new List<TestTypeClass>();
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
            //！！！结果显示太长，请自行运行！！！

            //获取公钥和私钥，每次生成的都不一样
            KeyModel encryptDecrypt = RsaEncrypt.GetKeyPair();
            Console.WriteLine($"私钥：{encryptDecrypt.PrivateKey}");
            Console.WriteLine($"公钥：{encryptDecrypt.PublicKey}");

            //------------------------------私钥--------------------------------------
            //加密 -- 私钥能加密的原因是私钥包含公钥
            Console.WriteLine(JsonConvert.SerializeObject(list));
            string encryptResultPrivate = RsaEncrypt.RSAEncrypt(JsonConvert.SerializeObject(list), encryptDecrypt.PublicKey);
            Console.WriteLine($"密文:{encryptResultPrivate}");
            //解密
            Console.WriteLine($"解密：{RsaEncrypt.RSADecrypt(encryptResultPrivate, encryptDecrypt.PrivateKey)}");

            //------------------------------公钥--------------------------------------
            //加密
            string encryptResultPublic = RsaEncrypt.Encrypt("生成签名的原文", encryptDecrypt.PublicKey);
            Console.WriteLine($"密文:{encryptResultPublic}");
            //解密
            Console.WriteLine($"解密：{RsaEncrypt.Decrypt(encryptResultPublic, encryptDecrypt.PrivateKey)}");

            //------------------------------签名--------------------------------------
            //生成签名
            Console.WriteLine($"生成签名");
            string autograph = RsaEncrypt.SignData("生成签名的原文", encryptDecrypt.PrivateKey);
            Console.WriteLine($"签名:{autograph}");

            //验证签名
            Console.WriteLine($"验证签名:{RsaEncrypt.VerifyData("生成签名的原文", autograph, encryptDecrypt.PublicKey)}");
            Console.WriteLine($"验证签名原文改动:{RsaEncrypt.VerifyData("生成签名的原文改动", autograph, encryptDecrypt.PublicKey)}");
        }
        //[TestMethod]
        public void TestFtpHelper()
        {
            string ftpServerIP = "120.77.58.105";
            string ftpRemotePath = "";
            string ftpUserID = "ftp_tzyy";
            string ftpPassword = "tzyy3241585746@com";
            string mask = ".txt";
            var targetPath = "tzyy";
            string basePath = string.Empty;

            if (!string.IsNullOrEmpty(basePath))
            {
                basePath = Path.Combine(targetPath, DateTime.Now.Year.ToString(), DateTime.Now.Month.ToString());
            }
            else
            {
                basePath = Path.Combine(Directory.GetCurrentDirectory(), DateTime.Now.Year.ToString(), DateTime.Now.Month.ToString());
            }
            FtpHelper ftpHelper = new FtpHelper(ftpServerIP, ftpRemotePath, ftpUserID, ftpPassword);
            var fileNameList = ftpHelper.GetFileList(mask, "_cs");
            foreach (var fileName in fileNameList)
            {
                string filePath = Path.Combine(basePath, fileName);

                string newPath = Path.Combine(basePath, new Guid().GetNextGuid().ToString() + $"{fileName[fileName.LastIndexOf(".")..]}");
                //StreamReader stream = null;
                try
                {
                    //保存到本地
                    ftpHelper.Download(basePath, fileName);
                    //复制临时文件
                    File.Copy(filePath, newPath, true);
                    //读取临时文件内容
                    //stream = new StreamReader(newPath, Encoding.UTF8);
                    //测试环境不删除ftp文件
                    //if (AppSetting.GetConfig("HangfireBasicAuth:environment").ToLower() != "test")
                    //{
                    //    //删除服务器文件
                    //    //ftpHelper.Delete(fileName);
                    //}
                }
                catch (Exception ex)
                {
                }
                finally
                {
                    //删除临时文件
                    //stream.Close();
                    //stream.Dispose();
                    if (File.Exists(newPath))
                        File.Delete(newPath);
                }
            }
        }
        //[TestMethod]
        public void TestFtpHelperDeleteFile()
        {
            string ftpServerIP = "120.77.58.105";
            string ftpRemotePath = "";
            string ftpUserID = "ftp_tzyy";
            string ftpPassword = "tzyy3241585746@com";
            string mask = ".txt";
            FtpHelper ftpHelper = new FtpHelper(ftpServerIP, ftpRemotePath, ftpUserID, ftpPassword);
            var fileNameList = ftpHelper.GetFileList(mask, "_cs");
            foreach (var fileName in fileNameList)
            {
                try
                {
                    //删除服务器文件
                    ftpHelper.Delete(fileName);
                }
                catch (Exception ex)
                {
                }
                finally
                {
                }
            }
        }

        public enum TestEnum
        {
            测试数据 = 1,
            正式数据 = 2,
            UAT数据 = 3
        }
    }
}