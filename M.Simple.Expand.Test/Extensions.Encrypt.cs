using M.Simple.Helper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace M.Simple.Expand.Test
{
    [TestClass]
    public class ExtensionsEncrypt
    {
        [TestMethod]
        public void TestEncryptReturn()
        {
            string str = "中国";
            string str1 = "中国湖南省长沙市开福区";
            var s = str.AesEncrypt();
            var s1 = str1.AesEncrypt();
            Console.WriteLine(JsonConvert.SerializeObject(str.AesEncrypt()));
            Console.WriteLine(JsonConvert.SerializeObject(str1.AesEncrypt()));
            Console.WriteLine();

            Console.WriteLine(s.CipherText.AesDecrypt(s.Key, s.IV));
            Console.WriteLine(s1.CipherText.AesDecrypt(s1.Key, s1.IV));

        }
        [TestMethod]
        public void TestEncryptBytes()
        {
            string str = "中国";
            string str1 = "中国湖南省长沙市开福区";
            string key = "KtRVUlzck5nWpdxjHutse6OtYu87hu0=";
            string iv = "Jpc/ihIFsjQMXg==";

            byte[] decBytes = Encoding.UTF8.GetBytes(key);
            byte[] ivBytes = Encoding.UTF8.GetBytes(iv);

            string s = str.AesEncrypt(decBytes, ivBytes);
            string s1 = str1.AesEncrypt(decBytes, ivBytes);

            Console.WriteLine(s);
            Console.WriteLine(s1);
            Console.WriteLine();

            Console.WriteLine(s.AesDecrypt(decBytes, ivBytes));
            Console.WriteLine(s1.AesDecrypt(decBytes, ivBytes));

        }
        [TestMethod]
        public void TestEncrypt()
        {
            string str = "中国";
            string str1 = "中国湖南省长沙市开福区";
            string key = "snNoeYgdGlP+KtRVUlzck5nWpdxjHuts";
            string iv = "ptkDRViyJpc/ihIF";

            string s = str.AesEncrypt(key, iv);
            string s1 = str1.AesEncrypt(key, iv);

            Console.WriteLine(s);
            Console.WriteLine(s1);
            Console.WriteLine();

            Console.WriteLine(s.AesDecrypt(key, iv));
            Console.WriteLine(s1.AesDecrypt(key, iv));

        }
        [TestMethod]
        public void TestRsaEncrypt()
        {
            var list = new List<Student>();
            for (int i = 1; i <= 10; i++)
            {
                var r = new Random();
                list.Add(new Student
                {
                    Age = r.Next(18, 28),
                    Id = i,
                    Name = $"张三{i}",
                    StudentCode = $"A00{i + 2}",
                });
            }

            Console.WriteLine(JsonConvert.SerializeObject(list));
            Console.WriteLine();
            var s = JsonConvert.SerializeObject(list).RsaEncryptWithBlock();
            Console.WriteLine($"分段加密，返回密文:{s.CipherText}");
            Console.WriteLine();
            Console.WriteLine($"分段加密，返回公钥:{s.PublicKey}");
            Console.WriteLine();
            Console.WriteLine($"分段加密，返回私钥:{s.PrivateKey}");
            Console.WriteLine();
            Console.WriteLine($"解密：{s.CipherText.RsaDecryptWithBlock(s.PrivateKey)}");
            Console.WriteLine();

            Console.WriteLine(JsonConvert.SerializeObject(list).RsaEncryptWithBlock(s.PublicKey));
            Console.WriteLine();

            string plainText = "Welcome to China";
            var cipherTextPublicKey = plainText.RsaEncrypt(s.PublicKey);
            Console.WriteLine($"公钥加密：{cipherTextPublicKey}");
            Console.WriteLine();
            var cipherTextPrivateKey = plainText.RsaEncrypt(s.PrivateKey);
            Console.WriteLine($"私钥加密：{cipherTextPrivateKey}");
            Console.WriteLine();

            Console.WriteLine($"解密：{cipherTextPublicKey.RsaDecrypt(s.PrivateKey)}");
            Console.WriteLine();
            Console.WriteLine($"解密：{cipherTextPrivateKey.RsaDecrypt(s.PrivateKey)}");
            Console.WriteLine();

            var sSignData = JsonConvert.SerializeObject(list).RsaSignData(s.PrivateKey);
            Console.WriteLine($"生成签名：{sSignData}");
            Console.WriteLine();
            var sVerifyData = JsonConvert.SerializeObject(list).RsaVerifyData(sSignData, s.PublicKey);
            Console.WriteLine($"验证签名：{sVerifyData}");
            Console.WriteLine();
        }
        [TestMethod]
        public void TestMD5Encrypt()
        {
            var str1 = "ABCDSDJF";
            var str2 = "ABCDSDJF202503017170255";
            Console.WriteLine($"密文:{JsonConvert.SerializeObject(str1.MD5SaltEncrypt())}");
            Console.WriteLine($"密文:{JsonConvert.SerializeObject(str2.MD5SaltEncrypt())}");

            string ci = "vZtTB4jRV11NkdVv5CBlKg==";
            string e = "iGKHXgDKwSNOY2c1ezMLfn+zH6kwmq4jLot38aaK1P4vwgSiwcv7LiKWFkUF6RD5lLw0BKMyMDI1LzMvMTcgMTc6MjQ6Mzk=";
            Console.WriteLine();
            Console.WriteLine(str2.MD5SaltVerify(ci,e));

            Console.WriteLine();
            Console.WriteLine($"密文:{JsonConvert.SerializeObject(str1.MD5Encrypt())}");
            Console.WriteLine($"密文:{JsonConvert.SerializeObject(str2.MD5Encrypt())}");
            string str3 = "1JvBtz3tqQA6WxPvSivgRg==1";
            Console.WriteLine(str2.MD5Verify(str3));
        }
    }
}
